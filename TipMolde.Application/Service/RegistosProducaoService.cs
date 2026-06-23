using AutoMapper;
using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.RegistoProducaoDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IFasesProducao;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Producao.IRegistosProducao;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa os casos de uso de registos de producao.
    /// </summary>
    /// <remarks>
    /// Centraliza validacoes de fase, operador, peca, maquina e transicoes de estado
    /// antes de persistir eventos de producao. A criacao do registo e a atualizacao
    /// do estado da maquina pertencem a uma unica fronteira transacional.
    /// </remarks>
    public class RegistosProducaoService : IRegistosProducaoService
    {
        private readonly IRegistosProducaoRepository _rpRepository;
        private readonly IFasesProducaoRepository _fpRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPecaRepository _pecaRepository;
        private readonly IMaquinaRepository _maquinaRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<RegistosProducaoService> _logger;

        /// <summary>
        /// Construtor de RegistosProducaoService.
        /// </summary>
        /// <param name="rpRepository">Repositorio responsavel pelos registos de producao.</param>
        /// <param name="fpRepository">Repositorio usado para validar fases de producao.</param>
        /// <param name="userRepository">Repositorio usado para validar operadores.</param>
        /// <param name="maquinaRepository">Repositorio usado para validar maquinas.</param>
        /// <param name="pecaRepository">Repositorio usado para validar pecas em producao.</param>
        /// <param name="mapper">Mapper para conversao entre DTOs e entidade.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes criticas.</param>
        public RegistosProducaoService(
            IRegistosProducaoRepository rpRepository,
            IFasesProducaoRepository fpRepository,
            IUserRepository userRepository,
            IMaquinaRepository maquinaRepository,
            IPecaRepository pecaRepository,
            IMapper mapper,
            ILogger<RegistosProducaoService> logger)
        {
            _rpRepository = rpRepository;
            _fpRepository = fpRepository;
            _userRepository = userRepository;
            _maquinaRepository = maquinaRepository;
            _pecaRepository = pecaRepository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Lista registos de producao com paginacao.
        /// </summary>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com DTOs de registos de producao.</returns>
        public async Task<PagedResult<ResponseRegistosProducaoDto>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _rpRepository.GetAllAsync(normalizedPage, normalizedPageSize);
            var items = _mapper.Map<IEnumerable<ResponseRegistosProducaoDto>>(result.Items);

            return new PagedResult<ResponseRegistosProducaoDto>(
                items,
                result.TotalCount,
                result.CurrentPage,
                result.PageSize);
        }

        /// <summary>
        /// Obtem um registo de producao pelo identificador.
        /// </summary>
        /// <param name="id">Identificador unico do registo.</param>
        /// <returns>DTO do registo encontrado ou nulo quando nao existe.</returns>
        public async Task<ResponseRegistosProducaoDto?> GetByIdAsync(int id)
        {
            var registo = await _rpRepository.GetByIdAsync(id);
            return registo == null ? null : _mapper.Map<ResponseRegistosProducaoDto>(registo);
        }

        /// <summary>
        /// Obtem o historico de producao de uma peca numa fase.
        /// </summary>
        /// <param name="faseId">Identificador da fase de producao.</param>
        /// <param name="pecaId">Identificador da peca.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado de registos historicos em ordem cronologica.</returns>
        public async Task<PagedResult<ResponseRegistosProducaoDto>> GetHistoricoAsync(int faseId, int pecaId, int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var historico = await _rpRepository.GetHistoricoAsync(faseId, pecaId, normalizedPage, normalizedPageSize);
            var items = _mapper.Map<IEnumerable<ResponseRegistosProducaoDto>>(historico.Items);

            return new PagedResult<ResponseRegistosProducaoDto>(
                items,
                historico.TotalCount,
                historico.CurrentPage,
                historico.PageSize);
        }
        /// <summary>
        /// Obtem o ultimo registo de producao de uma peca numa fase.
        /// </summary>
        /// <param name="faseId">Identificador da fase de producao.</param>
        /// <param name="pecaId">Identificador da peca.</param>
        /// <returns>Ultimo registo encontrado ou nulo quando ainda nao existe historico.</returns>
        public async Task<ResponseRegistosProducaoDto?> GetUltimoRegistoAsync(int faseId, int pecaId)
        {
            var registo = await _rpRepository.GetUltimoRegistoAsync(faseId, pecaId);
            return registo == null ? null : _mapper.Map<ResponseRegistosProducaoDto>(registo);
        }

        /// <summary>
        /// Cria um novo registo de producao e sincroniza o estado da maquina associada.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida fase, operador e peca.
        /// 2. Garante que a peca ja tem material recebido.
        /// 3. Valida a transicao de estado face ao ultimo registo.
        /// 4. Determina a alteracao de maquina necessaria.
        /// 5. Persiste registo e maquina na mesma fronteira transacional.
        /// </remarks>
        /// <param name="dto">Dados de entrada do registo de producao.</param>
        /// <returns>DTO do registo persistido.</returns>
        public async Task<ResponseRegistosProducaoDto> CreateAsync(CreateRegistosProducaoDto dto)
        {
            if (!dto.Estado_producao.HasValue)
                throw new ArgumentException("Estado de producao e obrigatorio.");

            var fase = await _fpRepository.GetByIdAsync(dto.Fase_id)
                ?? throw new KeyNotFoundException($"Fase com ID {dto.Fase_id} nao encontrada.");

            if (await _userRepository.GetByIdAsync(dto.Operador_id) == null)
                throw new KeyNotFoundException($"Operador com ID {dto.Operador_id} nao encontrado.");

            var peca = await _pecaRepository.GetByIdAsync(dto.Peca_id)
                ?? throw new KeyNotFoundException($"Peca com ID {dto.Peca_id} nao encontrada.");

            if (!peca.MaterialRecebido)
                throw new ArgumentException("Nao e possivel iniciar producao sem material recebido.");

            var ultimo = await _rpRepository.GetUltimoRegistoAsync(dto.Fase_id, dto.Peca_id);
            ValidarTransicaoEstado(ultimo?.Estado_producao, dto.Estado_producao.Value, fase.Nome);
            await ValidarRegrasDeMontagemAsync(dto, fase.Nome, peca);

            var registo = _mapper.Map<RegistosProducao>(dto);
            registo.Data_hora = DateTime.UtcNow;

            var maquinaToUpdate = await ResolveMaquinaToUpdateAsync(dto, ultimo, registo);
            await ValidarAlteracaoProximaFaseAsync(peca, dto.ProximaFase_id);
            var pecaToUpdate = await ResolvePecaToUpdateAsync(dto, peca);
            var created = await _rpRepository.AddWithMachineStateAsync(registo, maquinaToUpdate, pecaToUpdate);

            _logger.LogInformation(
                "Registo de producao {RegistoId} criado para peca {PecaId}, fase {FaseId}, estado {Estado}.",
                created.Registo_Producao_id,
                created.Peca_id,
                created.Fase_id,
                created.Estado_producao);

            return _mapper.Map<ResponseRegistosProducaoDto>(created);
        }

        /// <summary>
        /// Aplica validacoes transversais especificas da fase de montagem.
        /// </summary>
        /// <remarks>
        /// Regra de negocio: uma peca pode entrar em montagem com estado PENDENTE para sinalizar
        /// que esta pronta. O arranque efetivo da montagem (EM_CURSO) so e permitido quando todas
        /// as pecas do mesmo molde ja se encontram atualmente na fase de montagem.
        /// </remarks>
        /// <param name="dto">Dados do registo pedido.</param>
        /// <param name="nomeFase">Nome funcional da fase alvo.</param>
        /// <param name="peca">Peca associada ao registo.</param>
        private async Task ValidarRegrasDeMontagemAsync(
            CreateRegistosProducaoDto dto,
            NomeFases nomeFase,
            Peca peca)
        {
            if (nomeFase != NomeFases.MONTAGEM || dto.Estado_producao != EstadoProducao.EM_CURSO)
                return;

            var pecasDoMolde = await _pecaRepository.GetAllByMoldeIdAsync(peca.Molde_id);
            var ultimosRegistos = await _rpRepository.GetUltimosRegistosGlobaisAsync(pecasDoMolde.Select(p => p.Peca_id));
            var pecasEmMontagem = ultimosRegistos
                .Where(r => r.Fase_id == dto.Fase_id)
                .Select(r => r.Peca_id)
                .Distinct()
                .ToHashSet();

            if (pecasDoMolde.Count == 0 || pecasEmMontagem.Count != pecasDoMolde.Count)
                throw new ArgumentException(
                    "Nao e possivel iniciar a montagem: todas as pecas do molde devem estar primeiro na fase MONTAGEM.");
        }

        /// <summary>
        /// Determina a alteracao de maquina exigida pela nova transicao de producao.
        /// </summary>
        /// <remarks>
        /// Regra de negocio: PREPARACAO e EM_CURSO ocupam uma maquina valida; PAUSADO
        /// e CONCLUIDO libertam a maquina informada ou a maquina do ultimo registo.
        /// </remarks>
        /// <param name="dto">Dados de entrada do registo de producao.</param>
        /// <param name="ultimo">Ultimo registo conhecido para a peca/fase.</param>
        /// <param name="registo">Entidade que sera persistida.</param>
        /// <returns>Maquina com estado alterado ou nulo quando nao ha alteracao.</returns>
        private async Task<Maquina?> ResolveMaquinaToUpdateAsync(
            CreateRegistosProducaoDto dto,
            RegistosProducao? ultimo,
            RegistosProducao registo)
        {
            if (dto.Estado_producao is EstadoProducao.PREPARACAO or EstadoProducao.EM_CURSO)
                return await ResolveMaquinaEntradaAsync(dto, ultimo);

            if (dto.Estado_producao is EstadoProducao.PAUSADO or EstadoProducao.CONCLUIDO)
                return await ResolveMaquinaSaidaAsync(dto, ultimo, registo);

            return null;
        }

        /// <summary>
        /// Valida e ocupa a maquina usada para iniciar ou continuar producao.
        /// </summary>
        /// <param name="dto">Dados de entrada do registo de producao.</param>
        /// <param name="ultimo">Ultimo registo conhecido para a peca/fase.</param>
        /// <returns>Maquina marcada como em uso.</returns>
        private async Task<Maquina?> ResolveMaquinaEntradaAsync(CreateRegistosProducaoDto dto, RegistosProducao? ultimo)
        {
            if (!dto.Maquina_id.HasValue)
                return null;

            var maquina = await _maquinaRepository.GetByIdUnicoAsync(dto.Maquina_id.Value)
                ?? throw new KeyNotFoundException($"Maquina com ID {dto.Maquina_id.Value} nao encontrada.");

            if (maquina.FaseDedicada_id != dto.Fase_id)
                throw new ArgumentException("Maquina nao esta apta para esta fase.");

            if (maquina.Estado == EstadoMaquina.DISPONIVEL)
            {
                maquina.Estado = EstadoMaquina.EM_USO;
                return maquina;
            }

            if (maquina.Estado == EstadoMaquina.EM_USO && ultimo?.Maquina_id == maquina.Maquina_id)
                return maquina;

            throw new ArgumentException("Maquina indisponivel.");
        }

        private async Task<Peca?> ResolvePecaToUpdateAsync(CreateRegistosProducaoDto dto, Peca peca)
        {
            if (!dto.ProximaFase_id.HasValue)
                return null;

            var proximaFase = await _fpRepository.GetByIdAsync(dto.ProximaFase_id.Value)
                ?? throw new KeyNotFoundException($"Fase com ID {dto.ProximaFase_id.Value} nao encontrada.");

            peca.ProximaFase_id = proximaFase.Fases_producao_id;
            peca.ProximaFase = proximaFase;
            return peca;
        }

        /// <summary>
        /// Bloqueia alteracoes ao planeamento da peca enquanto existe producao ativa.
        /// </summary>
        /// <remarks>
        /// A alteracao so e permitida quando nao existe um registo global ativo para a peca,
        /// ou quando o valor pedido e igual ao planeamento ja persistido.
        /// </remarks>
        /// <param name="peca">Peca a atualizar.</param>
        /// <param name="novaProximaFaseId">Nova fase planeada pedida pelo cliente.</param>
        private async Task ValidarAlteracaoProximaFaseAsync(Peca peca, int? novaProximaFaseId)
        {
            if (!novaProximaFaseId.HasValue)
                return;

            if (peca.ProximaFase_id.HasValue && peca.ProximaFase_id.Value == novaProximaFaseId.Value)
                return;

            var ultimoRegistoGlobal = await _rpRepository.GetUltimosRegistosGlobaisAsync(new[] { peca.Peca_id });
            var registoAtual = ultimoRegistoGlobal.FirstOrDefault();

            if (registoAtual is null)
                return;

            if (registoAtual.Estado_producao is EstadoProducao.PREPARACAO or EstadoProducao.EM_CURSO)
                throw new ArgumentException(
                    "Nao e possivel alterar a proxima fase enquanto a peca tem producao ativa.");
        }

        /// <summary>
        /// Liberta a maquina associada a uma pausa ou conclusao de producao.
        /// </summary>
        /// <param name="dto">Dados de entrada do registo de producao.</param>
        /// <param name="ultimo">Ultimo registo conhecido para a peca/fase.</param>
        /// <param name="registo">Entidade que sera persistida.</param>
        /// <returns>Maquina marcada como disponivel ou nulo quando nao ha maquina associada.</returns>
        private async Task<Maquina?> ResolveMaquinaSaidaAsync(
            CreateRegistosProducaoDto dto,
            RegistosProducao? ultimo,
            RegistosProducao registo)
        {
            var maquinaId = dto.Maquina_id ?? ultimo?.Maquina_id;
            if (!maquinaId.HasValue)
                return null;

            var maquina = await _maquinaRepository.GetByIdUnicoAsync(maquinaId.Value);
            if (maquina == null)
                return null;

            registo.Maquina_id = maquinaId.Value;

            if (maquina.Estado == EstadoMaquina.EM_USO)
                maquina.Estado = EstadoMaquina.DISPONIVEL;

            return maquina;
        }

        /// <summary>
        /// Valida se a transicao de estado de producao e permitida.
        /// </summary>
        /// <remarks>
        /// Esta maquina de estados protege a sequencia operacional da fase e deve
        /// manter compatibilidade com o fluxo real de producao.
        /// </remarks>
        /// <param name="estadoActual">Estado persistido no ultimo registo, quando existe.</param>
        /// <param name="novoEstado">Novo estado solicitado.</param>
        private static void ValidarTransicaoEstado(
            EstadoProducao? estadoActual,
            EstadoProducao novoEstado,
            NomeFases nomeFase)
        {
            if (estadoActual is null)
            {
                var primeiroEstadoValido = nomeFase == NomeFases.MONTAGEM
                    ? EstadoProducao.PENDENTE
                    : EstadoProducao.PREPARACAO;

                if (novoEstado != primeiroEstadoValido)
                    throw new ArgumentException($"Primeiro estado deve ser {primeiroEstadoValido}.");
                return;
            }

            var transicoesValidas = nomeFase == NomeFases.MONTAGEM
                ? new Dictionary<EstadoProducao, List<EstadoProducao>>
                {
                    { EstadoProducao.PENDENTE, new() { EstadoProducao.EM_CURSO } },
                    { EstadoProducao.PREPARACAO, new() },
                    { EstadoProducao.EM_CURSO, new() { EstadoProducao.PAUSADO, EstadoProducao.CONCLUIDO } },
                    { EstadoProducao.PAUSADO, new() { EstadoProducao.EM_CURSO } },
                    { EstadoProducao.CONCLUIDO, new() }
                }
                : new Dictionary<EstadoProducao, List<EstadoProducao>>
            {
                { EstadoProducao.PENDENTE, new() { EstadoProducao.PREPARACAO } },
                { EstadoProducao.PREPARACAO, new() { EstadoProducao.EM_CURSO } },
                { EstadoProducao.EM_CURSO, new() { EstadoProducao.PAUSADO, EstadoProducao.CONCLUIDO } },
                { EstadoProducao.PAUSADO, new() { EstadoProducao.EM_CURSO, EstadoProducao.PREPARACAO } },
                { EstadoProducao.CONCLUIDO, new() { EstadoProducao.PREPARACAO } }
            };

            if (!transicoesValidas[estadoActual.Value].Contains(novoEstado))
                throw new ArgumentException(
                    $"Transicao de estado invalida: nao e possivel passar de {estadoActual} para {novoEstado}.");
        }
    }
}
