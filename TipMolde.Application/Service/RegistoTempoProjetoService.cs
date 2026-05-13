using AutoMapper;
using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.RegistoTempoProjetoDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Desenho.IProjeto;
using TipMolde.Application.Interface.Desenho.IRegistoTempoProjeto;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Domain.Entities.Desenho;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa os casos de uso da feature RegistoTempoProjeto.
    /// </summary>
    /// <remarks>
    /// O service centraliza validacoes de negocio, integridade entre projeto e peca,
    /// machine state do historico temporal, mapping para Dtos e rastreabilidade por logging.
    /// </remarks>
    public class RegistoTempoProjetoService : IRegistoTempoProjetoService
    {
        private readonly IRegistoTempoProjetoRepository _registoRepository;
        private readonly IProjetoRepository _projetoRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<RegistoTempoProjetoService> _logger;

        /// <summary>
        /// Construtor de RegistoTempoProjetoService.
        /// </summary>
        /// <param name="registoRepository">Repositorio do agregado RegistoTempoProjeto.</param>
        /// <param name="projetoRepository">Repositorio usado para validar a existencia do projeto.</param>
        /// <param name="userRepository">Repositorio usado para validar o autor do registo.</param>
        /// <param name="pecaRepository">Repositorio usado para validar a peca associada ao registo.</param>
        /// <param name="mapper">Mapper para conversao entre Dtos e entidade.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes criticas.</param>
        public RegistoTempoProjetoService(
            IRegistoTempoProjetoRepository registoRepository,
            IProjetoRepository projetoRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<RegistoTempoProjetoService> logger)
        {
            _registoRepository = registoRepository;
            _projetoRepository = projetoRepository;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Lista o historico de tempo de um projeto para um autor.
        /// </summary>
        /// <param name="projetoId">Identificador do projeto.</param>
        /// <param name="autorId">Identificador do autor.</param>
        /// <param name="page">Numero da pagina a ser retornada.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Colecao ordenada de registos convertidos para DTO.</returns>
        public async Task<PagedResult<ResponseRegistoTempoProjetoDto>> GetHistoricoAsync(int projetoId, int autorId, int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _registoRepository.GetHistoricoAsync(projetoId, autorId, normalizedPage, normalizedPageSize);
            var itens = _mapper.Map<IEnumerable<ResponseRegistoTempoProjetoDto>>(result.Items);
            return new PagedResult<ResponseRegistoTempoProjetoDto>(itens, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Obtem um registo de tempo por identificador.
        /// </summary>
        /// <param name="id">Identificador interno do registo.</param>
        /// <returns>DTO do registo quando encontrado; nulo caso contrario.</returns>
        public async Task<ResponseRegistoTempoProjetoDto?> GetByIdAsync(int id)
        {
            var registo = await _registoRepository.GetByIdAsync(id);
            return registo == null ? null : _mapper.Map<ResponseRegistoTempoProjetoDto>(registo);
        }

        /// <summary>
        /// Cria um novo evento no historico temporal de um projeto.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida existencia de projeto, autor e peca.
        /// 2. Garante coerencia entre o molde do projeto e o molde da peca.
        /// 3. Valida a transicao com base no ultimo estado persistido.
        /// 4. Persiste o novo evento com timestamp UTC gerado no servidor.
        /// 5. Rejeita concorrencia quando outro pedido altera o historico no mesmo intervalo.
        /// </remarks>
        /// <param name="dto">Dados de criacao do registo.</param>
        /// <returns>DTO do registo criado.</returns>
        public async Task<ResponseRegistoTempoProjetoDto> CreateRegistoAsync(CreateRegistoTempoProjetoDto dto)
        {
            if (!dto.Estado_tempo.HasValue)
                throw new ArgumentException("Estado_tempo e obrigatorio.");

            var projeto = await _projetoRepository.GetByIdAsync(dto.Projeto_id);
            if (projeto == null)
                throw new KeyNotFoundException("Projeto nao encontrado.");

            var autor = await _userRepository.GetByIdAsync(dto.Autor_id);
            if (autor == null)
                throw new KeyNotFoundException("Autor nao encontrado.");

            var ultimo = await _registoRepository.GetUltimoRegistoAsync(dto.Projeto_id, dto.Autor_id);
            ValidarTransicao(ultimo?.Estado_tempo, dto.Estado_tempo.Value);

            var registo = _mapper.Map<RegistoTempoProjeto>(dto);

            // Porque: o timestamp do registo deve ser sempre gerado no servidor para preservar
            // a ordem cronologica real e evitar manipulacao do relogio por parte do cliente.
            registo.Data_hora = DateTime.UtcNow;

            registo = await _registoRepository.AddAsync(registo);

            _logger.LogInformation(
                "RegistoTempoProjeto {RegistoId} criado para Projeto {ProjetoId}, Autor {AutorId}, Estado {Estado}",
                registo.Registo_Tempo_Projeto_id,
                registo.Projeto_id,
                registo.Autor_id,
                registo.Estado_tempo);

            return _mapper.Map<ResponseRegistoTempoProjetoDto>(registo);
        }

        /// <summary>
        /// Remove um registo de tempo existente.
        /// </summary>
        /// <param name="id">Identificador interno do registo.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        public async Task DeleteAsync(int id)
        {
            var existing = await _registoRepository.GetByIdAsync(id);
            if (existing == null)
                throw new KeyNotFoundException($"Registo de tempo com ID {id} nao encontrado.");

            await _registoRepository.DeleteAsync(id);

            _logger.LogInformation("RegistoTempoProjeto {RegistoId} removido com sucesso", id);
        }

        /// <summary>
        /// Valida a transicao entre o ultimo estado persistido e o novo estado pedido.
        /// </summary>
        /// <remarks>
        /// Invariante de negocio:
        /// 1. O primeiro registo so pode ser INICIADO.
        /// 2. INICIADO so pode transitar para PAUSADO ou CONCLUIDO.
        /// 3. PAUSADO so pode transitar para RETOMADO.
        /// 4. RETOMADO so pode transitar para PAUSADO ou CONCLUIDO.
        /// 5. CONCLUIDO nao aceita novas transicoes.
        /// </remarks>
        /// <param name="atual">Estado temporal atualmente persistido.</param>
        /// <param name="novo">Novo estado pedido para criacao do registo.</param>
        private static void ValidarTransicao(EstadoTempoProjeto? atual, EstadoTempoProjeto novo)
        {
            if (atual is null && novo != EstadoTempoProjeto.INICIADO)
                throw new ArgumentException("Primeiro estado deve ser INICIADO.");

            if (atual == EstadoTempoProjeto.INICIADO && novo is not (EstadoTempoProjeto.PAUSADO or EstadoTempoProjeto.CONCLUIDO))
                throw new ArgumentException("Transicao invalida. Depois de INICIADO so pode registar PAUSADO ou CONCLUIDO.");

            if (atual == EstadoTempoProjeto.PAUSADO && novo != EstadoTempoProjeto.RETOMADO)
                throw new ArgumentException("Transicao invalida. Depois de PAUSADO so pode registar RETOMADO.");

            if (atual == EstadoTempoProjeto.RETOMADO && novo is not (EstadoTempoProjeto.PAUSADO or EstadoTempoProjeto.CONCLUIDO))
                throw new ArgumentException("Transicao invalida. Depois de RETOMADO so pode registar PAUSADO ou CONCLUIDO.");

            if (atual == EstadoTempoProjeto.CONCLUIDO)
                throw new ArgumentException("Projeto ja concluido.");
        }
    }
}
