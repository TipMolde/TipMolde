using AutoMapper;
using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.EncomendaDto;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.ICliente;
using TipMolde.Application.Interface.Comercio.IEncomenda;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa os casos de uso de encomenda.
    /// </summary>
    /// <remarks>
    /// Centraliza validacoes de negocio, unicidade, transicoes de estado e orquestracao de persistencia.
    /// </remarks>
    public class EncomendaService : IEncomendaService
    {
        private readonly IEncomendaRepository _encomendaRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<EncomendaService> _logger;

        /// <summary>
        /// Construtor de EncomendaService.
        /// </summary>
        /// <param name="encomendaRepository">Repositorio de encomendas.</param>
        /// <param name="clienteRepository">Repositorio de clientes para validacao de FK.</param>
        /// <param name="mapper">Mapper para conversao entre entidades e Dtos.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes.</param>
        public EncomendaService(
            IEncomendaRepository encomendaRepository,
            IClienteRepository clienteRepository,
            IMapper mapper,
            ILogger<EncomendaService> logger)
        {
            _encomendaRepository = encomendaRepository;
            _clienteRepository = clienteRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Lista encomendas paginadas.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos de resposta.</returns>
        public async Task<PagedResult<ResponseEncomendaDto>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _encomendaRepository.GetAllAsync(normalizedPage, normalizedPageSize);
            var mappedItems = _mapper.Map<IEnumerable<ResponseEncomendaDto>>(result.Items);
            return new PagedResult<ResponseEncomendaDto>(mappedItems, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Obtem encomenda por ID.
        /// </summary>
        /// <param name="id">Identificador da encomenda.</param>
        /// <returns>DTO da encomenda encontrada ou nulo.</returns>
        public async Task<ResponseEncomendaDto?> GetByIdAsync(int id)
        {
            var encomenda = await _encomendaRepository.GetByIdAsync(id);
            return encomenda == null ? null : _mapper.Map<ResponseEncomendaDto>(encomenda);
        }

        /// <summary>
        /// Obtem encomenda por ID com moldes associados.
        /// </summary>
        /// <param name="id">Identificador da encomenda.</param>
        /// <returns>DTO da encomenda encontrada com relacoes carregadas ou nulo.</returns>
        public async Task<ResponseEncomendaDto?> GetEncomendaWithMoldesAsync(int id)
        {
            var encomenda = await _encomendaRepository.GetWithMoldesAsync(id);
            return encomenda == null ? null : _mapper.Map<ResponseEncomendaDto>(encomenda);
        }

        /// <summary>
        /// Lista encomendas por estado.
        /// </summary>
        /// <param name="estado">Estado textual para filtro.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Colecao de Dtos de encomenda no estado informado.</returns>
        public async Task<PagedResult<ResponseEncomendaDto>> GetByEstadoAsync(EstadoEncomenda estado, int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _encomendaRepository.GetByEstadoAsync(estado, normalizedPage, normalizedPageSize);
            var mappedItems = _mapper.Map<IEnumerable<ResponseEncomendaDto>>(result.Items);
            return new PagedResult<ResponseEncomendaDto>(mappedItems, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Lista encomendas com estado nao terminal.
        /// </summary>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Colecao de Dtos de encomendas por concluir.</returns>
        public async Task<PagedResult<ResponseEncomendaDto>> GetEncomendasPorConcluirAsync(int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _encomendaRepository.GetEncomendasPorConcluirAsync(normalizedPage, normalizedPageSize);
            var mappedItems = _mapper.Map<IEnumerable<ResponseEncomendaDto>>(result.Items);
            return new PagedResult<ResponseEncomendaDto>(mappedItems, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Lista encomendas em producao para a UI operacional.
        /// </summary>
        /// <remarks>
        /// Nesta regra operacional, em producao significa todas as encomendas cujo estado seja diferente de CONCLUIDA.
        /// </remarks>
        public async Task<PagedResult<ResponseEncomendaDto>> GetEncomendasEmProducaoAsync(int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _encomendaRepository.GetEncomendasEmProducaoAsync(normalizedPage, normalizedPageSize);
            var mappedItems = _mapper.Map<IEnumerable<ResponseEncomendaDto>>(result.Items);
            return new PagedResult<ResponseEncomendaDto>(mappedItems, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Obtem encomenda pelo numero de referencia do cliente.
        /// </summary>
        /// <param name="numero">Numero de encomenda do cliente.</param>
        /// <returns>DTO da encomenda encontrada ou nulo.</returns>
        public async Task<ResponseEncomendaDto?> GetByNumeroEncomendaClienteAsync(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentException("O numero de encomenda do cliente e obrigatorio.");

            var encomenda = await _encomendaRepository.GetByNumeroEncomendaClienteAsync(numero);
            return encomenda == null ? null : _mapper.Map<ResponseEncomendaDto>(encomenda);
        }

        /// <summary>
        /// Cria uma nova encomenda.
        /// </summary>
        /// <remarks>
        /// Fluxo:
        /// 1. Valida dados obrigatorios.
        /// 2. Valida existencia do cliente.
        /// 3. Valida unicidade do numero de encomenda.
        /// 4. Define estado inicial e data de registo.
        /// 5. Persiste a encomenda.
        /// </remarks>
        /// <param name="dto">Dados de criacao.</param>
        /// <returns>DTO da encomenda criada e persistida.</returns>
        public async Task<ResponseEncomendaDto> CreateAsync(CreateEncomendaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NumeroEncomendaCliente))
                throw new ArgumentException("O numero de encomenda do cliente e obrigatorio.");

            var numeroNormalizado = dto.NumeroEncomendaCliente.Trim();

            var cliente = await _clienteRepository.GetByIdAsync(dto.Cliente_id);
            if (cliente == null)
                throw new KeyNotFoundException($"Cliente com ID {dto.Cliente_id} nao encontrado.");
            var numeroDuplicado = await _encomendaRepository.ExistsNumeroEncomendaClienteAsync(numeroNormalizado);
            if (numeroDuplicado)
                throw new BusinessConflictException($"Ja existe uma encomenda com o numero '{numeroNormalizado}'.");

            var novaEncomenda = _mapper.Map<Encomenda>(dto);
            novaEncomenda.NumeroEncomendaCliente = numeroNormalizado;
            novaEncomenda.Estado = EstadoEncomenda.CONFIRMADA;
            novaEncomenda.DataRegisto = DateTime.UtcNow;

            await _encomendaRepository.AddAsync(novaEncomenda);
            _logger.LogInformation("Encomenda criada com sucesso {EncomendaId}", novaEncomenda.Encomenda_id);

            return _mapper.Map<ResponseEncomendaDto>(novaEncomenda);
        }

        /// <summary>
        /// Atualiza parcialmente os dados de uma encomenda.
        /// </summary>
        /// <remarks>
        /// Campos nulos sao ignorados.
        /// A validacao de unicidade do numero de encomenda acontece antes da persistencia para evitar falha tecnica da BD.
        /// </remarks>
        /// <param name="dto">DTO alvo e campos opcionais de patch.</param>
        /// <param name="id">Identificador da encomenda a ser atualizada.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        public async Task UpdateAsync(int id, UpdateEncomendaDto dto)
        {
            var existente = await _encomendaRepository.GetByIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException($"Encomenda com ID {id} nao encontrada.");

            var hasChanges =
                !string.IsNullOrWhiteSpace(dto.NumeroEncomendaCliente) ||
                !string.IsNullOrWhiteSpace(dto.NumeroProjetoCliente) ||
                !string.IsNullOrWhiteSpace(dto.NomeServicoCliente) ||
                !string.IsNullOrWhiteSpace(dto.NomeResponsavelCliente);

            if (!hasChanges)
                throw new ArgumentException("Pelo menos um campo deve ser informado para atualizacao.");

            if (!string.IsNullOrWhiteSpace(dto.NumeroEncomendaCliente))
            {
                var novoNumero = dto.NumeroEncomendaCliente.Trim();

                if (!string.Equals(existente.NumeroEncomendaCliente, novoNumero, StringComparison.Ordinal))
                {
                    var numeroDuplicado = await _encomendaRepository.ExistsNumeroEncomendaClienteAsync(novoNumero, existente.Encomenda_id);
                    if (numeroDuplicado)
                        throw new BusinessConflictException($"Ja existe uma encomenda com o numero '{novoNumero}'.");

                }
            }

            _mapper.Map(dto, existente);

            await _encomendaRepository.UpdateAsync(existente);
            _logger.LogInformation("Encomenda atualizada com sucesso {EncomendaId}", existente.Encomenda_id);
        }

        /// <summary>
        /// Atualiza o estado de uma encomenda respeitando a maquina de estados.
        /// </summary>
        /// <param name="id">Identificador da encomenda.</param>
        /// <param name="dto">DTO contendo o novo estado da encomenda.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        public async Task UpdateEstadoAsync(int id, UpdateEstadoEncomendaDto dto)
        {
            var encomenda = await _encomendaRepository.GetByIdAsync(id);
            if (encomenda == null)
            {
                _logger.LogWarning("Alteracao de estado falhou: encomenda nao encontrada {EncomendaId}", id);
                throw new KeyNotFoundException($"Encomenda com ID {id} nao encontrada.");
            }

            _logger.LogInformation(
                "Alteracao de estado da encomenda {EncomendaId}: {EstadoAtual} -> {NovoEstado}",
                id,
                encomenda.Estado,
                dto.Estado);

            ValidarTransicaoEstado(encomenda.Estado, dto.Estado);
            _mapper.Map(dto, encomenda);

            await _encomendaRepository.UpdateAsync(encomenda);
        }

        /// <summary>
        /// Remove uma encomenda por identificador.
        /// </summary>
        /// <param name="id">Identificador da encomenda.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        public async Task DeleteAsync(int id)
        {
            var encomenda = await _encomendaRepository.GetByIdAsync(id);
            if (encomenda == null)
                throw new KeyNotFoundException($"Encomenda com ID {id} nao encontrada.");

            await _encomendaRepository.DeleteAsync(id);
            _logger.LogInformation("Encomenda eliminada com sucesso {EncomendaId}", id);
        }

        /// <summary>
        /// Valida se a transicao de estado e permitida pela regra de negocio.
        /// </summary>
        /// <remarks>
        /// Porque: impede regressao de estado e preserva rastreabilidade do ciclo de vida.
        /// Risco: ao adicionar novos valores em EstadoEncomenda, este mapa deve ser atualizado no mesmo commit.
        /// </remarks>
        /// <param name="estadoAtual">Estado atual da encomenda.</param>
        /// <param name="novoEstado">Estado alvo para transicao.</param>
        private static void ValidarTransicaoEstado(EstadoEncomenda estadoAtual, EstadoEncomenda novoEstado)
        {
            var transicoesValidas = new Dictionary<EstadoEncomenda, List<EstadoEncomenda>>
            {
                { EstadoEncomenda.CONFIRMADA,            new() { EstadoEncomenda.EM_PRODUCAO, EstadoEncomenda.CANCELADA } },
                { EstadoEncomenda.EM_PRODUCAO,           new() { EstadoEncomenda.PARCIALMENTE_ENTREGUE, EstadoEncomenda.CONCLUIDA, EstadoEncomenda.CANCELADA } },
                { EstadoEncomenda.PARCIALMENTE_ENTREGUE, new() { EstadoEncomenda.CONCLUIDA } },
                { EstadoEncomenda.CONCLUIDA,             new() },
                { EstadoEncomenda.CANCELADA,             new() }
            };

            if (!transicoesValidas[estadoAtual].Contains(novoEstado))
                throw new ArgumentException(
                    $"Transicao de estado invalida: nao e possivel passar de {estadoAtual} para {novoEstado}.");
        }
    }
}
