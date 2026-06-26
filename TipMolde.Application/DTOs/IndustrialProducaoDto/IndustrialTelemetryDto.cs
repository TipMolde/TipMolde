using System.ComponentModel.DataAnnotations;

namespace TipMolde.Application.Dtos.IndustrialProducaoDto
{
    /// <summary>
    /// Evento tecnico normalizado recebido do middleware industrial.
    /// </summary>
    public class IndustrialTelemetryDto
    {
        [Required]
        public string MachineIp { get; set; } = string.Empty;

        [Required]
        public string Protocol { get; set; } = string.Empty;

        [Required]
        public string State { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        public string? Program { get; set; }

        public int? PieceCounter { get; set; }

        public string? OperatorCode { get; set; }

        public string? PartCode { get; set; }

        public string? MoldCode { get; set; }

        public string? SourceName { get; set; }

        public string? RawPayload { get; set; }
    }
}
