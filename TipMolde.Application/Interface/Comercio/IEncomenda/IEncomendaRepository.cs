using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Interface.Comercio.IEncomenda
{
    /// <summary>
    /// Define operacoes de persistencia especificas de encomenda.
    /// </summary>
    public interface IEncomendaRepository : IGenericRepository<Encomenda, int>
    {
        /// <summary>Obtem encomenda com moldes associados.</summary>
        Task<Encomenda?> GetWithMoldesAsync(int id);

        /// <summary>Obtem encomenda por numero de referencia do cliente.</summary>
        Task<Encomenda?> GetByNumeroEncomendaClienteAsync(string numero);

        /// <summary>Lista encomendas por estado.</summary>
        Task<PagedResult<Encomenda>> GetByEstadoAsync(EstadoEncomenda estado, int page, int pageSize);

        /// <summary>Lista encomendas com estado nao terminal.</summary>
        Task<PagedResult<Encomenda>> GetEncomendasPorConcluirAsync(int page, int pageSize);

        /// <summary>Lista encomendas em producao para a UI operacional, incluindo cliente associado.</summary>
        Task<PagedResult<Encomenda>> GetEncomendasEmProducaoAsync(int page, int pageSize);

        /// <summary>Pesquisa encomendas em producao pelo numero da encomenda do cliente, incluindo cliente associado.</summary>
        Task<PagedResult<Encomenda>> SearchEncomendasEmProducaoAsync(string searchTerm, int page, int pageSize);

        /// <summary>
        /// Verifica se ja existe encomenda com o numero informado.
        /// </summary>
        /// <param name="numero">Numero da encomenda do cliente.</param>
        /// <param name="excludeEncomendaId">ID a excluir da pesquisa (util em update).</param>
        Task<bool> ExistsNumeroEncomendaClienteAsync(string numero, int? excludeEncomendaId = null);
    }
}
