using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using TipMolde.Application.Dtos.EncomendaMoldeDto;
using TipMolde.Application.Dtos.PecaDto;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Desenho.IProjeto;
using TipMolde.Application.Interface.Producao.IFasesProducao;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Producao.IRegistosProducao;
using TipMolde.Application.Mappings;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa os casos de uso da feature Peca.
    /// </summary>
    /// <remarks>
    /// Centraliza validacoes de negocio, atualizacao parcial, importacao CSV e orquestracao funcional
    /// da criacao, consulta, edicao e remocao de pecas associadas a um molde.
    /// </remarks>
    public class PecaService : IPecaService
    {
        private static readonly string[] CsvHeader =
        [
            "N PECA",
            "DESIGNACAO",
            "QTD",
            "REF",
            "MATERIAL",
            "TRAT TERMICO",
            "MASS",
            "OBS"
        ];

        private readonly IPecaRepository _pecaRepository;
        private readonly IMoldeRepository _moldeRepository;
        private readonly IProjetoRepository _projetoRepository;
        private readonly IFasesProducaoRepository _fasesProducaoRepository;
        private readonly IEncomendaMoldeService _encomendaMoldeService;
        private readonly IRegistosProducaoRepository _registosProducaoRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PecaService> _logger;

        /// <summary>
        /// Construtor de PecaService.
        /// </summary>
        /// <param name="pecaRepository">Repositorio do agregado Peca.</param>
        /// <param name="moldeRepository">Repositorio usado para validar o molde associado.</param>
        /// <param name="mapper">Mapper para conversao entre Dtos e entidade.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes criticas.</param>
        public PecaService(
            IPecaRepository pecaRepository,
            IMoldeRepository moldeRepository,
            IProjetoRepository projetoRepository,
            IFasesProducaoRepository fasesProducaoRepository,
            IEncomendaMoldeService encomendaMoldeService,
            IRegistosProducaoRepository registosProducaoRepository,
            IMapper mapper,
            ILogger<PecaService> logger)
        {
            _pecaRepository = pecaRepository;
            _moldeRepository = moldeRepository;
            _projetoRepository = projetoRepository;
            _fasesProducaoRepository = fasesProducaoRepository;
            _encomendaMoldeService = encomendaMoldeService;
            _registosProducaoRepository = registosProducaoRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Lista pecas de forma paginada.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com Dtos de peca.</returns>
        public async Task<PagedResult<ResponsePecaDto>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _pecaRepository.GetAllAsync(normalizedPage, normalizedPageSize);
            var items = _mapper.Map<IEnumerable<ResponsePecaDto>>(result.Items);
            return new PagedResult<ResponsePecaDto>(items, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Obtem uma peca por identificador.
        /// </summary>
        /// <param name="id">Identificador interno da peca.</param>
        /// <returns>DTO da peca quando encontrada; nulo caso contrario.</returns>
        public async Task<ResponsePecaDto?> GetByIdAsync(int id)
        {
            var peca = await _pecaRepository.GetByIdAsync(id);
            return peca == null ? null : _mapper.Map<ResponsePecaDto>(peca);
        }

        /// <summary>
        /// Lista pecas associadas a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com Dtos de peca.</returns>
        public async Task<PagedResult<ResponsePecaDto>> GetByMoldeIdAsync(int moldeId, int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _pecaRepository.GetByMoldeIdAsync(moldeId, normalizedPage, normalizedPageSize);
            var items = _mapper.Map<IEnumerable<ResponsePecaDto>>(result.Items);
            return new PagedResult<ResponsePecaDto>(items, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Lista pecas de um molde que ainda nao foram adicionadas a qualquer pedido de material.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <param name="searchTerm">Termo opcional para filtrar as pecas elegiveis.</param>
        /// <returns>Resultado paginado com pecas elegiveis para pedido de material.</returns>
        public async Task<PagedResult<ResponsePecaDto>> GetByMoldeIdWithoutPedidoMaterialAsync(int moldeId, int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var normalizedSearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
            var result = await _pecaRepository.GetByMoldeIdWithoutPedidoMaterialAsync(moldeId, normalizedPage, normalizedPageSize, normalizedSearchTerm);
            var items = _mapper.Map<IEnumerable<ResponsePecaDto>>(result.Items);
            return new PagedResult<ResponsePecaDto>(items, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Lista pecas de um molde que possuem pedido de material pendente de rececao.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com pecas que aguardam rececao de material.</returns>
        public async Task<PagedResult<ResponsePecaDto>> GetByMoldeIdPendingMaterialReceiptAsync(int moldeId, int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _pecaRepository.GetByMoldeIdPendingMaterialReceiptAsync(moldeId, normalizedPage, normalizedPageSize);
            var items = _mapper.Map<IEnumerable<ResponsePecaDto>>(result.Items);
            return new PagedResult<ResponsePecaDto>(items, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Lista a fila de trabalho operacional das pecas da producao.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <param name="searchTerm">Termo de pesquisa opcional.</param>
        /// <param name="searchMode">Modo de pesquisa, por molde ou peca.</param>
        /// <returns>Resultado paginado com itens prontos para a pagina de producao.</returns>
        public async Task<PagedResult<ResponsePecaFilaTrabalhoDto>> GetFilaTrabalhoAsync(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string searchMode = "Molde")
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var moldesFila = await GetAllFilaGlobalMoldeAsync();
            if (moldesFila.Count == 0)
                return new PagedResult<ResponsePecaFilaTrabalhoDto>([], 0, normalizedPage, normalizedPageSize);

            var moldesPorId = moldesFila
                .GroupBy(item => item.Molde_id)
                .Select(group => group.First())
                .ToDictionary(item => item.Molde_id);

            var pecas = await _pecaRepository.GetByMoldeIdsAsync(moldesPorId.Keys);
            if (pecas.Count == 0)
                return new PagedResult<ResponsePecaFilaTrabalhoDto>([], 0, normalizedPage, normalizedPageSize);

            var ultimosRegistos = await _registosProducaoRepository.GetUltimosRegistosGlobaisAsync(pecas.Select(item => item.Peca_id));
            var ultimosPorPeca = ultimosRegistos.ToDictionary(item => item.Peca_id);

            var itens = pecas
                .Select(peca => BuildFilaTrabalhoItem(peca, moldesPorId, ultimosPorPeca))
                .Where(item => item is not null)
                .Cast<ResponsePecaFilaTrabalhoDto>()
                .Where(item => MatchesSearch(item, searchTerm, searchMode))
                .OrderBy(item => item.PrioridadeMolde)
                .ThenBy(item => item.PrioridadePeca)
                .ThenBy(item => item.DataEntregaPrevista > DateTime.MinValue ? item.DataEntregaPrevista : DateTime.MaxValue)
                .ThenBy(item => item.NumeroMolde)
                .ThenBy(item => item.NumeroPeca)
                .ThenBy(item => item.Designacao)
                .ToList();

            var totalItems = itens.Count;
            var pagedItems = itens
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToList();

            return new PagedResult<ResponsePecaFilaTrabalhoDto>(pagedItems, totalItems, normalizedPage, normalizedPageSize);
        }

        /// <summary>
        /// Obtem uma peca pela designacao dentro de um molde.
        /// </summary>
        /// <param name="designacao">Designacao funcional da peca.</param>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>DTO da peca quando encontrada; nulo caso contrario.</returns>
        public async Task<ResponsePecaDto?> GetByDesignacaoAsync(string designacao, int moldeId)
        {
            var designacaoNormalizada = designacao.Trim();
            var peca = await _pecaRepository.GetByDesignacaoAsync(designacaoNormalizada, moldeId);
            return peca == null ? null : _mapper.Map<ResponsePecaDto>(peca);
        }

        /// <summary>
        /// Cria uma nova peca.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida molde existente.
        /// 2. Valida designacao obrigatoria.
        /// 3. Garante unicidade por NumeroPeca quando esse identificador existe.
        /// 4. Usa a designacao como fallback de unicidade quando NumeroPeca nao e informado.
        /// 5. Persiste a peca e devolve o DTO estavel da feature.
        /// </remarks>
        /// <param name="dto">Dados de criacao da peca.</param>
        /// <returns>DTO da peca criada.</returns>
        public async Task<ResponsePecaDto> CreateAsync(CreatePecaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Designacao))
                throw new ArgumentException("Designacao e obrigatoria.");

            if (dto.Quantidade < 1)
                throw new ArgumentException("Quantidade deve ser maior ou igual a 1.");

            var molde = await _moldeRepository.GetByIdAsync(dto.Molde_id);
            if (molde == null)
                throw new KeyNotFoundException($"Molde com ID {dto.Molde_id} nao encontrado.");

            await EnsureMoldePodeReceberPecasAsync(dto.Molde_id);

            var numeroPecaNormalizado = MappingProfileExtensions.NormalizeOptionalString(dto.NumeroPeca);
            var designacaoNormalizada = dto.Designacao.Trim();
            var proximaFaseId = await ResolveProximaFaseIdAsync(dto.ProximaFase_id);

            await ValidateUniquePecaAsync(dto.Molde_id, numeroPecaNormalizado, designacaoNormalizada, null);

            var peca = _mapper.Map<Peca>(dto);
            peca.NumeroPeca = numeroPecaNormalizado;
            peca.Designacao = designacaoNormalizada;
            peca.Quantidade = dto.Quantidade;
            peca.ProximaFase_id = proximaFaseId;

            if (proximaFaseId.HasValue)
                peca.ProximaFase = await _fasesProducaoRepository.GetByIdAsync(proximaFaseId.Value);

            await _pecaRepository.AddAsync(peca);

            _logger.LogInformation(
                "Peca {PecaId} criada com sucesso no molde {MoldeId}",
                peca.Peca_id,
                dto.Molde_id);

            return _mapper.Map<ResponsePecaDto>(peca);
        }

        /// <summary>
        /// Atualiza parcialmente uma peca existente.
        /// </summary>
        /// <remarks>
        /// Campos nao enviados no DTO devem manter o valor atual da entidade.
        /// </remarks>
        /// <param name="id">Identificador da peca a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        public async Task UpdateAsync(int id, UpdatePecaDto dto)
        {
            var existente = await _pecaRepository.GetByIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException($"Peca com ID {id} nao encontrada.");

            if (!HasAnyChanges(dto))
                throw new ArgumentException("Pelo menos um campo deve ser informado para atualizacao.");

            var numeroPecaFuturo = ResolveFutureValue(dto.NumeroPeca, existente.NumeroPeca);
            var designacaoFutura = ResolveFutureRequiredValue(dto.Designacao, existente.Designacao);

            if (dto.Quantidade.HasValue && dto.Quantidade.Value < 1)
                throw new ArgumentException("Quantidade deve ser maior ou igual a 1.");

            await ValidateUniquePecaAsync(existente.Molde_id, numeroPecaFuturo, designacaoFutura, id);
            await ValidateProximaFaseAsync(dto.ProximaFase_id);
            await ValidateProximaFaseChangeAllowedAsync(existente, dto.ProximaFase_id);

            _mapper.Map(dto, existente);
            existente.NumeroPeca = numeroPecaFuturo;
            existente.Designacao = designacaoFutura;

            if (dto.ProximaFase_id.HasValue)
                existente.ProximaFase = await _fasesProducaoRepository.GetByIdAsync(dto.ProximaFase_id.Value);

            await _pecaRepository.UpdateAsync(existente);

            _logger.LogInformation("Peca {PecaId} atualizada com sucesso", id);
        }

        /// <summary>
        /// Importa pecas a partir de um ficheiro CSV da lista de materiais.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida molde existente e estrutura do ficheiro.
        /// 2. Le a linha-resumo do molde.
        /// 3. Agrupa linhas por NumeroPeca.
        /// 4. Consolida quantidades quando os restantes campos coincidem.
        /// 5. Rejeita grupos contraditorios para o mesmo NumeroPeca.
        /// 6. Persiste as pecas consolidadas no molde indicado.
        /// </remarks>
        /// <param name="moldeId">Identificador do molde que recebe as pecas importadas.</param>
        /// <param name="csvStream">Stream do ficheiro CSV a processar.</param>
        /// <returns>Resumo da importacao com as pecas persistidas.</returns>
        public async Task<ImportPecasCsvResultDto> ImportarCsvAsync(int moldeId, Stream csvStream)
        {
            ArgumentNullException.ThrowIfNull(csvStream);

            if (!csvStream.CanRead)
                throw new ArgumentException("O ficheiro CSV nao pode ser lido.");

            var molde = await _moldeRepository.GetByIdAsync(moldeId);
            if (molde == null)
                throw new KeyNotFoundException($"Molde com ID {moldeId} nao encontrado.");

            await EnsureMoldePodeReceberPecasAsync(moldeId);

            if (csvStream.CanSeek)
                csvStream.Seek(0, SeekOrigin.Begin);

            var parsedFile = await ParseCsvAsync(csvStream);
            var grupos = parsedFile.LinhasPeca
                .GroupBy(x => x.NumeroPeca, StringComparer.OrdinalIgnoreCase)
                .Select(g => new CsvPecaGroup(g.Key, g.OrderBy(x => x.NumeroLinha).ToList(), g.Min(x => x.NumeroLinha)))
                .OrderBy(g => GetCsvPriorityBucket(g.NumeroPeca))
                .ThenBy(g => g.FirstLineNumber)
                .ToList();

            var resultado = new ImportPecasCsvResultDto
            {
                MoldeId = moldeId,
                ReferenciaMolde = parsedFile.ReferenciaMolde,
                MassaMolde = parsedFile.MassaMolde,
                TotalLinhasPecaLidas = parsedFile.LinhasPeca.Count
            };

            var pecasConsolidadas = new List<Peca>();
            var prioridadeAtual = 1;

            foreach (var grupo in grupos)
            {
                var linhasGrupo = grupo.Linhas;
                ValidateCsvGroupConsistency(grupo.NumeroPeca, linhasGrupo);

                var pecaConsolidada = BuildPecaFromCsvGroup(moldeId, prioridadeAtual, linhasGrupo);
                pecaConsolidada.ProximaFase_id = await ResolveProximaFaseIdAsync(null);

                if (pecaConsolidada.ProximaFase_id.HasValue)
                {
                    pecaConsolidada.ProximaFase = await _fasesProducaoRepository.GetByIdAsync(pecaConsolidada.ProximaFase_id.Value);
                }

                await ValidateUniquePecaAsync(moldeId, pecaConsolidada.NumeroPeca, pecaConsolidada.Designacao, null);

                pecasConsolidadas.Add(pecaConsolidada);
                prioridadeAtual++;
            }

            foreach (var pecaConsolidada in pecasConsolidadas)
            {
                await _pecaRepository.AddAsync(pecaConsolidada);
                resultado.PecasImportadas.Add(_mapper.Map<ResponsePecaDto>(pecaConsolidada));
                resultado.TotalQuantidadeConsolidada += pecaConsolidada.Quantidade;
            }

            resultado.TotalPecasConsolidadas = pecasConsolidadas.Count;

            _logger.LogInformation(
                "Importacao CSV de pecas concluida para Molde {MoldeId} com {TotalPecas} pecas consolidadas e {TotalQuantidade} unidades",
                moldeId,
                resultado.TotalPecasConsolidadas,
                resultado.TotalQuantidadeConsolidada);

            return resultado;
        }

        /// <summary>
        /// Remove uma peca existente.
        /// </summary>
        /// <param name="id">Identificador da peca a remover.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        public async Task DeleteAsync(int id)
        {
            var peca = await _pecaRepository.GetByIdAsync(id);
            if (peca == null)
                throw new KeyNotFoundException($"Peca com ID {id} nao encontrada.");

            await _pecaRepository.DeleteAsync(id);

            _logger.LogInformation("Peca {PecaId} removida com sucesso", id);
        }

        private async Task<List<FilaGlobalMoldeItemDto>> GetAllFilaGlobalMoldeAsync()
        {
            const int pageSize = 100;
            var primeiraPagina = await _encomendaMoldeService.GetFilaGlobalAsync(1, pageSize);

            var moldes = primeiraPagina.Items.ToList();
            var totalPages = primeiraPagina.PageSize <= 0
                ? 0
                : (int)Math.Ceiling((double)primeiraPagina.TotalCount / primeiraPagina.PageSize);

            for (var page = 2; page <= totalPages; page++)
            {
                var pagina = await _encomendaMoldeService.GetFilaGlobalAsync(page, pageSize);
                moldes.AddRange(pagina.Items);
            }

            return moldes;
        }

        private static ResponsePecaFilaTrabalhoDto? BuildFilaTrabalhoItem(
            Peca peca,
            IReadOnlyDictionary<int, FilaGlobalMoldeItemDto> moldesPorId,
            IReadOnlyDictionary<int, RegistosProducao> ultimosPorPeca)
        {
            if (!moldesPorId.TryGetValue(peca.Molde_id, out var molde))
                return null;

            if (!peca.MaterialRecebido)
                return null;

            ultimosPorPeca.TryGetValue(peca.Peca_id, out var ultimo);
            if (ultimo is not null && EstadoEstaAtivo(ultimo.Estado_producao))
                return null;

            var proximaFaseNome = peca.ProximaFase?.Nome.ToString() ?? string.Empty;
            var estadoAtual = ultimo?.Estado_producao.ToString() ?? string.Empty;
            var faseAtual = ultimo?.Fase?.Nome.ToString() ?? string.Empty;

            return new ResponsePecaFilaTrabalhoDto
            {
                PecaId = peca.Peca_id,
                MoldeId = peca.Molde_id,
                EncomendaMolde_id = molde.EncomendaMolde_id,
                PrioridadeMolde = molde.Prioridade,
                PrioridadePeca = peca.Prioridade,
                Quantidade = peca.Quantidade,
                NumeroMolde = molde.NumeroMolde ?? string.Empty,
                NomeMolde = molde.NomeMolde ?? string.Empty,
                NumeroEncomendaCliente = molde.NumeroEncomendaCliente ?? string.Empty,
                NomeCliente = molde.NomeCliente ?? string.Empty,
                Designacao = peca.Designacao,
                NumeroPeca = peca.NumeroPeca ?? string.Empty,
                DataEntregaPrevista = molde.DataEntregaPrevista,
                UltimoEstadoGlobal = estadoAtual,
                UltimaFaseGlobal = faseAtual,
                ProximaFaseId = peca.ProximaFase_id,
                ProximaFaseNome = proximaFaseNome,
                FaseTrabalho = proximaFaseNome,
                ProximoPasso = BuildProximoPasso(estadoAtual, proximaFaseNome)
            };
        }

        private static bool MatchesSearch(ResponsePecaFilaTrabalhoDto item, string? searchTerm, string searchMode)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return true;

            var term = searchTerm.Trim();

            return NormalizeSearchMode(searchMode) switch
            {
                "PECA" => ContainsAny(term, item.Designacao, item.NumeroPeca),
                "PROXIMA FASE" => ContainsAny(term, item.ProximaFaseNome, item.FaseTrabalho, item.ProximoPasso),
                _ => ContainsAny(term, item.NumeroMolde, item.NomeMolde, item.NumeroEncomendaCliente, item.NomeCliente)
            };
        }

        private static bool ContainsAny(string term, params string?[] values)
        {
            var normalizedTerm = NormalizeSearchText(term);

            return values.Any(value =>
                !string.IsNullOrWhiteSpace(value) &&
                NormalizeSearchText(value).Contains(normalizedTerm, StringComparison.Ordinal));
        }

        private static string NormalizeSearchText(string value)
        {
            var normalized = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }

            return builder.ToString();
        }

        private static string NormalizeSearchMode(string searchMode)
        {
            return string.IsNullOrWhiteSpace(searchMode)
                ? "MOLDE"
                : NormalizeSearchText(searchMode);
        }

        private static string BuildProximoPasso(string estadoAtual, string proximaFaseNome)
        {
            var fase = string.IsNullOrWhiteSpace(proximaFaseNome) ? "fase" : proximaFaseNome;

            return NormalizeEstado(estadoAtual) switch
            {
                "PAUSADO" => $"Retomar {fase}",
                "PREPARACAO" => $"Continuar {fase}",
                "PENDENTE" => $"Iniciar {fase}",
                _ => $"Iniciar {fase}"
            };
        }

        private static bool EstadoEstaAtivo(EstadoProducao estado)
        {
            return estado is EstadoProducao.PREPARACAO or EstadoProducao.EM_CURSO;
        }

        private static string NormalizeEstado(string? estado)
        {
            return string.IsNullOrWhiteSpace(estado)
                ? string.Empty
                : estado.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Garante que o molde tem um projeto concluido e aprovado antes de permitir a criacao de pecas.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        private async Task EnsureMoldePodeReceberPecasAsync(int moldeId)
        {
            var projetos = await _projetoRepository.GetByMoldeIdAsync(moldeId, page: 1, pageSize: 1);
            var projetoMaisRecente = projetos?.Items?.FirstOrDefault();

            if (projetoMaisRecente == null)
                throw new BusinessConflictException("Nao e possivel adicionar pecas a um molde sem projeto aprovado pelo cliente.");

            var projetoComRevisoes = await _projetoRepository.GetWithRevisoesAsync(projetoMaisRecente.Projeto_id);
            if (projetoComRevisoes == null)
                throw new BusinessConflictException("Nao e possivel adicionar pecas a um molde sem projeto aprovado pelo cliente.");

            var ultimaRevisao = projetoComRevisoes.Revisoes
                .OrderByDescending(item => item.NumRevisao)
                .FirstOrDefault();

            if (ultimaRevisao == null || ultimaRevisao.Aprovado != true || !ultimaRevisao.DataResposta.HasValue)
                throw new BusinessConflictException("Nao e possivel adicionar pecas enquanto o projeto nao estiver concluido e aprovado pelo cliente.");
        }

        /// <summary>
        /// Define o bloco de prioridade das pecas importadas do CSV.
        /// </summary>
        /// <remarks>
        /// Regra do cliente: pecas base 100, 200 e 080/80 primeiro; depois as variantes
        /// desses grupos; depois pecas de 0 a 011; as restantes seguem a ordem original do CSV.
        /// </remarks>
        /// <param name="numeroPeca">Numero funcional da peca importada.</param>
        /// <returns>Bloco de ordenacao usado antes da prioridade sequencial.</returns>
        private static int GetCsvPriorityBucket(string numeroPeca)
        {
            var normalized = MappingProfileExtensions.NormalizeOptionalString(numeroPeca)?.ToUpperInvariant() ?? string.Empty;
            var hasLeadingNumber = TryGetLeadingNumber(normalized, out var leadingNumber, out var hasSuffix);

            if (hasLeadingNumber && leadingNumber == 100 && !hasSuffix)
                return 0;

            if (hasLeadingNumber && leadingNumber == 200 && !hasSuffix)
                return 1;

            if (hasLeadingNumber && leadingNumber == 80 && !hasSuffix)
                return 2;

            if (hasLeadingNumber && leadingNumber == 100)
                return 3;

            if (hasLeadingNumber && leadingNumber == 200)
                return 4;

            if (hasLeadingNumber && leadingNumber == 80)
                return 5;

            if (hasLeadingNumber && leadingNumber is >= 0 and <= 11)
                return 6;

            return 7;
        }

        /// <summary>
        /// Extrai o prefixo numerico inicial de um NumeroPeca.
        /// </summary>
        /// <param name="value">NumeroPeca normalizado.</param>
        /// <param name="number">Prefixo numerico extraido.</param>
        /// <param name="hasSuffix">Indica se existe texto apos o prefixo numerico.</param>
        /// <returns>True quando existe prefixo numerico valido.</returns>
        private static bool TryGetLeadingNumber(string value, out int number, out bool hasSuffix)
        {
            number = 0;
            hasSuffix = false;
            var digits = new StringBuilder();

            foreach (var character in value)
            {
                if (!char.IsDigit(character))
                    break;

                digits.Append(character);
            }

            hasSuffix = digits.Length < value.Length;
            return digits.Length > 0 && int.TryParse(digits.ToString(), out number);
        }

        /// <summary>
        /// Verifica se o pedido de update contem pelo menos uma alteracao funcional.
        /// </summary>
        /// <param name="dto">DTO de atualizacao parcial.</param>
        /// <returns>True quando existe pelo menos um campo preenchido; false caso contrario.</returns>
        private static bool HasAnyChanges(UpdatePecaDto dto)
        {
            return !string.IsNullOrWhiteSpace(dto.NumeroPeca)
                || !string.IsNullOrWhiteSpace(dto.Designacao)
                || dto.Prioridade.HasValue
                || dto.Quantidade.HasValue
                || dto.Referencia != null
                || dto.MaterialDesignacao != null
                || dto.TratamentoTermico != null
                || dto.Massa != null
                || dto.Observacao != null
                || dto.MaterialRecebido.HasValue
                || dto.ProximaFase_id.HasValue;
        }

        private async Task ValidateProximaFaseAsync(int? proximaFaseId)
        {
            if (!proximaFaseId.HasValue)
                return;

            if (await _fasesProducaoRepository.GetByIdAsync(proximaFaseId.Value) == null)
                throw new KeyNotFoundException($"Fase com ID {proximaFaseId.Value} nao encontrada.");
        }

        private async Task ValidateProximaFaseChangeAllowedAsync(Peca existente, int? requestedProximaFaseId)
        {
            if (!requestedProximaFaseId.HasValue)
                return;

            if (existente.ProximaFase_id.HasValue && existente.ProximaFase_id.Value == requestedProximaFaseId.Value)
                return;

            var ultimoRegistoGlobal = await _registosProducaoRepository.GetUltimosRegistosGlobaisAsync(new[] { existente.Peca_id });
            var registoAtual = ultimoRegistoGlobal.FirstOrDefault();

            if (registoAtual is null)
                return;

            if (registoAtual.Estado_producao is EstadoProducao.PREPARACAO or EstadoProducao.EM_CURSO)
                throw new BusinessConflictException(
                    "Nao e possivel alterar a proxima fase enquanto a peca tem producao ativa.");
        }

        private async Task<int?> ResolveProximaFaseIdAsync(int? requestedProximaFaseId)
        {
            if (requestedProximaFaseId.HasValue)
            {
                await ValidateProximaFaseAsync(requestedProximaFaseId);
                return requestedProximaFaseId;
            }

            foreach (var nomeFase in new[] { NomeFases.MAQUINACAO, NomeFases.EROSAO, NomeFases.MONTAGEM })
            {
                var fase = await _fasesProducaoRepository.GetByNomeAsync(nomeFase);
                if (fase is not null)
                    return fase.Fases_producao_id;
            }

            return null;
        }

        /// <summary>
        /// Valida a unicidade funcional da peca dentro do molde.
        /// </summary>
        /// <param name="moldeId">Identificador do molde ao qual a peca pertence.</param>
        /// <param name="numeroPeca">Numero funcional da peca, quando informado.</param>
        /// <param name="designacao">Designacao usada como fallback de unicidade.</param>
        /// <param name="currentPecaId">Identificador da peca atual a excluir em updates.</param>
        private async Task ValidateUniquePecaAsync(int moldeId, string? numeroPeca, string designacao, int? currentPecaId)
        {
            if (!string.IsNullOrWhiteSpace(numeroPeca))
            {
                var duplicadoPorNumero = await _pecaRepository.GetByNumeroPecaAsync(numeroPeca, moldeId);
                if (duplicadoPorNumero != null && duplicadoPorNumero.Peca_id != currentPecaId)
                    throw new ArgumentException($"Ja existe uma peca com o NumeroPeca '{numeroPeca}' neste molde.");

                return;
            }

            var duplicadoPorDesignacao = await _pecaRepository.GetByDesignacaoAsync(designacao, moldeId);
            if (duplicadoPorDesignacao != null && duplicadoPorDesignacao.Peca_id != currentPecaId)
                throw new ArgumentException("Ja existe uma peca com esta designacao neste molde.");
        }

        /// <summary>
        /// Resolve o valor opcional que uma propriedade tera apos um update parcial.
        /// </summary>
        /// <param name="incomingValue">Valor recebido no DTO de update.</param>
        /// <param name="currentValue">Valor atualmente persistido.</param>
        /// <returns>Valor normalizado quando enviado; caso contrario, valor atual.</returns>
        private static string? ResolveFutureValue(string? incomingValue, string? currentValue)
        {
            return incomingValue == null
                ? currentValue
                : MappingProfileExtensions.NormalizeOptionalString(incomingValue);
        }

        /// <summary>
        /// Resolve o valor obrigatorio que uma propriedade tera apos um update parcial.
        /// </summary>
        /// <param name="incomingValue">Valor recebido no DTO de update.</param>
        /// <param name="currentValue">Valor atualmente persistido.</param>
        /// <returns>Valor trimado quando enviado; caso contrario, valor atual.</returns>
        private static string ResolveFutureRequiredValue(string? incomingValue, string currentValue)
        {
            return string.IsNullOrWhiteSpace(incomingValue)
                ? currentValue
                : incomingValue.Trim();
        }

        /// <summary>
        /// Constroi uma peca consolidada a partir de um grupo de linhas CSV do mesmo NumeroPeca.
        /// </summary>
        /// <param name="moldeId">Identificador do molde de destino.</param>
        /// <param name="prioridade">Prioridade sequencial atribuida a peca importada.</param>
        /// <param name="linhasGrupo">Linhas CSV que representam a mesma peca.</param>
        /// <returns>Entidade Peca pronta para persistencia.</returns>
        private static Peca BuildPecaFromCsvGroup(int moldeId, int prioridade, IReadOnlyCollection<PecaCsvLinhaDto> linhasGrupo)
        {
            var primeiraLinha = linhasGrupo.OrderBy(x => x.NumeroLinha).First();

            return new Peca
            {
                NumeroPeca = primeiraLinha.NumeroPeca,
                Designacao = primeiraLinha.Designacao,
                Prioridade = prioridade,
                Quantidade = linhasGrupo.Sum(x => x.Quantidade),
                Referencia = primeiraLinha.Referencia,
                MaterialDesignacao = primeiraLinha.MaterialDesignacao,
                TratamentoTermico = primeiraLinha.TratamentoTermico,
                Massa = primeiraLinha.Massa,
                Observacao = primeiraLinha.Observacao,
                MaterialRecebido = false,
                Molde_id = moldeId
            };
        }

        /// <summary>
        /// Garante que linhas CSV com o mesmo NumeroPeca podem ser consolidadas.
        /// </summary>
        /// <param name="numeroPeca">Numero funcional da peca importada.</param>
        /// <param name="linhasGrupo">Linhas CSV agrupadas pelo mesmo NumeroPeca.</param>
        private static void ValidateCsvGroupConsistency(string numeroPeca, IReadOnlyCollection<PecaCsvLinhaDto> linhasGrupo)
        {
            var primeiraLinha = linhasGrupo.OrderBy(x => x.NumeroLinha).First();

            foreach (var linha in linhasGrupo.Skip(1))
            {
                var camposConflitantes = GetConflictingFields(primeiraLinha, linha);
                if (camposConflitantes.Count == 0)
                    continue;

                var numerosLinha = string.Join(", ", linhasGrupo.Select(x => x.NumeroLinha));
                var campos = string.Join(", ", camposConflitantes);

                throw new ArgumentException(
                    $"O NumeroPeca '{numeroPeca}' aparece com dados contraditorios nas linhas {numerosLinha}. " +
                    $"Os campos {campos} devem ser iguais para consolidar a quantidade.");
            }
        }

        /// <summary>
        /// Identifica campos divergentes entre duas linhas CSV da mesma peca.
        /// </summary>
        /// <param name="referencia">Linha usada como referencia do grupo.</param>
        /// <param name="atual">Linha atual a comparar.</param>
        /// <returns>Lista com nomes dos campos contraditorios.</returns>
        private static List<string> GetConflictingFields(PecaCsvLinhaDto referencia, PecaCsvLinhaDto atual)
        {
            var conflitos = new List<string>();

            if (!StringEquals(referencia.Designacao, atual.Designacao))
                conflitos.Add("Designacao");

            if (!StringEquals(referencia.Referencia, atual.Referencia))
                conflitos.Add("Referencia");

            if (!StringEquals(referencia.MaterialDesignacao, atual.MaterialDesignacao))
                conflitos.Add("Material");

            if (!StringEquals(referencia.TratamentoTermico, atual.TratamentoTermico))
                conflitos.Add("TratamentoTermico");

            if (!StringEquals(referencia.Massa, atual.Massa))
                conflitos.Add("Massa");

            if (!StringEquals(referencia.Observacao, atual.Observacao))
                conflitos.Add("Observacao");

            return conflitos;
        }

        /// <summary>
        /// Compara dois textos normalizados de forma insensivel a maiusculas.
        /// </summary>
        /// <param name="left">Primeiro valor textual.</param>
        /// <param name="right">Segundo valor textual.</param>
        /// <returns>True quando os textos normalizados sao equivalentes.</returns>
        private static bool StringEquals(string? left, string? right)
        {
            return string.Equals(
                MappingProfileExtensions.NormalizeOptionalString(left),
                MappingProfileExtensions.NormalizeOptionalString(right),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Interpreta o ficheiro CSV de pecas e valida a sua estrutura funcional.
        /// </summary>
        /// <param name="csvStream">Stream do ficheiro CSV recebido.</param>
        /// <returns>Modelo interno com metadados do molde e linhas de pecas lidas.</returns>
        private static async Task<ParsedPecaCsvFile> ParseCsvAsync(Stream csvStream)
        {
            using var memoryStream = new MemoryStream();
            await csvStream.CopyToAsync(memoryStream);

            var lines = DecodeCsvLines(memoryStream.ToArray());
            var headerLine = lines.ElementAtOrDefault(0);
            if (string.IsNullOrWhiteSpace(headerLine))
                throw new ArgumentException("O ficheiro CSV nao contem cabecalho.");

            var headerColumns = SplitCsvLine(headerLine);
            ValidateCsvHeader(headerColumns);

            var metadataLine = lines.ElementAtOrDefault(1);
            if (metadataLine == null)
                throw new ArgumentException("O ficheiro CSV nao contem a linha-resumo do molde.");

            var metadataColumns = SplitCsvLine(metadataLine);
            ValidateColumnCount(metadataColumns, 2);
            ValidateMetadataRow(metadataColumns);

            var linhasPeca = new List<PecaCsvLinhaDto>();

            for (var index = 2; index < lines.Length; index++)
            {
                var line = lines[index];
                var lineNumber = index + 1;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var columns = SplitCsvLine(line);
                ValidateColumnCount(columns, lineNumber);
                linhasPeca.Add(ParsePieceRow(columns, lineNumber));
            }

            if (linhasPeca.Count == 0)
                throw new ArgumentException("O ficheiro CSV nao contem linhas de pecas para importar.");

            return new ParsedPecaCsvFile(
                MappingProfileExtensions.NormalizeOptionalString(metadataColumns[3]),
                MappingProfileExtensions.NormalizeOptionalString(metadataColumns[6]),
                linhasPeca);
        }

        /// <summary>
        /// Valida se o cabecalho CSV corresponde ao formato esperado.
        /// </summary>
        /// <param name="headerColumns">Colunas lidas da primeira linha do ficheiro.</param>
        private static void ValidateCsvHeader(List<string> headerColumns)
        {
            ValidateColumnCount(headerColumns, 1);

            for (var i = 0; i < CsvHeader.Length; i++)
            {
                if (string.Equals(CanonicalizeCsvToken(headerColumns[i]), CsvHeader[i], StringComparison.Ordinal))
                    continue;

                throw new ArgumentException(
                    $"Cabecalho CSV invalido. Esperado '{CsvHeader[i]}' na coluna {i + 1}, mas recebido '{headerColumns[i]}'.");
            }
        }

        /// <summary>
        /// Valida se uma linha CSV tem o numero esperado de colunas.
        /// </summary>
        /// <param name="columns">Colunas lidas da linha.</param>
        /// <param name="lineNumber">Numero da linha no ficheiro CSV.</param>
        private static void ValidateColumnCount(List<string> columns, int lineNumber)
        {
            if (columns.Count == CsvHeader.Length)
                return;

            throw new ArgumentException(
                $"A linha {lineNumber} do CSV contem {columns.Count} colunas. Eram esperadas {CsvHeader.Length} colunas.");
        }

        /// <summary>
        /// Valida se a segunda linha do CSV representa o resumo do molde.
        /// </summary>
        /// <param name="metadataColumns">Colunas lidas da linha de metadados.</param>
        private static void ValidateMetadataRow(List<string> metadataColumns)
        {
            var numeroPeca = MappingProfileExtensions.NormalizeOptionalString(metadataColumns[0]);
            var designacao = MappingProfileExtensions.NormalizeOptionalString(metadataColumns[1]);
            var referencia = MappingProfileExtensions.NormalizeOptionalString(metadataColumns[3]);

            if (!string.IsNullOrWhiteSpace(numeroPeca) || !string.IsNullOrWhiteSpace(designacao))
                throw new ArgumentException("A linha 2 do CSV deve representar o resumo do molde e nao uma peca.");

            if (!string.Equals(CanonicalizeCsvToken(referencia), "MOLDE", StringComparison.Ordinal))
                throw new ArgumentException("A linha 2 do CSV deve ter 'Molde' na coluna Ref.");
        }

        /// <summary>
        /// Converte uma linha CSV de peca para DTO interno validado.
        /// </summary>
        /// <param name="columns">Colunas da linha CSV.</param>
        /// <param name="lineNumber">Numero da linha no ficheiro CSV.</param>
        /// <returns>DTO interno com os dados da peca importada.</returns>
        private static PecaCsvLinhaDto ParsePieceRow(List<string> columns, int lineNumber)
        {
            var numeroPeca = MappingProfileExtensions.NormalizeOptionalString(columns[0]);
            if (string.IsNullOrWhiteSpace(numeroPeca))
                throw new ArgumentException($"A linha {lineNumber} nao tem NumeroPeca preenchido.");

            var designacao = MappingProfileExtensions.NormalizeOptionalString(columns[1]);
            if (string.IsNullOrWhiteSpace(designacao))
                throw new ArgumentException($"A linha {lineNumber} nao tem Designacao preenchida.");

            var quantidadeRaw = MappingProfileExtensions.NormalizeOptionalString(columns[2]);
            if (!int.TryParse(quantidadeRaw, out var quantidade) || quantidade < 1)
                throw new ArgumentException($"A linha {lineNumber} tem uma quantidade invalida.");

            return new PecaCsvLinhaDto
            {
                NumeroLinha = lineNumber,
                NumeroPeca = numeroPeca,
                Designacao = designacao,
                Quantidade = quantidade,
                Referencia = MappingProfileExtensions.NormalizeOptionalString(columns[3]),
                MaterialDesignacao = MappingProfileExtensions.NormalizeOptionalString(columns[4]),
                TratamentoTermico = MappingProfileExtensions.NormalizeOptionalString(columns[5]),
                Massa = MappingProfileExtensions.NormalizeOptionalString(columns[6]),
                Observacao = MappingProfileExtensions.NormalizeOptionalString(columns[7])
            };
        }

        /// <summary>
        /// Divide uma linha CSV respeitando separadores dentro de aspas.
        /// </summary>
        /// <param name="line">Linha CSV a dividir.</param>
        /// <returns>Colunas extraidas da linha.</returns>
        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var insideQuotes = false;

            foreach (var character in line)
            {
                if (character == '"')
                {
                    insideQuotes = !insideQuotes;
                    continue;
                }

                if (character == ';' && !insideQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(character);
            }

            result.Add(current.ToString());
            return result;
        }

        /// <summary>
        /// Deteta a codificacao do CSV e devolve as linhas textuais.
        /// </summary>
        /// <param name="csvBytes">Conteudo binario do ficheiro CSV.</param>
        /// <returns>Linhas do ficheiro interpretadas na codificacao adequada.</returns>
        private static string[] DecodeCsvLines(byte[] csvBytes)
        {
            foreach (var encoding in GetCandidateEncodings())
            {
                var text = encoding.GetString(csvBytes);
                var lines = SplitLines(text);
                var headerLine = lines.ElementAtOrDefault(0);

                if (!string.IsNullOrWhiteSpace(headerLine) && IsExpectedCsvHeader(SplitCsvLine(headerLine)))
                    return lines;
            }

            return SplitLines(Encoding.UTF8.GetString(csvBytes));
        }

        /// <summary>
        /// Devolve as codificacoes aceites para ficheiros CSV importados.
        /// </summary>
        /// <returns>Sequencia de codificacoes testadas por ordem de preferencia.</returns>
        private static IEnumerable<Encoding> GetCandidateEncodings()
        {
            yield return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
            yield return Encoding.GetEncoding(1252);
        }

        /// <summary>
        /// Normaliza quebras de linha e divide o conteudo textual.
        /// </summary>
        /// <param name="content">Conteudo textual do CSV.</param>
        /// <returns>Linhas normalizadas do ficheiro.</returns>
        private static string[] SplitLines(string content)
        {
            return content
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n');
        }

        /// <summary>
        /// Verifica se as colunas recebidas correspondem ao cabecalho esperado.
        /// </summary>
        /// <param name="headerColumns">Colunas candidatas a cabecalho.</param>
        /// <returns>True quando o cabecalho corresponde ao formato aceite.</returns>
        private static bool IsExpectedCsvHeader(List<string> headerColumns)
        {
            if (headerColumns.Count != CsvHeader.Length)
                return false;

            for (var i = 0; i < CsvHeader.Length; i++)
            {
                if (!string.Equals(CanonicalizeCsvToken(headerColumns[i]), CsvHeader[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Normaliza texto CSV para comparacoes robustas de cabecalho e metadados.
        /// </summary>
        /// <param name="value">Valor textual lido do CSV.</param>
        /// <returns>Texto canonico sem acentos, pontuacao irrelevante ou espacos repetidos.</returns>
        private static string CanonicalizeCsvToken(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Replace("\uFEFF", string.Empty, StringComparison.Ordinal)
                .Trim()
                .Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder();

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (character <= '\u007F' && char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToUpperInvariant(character));
                    continue;
                }

                if (char.IsWhiteSpace(character) || character is '.' or '_' or '-')
                    builder.Append(' ');
            }

            return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private sealed record ParsedPecaCsvFile(
            string? ReferenciaMolde,
            string? MassaMolde,
            List<PecaCsvLinhaDto> LinhasPeca);

        private sealed record CsvPecaGroup(
            string NumeroPeca,
            List<PecaCsvLinhaDto> Linhas,
            int FirstLineNumber);
    }
}
