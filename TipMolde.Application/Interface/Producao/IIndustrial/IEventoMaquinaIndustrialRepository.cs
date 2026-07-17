using TipMolde.Domain.Entities.Producao;

namespace TipMolde.Application.Interface.Producao.IIndustrial
{
    /// <summary>
    /// Persistencia especifica dos eventos tecnicos recebidos de maquinas industriais.
    /// </summary>
    public interface IEventoMaquinaIndustrialRepository : IGenericRepository<EventoMaquinaIndustrial, int>
    {
        Task<PagedResult<EventoMaquinaIndustrial>> GetRecebidosAsync(int page, int pageSize);

        Task<PagedResult<EventoMaquinaIndustrial>> GetPendentesAsync(int page, int pageSize);

        Task<EventoMaquinaIndustrial?> GetMaisRecentePendentePorMaquinaAsync(int maquinaId);

        Task<EventoMaquinaIndustrial?> GetMaisRecentePendentePorMaquinaAsync(int maquinaId, string estadoMaquina);

        Task<EventoMaquinaIndustrial?> GetUltimoStoppedPendenteAsync(int sessaoId);

        Task<IReadOnlyList<EventoMaquinaIndustrial>> GetStoppedPendentesAsync(int sessaoId);
    }
}
