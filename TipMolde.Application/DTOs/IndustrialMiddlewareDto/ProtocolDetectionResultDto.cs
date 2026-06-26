namespace TipMolde.Application.Dtos.IndustrialMiddlewareDto
{
    /// <summary>
    /// Resultado tecnico devolvido pelo middleware industrial apos testar um IP.
    /// </summary>
    public sealed class ProtocolDetectionResultDto
    {
        public string MachineIp { get; set; } = string.Empty;

        public bool Detected { get; set; }

        public string? Protocol { get; set; }

        public string? EndpointUrl { get; set; }

        public string? Message { get; set; }
    }
}
