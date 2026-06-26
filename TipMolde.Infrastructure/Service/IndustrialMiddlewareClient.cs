using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TipMolde.Application.Dtos.IndustrialMiddlewareDto;
using TipMolde.Application.Interface.Industrial;
using TipMolde.Infrastructure.Settings;

namespace TipMolde.Infrastructure.Service
{
    public sealed class IndustrialMiddlewareClient : IIndustrialMiddlewareClient
    {
        private readonly HttpClient _httpClient;
        private readonly IndustrialMiddlewareOptions _options;
        private readonly ILogger<IndustrialMiddlewareClient> _logger;

        public IndustrialMiddlewareClient(
            HttpClient httpClient,
            IOptions<IndustrialMiddlewareOptions> options,
            ILogger<IndustrialMiddlewareClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ProtocolDetectionResultDto> DetectProtocolAsync(string machineIp)
        {
            var ip = machineIp.Trim();
            var request = new { MachineIp = ip };

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(_options.ProtocolDetectionPath, request);
                var result = await response.Content.ReadFromJsonAsync<ProtocolDetectionResultDto>();

                if (result != null)
                {
                    return result;
                }

                return BuildFailure(ip, "O middleware nao devolveu um resultado valido para o teste de protocolo.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Falha a contactar o middleware industrial para testar o IP {MachineIp}.", ip);
                return BuildFailure(ip, "Nao foi possivel contactar o middleware industrial para testar este IP.");
            }
        }

        private static ProtocolDetectionResultDto BuildFailure(string machineIp, string message)
        {
            return new ProtocolDetectionResultDto
            {
                MachineIp = machineIp,
                Detected = false,
                Message = message
            };
        }
    }
}
