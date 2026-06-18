using TipMolde.Application.Dtos.RevisaoDto;

namespace TipMolde.Application.Interface.Desenho.IRevisao
{
    /// <summary>
    /// Define os casos de uso da feature Revisao.
    /// </summary>
    public interface IRevisaoService
    {
        /// <summary>
        /// Lista revisoes associadas a um projeto.
        /// </summary>
        /// <param name="projetoId">Identificador do projeto.</param>
        /// <param name="page">Numero da pagina a ser retornada.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Colecao de Dtos de revisao ordenados por numero decrescente.</returns>
        Task<PagedResult<ResponseRevisaoDto>> GetByProjetoIdAsync(int projetoId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Obtem uma revisao por identificador.
        /// </summary>
        /// <param name="id">Identificador interno da revisao.</param>
        /// <returns>DTO da revisao quando encontrada; nulo caso contrario.</returns>
        Task<ResponseRevisaoDto?> GetByIdAsync(int id);

        /// <summary>
        /// Cria uma nova revisao para um projeto.
        /// </summary>
        /// <param name="dto">Dados de criacao da revisao.</param>
        /// <returns>DTO da revisao criada.</returns>
        Task<ResponseRevisaoDto> CreateAsync(CreateRevisaoDto dto);

        /// <summary>
        /// Regista a primeira resposta do cliente a uma revisao enviada.
        /// </summary>
        /// <param name="revisaoId">Identificador da revisao.</param>
        /// <param name="dto">Payload de resposta do cliente.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        Task UpdateRespostaClienteAsync(int revisaoId, UpdateRespostaRevisaoDto dto);

        /// <summary>
        /// Regista a resposta do cliente com um anexo opcional associado.
        /// </summary>
        /// <param name="revisaoId">Identificador da revisao.</param>
        /// <param name="dto">Payload de resposta do cliente.</param>
        /// <param name="attachmentContent">Conteudo binario do anexo submetido.</param>
        /// <param name="attachmentFileName">Nome original do anexo submetido.</param>
        /// <param name="attachmentContentType">Tipo MIME do anexo submetido.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        Task UpdateRespostaClienteAsync(
            int revisaoId,
            UpdateRespostaRevisaoDto dto,
            byte[]? attachmentContent,
            string? attachmentFileName,
            string? attachmentContentType);

        /// <summary>
        /// Remove uma revisao existente.
        /// </summary>
        /// <param name="id">Identificador da revisao a remover.</param>
        /// <returns>Task de conclusao da remocao.</returns>
        Task DeleteAsync(int id);
    }
}
