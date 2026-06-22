using AutoMapper;
using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.MaquinaDto;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa os casos de uso da feature Maquina.
    /// </summary>
    /// <remarks>
    /// Centraliza validacoes de negocio, conversao entre DTOs e entidade
    /// e protecao das invariantes operacionais usadas pelo modulo de producao.
    /// </remarks>
    public class MaquinaService : IMaquinaService
    {
        private readonly IMaquinaRepository _maquinaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<MaquinaService> _logger;

        /// <summary>
        /// Construtor de MaquinaService.
        /// </summary>
        /// <param name="maquinaRepository">Repositorio da feature Maquina.</param>
        /// <param name="mapper">Mapper para conversao entre DTOs e entidade.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes criticas.</param>
        public MaquinaService(
            IMaquinaRepository maquinaRepository,
            IMapper mapper,
            ILogger<MaquinaService> logger)
        {
            _maquinaRepository = maquinaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Lista maquinas com paginacao.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com DTOs de resposta.</returns>
        public async Task<PagedResult<ResponseMaquinaDto>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _maquinaRepository.GetAllAsync(normalizedPage, normalizedPageSize);
            return MapPagedResult(result);
        }

        /// <summary>
        /// Obtem uma maquina por identificador.
        /// </summary>
        /// <param name="id">Identificador interno da maquina.</param>
        /// <returns>DTO da maquina quando encontrada; nulo caso contrario.</returns>
        public async Task<ResponseMaquinaDto?> GetByIdAsync(int id)
        {
            var maquina = await _maquinaRepository.GetByIdUnicoAsync(id);
            return maquina == null ? null : _mapper.Map<ResponseMaquinaDto>(maquina);
        }

        /// <summary>
        /// Lista maquinas por estado com paginacao.
        /// </summary>
        /// <param name="estado">Estado operacional a filtrar.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com DTOs filtrados por estado.</returns>
        public async Task<PagedResult<ResponseMaquinaDto>> GetByEstadoAsync(EstadoMaquina estado, int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _maquinaRepository.GetByEstadoAsync(estado, normalizedPage, normalizedPageSize);
            return MapPagedResult(result);
        }

        /// <summary>
        /// Pesquisa maquinas por termo livre.
        /// </summary>
        /// <param name="searchTerm">Termo de pesquisa a aplicar.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com DTOs correspondentes ao termo.</returns>
        public async Task<PagedResult<ResponseMaquinaDto>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return PaginationDefaults.EmptyPage<ResponseMaquinaDto>(page, pageSize);

            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _maquinaRepository.SearchAsync(searchTerm.Trim(), normalizedPage, normalizedPageSize);
            return MapPagedResult(result);
        }

        /// <summary>
        /// Cria uma nova maquina.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida nome/modelo obrigatorio.
        /// 2. Valida unicidade do identificador e do numero fisico.
        /// 3. Valida existencia da fase dedicada.
        /// 4. Persiste a maquina.
        /// </remarks>
        /// <param name="dto">Dados de criacao da maquina.</param>
        /// <returns>DTO da maquina criada.</returns>
        public async Task<ResponseMaquinaDto> CreateAsync(CreateMaquinaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomeModelo))
                throw new ArgumentException("Nome/modelo e obrigatorio.");

            var existingById = await _maquinaRepository.GetByIdUnicoAsync(dto.Maquina_id);
            if (existingById != null)
                throw new BusinessConflictException($"Ja existe maquina com o id '{dto.Maquina_id}'.");

            // Invariante: a maquina so pode ser criada com numero fisico unico
            // e fase dedicada valida para manter rastreabilidade operacional.
            if (await _maquinaRepository.ExistsNumeroAsync(dto.Numero))
                throw new BusinessConflictException($"Ja existe uma maquina com o numero '{dto.Numero}'.");

            if (!await _maquinaRepository.ExistsFaseDedicadaAsync(dto.FaseDedicada_id))
                throw new KeyNotFoundException($"Fase de producao com ID {dto.FaseDedicada_id} nao encontrada.");

            var maquina = _mapper.Map<Maquina>(dto);
            var created = await _maquinaRepository.CreateAsync(maquina);

            _logger.LogInformation(
                "Maquina {MaquinaId} criada com sucesso com numero {Numero} e fase dedicada {FaseDedicadaId}.",
                created.Maquina_id,
                created.Numero,
                created.FaseDedicada_id);

            return _mapper.Map<ResponseMaquinaDto>(created);
        }

        /// <summary>
        /// Atualiza parcialmente uma maquina existente.
        /// </summary>
        /// <remarks>
        /// Campos nao enviados mantem o valor atual.
        /// Porque: o estado operacional nao pode ser reposto para DISPONIVEL por omissao do campo no payload.
        /// </remarks>
        /// <param name="id">Identificador da maquina a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        public async Task UpdateAsync(int id, UpdateMaquinaDto dto)
        {
            var existing = await _maquinaRepository.GetByIdUnicoAsync(id);
            if (existing == null)
                throw new KeyNotFoundException($"Maquina {id} nao encontrada.");

            if (!HasAnyChanges(dto))
                throw new ArgumentException("Pelo menos um campo deve ser informado para atualizacao.");

            if (dto.Numero.HasValue && dto.Numero.Value != existing.Numero && await _maquinaRepository.ExistsNumeroAsync(dto.Numero.Value, id))
                throw new BusinessConflictException($"Ja existe uma maquina com o numero '{dto.Numero.Value}'.");


            if (dto.FaseDedicada_id.HasValue && !await _maquinaRepository.ExistsFaseDedicadaAsync(dto.FaseDedicada_id.Value))
                throw new KeyNotFoundException($"Fase de producao com ID {dto.FaseDedicada_id.Value} nao encontrada.");


            _mapper.Map(dto, existing);

            await _maquinaRepository.UpdateExistingAsync(existing);

            _logger.LogInformation("Maquina {MaquinaId} atualizada com sucesso.", id);
        }

        /// <summary>
        /// Remove uma maquina existente.
        /// </summary>
        /// <param name="id">Identificador da maquina a remover.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        public async Task DeleteAsync(int id)
        {
            var existing = await _maquinaRepository.GetByIdUnicoAsync(id);
            if (existing == null)
                throw new KeyNotFoundException($"Maquina {id} nao encontrada.");

            await _maquinaRepository.DeleteAsync(id);

            _logger.LogInformation("Maquina {MaquinaId} removida com sucesso.", id);
        }

        /// <summary>
        /// Verifica se o DTO de update contem pelo menos uma alteracao funcional.
        /// </summary>
        /// <param name="dto">DTO de atualizacao parcial.</param>
        /// <returns>True quando existe pelo menos um campo preenchido; false caso contrario.</returns>
        private static bool HasAnyChanges(UpdateMaquinaDto dto)
        {
            return dto.Numero.HasValue
                || !string.IsNullOrWhiteSpace(dto.NomeModelo)
                || !string.IsNullOrWhiteSpace(dto.IpAddress)
                || dto.Estado.HasValue
                || dto.FaseDedicada_id.HasValue;
        }

        /// <summary>
        /// Converte um resultado paginado de entidade para DTO.
        /// </summary>
        /// <typeparam name="TEntity">Tipo da entidade de origem.</typeparam>
        /// <param name="result">Resultado paginado obtido do repositorio.</param>
        /// <returns>Resultado paginado com DTOs correspondentes.</returns>
        private PagedResult<ResponseMaquinaDto> MapPagedResult<TEntity>(PagedResult<TEntity> result)
        {
            var items = _mapper.Map<IEnumerable<ResponseMaquinaDto>>(result.Items);

            return new PagedResult<ResponseMaquinaDto>(
                items,
                result.TotalCount,
                result.CurrentPage,
                result.PageSize);
        }
    }
}
