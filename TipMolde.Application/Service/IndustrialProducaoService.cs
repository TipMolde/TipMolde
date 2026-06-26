using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.IndustrialProducaoDto;
using TipMolde.Application.Dtos.RegistoProducaoDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IIndustrial;
using TipMolde.Application.Interface.Producao.IMaquina;
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
        private readonly IRegistosProducaoService _registosProducaoService;
        private readonly ILogger<IndustrialProducaoService> _logger;

        public IndustrialProducaoService(
            IEventoMaquinaIndustrialRepository eventoRepository,
            ISessaoMaquinaIndustrialRepository sessaoRepository,
            IMaquinaRepository maquinaRepository,
            IRegistosProducaoService registosProducaoService,
            ILogger<IndustrialProducaoService> logger)
        {
            _eventoRepository = eventoRepository;
            _sessaoRepository = sessaoRepository;
            _maquinaRepository = maquinaRepository;
            _registosProducaoService = registosProducaoService;
            _logger = logger;
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

            var estadoProducao = dto.TrabalhoConcluido ? EstadoProducao.CONCLUIDO : EstadoProducao.PAUSADO;
            var registo = await CriarRegistoProducaoAsync(sessao, estadoProducao, NormalizeTimestamp(evento.OccurredAt));

            if (dto.TrabalhoConcluido)
            {
                sessao.EstadoSessao = EstadoSessaoMaquinaIndustrial.FECHADA;
                sessao.ClosedAt = NormalizeTimestamp(evento.OccurredAt);
                sessao.RegistoProducaoConclusao_id = registo.Registo_Producao_id;
            }
            else
            {
                sessao.EstadoSessao = EstadoSessaoMaquinaIndustrial.ATIVA;
            }

            sessao.LastSeenAt = NormalizeTimestamp(evento.OccurredAt);
            sessao.UltimoEstadoMaquina = EstadoStopped;
            sessao.UpdatedAt = DateTime.UtcNow;
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
                sessao.LastSeenAt = NormalizeTimestamp(evento.OccurredAt);
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
                else
                {
                    ResolverEvento(evento, null, "RUNNING_CONTEXTO_JA_ATIVO", null);
                }

                sessao.EstadoSessao = EstadoSessaoMaquinaIndustrial.ATIVA;
                sessao.UltimoEstadoMaquina = EstadoRunning;
                sessao.LastSeenAt = NormalizeTimestamp(evento.OccurredAt);
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
            sessao.LastSeenAt = NormalizeTimestamp(evento.OccurredAt);
            sessao.UpdatedAt = DateTime.UtcNow;
            await _sessaoRepository.UpdateAsync(sessao);
            await _eventoRepository.AddAsync(evento);

            return ProcessStatus.Resolvido;
        }

        private async Task<ResponseRegistosProducaoDto> CriarRegistoEmCursoAsync(SessaoMaquinaIndustrial sessao, DateTime dataHora)
        {
            var dto = BuildRegistoDto(sessao, EstadoProducao.EM_CURSO);

            try
            {
                return await _registosProducaoService.CreateFromIndustrialEventAsync(dto, dataHora);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Primeiro estado deve ser PREPARACAO", StringComparison.OrdinalIgnoreCase))
            {
                await CriarRegistoProducaoAsync(sessao, EstadoProducao.PREPARACAO, dataHora);
                return await CriarRegistoProducaoAsync(sessao, EstadoProducao.EM_CURSO, dataHora);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Primeiro estado deve ser PENDENTE", StringComparison.OrdinalIgnoreCase))
            {
                await CriarRegistoProducaoAsync(sessao, EstadoProducao.PENDENTE, dataHora);
                return await CriarRegistoProducaoAsync(sessao, EstadoProducao.EM_CURSO, dataHora);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("nao e possivel passar de CONCLUIDO para EM_CURSO", StringComparison.OrdinalIgnoreCase))
            {
                await CriarRegistoProducaoAsync(sessao, EstadoProducao.PREPARACAO, dataHora);
                return await CriarRegistoProducaoAsync(sessao, EstadoProducao.EM_CURSO, dataHora);
            }
        }

        private async Task<ResponseRegistosProducaoDto> CriarRegistoProducaoAsync(
            SessaoMaquinaIndustrial sessao,
            EstadoProducao estado,
            DateTime dataHora)
        {
            return await _registosProducaoService.CreateFromIndustrialEventAsync(BuildRegistoDto(sessao, estado), dataHora);
        }

        private static CreateRegistosProducaoDto BuildRegistoDto(SessaoMaquinaIndustrial sessao, EstadoProducao estado)
        {
            return new CreateRegistosProducaoDto
            {
                Operador_id = sessao.Operador_id,
                Peca_id = sessao.Peca_id,
                Fase_id = sessao.Fase_id,
                Maquina_id = sessao.Maquina_id,
                Estado_producao = estado
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

        private enum ProcessStatus
        {
            Ignorado,
            Pendente,
            Resolvido
        }
    }
}
