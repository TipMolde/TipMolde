using TipMolde.Domain.Enums;

namespace TipMolde.Domain.Entities.Producao
{
    /// <summary>
    /// Representa um evento tecnico recebido de uma maquina industrial.
    /// </summary>
    /// <remarks>
    /// Guarda o timestamp tecnico e a resolucao aplicada para permitir auditar como
    /// um sinal da maquina originou, ou nao, um registo de producao.
    /// </remarks>
    public class EventoMaquinaIndustrial
    {
        public int EventoMaquinaIndustrial_id { get; set; }

        public int? SessaoMaquinaIndustrial_id { get; set; }
        public SessaoMaquinaIndustrial? SessaoMaquinaIndustrial { get; set; }

        public int Maquina_id { get; set; }
        public Maquina? Maquina { get; set; }

        public required string IpMaquina { get; set; }

        public required string Protocolo { get; set; }

        /// <summary>
        /// Estado tecnico recebido da maquina, por exemplo RUNNING, STOPPED, IDLE ou ALARM.
        /// </summary>
        public required string EstadoMaquina { get; set; }

        public DateTime OccurredAt { get; set; }

        public string? Programa { get; set; }

        public int? ContadorPecas { get; set; }

        public string? CodigoOperador { get; set; }

        public string? CodigoPeca { get; set; }

        public string? CodigoMolde { get; set; }

        public string? CamposEmFalta { get; set; }

        public string? PayloadBruto { get; set; }

        public EstadoResolucaoEventoMaquinaIndustrial EstadoResolucao { get; set; } = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE;

        public EstadoProducao? ResolvidoComoEstadoProducao { get; set; }

        public string? FonteResolucao { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public int? RegistoProducao_id { get; set; }
        public RegistosProducao? RegistoProducao { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
