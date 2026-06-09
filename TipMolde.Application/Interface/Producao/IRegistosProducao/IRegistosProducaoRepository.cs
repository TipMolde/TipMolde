using TipMolde.Domain.Entities.Producao;

namespace TipMolde.Application.Interface.Producao.IRegistosProducao
{
    /// <summary>
    /// Define operacoes de persistencia especificas da feature RegistosProducao.
    /// </summary>
    /// <remarks>
    /// Inclui consultas por fase/peca e a operacao atomica que persiste registo
    /// e alteracao de estado da maquina na mesma fronteira transacional.
    /// </remarks>
    public interface IRegistosProducaoRepository : IGenericRepository<RegistosProducao, int>
    {
        /// <summary>
        /// Obtem o historico de registos de uma peca numa fase.
        /// </summary>
        /// <param name="faseId">Identificador da fase de producao.</param>
        /// <param name="pecaId">Identificador da peca.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com registos em ordem cronologica.</returns>
        Task<PagedResult<RegistosProducao>> GetHistoricoAsync(int faseId, int pecaId, int page, int pageSize);

        /// <summary>
        /// Obtem o ultimo registo de uma peca numa fase.
        /// </summary>
        /// <param name="faseId">Identificador da fase de producao.</param>
        /// <param name="pecaId">Identificador da peca.</param>
        /// <returns>Ultimo registo encontrado ou nulo quando nao existe historico.</returns>
        Task<RegistosProducao?> GetUltimoRegistoAsync(int faseId, int pecaId);

        /// <summary>
        /// Obtem o ultimo registo global conhecido de cada peca informada.
        /// </summary>
        /// <param name="pecaIds">Identificadores das pecas a analisar.</param>
        /// <returns>Ultimos registos globais por peca.</returns>
        Task<List<RegistosProducao>> GetUltimosRegistosGlobaisAsync(IEnumerable<int> pecaIds);

        /// <summary>
        /// Lista registos associados a uma maquina.
        /// </summary>
        /// <param name="maquinaId">Identificador da maquina.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com registos associados a maquina.</returns>
        Task<PagedResult<RegistosProducao>> GetByMaquinaAsync(int maquinaId, int page, int pageSize);

        /// <summary>
        /// Persiste um registo e a alteracao de estado da maquina na mesma transacao.
        /// </summary>
        /// <param name="registo">Registo de producao a criar.</param>
        /// <param name="maquinaToUpdate">Maquina a atualizar; nulo quando nao ha alteracao de maquina.</param>
        /// <param name="pecaToUpdate">Peca a atualizar; nulo quando nao ha alteracao de planeamento.</param>
        /// <returns>Registo persistido.</returns>
        Task<RegistosProducao> AddWithMachineStateAsync(RegistosProducao registo, Maquina? maquinaToUpdate, Peca? pecaToUpdate);
    }
}
