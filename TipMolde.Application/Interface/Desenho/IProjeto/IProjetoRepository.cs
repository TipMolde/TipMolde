using TipMolde.Domain.Entities.Desenho;

namespace TipMolde.Application.Interface.Desenho.IProjeto
{
    /// <summary>
    /// Define as operacoes de persistencia do agregado Projeto.
    /// </summary>
    public interface IProjetoRepository : IGenericRepository<Projeto, int>
    {
        /// <summary>
        /// Lista projetos associados a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com projetos associados.</returns>
        Task<PagedResult<Projeto>> GetByMoldeIdAsync(int moldeId, int page, int pageSize);

        /// <summary>
        /// Obtem um projeto com as revisoes carregadas.
        /// </summary>
        /// <param name="id">Identificador interno do projeto.</param>
        /// <returns>Entidade do projeto com revisoes quando encontrada; nulo caso contrario.</returns>
        Task<Projeto?> GetWithRevisoesAsync(int id);

        /// <summary>
        /// Obtem o projeto mais recente associado a um molde com revisoes e registos temporais carregados.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>Projeto mais recente quando existe; nulo caso contrario.</returns>
        Task<Projeto?> GetLatestWithRevisoesAndTempoByMoldeAsync(int moldeId);
    }
}
