using TipMolde.Application.Dtos.IndustrialProducaoDto;

namespace TipMolde.Application.Interface.Producao.IIndustrial
{
    /// <summary>
    /// Casos de uso que transformam eventos tecnicos de maquinas em contexto de producao.
    /// </summary>
    public interface IIndustrialProducaoService
    {
        Task<IndustrialTelemetryProcessResultDto> ProcessarTelemetriaAsync(IEnumerable<IndustrialTelemetryDto> eventos);

        Task<PagedResult<ResponseEventoMaquinaIndustrialDto>> GetEventosPendentesAsync(int page = 1, int pageSize = 10);

        Task<ResponseSessaoMaquinaIndustrialDto> CompletarContextoAsync(int eventoId, CompletarContextoEventoIndustrialDto dto);

        Task<ResponseEventoMaquinaIndustrialDto> ConfirmarParagemAsync(int eventoId, ConfirmarParagemIndustrialDto dto);
    }
}
