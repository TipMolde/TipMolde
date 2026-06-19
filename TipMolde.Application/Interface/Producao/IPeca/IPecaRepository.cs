using TipMolde.Domain.Entities.Producao;

namespace TipMolde.Application.Interface.Producao.IPeca
{
    /// <summary>
    /// Define operacoes de persistencia especificas para a entidade Peca.
    /// </summary>
    public interface IPecaRepository : IGenericRepository<Peca, int>
    {
        /// <summary>
        /// Lista pecas associadas a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com pecas pertencentes ao molde informado.</returns>
        Task<PagedResult<Peca>> GetByMoldeIdAsync(int moldeId, int page, int pageSize);

        /// <summary>
        /// Lista pecas associadas a um conjunto de moldes.
        /// </summary>
        /// <param name="moldeIds">Identificadores dos moldes a consultar.</param>
        /// <returns>Colecao de pecas pertencentes aos moldes informados.</returns>
        Task<IReadOnlyList<Peca>> GetByMoldeIdsAsync(IEnumerable<int> moldeIds);

        /// <summary>
        /// Lista pecas de um molde que ainda nao foram adicionadas a qualquer pedido de material.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com pecas elegiveis para criar um pedido de material.</returns>
        Task<PagedResult<Peca>> GetByMoldeIdWithoutPedidoMaterialAsync(int moldeId, int page, int pageSize);

        /// <summary>
        /// Lista pecas de um molde com pedido de material ainda pendente de rececao.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com pecas que aguardam rececao de material.</returns>
        Task<PagedResult<Peca>> GetByMoldeIdPendingMaterialReceiptAsync(int moldeId, int page, int pageSize);

        /// <summary>
        /// Obtem todas as pecas associadas a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>Colecao completa de pecas do molde informado.</returns>
        Task<IReadOnlyList<Peca>> GetAllByMoldeIdAsync(int moldeId);

        /// <summary>
        /// Obtem uma peca pela designacao dentro de um molde.
        /// </summary>
        /// <param name="designacao">Designacao funcional da peca.</param>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>Peca encontrada ou nulo quando nao existe correspondencia.</returns>
        Task<Peca?> GetByDesignacaoAsync(string designacao, int moldeId);

        /// <summary>
        /// Obtem uma peca pelo numero funcional dentro de um molde.
        /// </summary>
        /// <param name="numeroPeca">Numero funcional da peca.</param>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>Peca encontrada ou nulo quando nao existe correspondencia.</returns>
        Task<Peca?> GetByNumeroPecaAsync(string numeroPeca, int moldeId);

        /// <summary>
        /// Obtem todas as pecas correspondentes aos identificadores informados.
        /// </summary>
        /// <param name="ids">Colecao de identificadores a pesquisar.</param>
        /// <returns>Colecao de pecas encontradas para os ids informados.</returns>
        Task<IReadOnlyList<Peca>> GetByIdsAsync(IEnumerable<int> ids);
    }
}
