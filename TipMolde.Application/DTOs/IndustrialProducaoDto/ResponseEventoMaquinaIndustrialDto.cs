using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.IndustrialProducaoDto
{
    /// <summary>
    /// Resposta publica de um evento tecnico recebido de uma maquina.
    /// </summary>
    public class ResponseEventoMaquinaIndustrialDto
    {
        public int EventoMaquinaIndustrial_id { get; set; }
        public int? SessaoMaquinaIndustrial_id { get; set; }
        public int Maquina_id { get; set; }
        public string IpMaquina { get; set; } = string.Empty;
        public string Protocolo { get; set; } = string.Empty;
        public string EstadoMaquina { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string? Programa { get; set; }
        public int? ContadorPecas { get; set; }
        public string? CodigoOperador { get; set; }
        public string? CodigoPeca { get; set; }
        public string? CodigoMolde { get; set; }
        public string? CamposEmFalta { get; set; }
        public EstadoResolucaoEventoMaquinaIndustrial EstadoResolucao { get; set; }
        public EstadoProducao? ResolvidoComoEstadoProducao { get; set; }
        public string? FonteResolucao { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? RegistoProducao_id { get; set; }
    }
}
