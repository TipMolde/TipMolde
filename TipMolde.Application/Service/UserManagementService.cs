using AutoMapper;
using Microsoft.Extensions.Logging;
using TipMolde.Application.Dtos.UserDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Utilizador.ISecurity;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Service
{
    /// <summary>
    /// Implementa os casos de uso de gestao de utilizadores.
    /// </summary>
    /// <remarks>
    /// Centraliza listagem, pesquisa, criacao, atualizacao de dados base,
    /// alteracao de roles e remocao de utilizadores.
    /// </remarks>
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasher;
        private readonly IMapper _mapper;
        private readonly ILogger<UserManagementService> _logger;

        /// <summary>
        /// Construtor de UserManagementService.
        /// </summary>
        /// <param name="userRepository">Repositorio responsavel pela persistencia de utilizadores.</param>
        /// <param name="passwordHasher">Servico responsavel por gerar hashes de passwords.</param>
        /// <param name="mapper">Mapeador de objetos para conversao entre Dtos e entidades.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes de utilizador.</param>
        public UserManagementService(
            IUserRepository userRepository,
            IPasswordHasherService passwordHasher,
            IMapper mapper,
            ILogger<UserManagementService> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Lista utilizadores com paginacao.
        /// </summary>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com utilizadores e metadados de navegacao.</returns>
        public async Task<PagedResult<ResponseUserDto>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _userRepository.GetAllAsync(normalizedPage, normalizedPageSize);
            var mappedItems = _mapper.Map<IEnumerable<ResponseUserDto>>(result.Items);
            return new PagedResult<ResponseUserDto>(mappedItems, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Obtem um utilizador pelo identificador.
        /// </summary>
        /// <param name="id">Identificador unico do utilizador.</param>
        /// <returns>DTO do utilizador encontrado ou nulo quando nao existe registo.</returns>
        public async Task<ResponseUserDto?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user == null ? null : _mapper.Map<ResponseUserDto>(user);
        }

        /// <summary>
        /// Obtem o utilizador autenticado a partir do identificador validado pelo pipeline de autenticacao.
        /// </summary>
        /// <param name="authenticatedUserId">Identificador do utilizador autenticado.</param>
        /// <returns>DTO do utilizador autenticado ou nulo quando o registo nao existe.</returns>
        public async Task<ResponseUserDto?> GetCurrentAsync(int authenticatedUserId)
        {
            if (authenticatedUserId <= 0)
                throw new ArgumentException("O identificador do utilizador autenticado e invalido.");

            var user = await _userRepository.GetByIdAsync(authenticatedUserId);
            return user == null ? null : _mapper.Map<ResponseUserDto>(user);
        }

        /// <summary>
        /// Pesquisa utilizadores por nome com paginacao.
        /// </summary>
        /// <param name="searchTerm">Termo parcial para pesquisa no nome.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com utilizadores que correspondem ao termo informado.</returns>
        public async Task<PagedResult<ResponseUserDto>> SearchByNameAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return PaginationDefaults.EmptyPage<ResponseUserDto>(page, pageSize);

            var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
            var result = await _userRepository.SearchByNameAsync(searchTerm, normalizedPage, normalizedPageSize);
            var mappedItems = _mapper.Map<IEnumerable<ResponseUserDto>>(result.Items);
            return new PagedResult<ResponseUserDto>(mappedItems, result.TotalCount, result.CurrentPage, result.PageSize);
        }

        /// <summary>
        /// Obtem um utilizador pelo email.
        /// </summary>
        /// <param name="email">Email funcional do utilizador.</param>
        /// <returns>DTO do utilizador encontrado ou nulo quando nao existe registo.</returns>
        public async Task<ResponseUserDto?> GetByEmailAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user == null ? null : _mapper.Map<ResponseUserDto>(user);
        }

        /// <summary>
        /// Cria um novo utilizador apos validacao de unicidade e complexidade da password.
        /// </summary>
        /// <remarks>
        /// Fluxo principal:
        /// 1. Normaliza o email.
        /// 2. Rejeita email duplicado.
        /// 3. Valida campos obrigatorios e complexidade da password.
        /// 4. Persiste o utilizador com password em hash.
        /// </remarks>
        /// <param name="dto">DTO com dados do utilizador a criar.</param>
        /// <returns>DTO do utilizador criado apos persistencia.</returns>
        public async Task<ResponseUserDto> CreateAsync(CreateUserDto dto)
        {
            var user = _mapper.Map<User>(dto);
            _logger.LogInformation("Criacao de utilizador iniciada para email {Email}", user.Email);
            user.Email = user.Email.Trim().ToLowerInvariant();

            var existing = await _userRepository.GetByEmailAsync(user.Email);
            if (existing is not null)
            {
                _logger.LogWarning("Criacao de utilizador falhou: email duplicado {Email}", user.Email);
                throw new ArgumentException("Ja existe utilizador com este email.");
            }

            if (string.IsNullOrWhiteSpace(user.Nome)) throw new ArgumentException("Nome e obrigatorio.");
            if (string.IsNullOrWhiteSpace(user.Email)) throw new ArgumentException("Email e obrigatorio.");
            if (string.IsNullOrWhiteSpace(user.Password)) throw new ArgumentException("Senha e obrigatoria.");

            user.Nome = user.Nome.Trim();
            ValidatePasswordComplexity(user.Password);
            user.Password = _passwordHasher.Hash(user.Password);

            await _userRepository.AddAsync(user);
            _logger.LogInformation("Utilizador criado com sucesso {UserId}", user.User_id);
            return _mapper.Map<ResponseUserDto>(user);
        }

        /// <summary>
        /// Atualiza os dados base de um utilizador existente.
        /// </summary>
        /// <param name="id">Identificador unico do utilizador a atualizar.</param>
        /// <param name="dto">DTO com os dados parciais de atualizacao.</param>
        /// <returns>Task assincrona concluida apos persistencia.</returns>
        public async Task UpdateAsync(int id, UpdateUserDto dto)
        {
            _logger.LogInformation("Atualizacao de utilizador iniciada {UserId}", id);
            var existing = await _userRepository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Atualizacao falhou: utilizador nao encontrado {UserId}", id);
                throw new KeyNotFoundException($"Utilizador com ID {id} não encontrado.");
            }

            var hasChanges =
                !string.IsNullOrWhiteSpace(dto.Nome) ||
                !string.IsNullOrWhiteSpace(dto.Email);

            if (!hasChanges)
                throw new ArgumentException("Pelo menos um campo deve ser informado para atualizacao.");

            _mapper.Map(dto, existing);

            if (!string.IsNullOrWhiteSpace(dto.Email))
                existing.Email = existing.Email.Trim().ToLowerInvariant();

            await _userRepository.UpdateAsync(existing);
            _logger.LogInformation("Utilizador atualizado com sucesso {UserId}", id);
        }

        /// <summary>
        /// Altera a role funcional de um utilizador existente.
        /// </summary>
        /// <param name="id">Identificador unico do utilizador.</param>
        /// <param name="newRole">Nova role a atribuir.</param>
        /// <returns>Task assincrona concluida apos persistencia.</returns>
        public async Task ChangeRoleAsync(int id, UserRole newRole)
        {
            _logger.LogInformation("Alteracao de role iniciada {UserId} -> {Role}", id, newRole);
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Alteracao de role falhou: utilizador nao encontrado {UserId}", id);
                throw new KeyNotFoundException($"Utilizador com ID {id} não encontrado.");
            }

            user.Role = newRole;
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Role alterada com sucesso {UserId} -> {Role}", id, newRole);
        }

        /// <summary>
        /// Remove um utilizador pelo identificador.
        /// </summary>
        /// <param name="id">Identificador unico do utilizador a remover.</param>
        /// <returns>Task assincrona concluida apos remocao.</returns>
        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Eliminacao de utilizador iniciada {UserId}", id);
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Eliminacao falhou: utilizador nao encontrado {UserId}", id);
                throw new KeyNotFoundException($"User com ID {id} nao encontrado.");
            }

            await _userRepository.DeleteAsync(id);
            _logger.LogInformation("Utilizador eliminado com sucesso {UserId}", id);
        }

        /// <summary>
        /// Valida os requisitos minimos de complexidade da password.
        /// </summary>
        /// <param name="password">Password em texto claro recebida no pedido de criacao.</param>
        private static void ValidatePasswordComplexity(string password)
        {
            if (password.Length < 8 ||
                !password.Any(char.IsUpper) ||
                !password.Any(char.IsLower) ||
                !password.Any(char.IsDigit) ||
                !password.Any(ch => !char.IsLetterOrDigit(ch)))
                throw new ArgumentException("A password deve ter pelo menos 8 caracteres, maiuscula, minuscula, numero e simbolo.");
        }
    }
}
