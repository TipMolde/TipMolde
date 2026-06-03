using AutoMapper;
using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.MoldeDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomenda;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Producao;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa os casos de uso da feature Molde.
    /// </summary>
    /// <remarks>
    /// Centraliza validacoes de negocio, atualizacao parcial e orquestracao transacional
    /// do agregado Molde com especificacoes e associacao inicial a encomenda.
    /// </remarks>
    public class MoldeService : IMoldeService
    {
        private readonly IMoldeRepository _moldeRepository;
        private readonly IEncomendaRepository _encomendaRepository;
        private readonly IPrioridadeGlobalMoldeService _prioridadeGlobalMoldeService;
        private readonly IMapper _mapper;
        private readonly ILogger<MoldeService> _logger;

        /// <summary>
        /// Construtor de MoldeService.
        /// </summary>
        /// <param name="moldeRepository">Repositorio do agregado Molde.</param>
        /// <param name="encomendaRepository">Repositorio usado para validar a encomenda referenciada na criacao.</param>
        /// <param name="prioridadeGlobalMoldeService">Servico para recalcular a prioridade global dos moldes.</param>
        /// <param name="mapper">Mapper para conversao entre Dtos e entidades.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes criticas.</param>
        public MoldeService(
            IMoldeRepository moldeRepository,
            IEncomendaRepository encomendaRepository,
            IPrioridadeGlobalMoldeService prioridadeGlobalMoldeService,
            IMapper mapper,
            ILogger<MoldeService> logger)
        {
            _moldeRepository = moldeRepository;
            _encomendaRepository = encomendaRepository;
            _prioridadeGlobalMoldeService = prioridadeGlobalMoldeService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Lista moldes de forma paginada.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com Dtos de molde.</returns>
        public async Task<PagedResult<ResponseMoldeDto>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _moldeRepository.GetAllAsync(normalizedPage, normalizedPageSize);
            var items = _mapper.Map<IEnumerable<ResponseMoldeDto>>(result.Items);
            return new PagedResult<ResponseMoldeDto>(items, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Obtem um molde por identificador.
        /// </summary>
        /// <param name="id">Identificador interno do molde.</param>
        /// <returns>DTO do molde quando encontrado; nulo caso contrario.</returns>
        public async Task<ResponseMoldeDto?> GetByIdAsync(int id)
        {
            var molde = await _moldeRepository.GetByIdAsync(id);
            return molde == null ? null : _mapper.Map<ResponseMoldeDto>(molde);
        }


        /// <summary>
        /// Lista moldes associados a uma encomenda.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Colecao de Dtos de molde.</returns>
        public async Task<PagedResult<ResponseMoldeDto>> GetByEncomendaIdAsync(int encomendaId, int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _moldeRepository.GetByEncomendaIdAsync(encomendaId, normalizedPage, normalizedPageSize);
            var items = _mapper.Map<IEnumerable<ResponseMoldeDto>>(result.Items);
            return new PagedResult<ResponseMoldeDto>(items, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Obtem um molde pelo numero funcional.
        /// </summary>
        /// <param name="numero">Numero funcional do molde.</param>
        /// <returns>DTO do molde quando encontrado; nulo caso contrario.</returns>
        public async Task<ResponseMoldeDto?> GetByNumeroAsync(string numero)
        {
            var molde = await _moldeRepository.GetByNumeroAsync(numero.Trim());
            return molde == null ? null : _mapper.Map<ResponseMoldeDto>(molde);
        }

        /// <summary>
        /// Verifica se ja existe molde com o numero indicado.
        /// </summary>
        /// <param name="numero">Numero funcional a validar.</param>
        /// <returns>True quando o numero ja existe; false caso contrario.</returns>
        public async Task<bool> ExistsByNumeroAsync(string numero)
        {
            var molde = await _moldeRepository.GetByNumeroAsync(numero.Trim());
            return molde is not null;
        }

        /// <summary>
        /// Cria um novo agregado Molde.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida numero unico.
        /// 2. Valida encomenda referenciada.
        /// 3. Persiste molde, especificacoes e associacao EncomendaMolde na mesma transacao.
        /// 4. Recalcula a fila global de prioridades dos moldes em aberto.
        /// </remarks>
        /// <param name="dto">Dados de criacao do molde.</param>
        /// <returns>DTO do molde criado.</returns>
        public async Task<ResponseMoldeDto> CreateAsync(CreateMoldeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Numero))
                throw new ArgumentException("Numero do molde e obrigatorio.");

            var numeroNormalizado = dto.Numero.Trim();

            var existente = await _moldeRepository.GetByNumeroAsync(numeroNormalizado);
            if (existente != null)
                throw new ArgumentException("Ja existe um molde com este numero.");

            var encomenda = await _encomendaRepository.GetByIdAsync(dto.EncomendaId);
            if (encomenda == null)
                throw new KeyNotFoundException($"Encomenda com ID {dto.EncomendaId} nao encontrada.");

            var molde = _mapper.Map<Molde>(dto);
            var specs = _mapper.Map<EspecificacoesTecnicas>(dto);
            var link = _mapper.Map<EncomendaMolde>(dto);

            molde.Numero = numeroNormalizado;
            molde.Especificacoes = specs;
            molde.EncomendasMoldes.Add(link);

            await _moldeRepository.AddMoldeWithSpecsAndLinkAsync(molde, specs, link);
            await _prioridadeGlobalMoldeService.RecalcularAsync();

            _logger.LogInformation(
                "Molde {MoldeId} criado com sucesso e associado a encomenda {EncomendaId}",
                molde.Molde_id,
                dto.EncomendaId);

            return _mapper.Map<ResponseMoldeDto>(molde);
        }

        /// <summary>
        /// Atualiza parcialmente um molde existente.
        /// </summary>
        /// <remarks>
        /// Campos nao enviados no DTO sao preservados na entidade existente.
        /// </remarks>
        /// <param name="id">Identificador do molde a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        public async Task UpdateAsync(int id, UpdateMoldeDto dto)
        {
            var existente = await _moldeRepository.GetByIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException($"Molde com ID {id} nao encontrado.");

            if (!HasAnyChanges(dto))
                throw new ArgumentException("Pelo menos um campo deve ser informado para atualizacao.");

            if (!string.IsNullOrWhiteSpace(dto.Numero))
            {
                var numeroNormalizado = dto.Numero.Trim();
                if (!string.Equals(existente.Numero, numeroNormalizado, StringComparison.OrdinalIgnoreCase))
                {
                    var duplicado = await _moldeRepository.GetByNumeroAsync(numeroNormalizado);
                    if (duplicado != null && duplicado.Molde_id != id)
                        throw new ArgumentException("Ja existe um molde com este numero.");
                }
            }

            // Porque: update parcial preserva o valor atual quando o campo nao e enviado.
            _mapper.Map(dto, existente);

            if (HasTechnicalSpecsChanges(dto))
            {
                existente.Especificacoes ??= new EspecificacoesTecnicas { Molde_id = existente.Molde_id };
                _mapper.Map(dto, existente.Especificacoes);
            }

            await _moldeRepository.UpdateAsync(existente);
            await _prioridadeGlobalMoldeService.RecalcularAsync();

            _logger.LogInformation("Molde {MoldeId} atualizado com sucesso", id);
        }

        /// <summary>
        /// Remove um molde existente.
        /// </summary>
        /// <remarks>
        /// A remocao obriga a rebalancear a fila global porque um elemento operativo deixa de existir.
        /// </remarks>
        /// <param name="id">Identificador do molde a remover.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        public async Task DeleteAsync(int id)
        {
            var molde = await _moldeRepository.GetByIdAsync(id);
            if (molde == null)
                throw new KeyNotFoundException($"Molde com ID {id} nao encontrado.");

            await _moldeRepository.DeleteAsync(id);
            await _prioridadeGlobalMoldeService.RecalcularAsync();

            _logger.LogInformation("Molde {MoldeId} removido com sucesso", id);
        }

        /// <summary>
        /// Verifica se o pedido de update contem pelo menos uma alteracao funcional.
        /// </summary>
        /// <param name="dto">DTO de atualizacao parcial.</param>
        /// <returns>True quando existe pelo menos um campo preenchido; false caso contrario.</returns>
        private static bool HasAnyChanges(UpdateMoldeDto dto)
        {
            return !string.IsNullOrWhiteSpace(dto.Numero)
                || !string.IsNullOrWhiteSpace(dto.NumeroMoldeCliente)
                || !string.IsNullOrWhiteSpace(dto.Nome)
                || !string.IsNullOrWhiteSpace(dto.ImagemCapaPath)
                || !string.IsNullOrWhiteSpace(dto.Descricao)
                || dto.Numero_cavidades.HasValue
                || dto.TipoPedido.HasValue
                || HasTechnicalSpecsChanges(dto);
        }

        /// <summary>
        /// Verifica se o pedido de update contem alteracoes nas especificacoes tecnicas.
        /// </summary>
        /// <param name="dto">DTO de atualizacao parcial.</param>
        /// <returns>True quando existe pelo menos um campo tecnico preenchido; false caso contrario.</returns>
        private static bool HasTechnicalSpecsChanges(UpdateMoldeDto dto)
        {
            return dto.Largura.HasValue
                || dto.Comprimento.HasValue
                || dto.Altura.HasValue
                || dto.PesoEstimado.HasValue
                || !string.IsNullOrWhiteSpace(dto.TipoInjecao)
                || !string.IsNullOrWhiteSpace(dto.SistemaInjecao)
                || dto.Contracao.HasValue
                || !string.IsNullOrWhiteSpace(dto.AcabamentoPeca)
                || dto.Cor.HasValue
                || !string.IsNullOrWhiteSpace(dto.MaterialMacho)
                || !string.IsNullOrWhiteSpace(dto.MaterialCavidade)
                || !string.IsNullOrWhiteSpace(dto.MaterialMovimentos)
                || !string.IsNullOrWhiteSpace(dto.MaterialInjecao);
        }
    }
}
