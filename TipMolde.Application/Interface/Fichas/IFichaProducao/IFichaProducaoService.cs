using TipMolde.Application.Dtos.FichaProducaoDto;

namespace TipMolde.Application.Interface.Fichas.IFichaProducao
{
    /// <summary>
    /// Define os casos de uso publicos das fichas editaveis de producao.
    /// </summary>
    public interface IFichaProducaoService
    {
        /// <summary>
        /// Lista as fichas editaveis associadas a uma relacao Encomenda-Molde.
        /// </summary>
        /// <param name="encomendaMoldeId">Identificador da relacao Encomenda-Molde usada como contexto da ficha.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Pagina com os cabecalhos das fichas editaveis encontradas.</returns>
        Task<PagedResult<ResponseFichaProducaoDto>> GetByEncomendaMoldeIdAsync(int encomendaMoldeId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Lista as fichas editaveis associadas a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador interno do molde.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Pagina com os cabecalhos das fichas editaveis encontradas.</returns>
        Task<PagedResult<ResponseFichaProducaoDto>> GetByMoldeIdAsync(int moldeId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Obtem o detalhe completo de uma ficha editavel.
        /// </summary>
        /// <param name="id">Identificador interno da ficha editavel.</param>
        /// <returns>DTO detalhado da ficha ou nulo quando a ficha nao existe.</returns>
        Task<ResponseFichaProducaoDetalheDto?> GetByIdAsync(int id);

        /// <summary>
        /// Cria uma nova ficha editavel de producao.
        /// </summary>
        /// <remarks>
        /// Apenas FRE, FRM, FRA e FOP podem ser criadas manualmente.
        /// A FLT e sempre gerada a partir dos dados ja registados no sistema.
        /// </remarks>
        /// <param name="dto">Dados minimos necessarios para abrir a ficha no contexto Encomenda-Molde.</param>
        /// <returns>Cabecalho da ficha criada em estado de rascunho.</returns>
        Task<ResponseFichaProducaoDto> CreateAsync(CreateFichaProducaoDto dto);

        /// <summary>
        /// Garante a existencia de uma ficha editavel para o contexto indicado.
        /// </summary>
        /// <remarks>
        /// Se a ficha ja existir para o mesmo contexto e tipo, devolve a existente.
        /// Caso contrario, cria uma nova ficha com os mesmos dados minimos do Create.
        /// </remarks>
        /// <param name="dto">Dados minimos da ficha a garantir.</param>
        /// <returns>Cabecalho da ficha existente ou criada.</returns>
        Task<ResponseFichaProducaoDto> EnsureAsync(CreateFichaProducaoDto dto);

        /// <summary>
        /// Lista as linhas manuais da ficha FRM.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FRM.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Pagina com as linhas manuais da ficha FRM.</returns>
        Task<PagedResult<ResponseFichaFrmLinhaDto>> GetLinhasFrmAsync(int fichaId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Adiciona uma nova linha manual a uma ficha FRM.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FRM.</param>
        /// <param name="dto">Dados manuais da linha de melhoria.</param>
        /// <returns>Linha criada com o identificador persistido.</returns>
        Task<ResponseFichaFrmLinhaDto> CreateLinhaFrmAsync(int fichaId, CreateFichaFrmLinhaDto dto);

        /// <summary>
        /// Lista as linhas manuais da ficha FRA.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FRA.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Pagina com as linhas manuais da ficha FRA.</returns>
        Task<PagedResult<ResponseFichaFraLinhaDto>> GetLinhasFraAsync(int fichaId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Adiciona uma nova linha manual a uma ficha FRA.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FRA.</param>
        /// <param name="dto">Dados manuais da linha de alteracao.</param>
        /// <returns>Linha criada com o identificador persistido.</returns>
        Task<ResponseFichaFraLinhaDto> CreateLinhaFraAsync(int fichaId, CreateFichaFraLinhaDto dto);

        /// <summary>
        /// Lista as linhas manuais da ficha FOP.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FOP.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Pagina com as linhas manuais da ficha FOP.</returns>
        Task<PagedResult<ResponseFichaFopLinhaDto>> GetLinhasFopAsync(int fichaId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Adiciona uma nova linha manual a uma ficha FOP.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FOP.</param>
        /// <param name="dto">Dados manuais da linha de ocorrencia.</param>
        /// <returns>Linha criada com o identificador persistido.</returns>
        Task<ResponseFichaFopLinhaDto> CreateLinhaFopAsync(int fichaId, CreateFichaFopLinhaDto dto);
    }
}
