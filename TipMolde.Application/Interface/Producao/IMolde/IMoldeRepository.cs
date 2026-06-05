using TipMolde.Domain.Entities.Producao;

namespace TipMolde.Application.Interface.Producao.IMolde
{
    /// <summary>
    /// Define operacoes de persistencia especificas do agregado Molde.
    /// </summary>
    /// <remarks>
    /// As queries desta feature devem suportar leituras com especificacoes tecnicas
    /// e a criacao transacional do agregado principal com as respetivas especificacoes.
    /// </remarks>
    public interface IMoldeRepository : IGenericRepository<Molde, int>
    {

        /// <summary>
        /// Obtem um molde pelo numero funcional.
        /// </summary>
        /// <param name="numero">Numero de negocio usado para identificar o molde.</param>
        /// <returns>Molde encontrado; nulo caso o numero nao exista.</returns>
        Task<Molde?> GetByNumeroAsync(string numero);

        /// <summary>
        /// Lista moldes associados a uma encomenda com paginacao.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda usada como filtro.</param>
        /// <param name="page">Pagina atual da pesquisa.</param>
        /// <param name="pageSize">Numero maximo de registos por pagina.</param>
        /// <returns>Resultado paginado com os moldes ligados a encomenda indicada.</returns>
        Task<PagedResult<Molde>> GetByEncomendaIdAsync(int encomendaId, int page, int pageSize);

        /// <summary>
        /// Persiste o molde e as respetivas especificacoes tecnicas.
        /// </summary>
        /// <remarks>
        /// Este metodo suporta o contrato de criacao do agregado Molde sem acoplamento
        /// a uma encomenda no mesmo pedido.
        /// </remarks>
        /// <param name="molde">Entidade principal do agregado.</param>
        /// <param name="specs">Especificacoes tecnicas a associar ao molde criado.</param>
        /// <returns>Task de conclusao da operacao de persistencia.</returns>
        Task AddMoldeWithSpecsAsync(Molde molde, EspecificacoesTecnicas specs);
    }
}
