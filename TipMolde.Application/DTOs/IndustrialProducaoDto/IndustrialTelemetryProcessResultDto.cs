namespace TipMolde.Application.Dtos.IndustrialProducaoDto
{
    /// <summary>
    /// Resultado resumido do processamento de telemetria industrial.
    /// </summary>
    public class IndustrialTelemetryProcessResultDto
    {
        public int Recebidos { get; set; }
        public int Processados { get; set; }
        public int Ignorados { get; set; }
        public int Pendentes { get; set; }
        public int Resolvidos { get; set; }
    }
}
