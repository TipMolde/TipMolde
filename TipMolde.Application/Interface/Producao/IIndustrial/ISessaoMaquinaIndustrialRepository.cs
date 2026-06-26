using TipMolde.Domain.Entities.Producao;

namespace TipMolde.Application.Interface.Producao.IIndustrial
{
    /// <summary>
    /// Persistencia especifica do contexto operacional das maquinas industriais.
    /// </summary>
    public interface ISessaoMaquinaIndustrialRepository : IGenericRepository<SessaoMaquinaIndustrial, int>
    {
        Task<SessaoMaquinaIndustrial?> GetAbertaPorMaquinaAsync(int maquinaId);
    }
}
