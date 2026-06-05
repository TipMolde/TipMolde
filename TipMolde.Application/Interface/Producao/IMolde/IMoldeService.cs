using TipMolde.Application.Dtos.MoldeDto;

namespace TipMolde.Application.Interface.Producao.IMolde
{
    /// <summary>
    /// Define os casos de uso publicos da feature Molde.
    /// </summary>
    /// <remarks>
    /// O contrato expoe Dtos para evitar acoplamento direto entre API e entidades de dominio.
    /// </remarks>
    public interface IMoldeService
    {
        /// <summary>
        /// Lista moldes de forma paginada.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com moldes.</returns>
        Task<PagedResult<ResponseMoldeDto>> GetAllAsync(int page = 1, int pageSize = 10);

        /// <summary>
        /// Obtem um molde por identificador.
        /// </summary>
        /// <param name="id">Identificador interno do molde.</param>
        /// <returns>DTO do molde quando encontrado; nulo caso contrario.</returns>
        Task<ResponseMoldeDto?> GetByIdAsync(int id);


        /// <summary>
        /// Obtem um molde pelo numero funcional.
        /// </summary>
        /// <param name="numero">Numero funcional do molde.</param>
        /// <returns>DTO do molde quando encontrado; nulo caso contrario.</returns>
        Task<ResponseMoldeDto?> GetByNumeroAsync(string numero);

        /// <summary>
        /// Lista moldes associados a uma encomenda.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Colecao de moldes associados.</returns>
        Task<PagedResult<ResponseMoldeDto>> GetByEncomendaIdAsync(int encomendaId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Verifica se ja existe um molde com o numero indicado.
        /// </summary>
        /// <param name="numero">Numero funcional a validar.</param>
        /// <returns>True quando o numero ja existe; false caso contrario.</returns>
        Task<bool> ExistsByNumeroAsync(string numero);

        /// <summary>
        /// Cria um novo agregado Molde.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida numero unico.
        /// 2. Persiste molde e especificacoes tecnicas na mesma transacao.
        /// </remarks>
        /// <param name="dto">Dados de criacao do molde.</param>
        /// <returns>DTO do molde criado.</returns>
        Task<ResponseMoldeDto> CreateAsync(CreateMoldeDto dto);

        /// <summary>
        /// Atualiza parcialmente um molde existente.
        /// </summary>
        /// <remarks>
        /// Campos nao enviados no DTO devem manter o valor atual do agregado.
        /// </remarks>
        /// <param name="id">Identificador do molde a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        Task UpdateAsync(int id, UpdateMoldeDto dto);

        /// <summary>
        /// Remove um molde existente.
        /// </summary>
        /// <param name="id">Identificador do molde a remover.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        Task DeleteAsync(int id);
    }
}
