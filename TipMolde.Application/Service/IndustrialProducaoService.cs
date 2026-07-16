using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.IndustrialProducaoDto;
using TipMolde.Application.Dtos.RegistoProducaoDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IIndustrial;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Producao.IRegistosProducao;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Transforma telemetria tecnica de maquinas em eventos auditaveis e registos de producao.
    /// </summary>
    public class IndustrialProducaoService : IIndustrialProducaoService
    {
        private const string EstadoRunning = "RUNNING";
        private const string EstadoStopped = "STOPPED";
        private const string EstadoIdle = "IDLE";
        private const string EstadoAlarm = "ALARM";

        private readonly IEventoMaquinaIndustrialRepository _eventoRepository;
        private readonly ISessaoMaquinaIndustrialRepository _sessaoRepository;
        private readonly IMaquinaRepository _maquinaRepository;
        private readonly IPecaRepository _pecaRepository;
        private readonly IRegistosProducaoService _registosProducaoService;
        private readonly ILogger<IndustrialProducaoService> _logger;

        public IndustrialProducaoService(
            IEventoMaquinaIndustrialRepository eventoRepository,
            ISessaoMaquinaIndustrialRepository sessaoRepository,
            IMaquinaRepository maquinaRepository,
            IPecaRepository pecaRepository,
            IRegistosProducaoService registosProducaoService,
            ILogger<IndustrialProducaoService> logger)
        {
            _eventoRepository = eventoRepository;
            _sessaoRepository = sessaoRepository;
            _maquinaRepository = maquinaRepository;
            _pecaRepository = pecaRepository;
            _registosProducaoService = registosProducaoService;
            _logger = logger;
        }

        /// <summary>
        /// Recebe telemetria industrial, valida apenas o necessario para identificar a maquina
        /// e persiste o evento bruto para processamento posterior.
        /// </summary>
        public async Task<IndustrialTelemetryIngestResultDto> ReceberTelemetriaAsync(IEnumerable<IndustrialTelemetryDto> eventos)
        {
            if (eventos == null)
                throw new ArgumentException("Eventos industriais sao obrigatorios.");

            var result = new IndustrialTelemetryIngestResultDto();

            foreach (var dto in eventos)
            {
                result.Recebidos++;

                var ip = dto.MachineIp?.Trim();
                if (string.IsNullOrWhiteSpace(ip))
                    throw new ArgumentException("MachineIp e obrigatorio.");

                var maquina = await _maquinaRepository.GetByIpAddressAsync(ip);
                if (maquina == null)
                {
                    _logger.LogWarning("Telemetria industrial ignorada na ingestao: maquina com IP {MachineIp} nao existe no backend.", ip);
                    result.Ignorados++;
                    continue;
                }

                var receivedAt = DateTime.UtcNow;
                var sessaoAberta = await _sessaoRepository.GetAbertaPorMaquinaAsync(maquina.Maquina_id);
                var evento = BuildEvento(dto, maquina.Maquina_id, NormalizeState(dto.State));
                evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.RECEBIDO;
                evento.FonteResolucao = "RECEBIDO";
                evento.CreatedAt = receivedAt;
                evento.UpdatedAt = receivedAt;

                if (sessaoAberta is not null)
                {
                    evento.SessaoMaquinaIndustrial_id = sessaoAberta.SessaoMaquinaIndustrial_id;

                    sessaoAberta.LastSeenAt = receivedAt;
                    sessaoAberta.UltimoEstadoMaquina = evento.EstadoMaquina;
                    sessaoAberta.UpdatedAt = receivedAt;

                    if (IsState(evento.EstadoMaquina, EstadoStopped))
                    {
                        evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE;
                        evento.FonteResolucao = "STOPPED_RECEBIDO_COM_SESSAO";
                        sessaoAberta.EstadoSessao = EstadoSessaoMaquinaIndustrial.AGUARDAR_CONFIRMACAO_PARAGEM;
                    }

                    await _sessaoRepository.UpdateAsync(sessaoAberta);
                }

                await _eventoRepository.AddAsync(evento);
                result.Guardados++;
            }

            return result;
        }

        /// <summary>
        /// Processa uma lista de eventos recebidos do middleware industrial.
        /// </summary>
        /// <param name="eventos">Eventos tecnicos normalizados.</param>
        /// <returns>Resumo do processamento.</returns>
        public async Task<IndustrialTelemetryProcessResultDto> ProcessarTelemetriaAsync(IEnumerable<IndustrialTelemetryDto> eventos)
        {
            if (eventos == null)
                throw new ArgumentException("Eventos industriais sao obrigatorios.");

            var result = new IndustrialTelemetryProcessResultDto();

            foreach (var evento in eventos)
            {
                result.Recebidos++;

                var status = await ProcessarEventoAsync(evento);
                if (status == ProcessStatus.Ignorado)
                {
                    result.Ignorados++;
                    continue;
                }

                result.Processados++;
                if (status == ProcessStatus.Pendente)
                    result.Pendentes++;
                else
                    result.Resolvidos++;
            }

            return result;
        }

        /// <summary>
        /// Processa todos os eventos industriais que ainda estao apenas recebidos e nao foram resolvidos.
        /// </summary>
        public async Task ProcessarEventosRecebidosAsync(CancellationToken cancellationToken = default)
        {
            const int batchSize = 50;

            while (!cancellationToken.IsCancellationRequested)
            {
                var pagina = await _eventoRepository.GetRecebidosAsync(1, batchSize);
                var eventos = pagina.Items.ToList();
                if (eventos.Count == 0)
                    return;

                foreach (var evento in eventos)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await ProcessarEventoRecebidoAsync(evento);
                }

                if (eventos.Count < batchSize)
                    return;
            }
        }

        private async Task<ProcessStatus> ProcessarEventoRecebidoAsync(EventoMaquinaIndustrial evento)
        {
            if (evento.EstadoResolucao != EstadoResolucaoEventoMaquinaIndustrial.RECEBIDO)
                return ProcessStatus.Resolvido;

            var maquina = await _maquinaRepository.GetByIdAsync(evento.Maquina_id);
            if (maquina == null)
            {
                IgnorarEvento(evento, "MAQUINA_NAO_ENCONTRADA");
                await _eventoRepository.UpdateAsync(evento);
                return ProcessStatus.Ignorado;
            }

            var sessao = await _sessaoRepository.GetAbertaPorMaquinaAsync(maquina.Maquina_id);
            var bloqueioPendente = await _eventoRepository.GetMaisRecentePendentePorMaquinaAsync(maquina.Maquina_id);
            if (bloqueioPendente != null)
            {
                if (PodeRetomarAutomaticamenteComRunning(evento, sessao, bloqueioPendente))
                {
                    evento.SessaoMaquinaIndustrial_id = sessao!.SessaoMaquinaIndustrial_id;
                    return await ProcessarEventoRecebidoComSessaoAsync(evento, sessao);
                }

                _logger.LogDebug(
                    "Processamento adiado para maquina {MaquinaId}: existe evento pendente {EventoId} a aguardar resolucao.",
                    maquina.Maquina_id,
                    bloqueioPendente.EventoMaquinaIndustrial_id);
                return ProcessStatus.Pendente;
            }

            if (sessao != null && sessao.EstadoSessao == EstadoSessaoMaquinaIndustrial.AGUARDAR_CONFIRMACAO_PARAGEM)
            {
                _logger.LogDebug(
                    "Processamento adiado para maquina {MaquinaId}: existe sessao {SessaoId} a aguardar confirmacao de paragem.",
                    maquina.Maquina_id,
                    sessao.SessaoMaquinaIndustrial_id);
                return ProcessStatus.Pendente;
            }

            if (sessao == null)
                return await ProcessarEventoRecebidoSemSessaoAsync(evento);

            evento.SessaoMaquinaIndustrial_id = sessao.SessaoMaquinaIndustrial_id;
            return await ProcessarEventoRecebidoComSessaoAsync(evento, sessao);
        }

        private static bool PodeRetomarAutomaticamenteComRunning(
            EventoMaquinaIndustrial evento,
            SessaoMaquinaIndustrial? sessao,
            EventoMaquinaIndustrial bloqueioPendente)
        {
            return sessao is not null
                && sessao.EstadoSessao == EstadoSessaoMaquinaIndustrial.AGUARDAR_CONFIRMACAO_PARAGEM
                && IsState(evento.EstadoMaquina, EstadoRunning)
                && IsState(bloqueioPendente.EstadoMaquina, EstadoStopped);
        }

        private async Task<ProcessStatus> ProcessarEventoRecebidoSemSessaoAsync(EventoMaquinaIndustrial evento)
        {
            if (IsState(evento.EstadoMaquina, EstadoRunning))
            {
                evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE;
                evento.CamposEmFalta = "Operador_id,Peca_id,Fase_id";
                evento.FonteResolucao = "RUNNING_PENDENTE_CONTEXTO";
                evento.ResolvedAt = null;
                evento.UpdatedAt = DateTime.UtcNow;
                await _eventoRepository.UpdateAsync(evento);
                return ProcessStatus.Pendente;
            }

            if (IsState(evento.EstadoMaquina, EstadoAlarm))
            {
                evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE;
                evento.CamposEmFalta = "Ocorrencia";
                evento.FonteResolucao = "ALARM_PENDENTE_CONTEXTO";
                evento.ResolvedAt = null;
                evento.UpdatedAt = DateTime.UtcNow;
                await _eventoRepository.UpdateAsync(evento);
                return ProcessStatus.Pendente;
            }

            IgnorarEvento(evento, IsState(evento.EstadoMaquina, EstadoStopped)
                ? "STOPPED_SEM_SESSAO_ATIVA"
                : "ESTADO_SEM_SESSAO_ATIVA");

            await _eventoRepository.UpdateAsync(evento);
            return ProcessStatus.Ignorado;
        }

        private async Task<ProcessStatus> ProcessarEventoRecebidoComSessaoAsync(EventoMaquinaIndustrial evento, SessaoMaquinaIndustrial sessao)
        {
            if (IsState(evento.EstadoMaquina, EstadoStopped))
            {
                evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE;
                evento.FonteResolucao = "STOPPED_AGUARDA_CONFIRMACAO";
                evento.ResolvedAt = null;
                evento.UpdatedAt = DateTime.UtcNow;
                await _eventoRepository.UpdateAsync(evento);

                sessao.EstadoSessao = EstadoSessaoMaquinaIndustrial.AGUARDAR_CONFIRMACAO_PARAGEM;
                sessao.UltimoEstadoMaquina = EstadoStopped;
                sessao.LastSeenAt = GetRececaoTimestampUtc(evento);
                sessao.UpdatedAt = DateTime.UtcNow;
                await _sessaoRepository.UpdateAsync(sessao);

                return ProcessStatus.Pendente;
            }

            if (IsState(evento.EstadoMaquina, EstadoRunning))
            {
                var stoppedPendente = await _eventoRepository.GetUltimoStoppedPendenteAsync(sessao.SessaoMaquinaIndustrial_id);
                if (stoppedPendente != null)
                {
                    var registoPausa = await CriarRegistoProducaoAsync(sessao, EstadoProducao.PAUSADO, NormalizeTimestamp(stoppedPendente.OccurredAt));
                    ResolverEvento(stoppedPendente, EstadoProducao.PAUSADO, "AUTO_RUNNING_APOS_STOPPED", registoPausa.Registo_Producao_id);
                    await _eventoRepository.UpdateAsync(stoppedPendente);

                    var registoRetoma = await CriarRegistoProducaoAsync(sessao, EstadoProducao.EM_CURSO, NormalizeTimestamp(evento.OccurredAt));
                    ResolverEvento(evento, EstadoProducao.EM_CURSO, "AUTO_RUNNING_RETOMOU_PRODUCAO", registoRetoma.Registo_Producao_id);
                }
                else if (IsState(sessao.UltimoEstadoMaquina, EstadoStopped))
                {
                    var registoRetoma = await CriarRegistoProducaoAsync(sessao, EstadoProducao.EM_CURSO, NormalizeTimestamp(evento.OccurredAt));
                    ResolverEvento(evento, EstadoProducao.EM_CURSO, "AUTO_RUNNING_APOS_PARAGEM_RESOLVIDA", registoRetoma.Registo_Producao_id);
                }
                else if (!sessao.RegistoProducaoInicio_id.HasValue)
                {
                    _logger.LogWarning(
                        "Evento RUNNING recebido para a sessao industrial {SessaoId} sem registo de inicio associado. O contexto deve ser completado pelo utilizador antes de criar novos registos de producao.",
                        sessao.SessaoMaquinaIndustrial_id);

                    ResolverEvento(evento, null, "RUNNING_SEM_REGISTO_INICIAL", null);
                }
                else
                {
                    ResolverEvento(evento, null, "RUNNING_CONTEXTO_JA_ATIVO", null);
                }

                sessao.EstadoSessao = EstadoSessaoMaquinaIndustrial.ATIVA;
                sessao.UltimoEstadoMaquina = EstadoRunning;
                sessao.LastSeenAt = GetRececaoTimestampUtc(evento);
                sessao.UpdatedAt = DateTime.UtcNow;
                await _sessaoRepository.UpdateAsync(sessao);
                await _eventoRepository.UpdateAsync(evento);

                return ProcessStatus.Resolvido;
            }

            if (IsState(evento.EstadoMaquina, EstadoAlarm))
            {
                evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE;
                evento.CamposEmFalta = "Ocorrencia";
                evento.FonteResolucao = "ALARM_PENDENTE_CONTEXTO";
                evento.ResolvedAt = null;
                evento.UpdatedAt = DateTime.UtcNow;
                await _eventoRepository.UpdateAsync(evento);
                return ProcessStatus.Pendente;
            }

            ResolverEvento(evento, null, IsState(evento.EstadoMaquina, EstadoIdle) ? "IDLE_RECEBIDO" : "ESTADO_TECNICO_RECEBIDO", null);
            sessao.UltimoEstadoMaquina = evento.EstadoMaquina;
            sessao.LastSeenAt = GetRececaoTimestampUtc(evento);
            sessao.UpdatedAt = DateTime.UtcNow;
            await _sessaoRepository.UpdateAsync(sessao);
            await _eventoRepository.UpdateAsync(evento);

            return ProcessStatus.Resolvido;
        }

        /// <summary>
        /// Lista eventos pendentes de decisao humana.
        /// </summary>
        public async Task<PagedResult<ResponseEventoMaquinaIndustrialDto>> GetEventosPendentesAsync(int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _eventoRepository.GetPendentesAsync(normalizedPage, normalizedPageSize);

            return new PagedResult<ResponseEventoMaquinaIndustrialDto>(
                result.Items.Select(MapEvento),
                result.TotalCount,
                result.CurrentPage,
                result.PageSize);
        }

        /// <summary>
        /// Obtem a acao manual pendente relevante para o detalhe de uma maquina.
        /// </summary>
        public async Task<ResponseEventoMaquinaIndustrialDto?> GetEventoPendenteMaquinaAsync(int maquinaId)
        {
            var sessao = await _sessaoRepository.GetAbertaPorMaquinaAsync(maquinaId);

            if (sessao is not null)
            {
                if (sessao.EstadoSessao == EstadoSessaoMaquinaIndustrial.AGUARDAR_CONFIRMACAO_PARAGEM)
                {
                    var stoppedPendente = await _eventoRepository.GetUltimoStoppedPendenteAsync(sessao.SessaoMaquinaIndustrial_id);
                    return stoppedPendente is null ? null : MapEvento(stoppedPendente);
                }

                return null;
            }

            var runningPendente = await _eventoRepository.GetMaisRecentePendentePorMaquinaAsync(maquinaId, EstadoRunning);
            return runningPendente is null ? null : MapEvento(runningPendente);
        }

        /// <summary>
        /// Obtem a sessao industrial ainda ativa de uma maquina para apresentacao no frontend.
        /// </summary>
        public async Task<ResponseContextoAtivoMaquinaIndustrialDto?> GetSessaoAtivaAsync(int maquinaId)
        {
            var sessao = await _sessaoRepository.GetAbertaComDetalhePorMaquinaAsync(maquinaId);
            return sessao is null ? null : MapContextoAtivo(sessao);
        }

        /// <summary>
        /// Completa o contexto de um evento RUNNING pendente e inicia a sessao industrial.
        /// </summary>
        public async Task<ResponseSessaoMaquinaIndustrialDto> CompletarContextoAsync(int eventoId, CompletarContextoEventoIndustrialDto dto)
        {
            var evento = await _eventoRepository.GetByIdAsync(eventoId)
                ?? throw new KeyNotFoundException($"Evento industrial com ID {eventoId} nao encontrado.");

            if (evento.EstadoResolucao != EstadoResolucaoEventoMaquinaIndustrial.PENDENTE)
                throw new ArgumentException("Evento industrial ja foi resolvido.");

            if (!IsState(evento.EstadoMaquina, EstadoRunning))
                throw new ArgumentException("Apenas eventos RUNNING pendentes podem iniciar contexto de producao.");

            var sessaoAberta = await _sessaoRepository.GetAbertaPorMaquinaAsync(evento.Maquina_id);
            if (sessaoAberta != null)
                throw new ArgumentException("A maquina ja tem uma sessao industrial aberta.");

            var maquina = await _maquinaRepository.GetByIdAsync(evento.Maquina_id)
                ?? throw new KeyNotFoundException($"Maquina com ID {evento.Maquina_id} nao encontrada.");

            if (maquina.FaseDedicada_id != dto.Fase_id)
                throw new ArgumentException("A fase informada nao corresponde a fase dedicada da maquina.");

            var peca = await _pecaRepository.GetByIdAsync(dto.Peca_id)
                ?? throw new KeyNotFoundException($"Peca com ID {dto.Peca_id} nao encontrada.");

            if (!peca.ProximaFase_id.HasValue)
                throw new ArgumentException("A peca selecionada nao tem proxima fase planeada.");

            if (peca.ProximaFase_id.Value != maquina.FaseDedicada_id)
                throw new ArgumentException("A peca selecionada nao pertence a fase dedicada desta maquina.");

            var dataEvento = NormalizeTimestamp(evento.OccurredAt);
            var now = DateTime.UtcNow;
            var sessao = new SessaoMaquinaIndustrial
            {
                Maquina_id = evento.Maquina_id,
                Operador_id = dto.Operador_id,
                Peca_id = dto.Peca_id,
                Fase_id = dto.Fase_id,
                EstadoSessao = EstadoSessaoMaquinaIndustrial.ATIVA,
                UltimoEstadoMaquina = EstadoRunning,
                StartedAt = dataEvento,
                LastSeenAt = dataEvento,
                CreatedAt = now,
                UpdatedAt = now
            };

            sessao = await _sessaoRepository.AddAsync(sessao);

            var registoInicio = await CriarRegistoEmCursoAsync(sessao, dataEvento);
            sessao.RegistoProducaoInicio_id = registoInicio.Registo_Producao_id;
            sessao.UpdatedAt = DateTime.UtcNow;
            await _sessaoRepository.UpdateAsync(sessao);

            evento.SessaoMaquinaIndustrial_id = sessao.SessaoMaquinaIndustrial_id;
            ResolverEvento(evento, EstadoProducao.EM_CURSO, "UTILIZADOR_COMPLETOU_CONTEXTO", registoInicio.Registo_Producao_id);
            await _eventoRepository.UpdateAsync(evento);

            return MapSessao(sessao);
        }

        /// <summary>
        /// Resolve uma paragem pendente como pausa ou conclusao do trabalho.
        /// </summary>
        public async Task<ResponseEventoMaquinaIndustrialDto> ConfirmarParagemAsync(int eventoId, ConfirmarParagemIndustrialDto dto)
        {
            var evento = await _eventoRepository.GetByIdAsync(eventoId)
                ?? throw new KeyNotFoundException($"Evento industrial com ID {eventoId} nao encontrado.");

            if (evento.EstadoResolucao != EstadoResolucaoEventoMaquinaIndustrial.PENDENTE)
                throw new ArgumentException("Evento industrial ja foi resolvido.");

            if (!IsState(evento.EstadoMaquina, EstadoStopped))
                throw new ArgumentException("Apenas eventos STOPPED pendentes podem ser confirmados como paragem.");

            if (!evento.SessaoMaquinaIndustrial_id.HasValue)
                throw new ArgumentException("Evento STOPPED nao tem sessao industrial associada.");

            var sessao = await _sessaoRepository.GetByIdAsync(evento.SessaoMaquinaIndustrial_id.Value)
                ?? throw new KeyNotFoundException($"Sessao industrial com ID {evento.SessaoMaquinaIndustrial_id.Value} nao encontrada.");

            if (dto.TrabalhoConcluido && !dto.ProximaFase_id.HasValue)
                throw new ArgumentException("Na conclusao do trabalho e obrigatorio indicar a proxima fase da peca.");

            var estadoProducao = dto.TrabalhoConcluido ? EstadoProducao.CONCLUIDO : EstadoProducao.PAUSADO;
            var ultimoRegisto = await _registosProducaoService.GetUltimoRegistoAsync(sessao.Fase_id, sessao.Peca_id);
            ultimoRegisto = await EnsureSessaoInicioSincronizadoAsync(sessao, ultimoRegisto);
            if (ultimoRegisto?.Estado_producao == estadoProducao)
            {
                _logger.LogWarning(
                    "Evento STOPPED {EventoId} recebido para uma sessao ja sincronizada no estado {Estado}. A resolver sem criar novo registo.",
                    eventoId,
                    estadoProducao);

                AtualizarSessaoAposConfirmacao(sessao, evento, dto.TrabalhoConcluido, ultimoRegisto.Registo_Producao_id);
                await _sessaoRepository.UpdateAsync(sessao);

                ResolverEvento(
                    evento,
                    estadoProducao,
                    dto.TrabalhoConcluido ? "UTILIZADOR_CONFIRMOU_CONCLUIDO_IDEMPOTENTE" : "UTILIZADOR_CONFIRMOU_PAUSADO_IDEMPOTENTE",
                    ultimoRegisto.Registo_Producao_id);
                await _eventoRepository.UpdateAsync(evento);

                return MapEvento(evento);
            }

            var registo = await CriarRegistoProducaoAsync(
                sessao,
                estadoProducao,
                NormalizeTimestamp(evento.OccurredAt),
                dto.TrabalhoConcluido ? dto.ProximaFase_id : null);

            AtualizarSessaoAposConfirmacao(sessao, evento, dto.TrabalhoConcluido, registo.Registo_Producao_id);
            await _sessaoRepository.UpdateAsync(sessao);

            ResolverEvento(
                evento,
                estadoProducao,
                dto.TrabalhoConcluido ? "UTILIZADOR_CONFIRMOU_CONCLUIDO" : "UTILIZADOR_CONFIRMOU_PAUSADO",
                registo.Registo_Producao_id);
            await _eventoRepository.UpdateAsync(evento);

            return MapEvento(evento);
        }

        private async Task<ProcessStatus> ProcessarEventoAsync(IndustrialTelemetryDto dto)
        {
            var ip = dto.MachineIp?.Trim();
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentException("MachineIp e obrigatorio.");

            var maquina = await _maquinaRepository.GetByIpAddressAsync(ip);
            if (maquina == null)
            {
                _logger.LogWarning("Evento industrial ignorado: maquina com IP {MachineIp} nao existe no backend.", ip);
                return ProcessStatus.Ignorado;
            }

            var estado = NormalizeState(dto.State);
            var sessao = await _sessaoRepository.GetAbertaPorMaquinaAsync(maquina.Maquina_id);
            var evento = BuildEvento(dto, maquina.Maquina_id, estado);

            if (sessao == null)
                return await ProcessarEventoSemSessaoAsync(evento);

            evento.SessaoMaquinaIndustrial_id = sessao.SessaoMaquinaIndustrial_id;
            return await ProcessarEventoComSessaoAsync(evento, sessao);
        }

        private async Task<ProcessStatus> ProcessarEventoSemSessaoAsync(EventoMaquinaIndustrial evento)
        {
            if (IsState(evento.EstadoMaquina, EstadoRunning))
            {
                evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE;
                evento.CamposEmFalta = "Operador_id,Peca_id,Fase_id";
                await _eventoRepository.AddAsync(evento);
                return ProcessStatus.Pendente;
            }

            if (IsState(evento.EstadoMaquina, EstadoAlarm))
            {
                evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE;
                evento.CamposEmFalta = "Ocorrencia";
                await _eventoRepository.AddAsync(evento);
                return ProcessStatus.Pendente;
            }

            IgnorarEvento(evento, IsState(evento.EstadoMaquina, EstadoStopped)
                ? "STOPPED_SEM_SESSAO_ATIVA"
                : "ESTADO_SEM_SESSAO_ATIVA");

            await _eventoRepository.AddAsync(evento);
            return ProcessStatus.Resolvido;
        }

        private async Task<ProcessStatus> ProcessarEventoComSessaoAsync(EventoMaquinaIndustrial evento, SessaoMaquinaIndustrial sessao)
        {
            if (IsState(evento.EstadoMaquina, EstadoStopped))
            {
                evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE;
                await _eventoRepository.AddAsync(evento);

                sessao.EstadoSessao = EstadoSessaoMaquinaIndustrial.AGUARDAR_CONFIRMACAO_PARAGEM;
                sessao.UltimoEstadoMaquina = EstadoStopped;
                sessao.LastSeenAt = GetRececaoTimestampUtc(evento);
                sessao.UpdatedAt = DateTime.UtcNow;
                await _sessaoRepository.UpdateAsync(sessao);

                return ProcessStatus.Pendente;
            }

            if (IsState(evento.EstadoMaquina, EstadoRunning))
            {
                var stoppedPendente = await _eventoRepository.GetUltimoStoppedPendenteAsync(sessao.SessaoMaquinaIndustrial_id);
                if (stoppedPendente != null)
                {
                    var registoPausa = await CriarRegistoProducaoAsync(sessao, EstadoProducao.PAUSADO, NormalizeTimestamp(stoppedPendente.OccurredAt));
                    ResolverEvento(stoppedPendente, EstadoProducao.PAUSADO, "AUTO_RUNNING_APOS_STOPPED", registoPausa.Registo_Producao_id);
                    await _eventoRepository.UpdateAsync(stoppedPendente);

                    var registoRetoma = await CriarRegistoProducaoAsync(sessao, EstadoProducao.EM_CURSO, NormalizeTimestamp(evento.OccurredAt));
                    ResolverEvento(evento, EstadoProducao.EM_CURSO, "AUTO_RUNNING_RETOMOU_PRODUCAO", registoRetoma.Registo_Producao_id);
                }
                else if (IsState(sessao.UltimoEstadoMaquina, EstadoStopped))
                {
                    var registoRetoma = await CriarRegistoProducaoAsync(sessao, EstadoProducao.EM_CURSO, NormalizeTimestamp(evento.OccurredAt));
                    ResolverEvento(evento, EstadoProducao.EM_CURSO, "AUTO_RUNNING_APOS_PARAGEM_RESOLVIDA", registoRetoma.Registo_Producao_id);
                }
                else if (!sessao.RegistoProducaoInicio_id.HasValue)
                {
                    _logger.LogWarning(
                        "Evento RUNNING recebido para a sessao industrial {SessaoId} sem registo de inicio associado. O contexto deve ser completado pelo utilizador antes de criar novos registos de producao.",
                        sessao.SessaoMaquinaIndustrial_id);

                    ResolverEvento(evento, null, "RUNNING_SEM_REGISTO_INICIAL", null);
                }
                else
                {
                    ResolverEvento(evento, null, "RUNNING_CONTEXTO_JA_ATIVO", null);
                }

                sessao.EstadoSessao = EstadoSessaoMaquinaIndustrial.ATIVA;
                sessao.UltimoEstadoMaquina = EstadoRunning;
                sessao.LastSeenAt = GetRececaoTimestampUtc(evento);
                sessao.UpdatedAt = DateTime.UtcNow;
                await _sessaoRepository.UpdateAsync(sessao);
                await _eventoRepository.AddAsync(evento);

                return ProcessStatus.Resolvido;
            }

            if (IsState(evento.EstadoMaquina, EstadoAlarm))
            {
                evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE;
                evento.CamposEmFalta = "Ocorrencia";
                await _eventoRepository.AddAsync(evento);
                return ProcessStatus.Pendente;
            }

            ResolverEvento(evento, null, IsState(evento.EstadoMaquina, EstadoIdle) ? "IDLE_RECEBIDO" : "ESTADO_TECNICO_RECEBIDO", null);
            sessao.UltimoEstadoMaquina = evento.EstadoMaquina;
            sessao.LastSeenAt = GetRececaoTimestampUtc(evento);
            sessao.UpdatedAt = DateTime.UtcNow;
            await _sessaoRepository.UpdateAsync(sessao);
            await _eventoRepository.AddAsync(evento);

            return ProcessStatus.Resolvido;
        }

        private async Task<ResponseRegistosProducaoDto> CriarRegistoEmCursoAsync(SessaoMaquinaIndustrial sessao, DateTime dataHora)
        {
            var dto = BuildRegistoDto(sessao, EstadoProducao.EM_CURSO);
            return await _registosProducaoService.CreateFromIndustrialEventAsync(dto, dataHora, permitirPrimeiroEstadoEmCurso: true);
        }

        private async Task<ResponseRegistosProducaoDto> CriarRegistoProducaoAsync(
            SessaoMaquinaIndustrial sessao,
            EstadoProducao estado,
            DateTime dataHora,
            int? proximaFaseId = null)
        {
            return await _registosProducaoService.CreateFromIndustrialEventAsync(
                BuildRegistoDto(sessao, estado, proximaFaseId),
                dataHora);
        }

        private async Task<ResponseRegistosProducaoDto?> EnsureSessaoInicioSincronizadoAsync(
            SessaoMaquinaIndustrial sessao,
            ResponseRegistosProducaoDto? ultimoRegisto = null)
        {
            if (sessao.RegistoProducaoInicio_id.HasValue)
                return ultimoRegisto ?? await _registosProducaoService.GetUltimoRegistoAsync(sessao.Fase_id, sessao.Peca_id);

            ultimoRegisto ??= await _registosProducaoService.GetUltimoRegistoAsync(sessao.Fase_id, sessao.Peca_id);
            var startedAt = NormalizeTimestamp(sessao.StartedAt);

            if (ultimoRegisto?.Estado_producao == EstadoProducao.PREPARACAO)
            {
                _logger.LogWarning(
                    "Sessao industrial {SessaoId} ficou apenas com PREPARACAO no arranque. A completar automaticamente EM_CURSO para recuperar a sessao.",
                    sessao.SessaoMaquinaIndustrial_id);

                var registoEmCurso = await CriarRegistoProducaoAsync(sessao, EstadoProducao.EM_CURSO, startedAt);
                sessao.RegistoProducaoInicio_id = registoEmCurso.Registo_Producao_id;
                sessao.UpdatedAt = DateTime.UtcNow;
                await _sessaoRepository.UpdateAsync(sessao);
                return registoEmCurso;
            }

            if (ultimoRegisto is not null && ultimoRegisto.Data_hora >= startedAt)
            {
                if (ultimoRegisto.Estado_producao == EstadoProducao.EM_CURSO)
                {
                    sessao.RegistoProducaoInicio_id = ultimoRegisto.Registo_Producao_id;
                    sessao.UpdatedAt = DateTime.UtcNow;
                    await _sessaoRepository.UpdateAsync(sessao);
                    return ultimoRegisto;
                }

                return ultimoRegisto;
            }

            _logger.LogWarning(
                "Sessao industrial {SessaoId} sem registo inicial associado. A sincronizar arranque da sessao antes de processar novos estados.",
                sessao.SessaoMaquinaIndustrial_id);

            var registoInicio = await CriarRegistoEmCursoAsync(sessao, startedAt);
            sessao.RegistoProducaoInicio_id = registoInicio.Registo_Producao_id;
            sessao.UpdatedAt = DateTime.UtcNow;
            await _sessaoRepository.UpdateAsync(sessao);

            return registoInicio;
        }

        private static CreateRegistosProducaoDto BuildRegistoDto(
            SessaoMaquinaIndustrial sessao,
            EstadoProducao estado,
            int? proximaFaseId = null)
        {
            return new CreateRegistosProducaoDto
            {
                Operador_id = sessao.Operador_id,
                Peca_id = sessao.Peca_id,
                Fase_id = sessao.Fase_id,
                Maquina_id = sessao.Maquina_id,
                Estado_producao = estado,
                ProximaFase_id = proximaFaseId
            };
        }

        private static EventoMaquinaIndustrial BuildEvento(IndustrialTelemetryDto dto, int maquinaId, string estado)
        {
            return new EventoMaquinaIndustrial
            {
                Maquina_id = maquinaId,
                IpMaquina = dto.MachineIp.Trim(),
                Protocolo = string.IsNullOrWhiteSpace(dto.Protocol) ? "DESCONHECIDO" : dto.Protocol.Trim(),
                EstadoMaquina = estado,
                OccurredAt = NormalizeTimestamp(dto.OccurredAt),
                Programa = NormalizeNullable(dto.Program),
                ContadorPecas = dto.PieceCounter,
                CodigoOperador = NormalizeNullable(dto.OperatorCode),
                CodigoPeca = NormalizeNullable(dto.PartCode),
                CodigoMolde = NormalizeNullable(dto.MoldCode),
                PayloadBruto = NormalizeNullable(dto.RawPayload)
            };
        }

        private static void ResolverEvento(
            EventoMaquinaIndustrial evento,
            EstadoProducao? estadoProducao,
            string fonteResolucao,
            int? registoProducaoId)
        {
            evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.RESOLVIDO;
            evento.ResolvidoComoEstadoProducao = estadoProducao;
            evento.FonteResolucao = fonteResolucao;
            evento.ResolvedAt = DateTime.UtcNow;
            evento.RegistoProducao_id = registoProducaoId;
            evento.UpdatedAt = DateTime.UtcNow;
        }

        private static void IgnorarEvento(EventoMaquinaIndustrial evento, string fonteResolucao)
        {
            evento.EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.IGNORADO;
            evento.FonteResolucao = fonteResolucao;
            evento.ResolvedAt = DateTime.UtcNow;
            evento.UpdatedAt = DateTime.UtcNow;
        }

        private static ResponseEventoMaquinaIndustrialDto MapEvento(EventoMaquinaIndustrial evento)
        {
            return new ResponseEventoMaquinaIndustrialDto
            {
                EventoMaquinaIndustrial_id = evento.EventoMaquinaIndustrial_id,
                SessaoMaquinaIndustrial_id = evento.SessaoMaquinaIndustrial_id,
                Maquina_id = evento.Maquina_id,
                IpMaquina = evento.IpMaquina,
                Protocolo = evento.Protocolo,
                EstadoMaquina = evento.EstadoMaquina,
                OccurredAt = evento.OccurredAt,
                Programa = evento.Programa,
                ContadorPecas = evento.ContadorPecas,
                CodigoOperador = evento.CodigoOperador,
                CodigoPeca = evento.CodigoPeca,
                CodigoMolde = evento.CodigoMolde,
                CamposEmFalta = evento.CamposEmFalta,
                EstadoResolucao = evento.EstadoResolucao,
                ResolvidoComoEstadoProducao = evento.ResolvidoComoEstadoProducao,
                FonteResolucao = evento.FonteResolucao,
                ResolvedAt = evento.ResolvedAt,
                RegistoProducao_id = evento.RegistoProducao_id
            };
        }

        private static ResponseSessaoMaquinaIndustrialDto MapSessao(SessaoMaquinaIndustrial sessao)
        {
            return new ResponseSessaoMaquinaIndustrialDto
            {
                SessaoMaquinaIndustrial_id = sessao.SessaoMaquinaIndustrial_id,
                Maquina_id = sessao.Maquina_id,
                Operador_id = sessao.Operador_id,
                Peca_id = sessao.Peca_id,
                Fase_id = sessao.Fase_id,
                RegistoProducaoInicio_id = sessao.RegistoProducaoInicio_id,
                RegistoProducaoConclusao_id = sessao.RegistoProducaoConclusao_id,
                EstadoSessao = sessao.EstadoSessao,
                UltimoEstadoMaquina = sessao.UltimoEstadoMaquina,
                StartedAt = sessao.StartedAt,
                LastSeenAt = sessao.LastSeenAt,
                ClosedAt = sessao.ClosedAt
            };
        }

        private static ResponseContextoAtivoMaquinaIndustrialDto MapContextoAtivo(SessaoMaquinaIndustrial sessao)
        {
            return new ResponseContextoAtivoMaquinaIndustrialDto
            {
                SessaoMaquinaIndustrial_id = sessao.SessaoMaquinaIndustrial_id,
                Maquina_id = sessao.Maquina_id,
                Operador_id = sessao.Operador_id,
                OperadorNome = sessao.Operador?.Nome ?? string.Empty,
                Peca_id = sessao.Peca_id,
                NumeroPeca = sessao.Peca?.NumeroPeca ?? string.Empty,
                DesignacaoPeca = sessao.Peca?.Designacao ?? string.Empty,
                Molde_id = sessao.Peca?.Molde_id ?? 0,
                NumeroMolde = sessao.Peca?.Molde?.Numero ?? string.Empty,
                Fase_id = sessao.Fase_id,
                FaseNome = sessao.Fase?.Nome.ToString() ?? string.Empty,
                ProximaFasePlaneada_id = sessao.Peca?.ProximaFase_id,
                ProximaFasePlaneadaNome = sessao.Peca?.ProximaFase?.Nome.ToString() ?? string.Empty,
                EstadoSessao = sessao.EstadoSessao.ToString(),
                UltimoEstadoMaquina = sessao.UltimoEstadoMaquina ?? string.Empty,
                StartedAt = sessao.StartedAt,
                LastSeenAt = sessao.LastSeenAt,
                ClosedAt = sessao.ClosedAt
            };
        }

        private static void AtualizarSessaoAposConfirmacao(
            SessaoMaquinaIndustrial sessao,
            EventoMaquinaIndustrial evento,
            bool trabalhoConcluido,
            int registoProducaoId)
        {
            if (trabalhoConcluido)
            {
                sessao.EstadoSessao = EstadoSessaoMaquinaIndustrial.FECHADA;
                sessao.ClosedAt = GetRececaoTimestampUtc(evento);
                sessao.RegistoProducaoConclusao_id = registoProducaoId;
            }
            else
            {
                sessao.EstadoSessao = EstadoSessaoMaquinaIndustrial.ATIVA;
            }

            sessao.LastSeenAt = GetRececaoTimestampUtc(evento);
            sessao.UltimoEstadoMaquina = EstadoStopped;
            sessao.UpdatedAt = DateTime.UtcNow;
        }

        private static string NormalizeState(string? state)
        {
            if (string.IsNullOrWhiteSpace(state))
                throw new ArgumentException("State e obrigatorio.");

            return state.Trim().ToUpperInvariant();
        }

        private static bool IsState(string? value, string expected)
        {
            return string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizeNullable(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            return string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase) ? null : trimmed;
        }

        private static DateTime NormalizeTimestamp(DateTime timestamp)
        {
            return timestamp.Kind switch
            {
                DateTimeKind.Utc => timestamp,
                DateTimeKind.Local => timestamp.ToUniversalTime(),
                _ => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
            };
        }

        private static DateTime GetRececaoTimestampUtc(EventoMaquinaIndustrial evento)
        {
            if (evento.CreatedAt > DateTime.MinValue)
                return NormalizeTimestamp(evento.CreatedAt);

            return NormalizeTimestamp(evento.OccurredAt);
        }

        private enum ProcessStatus
        {
            Ignorado,
            Pendente,
            Resolvido
        }
    }
}
