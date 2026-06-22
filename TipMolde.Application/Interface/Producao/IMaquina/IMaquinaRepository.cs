using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Interface.Producao.IMaquina
{
    /// <summary>
    /// Define operacoes de persistencia especificas da feature Maquina.
    /// </summary>
    public interface IMaquinaRepository : IGenericRepository<Maquina, int>
    {
        /// <summary>
        /// Obtem uma maquina pelo identificador interno.
        /// </summary>
        /// <param name="id">Identificador da maquina.</param>
        /// <returns>Entidade encontrada; nulo caso nao exista.</returns>
        Task<Maquina?> GetByIdUnicoAsync(int id);

        /// <summary>
        /// Lista maquinas por estado com paginacao.
        /// </summary>
        /// <param name="estado">Estado operacional a filtrar.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com maquinas filtradas.</returns>
        Task<PagedResult<Maquina>> GetByEstadoAsync(EstadoMaquina estado, int page, int pageSize);

        /// <summary>
        /// Pesquisa maquinas por termo livre em numero, modelo, estado, IP ou fase dedicada.
        /// </summary>
        /// <param name="searchTerm">Termo de pesquisa a aplicar.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com maquinas correspondentes ao termo.</returns>
        Task<PagedResult<Maquina>> SearchAsync(string searchTerm, int page, int pageSize);

        /// <summary>
        /// Verifica se ja existe uma maquina com o numero fisico informado.
        /// </summary>
        /// <param name="numero">Numero fisico a validar.</param>
        /// <param name="excludeMaquinaId">Identificador a excluir na validacao, usado em updates.</param>
        /// <returns>True quando o numero ja estiver em uso.</returns>
        Task<bool> ExistsNumeroAsync(int numero, int? excludeMaquinaId = null);

        /// <summary>
        /// Verifica se a fase dedicada existe.
        /// </summary>
        /// <param name="faseDedicadaId">Identificador da fase a validar.</param>
        /// <returns>True quando a fase existir.</returns>
        Task<bool> ExistsFaseDedicadaAsync(int faseDedicadaId);

        /// <summary>
        /// Persiste uma nova maquina traduzindo conflitos tecnicos em conflitos de negocio.
        /// </summary>
        /// <param name="maquina">Entidade a criar.</param>
        /// <returns>Entidade criada.</returns>
        Task<Maquina> CreateAsync(Maquina maquina);

        /// <summary>
        /// Atualiza uma maquina existente traduzindo conflitos tecnicos em conflitos de negocio.
        /// </summary>
        /// <param name="maquina">Entidade a atualizar.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        Task UpdateExistingAsync(Maquina maquina);
    }
}
