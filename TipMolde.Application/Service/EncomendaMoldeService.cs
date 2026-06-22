using AutoMapper;
using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.EncomendaMoldeDto;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomenda;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa os casos de uso da relacao Encomenda-Molde.
    /// </summary>
    /// <remarks>
    /// Centraliza validacoes de FK, unicidade da associacao e atualizacao parcial.
    /// </remarks>
    public class EncomendaMoldeService : IEncomendaMoldeService
    {
        private readonly IEncomendaMoldeRepository _repo;
        private readonly IEncomendaRepository _encomendaRepo;
        private readonly IMoldeRepository _moldeRepo;
        private readonly IPrioridadeGlobalMoldeService _prioridadeGlobalMoldeService;
        private readonly IMapper _mapper;
        private readonly ILogger<EncomendaMoldeService> _logger;

        /// <summary>
        /// Construtor de EncomendaMoldeService.
        /// </summary>
        /// <param name="repo">Repositorio da relacao Encomenda-Molde.</param>
        /// <param name="encomendaRepo">Repositorio de encomenda para validacao de FK e atualizacao do agregado.</param>
        /// <param name="moldeRepo">Repositorio de molde para validacao de FK.</param>
        /// <param name="prioridadeGlobalMoldeService">Servico para recalcular a prioridade global dos moldes.</param>
        /// <param name="mapper">Mapper para conversao entre entidades e Dtos.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes criticas.</param>
        public EncomendaMoldeService(
            IEncomendaMoldeRepository repo,
            IEncomendaRepository encomendaRepo,
            IMoldeRepository moldeRepo,
            IPrioridadeGlobalMoldeService prioridadeGlobalMoldeService,
            IMapper mapper,
            ILogger<EncomendaMoldeService> logger)
        {
            _repo = repo;
            _encomendaRepo = encomendaRepo;
            _moldeRepo = moldeRepo;
            _prioridadeGlobalMoldeService = prioridadeGlobalMoldeService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Obtem associacao Encomenda-Molde por ID.
        /// </summary>
        /// <param name="id">Identificador da associacao.</param>
        /// <returns>DTO de resposta quando encontrado; nulo caso nao exista.</returns>
        public async Task<ResponseEncomendaMoldeDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ResponseEncomendaMoldeDto>(entity);
        }

        /// <summary>
        /// Lista associacoes por encomenda com paginacao.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda para filtro.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos de associacao.</returns>
        public async Task<PagedResult<ResponseEncomendaMoldeDto>> GetByEncomendaIdAsync(
            int encomendaId,
            int page = 1,
            int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _repo.GetByEncomendaIdAsync(encomendaId, normalizedPage, normalizedPageSize);
            var mapped = _mapper.Map<IEnumerable<ResponseEncomendaMoldeDto>>(result.Items);
            return new PagedResult<ResponseEncomendaMoldeDto>(mapped, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Lista a fila global de moldes com paginacao.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos da fila global de moldes.</returns>
        public Task<PagedResult<FilaGlobalMoldeItemDto>> GetFilaGlobalAsync(int page = 1, int pageSize = 10)
            => _prioridadeGlobalMoldeService.GetFilaGlobalAsync(page, pageSize);

        /// <summary>
        /// Lista associacoes por molde com paginacao.
        /// </summary>
        /// <param name="moldeId">Identificador do molde para filtro.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos de associacao.</returns>
        public async Task<PagedResult<ResponseEncomendaMoldeDto>> GetByMoldeIdAsync(
            int moldeId,
            int page = 1,
            int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _repo.GetByMoldeIdAsync(moldeId, normalizedPage, normalizedPageSize);
            var mapped = _mapper.Map<IEnumerable<ResponseEncomendaMoldeDto>>(result.Items);
            return new PagedResult<ResponseEncomendaMoldeDto>(mapped, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Lista associacoes Encomenda-Molde cujas encomendas estao confirmadas.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos prontos para consumo pelo modulo de desenho.</returns>
        public async Task<PagedResult<ResponseEncomendaMoldeDto>> GetByEncomendasConfirmadasAsync(
            int page = 1,
            int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _repo.GetByEncomendasConfirmadasAsync(normalizedPage, normalizedPageSize);
            var mapped = _mapper.Map<IEnumerable<ResponseEncomendaMoldeDto>>(result.Items);
            return new PagedResult<ResponseEncomendaMoldeDto>(mapped, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Lista associacoes de moldes aptos para desenho.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos de associacao filtrados pela regra funcional do desenho.</returns>
        public async Task<PagedResult<ResponseEncomendaMoldeDto>> GetByEncomendasConfirmadasParaDesenhoAsync(
            int page = 1,
            int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _repo.GetByEncomendasConfirmadasParaDesenhoAsync(normalizedPage, normalizedPageSize);
            var mapped = _mapper.Map<IEnumerable<ResponseEncomendaMoldeDto>>(result.Items);
            return new PagedResult<ResponseEncomendaMoldeDto>(mapped, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Pesquisa associacoes aptas para desenho por termo livre.
        /// </summary>
        /// <remarks>
        /// Esta pesquisa aplica-se apenas ao subconjunto funcional ja elegivel para desenho,
        /// preservando a mesma ordenacao operacional da lista base.
        /// </remarks>
        /// <param name="searchTerm">Termo de pesquisa a aplicar.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos filtrados para a pagina de desenho.</returns>
        public async Task<PagedResult<ResponseEncomendaMoldeDto>> SearchByTermForDesenhoAsync(
            string searchTerm,
            int page = 1,
            int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return PaginationDefaults.EmptyPage<ResponseEncomendaMoldeDto>(page, pageSize);

            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _repo.SearchByTermForDesenhoAsync(searchTerm.Trim(), normalizedPage, normalizedPageSize);
            var mapped = _mapper.Map<IEnumerable<ResponseEncomendaMoldeDto>>(result.Items);
            return new PagedResult<ResponseEncomendaMoldeDto>(mapped, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Cria uma associacao Encomenda-Molde.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida existencia das FK Encomenda e Molde.
        /// 2. Valida unicidade do par Encomenda_id + Molde_id.
        /// 3. Persiste associacao.
        /// </remarks>
        /// <param name="dto">Dados de criacao da associacao.</param>
        /// <returns>DTO da associacao criada e persistida.</returns>
        public async Task<ResponseEncomendaMoldeDto> CreateAsync(CreateEncomendaMoldeDto dto)
        {
            var encomenda = await _encomendaRepo.GetByIdAsync(dto.Encomenda_id);
            if (encomenda == null)
                throw new KeyNotFoundException($"Encomenda com ID {dto.Encomenda_id} nao encontrada.");

            var molde = await _moldeRepo.GetByIdAsync(dto.Molde_id);
            if (molde == null)
                throw new KeyNotFoundException($"Molde com ID {dto.Molde_id} nao encontrado.");

            var duplicated = await _repo.ExistsAssociationAsync(dto.Encomenda_id, dto.Molde_id, null);
            if (duplicated)
                throw new BusinessConflictException($"Ja existe associacao para Encomenda_id={dto.Encomenda_id} e Molde_id={dto.Molde_id}.");

            var entity = _mapper.Map<EncomendaMolde>(dto);
            entity.Estado = EstadoEncomendaMolde.PENDENTE;
            await _repo.AddAsync(entity);

            _logger.LogInformation(
                "EncomendaMolde criado com sucesso {EncomendaMoldeId} (EncomendaId={EncomendaId}, MoldeId={MoldeId})",
                entity.EncomendaMolde_id,
                entity.Encomenda_id,
                entity.Molde_id);

            return _mapper.Map<ResponseEncomendaMoldeDto>(entity);
        }

        /// <summary>
        /// Atualiza parcialmente uma associacao Encomenda-Molde.
        /// </summary>
        /// <remarks>
        /// Campos nao enviados no DTO sao preservados na entidade.
        /// </remarks>
        /// <param name="id">Identificador da associacao a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        public async Task UpdateAsync(int id, UpdateEncomendaMoldeDto dto)
        {
            var existente = await _repo.GetByIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException($"EncomendaMolde com ID {id} nao encontrada.");

            var hasChanges = dto.Quantidade.HasValue || dto.Prioridade.HasValue || dto.DataEntregaPrevista.HasValue;
            if (!hasChanges)
                throw new ArgumentException("Pelo menos um campo deve ser informado para atualizacao.");

            if (dto.Quantidade.HasValue) existente.Quantidade = dto.Quantidade.Value;
            if (dto.Prioridade.HasValue) existente.Prioridade = dto.Prioridade.Value;
            if (dto.DataEntregaPrevista.HasValue) existente.DataEntregaPrevista = dto.DataEntregaPrevista.Value;

            await _repo.UpdateAsync(existente);
            _logger.LogInformation("EncomendaMolde {EncomendaMoldeId} atualizado com sucesso", id);
        }

        /// <summary>
        /// Atualiza o estado operacional de um molde dentro da encomenda e reflete o agregado da encomenda.
        /// </summary>
        /// <remarks>
        /// O backend autoriza a mudanca manual para EM_PRODUCAO apenas quando todas as pecas
        /// do molde ja receberam material. A mudanca manual para CONCLUIDO so e autorizada
        /// quando todas as pecas estao concluidas na fase de montagem.
        /// </remarks>
        /// <param name="id">Identificador da associacao a atualizar.</param>
        /// <param name="dto">Estado de destino do molde.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        public async Task UpdateEstadoAsync(int id, UpdateEstadoEncomendaMoldeDto dto)
        {
            var existente = await _repo.GetByIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException($"EncomendaMolde com ID {id} nao encontrada.");

            _logger.LogInformation(
                "Alteracao de estado do EncomendaMolde {EncomendaMoldeId}: {EstadoAtual} -> {NovoEstado}",
                id,
                existente.Estado,
                dto.Estado);

            ValidarTransicaoEstado(existente.Estado, dto.Estado);

            if (dto.Estado == EstadoEncomendaMolde.EM_PRODUCAO &&
                !await PodeEntrarEmProducaoAsync(existente.Molde_id))
            {
                throw new ArgumentException(
                    "Nao e possivel colocar o molde em producao: todas as pecas devem ter MaterialRecebido = true.");
            }

            if (dto.Estado == EstadoEncomendaMolde.CONCLUIDO &&
                !await PodeConcluirAsync(existente.Molde_id))
            {
                throw new ArgumentException(
                    "Nao e possivel concluir o molde: todas as pecas devem estar concluidas na fase MONTAGEM.");
            }

            existente.Estado = dto.Estado;
            await _repo.UpdateAsync(existente);
            await AtualizarEstadoEncomendaAssociadaAsync(existente.Encomenda_id);

            if (dto.Estado == EstadoEncomendaMolde.CONCLUIDO)
                await _prioridadeGlobalMoldeService.RecalcularAsync();
        }

        /// <summary>
        /// Remove uma associacao Encomenda-Molde.
        /// </summary>
        /// <param name="id">Identificador da associacao a remover.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        public async Task DeleteAsync(int id)
        {
            var existente = await _repo.GetByIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException($"EncomendaMolde com ID {id} nao encontrada.");

            await _repo.DeleteAsync(id);
            _logger.LogInformation("EncomendaMolde {EncomendaMoldeId} removido com sucesso", id);
        }

        /// <summary>
        /// Indica se o molde pode entrar manualmente em producao.
        /// </summary>
        /// <param name="moldeId">Identificador do molde a validar.</param>
        /// <returns>True quando todas as pecas do molde possuem material recebido.</returns>
        private Task<bool> PodeEntrarEmProducaoAsync(int moldeId)
            => _repo.TodasPecasTemMaterialRecebidoAsync(moldeId);

        /// <summary>
        /// Indica se o molde pode ser marcado manualmente como concluido.
        /// </summary>
        /// <param name="moldeId">Identificador do molde a validar.</param>
        /// <returns>True quando todas as pecas estao concluidas na fase MONTAGEM.</returns>
        private Task<bool> PodeConcluirAsync(int moldeId)
            => _repo.TodasPecasConcluidasNaMontagemAsync(moldeId);

        /// <summary>
        /// Recalcula o estado agregado da encomenda com base em todos os moldes associados.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda a recalcular.</param>
        /// <returns>Task de conclusao da logica de propagacao.</returns>
        private async Task AtualizarEstadoEncomendaAssociadaAsync(int encomendaId)
        {
            var encomenda = await _encomendaRepo.GetByIdAsync(encomendaId);
            if (encomenda == null)
                return;

            if (encomenda.Estado == EstadoEncomenda.CANCELADA || encomenda.Estado == EstadoEncomenda.CONCLUIDA)
                return;

            var estados = await _repo.GetEstadosByEncomendaIdAsync(encomendaId);
            if (estados.Count == 0)
                return;

            var novoEstado = DeterminarEstadoEncomenda(estados);
            if (encomenda.Estado == novoEstado)
                return;

            encomenda.Estado = novoEstado;
            await _encomendaRepo.UpdateAsync(encomenda);
        }

        /// <summary>
        /// Determina o estado agregado da encomenda a partir dos estados dos moldes.
        /// </summary>
        /// <param name="estados">Estados atuais de todos os moldes da encomenda.</param>
        /// <returns>Estado agregado que a encomenda deve assumir.</returns>
        private static EstadoEncomenda DeterminarEstadoEncomenda(List<EstadoEncomendaMolde> estados)
        {
            if (estados.All(e => e == EstadoEncomendaMolde.CONCLUIDO))
                return EstadoEncomenda.CONCLUIDA;

            if (estados.Any(e => e == EstadoEncomendaMolde.EM_PRODUCAO))
                return EstadoEncomenda.EM_PRODUCAO;

            var concluidos = estados.Count(e => e == EstadoEncomendaMolde.CONCLUIDO);
            if (concluidos > 0 && concluidos < estados.Count)
                return EstadoEncomenda.PARCIALMENTE_ENTREGUE;

            return EstadoEncomenda.CONFIRMADA;
        }

        /// <summary>
        /// Valida as transicoes permitidas para o estado do molde na encomenda.
        /// </summary>
        /// <param name="estadoAtual">Estado atual da associacao.</param>
        /// <param name="novoEstado">Estado alvo.</param>
        private static void ValidarTransicaoEstado(EstadoEncomendaMolde estadoAtual, EstadoEncomendaMolde novoEstado)
        {
            var transicoesValidas = new Dictionary<EstadoEncomendaMolde, List<EstadoEncomendaMolde>>
            {
                { EstadoEncomendaMolde.PENDENTE, new() { EstadoEncomendaMolde.EM_PRODUCAO } },
                { EstadoEncomendaMolde.EM_PRODUCAO, new() { EstadoEncomendaMolde.CONCLUIDO } },
                { EstadoEncomendaMolde.CONCLUIDO, new() }
            };

            if (!transicoesValidas[estadoAtual].Contains(novoEstado))
                throw new ArgumentException(
                    $"Transicao de estado invalida: nao e possivel passar de {estadoAtual} para {novoEstado}.");
        }
    }
}
