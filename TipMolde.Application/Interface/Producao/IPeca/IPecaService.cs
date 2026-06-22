using TipMolde.Application.Dtos.PecaDto;

namespace TipMolde.Application.Interface.Producao.IPeca
{
    /// <summary>
    /// Define os casos de uso publicos da feature Peca.
    /// </summary>
    /// <remarks>
    /// O contrato expoe Dtos para evitar acoplamento direto entre API e entidades de dominio.
    /// </remarks>
    public interface IPecaService
    {
        /// <summary>
        /// Lista pecas de forma paginada.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com pecas.</returns>
        Task<PagedResult<ResponsePecaDto>> GetAllAsync(int page = 1, int pageSize = 10);

        /// <summary>
        /// Obtem uma peca por identificador.
        /// </summary>
        /// <param name="id">Identificador interno da peca.</param>
        /// <returns>DTO da peca quando encontrada; nulo caso contrario.</returns>
        Task<ResponsePecaDto?> GetByIdAsync(int id);

        /// <summary>
        /// Lista pecas associadas a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com pecas do molde.</returns>
        Task<PagedResult<ResponsePecaDto>> GetByMoldeIdAsync(int moldeId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Lista pecas de um molde que ainda nao foram adicionadas a qualquer pedido de material.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <param name="searchTerm">Termo opcional para filtrar as pecas elegiveis.</param>
        /// <returns>Resultado paginado com pecas elegiveis para pedido de material.</returns>
        Task<PagedResult<ResponsePecaDto>> GetByMoldeIdWithoutPedidoMaterialAsync(int moldeId, int page = 1, int pageSize = 10, string? searchTerm = null);

        /// <summary>
        /// Lista pecas de um molde que possuem pedido de material pendente de rececao.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com pecas que aguardam rececao de material.</returns>
        Task<PagedResult<ResponsePecaDto>> GetByMoldeIdPendingMaterialReceiptAsync(int moldeId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Lista a fila de trabalho operacional das pecas da producao.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <param name="searchTerm">Termo de pesquisa opcional.</param>
        /// <param name="searchMode">Modo de pesquisa, por molde ou peca.</param>
        /// <returns>Resultado paginado com itens prontos para a pagina de producao.</returns>
        Task<PagedResult<ResponsePecaFilaTrabalhoDto>> GetFilaTrabalhoAsync(
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string searchMode = "Molde");

        /// <summary>
        /// Obtem uma peca pela designacao dentro de um molde.
        /// </summary>
        /// <param name="designacao">Designacao funcional da peca.</param>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>DTO da peca quando encontrada; nulo caso contrario.</returns>
        Task<ResponsePecaDto?> GetByDesignacaoAsync(string designacao, int moldeId);

        /// <summary>
        /// Cria uma nova peca.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida molde existente.
        /// 2. Valida designacao obrigatoria.
        /// 3. Garante unicidade da designacao dentro do molde.
        /// 4. Persiste a peca.
        /// </remarks>
        /// <param name="dto">Dados de criacao da peca.</param>
        /// <returns>DTO da peca criada.</returns>
        Task<ResponsePecaDto> CreateAsync(CreatePecaDto dto);

        /// <summary>
        /// Atualiza parcialmente uma peca existente.
        /// </summary>
        /// <remarks>
        /// Campos omitidos no DTO devem manter o valor atual da entidade.
        /// </remarks>
        /// <param name="id">Identificador da peca a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        Task UpdateAsync(int id, UpdatePecaDto dto);

        /// <summary>
        /// Importa pecas a partir de um ficheiro CSV da lista de materiais.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Valida a estrutura do ficheiro e a linha-resumo do molde.
        /// 2. Agrupa linhas por NumeroPeca.
        /// 3. Consolida quantidades quando os restantes campos coincidem.
        /// 4. Rejeita grupos com dados contraditorios para o mesmo NumeroPeca.
        /// 5. Persiste as pecas consolidadas no molde indicado.
        /// </remarks>
        /// <param name="moldeId">Identificador do molde que recebe as pecas importadas.</param>
        /// <param name="csvStream">Stream do ficheiro CSV a processar.</param>
        /// <returns>Resumo da importacao com as pecas persistidas.</returns>
        Task<ImportPecasCsvResultDto> ImportarCsvAsync(int moldeId, Stream csvStream);

        /// <summary>
        /// Remove uma peca existente.
        /// </summary>
        /// <param name="id">Identificador da peca a remover.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        Task DeleteAsync(int id);
    }
}

