using TipMolde.Application.Dtos.EncomendaMoldeDto;

namespace TipMolde.Application.Interface.Comercio.IEncomendaMolde
{
    /// <summary>
    /// Define os casos de uso da feature EncomendaMolde.
    /// </summary>
    /// <remarks>
    /// O contrato usa Dtos de aplicacao para evitar acoplamento API-Domain.
    /// </remarks>
    public interface IEncomendaMoldeService
    {
        /// <summary>
        /// Obtem uma associacao Encomenda-Molde por identificador.
        /// </summary>
        /// <param name="id">Identificador da associacao.</param>
        /// <returns>DTO de resposta quando encontrado; nulo caso nao exista.</returns>
        Task<ResponseEncomendaMoldeDto?> GetByIdAsync(int id);

        /// <summary>
        /// Lista associacoes por encomenda com paginacao.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda para filtro.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos de associacao.</returns>
        Task<PagedResult<ResponseEncomendaMoldeDto>> GetByEncomendaIdAsync(
            int encomendaId,
            int page = 1,
            int pageSize = 10);

        /// <summary>
        /// Lista associacoes por molde com paginacao.
        /// </summary>
        /// <param name="moldeId">Identificador do molde para filtro.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos de associacao.</returns>
        Task<PagedResult<ResponseEncomendaMoldeDto>> GetByMoldeIdAsync(
            int moldeId,
            int page = 1,
            int pageSize = 10);

        /// <summary>
        /// Lista associacoes Encomenda-Molde cujas encomendas estao confirmadas.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos de associacao aptos para o modulo de desenho.</returns>
        Task<PagedResult<ResponseEncomendaMoldeDto>> GetByEncomendasConfirmadasAsync(
            int page = 1,
            int pageSize = 10);

        /// <summary>
        /// Lista associacoes de moldes aptos para desenho, isto e, com encomenda confirmada,
        /// projeto concluido e ultima revisao aprovada pelo cliente.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos prontos para a pagina de desenho.</returns>
        Task<PagedResult<ResponseEncomendaMoldeDto>> GetByEncomendasConfirmadasParaDesenhoAsync(
            int page = 1,
            int pageSize = 10);

        /// <summary>
        /// Pesquisa as associacoes aptas para desenho por termo livre.
        /// </summary>
        /// <remarks>
        /// O termo e aplicado nos campos funcionais expostos para a pagina de desenho,
        /// como numeros, cliente, molde e descricao.
        /// </remarks>
        /// <param name="searchTerm">Termo de pesquisa a aplicar.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos filtrados para a pagina de desenho.</returns>
        Task<PagedResult<ResponseEncomendaMoldeDto>> SearchByTermForDesenhoAsync(
            string searchTerm,
            int page = 1,
            int pageSize = 10);
        
        /// <summary>
        /// Lista a fila global de moldes com paginacao.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com Dtos da fila global de moldes.</returns>
        Task<PagedResult<FilaGlobalMoldeItemDto>> GetFilaGlobalAsync(int page = 1, int pageSize = 10);

        /// <summary>
        /// Cria uma nova associacao Encomenda-Molde.
        /// </summary>
        /// <param name="dto">Dados de criacao da associacao.</param>
        /// <returns>DTO da associacao criada e persistida.</returns>
        Task<ResponseEncomendaMoldeDto> CreateAsync(CreateEncomendaMoldeDto dto);

        /// <summary>
        /// Atualiza parcialmente uma associacao Encomenda-Molde.
        /// </summary>
        /// <param name="id">Identificador da associacao a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        Task UpdateAsync(int id, UpdateEncomendaMoldeDto dto);

        /// <summary>
        /// Atualiza o estado operacional de uma associacao Encomenda-Molde.
        /// </summary>
        /// <param name="id">Identificador da associacao a atualizar.</param>
        /// <param name="dto">Estado de destino do molde dentro da encomenda.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        Task UpdateEstadoAsync(int id, UpdateEstadoEncomendaMoldeDto dto);

        /// <summary>
        /// Remove uma associacao Encomenda-Molde por identificador.
        /// </summary>
        /// <param name="id">Identificador da associacao a remover.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        Task DeleteAsync(int id);
    }
}
