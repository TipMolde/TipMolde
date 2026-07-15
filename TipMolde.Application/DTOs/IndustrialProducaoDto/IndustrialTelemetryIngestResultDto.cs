namespace TipMolde.Application.Dtos.IndustrialProducaoDto
{
    /// <summary>
    /// Resultado da rececao tecnica de telemetria industrial.
    /// </summary>
    public class IndustrialTelemetryIngestResultDto
    {
        public int Recebidos { get; set; }
        public int Guardados { get; set; }
        public int Ignorados { get; set; }
    }
}
