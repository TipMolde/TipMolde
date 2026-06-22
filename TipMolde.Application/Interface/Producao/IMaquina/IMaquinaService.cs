using TipMolde.Application.Dtos.MaquinaDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Interface.Producao.IMaquina
{
    /// <summary>
    /// Define os casos de uso publicos da feature Maquina.
    /// </summary>
    /// <remarks>
    /// O contrato expoe apenas DTOs para evitar acoplamento direto entre API e entidades de dominio.
    /// </remarks>
    public interface IMaquinaService
    {
        /// <summary>
        /// Lista maquinas com paginacao.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com DTOs de resposta.</returns>
        Task<PagedResult<ResponseMaquinaDto>> GetAllAsync(int page = 1, int pageSize = 10);

        /// <summary>
        /// Obtem uma maquina por identificador.
        /// </summary>
        /// <param name="id">Identificador interno da maquina.</param>
        /// <returns>DTO da maquina quando encontrada; nulo caso contrario.</returns>
        Task<ResponseMaquinaDto?> GetByIdAsync(int id);

        /// <summary>
        /// Lista maquinas por estado com paginacao.
        /// </summary>
        /// <param name="estado">Estado operacional a filtrar.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com DTOs filtrados por estado.</returns>
        Task<PagedResult<ResponseMaquinaDto>> GetByEstadoAsync(EstadoMaquina estado, int page = 1, int pageSize = 10);

        /// <summary>
        /// Pesquisa maquinas por termo livre.
        /// </summary>
        /// <param name="searchTerm">Termo de pesquisa aplicado nos campos de negocio.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com DTOs correspondentes ao termo.</returns>
        Task<PagedResult<ResponseMaquinaDto>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);

        /// <summary>
        /// Cria uma nova maquina.
        /// </summary>
        /// <remarks>
        /// O fluxo valida duplicados funcionais e existencia da fase dedicada antes da persistencia.
        /// </remarks>
        /// <param name="dto">Dados de criacao da maquina.</param>
        /// <returns>DTO da maquina criada.</returns>
        Task<ResponseMaquinaDto> CreateAsync(CreateMaquinaDto dto);

        /// <summary>
        /// Atualiza parcialmente uma maquina existente.
        /// </summary>
        /// <remarks>
        /// Campos omitidos mantem o valor atual e nunca aplicam defaults silenciosos.
        /// </remarks>
        /// <param name="id">Identificador da maquina a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        Task UpdateAsync(int id, UpdateMaquinaDto dto);

        /// <summary>
        /// Remove uma maquina existente.
        /// </summary>
        /// <param name="id">Identificador da maquina a remover.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        Task DeleteAsync(int id);
    }
}
