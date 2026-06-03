using TipMolde.Domain.Entities.Comercio;

namespace TipMolde.Application.Interface.Comercio.IEncomendaMolde
{
    /// <summary>
    /// Define operacoes de persistencia especificas da relacao Encomenda-Molde.
    /// </summary>
    /// <remarks>
    /// Expõe consultas paginadas por FK e validacao de unicidade da associacao.
    /// </remarks>
    public interface IEncomendaMoldeRepository : IGenericRepository<EncomendaMolde, int>
    {
        /// <summary>
        /// Lista associacoes por encomenda com paginacao.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda para filtro.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com associacoes da encomenda.</returns>
        Task<PagedResult<EncomendaMolde>> GetByEncomendaIdAsync(
            int encomendaId,
            int page,
            int pageSize);

        /// <summary>
        /// Lista associacoes por molde com paginacao.
        /// </summary>
        /// <param name="moldeId">Identificador do molde para filtro.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com associacoes do molde.</returns>
        Task<PagedResult<EncomendaMolde>> GetByMoldeIdAsync(
            int moldeId,
            int page,
            int pageSize);

        /// <summary>
        /// Verifica se ja existe associacao para o par Encomenda_id + Molde_id.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda da associacao.</param>
        /// <param name="moldeId">Identificador do molde da associacao.</param>
        /// <param name="excludeEncomendaMoldeId">ID opcional a excluir da validacao em cenarios de update.</param>
        /// <returns>True quando existe duplicado; caso contrario, false.</returns>
        Task<bool> ExistsAssociationAsync(
            int encomendaId,
            int moldeId,
            int? excludeEncomendaMoldeId = null);

        /// <summary>
        /// Obtem todas as associacoes Encomenda-Molde pertencentes a encomendas em aberto
        /// para calculo da fila global.
        /// </summary>
        /// <returns>Colecao materializada com as associacoes operacionais relevantes.</returns>
        Task<List<EncomendaMolde>> GetFilaGlobalAbertosAsync();

        /// <summary>
        /// Obtem a fila global de moldes com paginacao e ordenacao operacional.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com as associacoes da fila global.</returns>
        Task<PagedResult<EncomendaMolde>> GetFilaGlobalAsync(int page, int pageSize);

        /// <summary>
        /// Atualiza em lote as associacoes Encomenda-Molde apos um rebalanceamento de prioridades.
        /// </summary>
        /// <param name="entities">Associacoes a persistir com a nova prioridade.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        Task UpdateRangeAsync(IEnumerable<EncomendaMolde> entities);
    }
}
