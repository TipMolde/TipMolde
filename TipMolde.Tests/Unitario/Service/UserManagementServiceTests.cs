using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TipMolde.Application.Dtos.UserDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Utilizador.ISecurity;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Application.Mappings;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class UserManagementServiceTests
{
    private static readonly string[] ExpectedUserNames = ["Ana", "Bruno"];
    private static readonly int[] ExpectedUserIds = [1, 2];

    private Mock<IUserRepository> _userRepository = null!;
    private Mock<IPasswordHasherService> _passwordHasher = null!;
    private Mock<ILogger<UserManagementService>> _logger = null!;
    private IMapper _mapper = null!;
    private UserManagementService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepository = new Mock<IUserRepository>();
        _passwordHasher = new Mock<IPasswordHasherService>();
        _logger = new Mock<ILogger<UserManagementService>>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<UserProfile>());
        _mapper = config.CreateMapper();
        _sut = new UserManagementService(_userRepository.Object, _passwordHasher.Object, _mapper, _logger.Object);
    }

    /// <summary>
    /// Helper para criar entidades User em cenarios de teste.
    /// </summary>
    /// <param name="id">Identificador do utilizador.</param>
    /// <param name="nome">Nome do utilizador.</param>
    /// <param name="email">Email do utilizador.</param>
    /// <param name="password">Password inicial do utilizador.</param>
    /// <returns>Instancia de User para composicao dos testes.</returns>
    private static User BuildUser(int id = 1, string nome = "Operador", string email = "operador@tipmolde.pt", string password = "Passw0rd!") => new()
    {
        User_id = id,
        Nome = nome,
        Email = email,
        Password = password,
        Role = UserRole.GESTOR_PRODUCAO
    };

    private static CreateUserDto BuildCreateUserDto(
        string nome = "Operador",
        string email = "operador@tipmolde.pt",
        string password = "Passw0rd!",
        UserRole role = UserRole.GESTOR_PRODUCAO) => new()
        {
            Nome = nome,
            Email = email,
            Password = password,
            Role = role
        };

    private static UpdateUserDto BuildUpdateUserDto(string? nome = "Operador", string? email = "operador@tipmolde.pt") => new()
    {
        Nome = nome,
        Email = email
    };

    [Test(Description = "T1USR - GetAll deve devolver lista paginada de utilizadores.")]
    public async Task GetAllAsync_Should_ReturnPagedUsers_When_UsersExist()
    {
        // ARRANGE
        var users = new List<User>
        {
            BuildUser(id: 1, nome: "Ana", email: "ana@tipmolde.pt"),
            BuildUser(id: 2, nome: "Bruno", email: "bruno@tipmolde.pt")
        };

        var pagedResult = new PagedResult<User>(users, users.Count, 1, 10);
        _userRepository.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync(pagedResult);

        // ACT
        var result = await _sut.GetAllAsync(1, 10);

        // ASSERT
        result.Items.Should().HaveCount(2);
        result.Items.Select(u => u.Nome).Should().Contain(ExpectedUserNames);
        result.TotalCount.Should().Be(2);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(10);
        _userRepository.Verify(r => r.GetAllAsync(1, 10), Times.Once);
    }

    [Test(Description = "T2USR - Create deve falhar quando email ja existe.")]
    public async Task CreateAsync_Should_ThrowArgumentException_When_EmailAlreadyExists()
    {
        // ARRANGE
        var user = BuildCreateUserDto(email: "duplicado@tipmolde.pt");
        _userRepository.Setup(r => r.GetByEmailAsync("duplicado@tipmolde.pt")).ReturnsAsync(BuildUser(id: 2));

        // ACT
        Func<Task> act = () => _sut.CreateAsync(user);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "T3USR - Create deve falhar quando password e fraca.")]
    public async Task CreateAsync_Should_ThrowArgumentException_When_PasswordIsWeak()
    {
        // ARRANGE
        var user = BuildCreateUserDto(password: "fraca");
        _userRepository.Setup(r => r.GetByEmailAsync("operador@tipmolde.pt")).ReturnsAsync((User?)null);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(user);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "T4USR - Create deve normalizar dados e aplicar hash quando utilizador e valido.")]
    public async Task CreateAsync_Should_TrimNormalizeAndHash_When_UserIsValid()
    {
        // ARRANGE
        var user = BuildCreateUserDto(nome: "  Operador  ", email: "  OP@TipMolde.PT  ", password: "Valida123!");
        _userRepository.Setup(r => r.GetByEmailAsync("op@tipmolde.pt")).ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.Hash("Valida123!")).Returns("hash_gerado");

        // ACT
        var result = await _sut.CreateAsync(user);

        // ASSERT
        result.Nome.Should().Be("Operador");
        result.Email.Should().Be("op@tipmolde.pt");
        _userRepository.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.Nome == "Operador" &&
            u.Email == "op@tipmolde.pt" &&
            u.Password == "hash_gerado")), Times.Once);
    }

    [Test(Description = "T5USR - Update deve falhar quando utilizador nao existe.")]
    public async Task UpdateAsync_Should_ThrowKeyNotFoundException_When_UserDoesNotExist()
    {
        // ARRANGE
        _userRepository.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((User?)null);

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(404, BuildUpdateUserDto(nome: "Novo Nome", email: "novo@tipmolde.pt"));

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "T6USR - Update deve atualizar apenas campos informados.")]
    public async Task UpdateAsync_Should_UpdateOnlyProvidedFields_When_UserExists()
    {
        // ARRANGE
        var existing = BuildUser(id: 5, nome: "Nome Antigo", email: "antigo@tipmolde.pt");
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);

        var update = BuildUpdateUserDto(nome: "  Nome Novo  ", email: "  NOVO@TipMolde.PT ");

        // ACT
        await _sut.UpdateAsync(5, update);

        // ASSERT
        _userRepository.Verify(r => r.UpdateAsync(It.Is<User>(u =>
            u.User_id == 5 &&
            u.Nome == "Nome Novo" &&
            u.Email == "novo@tipmolde.pt")), Times.Once);
    }

    [Test(Description = "T7USR - ChangeRole deve falhar quando utilizador nao existe.")]
    public async Task ChangeRoleAsync_Should_ThrowKeyNotFoundException_When_UserDoesNotExist()
    {
        // ARRANGE
        _userRepository.Setup(r => r.GetByIdAsync(777)).ReturnsAsync((User?)null);

        // ACT
        Func<Task> act = () => _sut.ChangeRoleAsync(777, UserRole.ADMIN);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "T8USR - ChangeRole deve atualizar perfil quando utilizador existe.")]
    public async Task ChangeRoleAsync_Should_ChangeRole_When_UserExists()
    {
        // ARRANGE
        var user = BuildUser(id: 3);
        _userRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(user);

        // ACT
        await _sut.ChangeRoleAsync(3, UserRole.ADMIN);

        // ASSERT
        user.Role.Should().Be(UserRole.ADMIN);
        _userRepository.Verify(r => r.UpdateAsync(It.Is<User>(u => u.Role == UserRole.ADMIN)), Times.Once);
    }

    [Test(Description = "T9USR - Delete deve falhar quando utilizador nao existe.")]
    public async Task DeleteAsync_Should_ThrowKeyNotFoundException_When_UserDoesNotExist()
    {
        // ARRANGE
        _userRepository.Setup(r => r.GetByIdAsync(1000)).ReturnsAsync((User?)null);

        // ACT
        Func<Task> act = () => _sut.DeleteAsync(1000);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "T10USR - Delete deve remover utilizador quando registo existe.")]
    public async Task DeleteAsync_Should_DeleteUser_When_UserExists()
    {
        // ARRANGE
        _userRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(BuildUser(id: 7));

        // ACT
        await _sut.DeleteAsync(7);

        // ASSERT
        _userRepository.Verify(r => r.DeleteAsync(7), Times.Once);
    }

    [Test(Description = "T11USR - GetById deve devolver nulo quando utilizador nao existe.")]
    public async Task GetByIdAsync_Should_ReturnNull_When_UserDoesNotExist()
    {
        // ARRANGE
        _userRepository.Setup(r => r.GetByIdAsync(90)).ReturnsAsync((User?)null);

        // ACT
        var result = await _sut.GetByIdAsync(90);

        // ASSERT
        result.Should().BeNull();
    }

    [Test(Description = "T12USR - GetById deve mapear utilizador quando registo existe.")]
    public async Task GetByIdAsync_Should_MapResponse_When_UserExists()
    {
        // ARRANGE
        _userRepository.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(BuildUser(id: 8, nome: "Ana", email: "ana@tipmolde.pt"));

        // ACT
        var result = await _sut.GetByIdAsync(8);

        // ASSERT
        result.Should().NotBeNull();
        result!.User_id.Should().Be(8);
        result.Email.Should().Be("ana@tipmolde.pt");
    }

    [Test(Description = "T13USR - Search por nome deve devolver vazio quando termo e branco.")]
    public async Task SearchByNameAsync_Should_ReturnEmpty_When_SearchTermIsBlank()
    {
        // ACT
        var result = await _sut.SearchByNameAsync("   ", 0, 500);

        // ASSERT
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(200);
    }

    [Test(Description = "T14USR - Search por nome deve paginar e mapear utilizadores.")]
    public async Task SearchByNameAsync_Should_PaginateAndMap_When_UsersExist()
    {
        // ARRANGE
        var users = new List<User>
        {
            BuildUser(id: 1, nome: "Ana"),
            BuildUser(id: 2, nome: "Anabela"),
            BuildUser(id: 3, nome: "Anselmo")
        };

        _userRepository
       .Setup(r => r.SearchByNameAsync("Ana", 1, 10))
       .ReturnsAsync(new PagedResult<User>(users.Take(2).ToList(), users.Count, 1, 10));

        // ACT
        var result = await _sut.SearchByNameAsync("Ana", 1, 2);

        // ASSERT
        result.TotalCount.Should().Be(3);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.User_id).Should().Contain(ExpectedUserIds);
        result.Items.Select(x => x.User_id).Should().NotContain(3);
    }

    [Test(Description = "T15USR - GetByEmail deve devolver nulo quando utilizador nao existe.")]
    public async Task GetByEmailAsync_Should_ReturnNull_When_UserDoesNotExist()
    {
        // ARRANGE
        _userRepository.Setup(r => r.GetByEmailAsync("ghost@tipmolde.pt")).ReturnsAsync((User?)null);

        // ACT
        var result = await _sut.GetByEmailAsync("ghost@tipmolde.pt");

        // ASSERT
        result.Should().BeNull();
    }

    [Test(Description = "T16USR - GetByEmail deve mapear utilizador quando registo existe.")]
    public async Task GetByEmailAsync_Should_MapResponse_When_UserExists()
    {
        // ARRANGE
        _userRepository.Setup(r => r.GetByEmailAsync("ana@tipmolde.pt")).ReturnsAsync(BuildUser(id: 11, nome: "Ana", email: "ana@tipmolde.pt"));

        // ACT
        var result = await _sut.GetByEmailAsync("ana@tipmolde.pt");

        // ASSERT
        result.Should().NotBeNull();
        result!.User_id.Should().Be(11);
    }

    [Test(Description = "T17USR - Update deve falhar quando nenhum campo e enviado.")]
    public async Task UpdateAsync_Should_ThrowArgumentException_When_NoFieldsProvided()
    {
        // ARRANGE
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildUser(id: 5));

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(5, new UpdateUserDto());

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
