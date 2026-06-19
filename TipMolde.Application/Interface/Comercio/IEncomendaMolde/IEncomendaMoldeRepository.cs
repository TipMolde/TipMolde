using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Interface.Comercio.IEncomendaMolde
{
    /// <summary>
    /// Define operacoes de persistencia especificas da relacao Encomenda-Molde.
    /// </summary>
    /// <remarks>
    /// Expoe consultas paginadas por FK e validacao de unicidade da associacao.
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
        /// Lista associacoes Encomenda-Molde cujas encomendas estao confirmadas.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com associacoes aptas para inicio do desenho.</returns>
        Task<PagedResult<EncomendaMolde>> GetByEncomendasConfirmadasAsync(int page, int pageSize);

        /// <summary>
        /// Lista associacoes de encomendas confirmadas que tambem possuem projeto mais recente
        /// com a ultima revisao aprovada.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com associacoes aptas para o modulo de desenho.</returns>
        Task<PagedResult<EncomendaMolde>> GetByEncomendasConfirmadasParaDesenhoAsync(int page, int pageSize);

        /// <summary>
        /// Obtem uma associacao por ID com a encomenda associada carregada.
        /// </summary>
        /// <param name="id">Identificador da associacao.</param>
        /// <returns>Associacao com a encomenda carregada ou nulo quando nao existe.</returns>
        Task<EncomendaMolde?> GetByIdWithEncomendaAsync(int id);

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
        /// Indica se a encomenda ainda tem moldes por concluir.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda.</param>
        /// <param name="excludeEncomendaMoldeId">Associacao opcional a ignorar na contagem.</param>
        /// <returns>True quando ainda existe pelo menos um molde nao concluido.</returns>
        Task<bool> HasMoldesNaoConcluidosAsync(int encomendaId, int? excludeEncomendaMoldeId = null);

        /// <summary>
        /// Indica se todas as pecas do molde ja receberam material.
        /// </summary>
        /// <param name="moldeId">Identificador do molde a validar.</param>
        /// <returns>True quando o molde tem pecas e todas possuem MaterialRecebido = true.</returns>
        Task<bool> TodasPecasTemMaterialRecebidoAsync(int moldeId);

        /// <summary>
        /// Indica se todas as pecas do molde estao concluidas na fase de montagem.
        /// </summary>
        /// <param name="moldeId">Identificador do molde a validar.</param>
        /// <returns>True quando cada peca tem como ultimo registo a fase MONTAGEM em estado CONCLUIDO.</returns>
        Task<bool> TodasPecasConcluidasNaMontagemAsync(int moldeId);

        /// <summary>
        /// Obtem o conjunto de estados atuais dos moldes associados a uma encomenda.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda.</param>
        /// <returns>Lista materializada com os estados correntes dos moldes da encomenda.</returns>
        Task<List<EstadoEncomendaMolde>> GetEstadosByEncomendaIdAsync(int encomendaId);

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
