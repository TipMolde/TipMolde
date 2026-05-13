using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.Linhas;

namespace TipMolde.Application.Interface.Fichas.IFichaProducao
{
    /// <summary>
    /// Define operacoes de persistencia e query das fichas editaveis de producao.
    /// </summary>
    public interface IFichaProducaoRepository : IGenericRepository<FichaProducao, int>
    {
        Task<PagedResult<FichaProducao>> GetByEncomendaMoldeIdAsync(int encomendaMoldeId, int page, int pageSize);
        Task<PagedResult<FichaProducao>> GetByMoldeIdAsync(int moldeId, int page, int pageSize);
        Task<FichaProducao?> GetByIdDetalheAsync(int id);

        Task<PagedResult<FichaFrmLinha>> GetLinhasFrmByFichaIdAsync(int fichaId, int page, int pageSize);
        Task<FichaFrmLinha?> GetLinhaFrmByIdAsync(int fichaId, int linhaId);
        Task<FichaFrmLinha> AddLinhaFrmAsync(FichaFrmLinha linha);
        Task UpdateLinhaFrmAsync(FichaFrmLinha linha);

        Task<PagedResult<FichaFraLinha>> GetLinhasFraByFichaIdAsync(int fichaId, int page, int pageSize);
        Task<FichaFraLinha?> GetLinhaFraByIdAsync(int fichaId, int linhaId);
        Task<FichaFraLinha> AddLinhaFraAsync(FichaFraLinha linha);
        Task UpdateLinhaFraAsync(FichaFraLinha linha);

        Task<PagedResult<FichaFopLinha>> GetLinhasFopByFichaIdAsync(int fichaId, int page, int pageSize);
        Task<FichaFopLinha?> GetLinhaFopByIdAsync(int fichaId, int linhaId);
        Task<FichaFopLinha> AddLinhaFopAsync(FichaFopLinha linha);
        Task UpdateLinhaFopAsync(FichaFopLinha linha);
    }
}
