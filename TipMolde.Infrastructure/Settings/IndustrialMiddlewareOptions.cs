namespace TipMolde.Infrastructure.Settings
{
    public sealed class IndustrialMiddlewareOptions
    {
        public const string SectionName = "IndustrialMiddleware";

        public string BaseUrl { get; set; } = "http://localhost:5087";

        public string ProtocolDetectionPath { get; set; } = "/api/protocol-detection";

        public int RequestTimeoutSeconds { get; set; } = 10;
    }
}
