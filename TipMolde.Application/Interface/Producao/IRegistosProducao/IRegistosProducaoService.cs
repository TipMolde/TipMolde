using TipMolde.Application.Dtos.RegistoProducaoDto;

namespace TipMolde.Application.Interface.Producao.IRegistosProducao
{
    /// <summary>
    /// Define os casos de uso publicos da feature RegistosProducao.
    /// </summary>
    /// <remarks>
    /// O contrato expoe DTOs para manter a API desacoplada das entidades de dominio
    /// e centralizar regras de producao na camada Application.
    /// </remarks>
    public interface IRegistosProducaoService
    {
        /// <summary>
        /// Lista registos de producao com paginacao.
        /// </summary>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com DTOs de registos de producao.</returns>
        Task<PagedResult<ResponseRegistosProducaoDto>> GetAllAsync(int page = 1, int pageSize = 10);

        /// <summary>
        /// Obtem um registo de producao pelo identificador.
        /// </summary>
        /// <param name="id">Identificador unico do registo de producao.</param>
        /// <returns>DTO do registo encontrado ou nulo quando nao existe.</returns>
        Task<ResponseRegistosProducaoDto?> GetByIdAsync(int id);

        /// <summary>
        /// Obtem o historico de producao de uma peca numa fase.
        /// </summary>
        /// <param name="faseId">Identificador da fase de producao.</param>
        /// <param name="pecaId">Identificador da peca.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado de registos historicos em ordem cronologica.</returns>
        Task<PagedResult<ResponseRegistosProducaoDto>> GetHistoricoAsync(int faseId, int pecaId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Obtem o ultimo registo de producao de uma peca numa fase.
        /// </summary>
        /// <param name="faseId">Identificador da fase de producao.</param>
        /// <param name="pecaId">Identificador da peca.</param>
        /// <returns>Ultimo registo encontrado ou nulo quando ainda nao existe historico.</returns>
        Task<ResponseRegistosProducaoDto?> GetUltimoRegistoAsync(int faseId, int pecaId);

        /// <summary>
        /// Cria um novo registo de producao.
        /// </summary>
        /// <param name="dto">Dados de entrada do registo de producao.</param>
        /// <returns>DTO do registo criado.</returns>
        Task<ResponseRegistosProducaoDto> CreateAsync(CreateRegistosProducaoDto dto);
    }
}
