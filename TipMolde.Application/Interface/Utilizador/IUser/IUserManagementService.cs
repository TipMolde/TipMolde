using TipMolde.Application.Dtos.UserDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Interface.Utilizador.IUser
{
    /// <summary>
    /// Define os casos de uso de gestao de utilizadores.
    /// </summary>
    /// <remarks>
    /// Responsavel por consultas, pesquisa, criacao, atualizacao, alteracao de perfil e remocao de utilizadores,
    /// dependendo do role, seguindo o padrão RBAC.
    /// </remarks>
    public interface IUserManagementService
    {
        /// <summary>
        /// Lista utilizadores com paginacao.
        /// </summary>
        /// <param name="page">Numero da pagina solicitada.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com utilizadores e metadados de navegacao.</returns>
        Task<PagedResult<ResponseUserDto>> GetAllAsync(int page = 1, int pageSize = 10);

        /// <summary>
        /// Obtem um utilizador pelo identificador.
        /// </summary>
        /// <param name="id">Identificador unico do utilizador.</param>
        /// <returns>Utilizador encontrado ou nulo quando nao existe registo.</returns>
        Task<ResponseUserDto?> GetByIdAsync(int id);

        /// <summary>
        /// Obtem o utilizador autenticado a partir do identificador resolvido pelo pipeline HTTP.
        /// </summary>
        /// <param name="authenticatedUserId">Identificador do utilizador autenticado.</param>
        /// <returns>Utilizador autenticado encontrado ou nulo quando nao existe registo correspondente.</returns>
        Task<ResponseUserDto?> GetCurrentAsync(int authenticatedUserId);

        /// <summary>
        /// Obtem um utilizador pelo email.
        /// </summary>
        /// <param name="email">Email unico do utilizador.</param>
        /// <returns>Utilizador encontrado ou nulo quando nao existe registo.</returns>
        Task<ResponseUserDto?> GetByEmailAsync(string email);

        /// <summary>
        /// Pesquisa utilizadores por nome.
        /// </summary>
        /// <param name="searchTerm">Termo parcial para pesquisa no nome do utilizador.</param>
        /// <param name="page">Numero da pagina solicitada.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com utilizadores que correspondem ao termo informado.</returns>
        Task<PagedResult<ResponseUserDto>> SearchByNameAsync(string searchTerm, int page = 1, int pageSize = 10);

        /// <summary>
        /// Cria um novo utilizador.
        /// </summary>
        /// <param name="dto">Dados do utilizador a persistir.</param>
        /// <returns>Utilizador criado apos validacao e persistencia.</returns>
        Task<ResponseUserDto> CreateAsync(CreateUserDto dto);

        /// <summary>
        /// Atualiza os dados de um utilizador existente.
        /// </summary>
        /// <param name="id">Identificador do utilizador.</param>
        /// <param name="dto">Dados enviados para atualizacao parcial.</param>
        /// <returns>Task assincrona concluida apos atualizacao do utilizador.</returns>
        Task UpdateAsync(int id, UpdateUserDto dto);

        /// <summary>
        /// Altera o perfil de acesso de um utilizador.
        /// </summary>
        /// <param name="id">Identificador do utilizador alvo.</param>
        /// <param name="newRole">Novo perfil de acesso a aplicar.</param>
        /// <returns>Task assincrona concluida apos atualizacao do perfil.</returns>
        Task ChangeRoleAsync(int id, UserRole newRole);

        /// <summary>
        /// Remove um utilizador pelo identificador.
        /// </summary>
        /// <param name="id">Identificador unico do utilizador a remover.</param>
        /// <returns>Task assincrona concluida apos remocao do utilizador.</returns>
        Task DeleteAsync(int id);
    }
}
