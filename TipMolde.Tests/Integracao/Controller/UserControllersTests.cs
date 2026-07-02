using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.UserDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller;

[TestFixture]
[Category("Integration")]
public sealed class UserControllerTests : ControllerHttpTestBase
{
    [Test(Description = "TUSERAPI1 - GET /api/users devolve 401 quando pedido nao esta autenticado.")]
    public async Task GetAllUsers_Should_ReturnUnauthorized_When_RequestIsAnonymous()
    {
        // ARRANGE
        Client.DefaultRequestHeaders.Authorization = null;

        // ACT
        var response = await Client.GetAsync("/api/users");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        Factory.UserManagementService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test(Description = "TUSERAPI2 - GET /api/users devolve ProblemDetails quando paginacao e invalida.")]
    public async Task GetAllUsers_Should_ReturnProblemDetails_When_PaginationIsInvalid()
    {
        // ARRANGE

        // ACT
        var response = await Client.GetAsync("/api/users?page=0&pageSize=10");

        // ASSERT
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
        Factory.UserManagementService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test(Description = "TUSERAPI3 - GET /api/users/{id} devolve ProblemDetails quando utilizador nao existe.")]
    public async Task GetUserById_Should_ReturnProblemDetails_When_UserDoesNotExist()
    {
        // ARRANGE
        Factory.UserManagementService
            .Setup(s => s.GetByIdAsync(44))
            .ReturnsAsync((ResponseUserDto?)null);

        // ACT
        var response = await Client.GetAsync("/api/users/44");

        // ASSERT
        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
    }

    [Test(Description = "TUSERAPI3.1 - GET /api/users/me devolve o utilizador autenticado quando o token e valido.")]
    public async Task GetCurrentUser_Should_ReturnCurrentUser_When_RequestIsAuthenticated()
    {
        // ARRANGE
        Client.AuthenticateAs("7", "GESTOR_PRODUCAO");
        var currentUser = new ResponseUserDto
        {
            User_id = 7,
            Nome = "Gestor Producao",
            Email = "gestor@tipmolde.pt",
            Role = "GESTOR_PRODUCAO"
        };

        Factory.UserManagementService
            .Setup(s => s.GetCurrentAsync(7))
            .ReturnsAsync(currentUser);

        // ACT
        var response = await Client.GetAsync("/api/users/me");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadBodyAsync<ResponseUserDto>(response);
        body.Should().BeEquivalentTo(currentUser);
        Factory.UserManagementService.Verify(s => s.GetCurrentAsync(7), Times.Once);
    }

    [Test(Description = "TUSERAPI3.2 - GET /api/users/me devolve 401 quando o token nao contem utilizador.")]
    public async Task GetCurrentUser_Should_ReturnUnauthorized_When_UserClaimIsMissing()
    {
        // ARRANGE
        Client.AuthenticateAs(TestAuthHandler.MissingUserId, "GESTOR_PRODUCAO");

        // ACT
        var response = await Client.GetAsync("/api/users/me");

        // ASSERT
        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "Nao autorizado");
        Factory.UserManagementService.Verify(s => s.GetCurrentAsync(It.IsAny<int>()), Times.Never);
    }

    [Test(Description = "TUSERAPI4 - POST /api/users devolve 201 quando request e valida.")]
    public async Task CreateUser_Should_ReturnCreatedJson_When_RequestIsValid()
    {
        // ARRANGE
        var created = new ResponseUserDto
        {
            User_id = 12,
            Nome = "Ana Silva",
            Email = "ana@tipmolde.pt",
            Role = "ADMIN"
        };

        Factory.UserManagementService
            .Setup(s => s.CreateAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(created);

        var payload = new
        {
            nome = "Ana Silva",
            email = "ana@tipmolde.pt",
            password = "Password123!",
            role = UserRole.ADMIN
        };

        // ACT
        var response = await Client.PostAsJsonAsync("/api/users", payload);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ResponseUserDto>();
        body.Should().BeEquivalentTo(created);
    }

    [Test(Description = "TUSERAPI5 - PUT /api/users/{id} devolve 403 quando utilizador nao admin altera outra conta.")]
    public async Task UpdateUser_Should_ReturnForbidden_When_NonAdminUpdatesAnotherUser()
    {
        // ARRANGE
        Client.AuthenticateAs("2", "GESTOR_PRODUCAO");
        Factory.UserManagementService
            .Setup(s => s.GetByIdAsync(2))
            .ReturnsAsync(new ResponseUserDto
            {
                User_id = 2,
                Nome = "Gestor Producao",
                Email = "gestor@tipmolde.pt",
                Role = "GESTOR_PRODUCAO"
            });

        // ACT
        var response = await Client.PutAsJsonAsync("/api/users/5", new { nome = "Novo Nome" });

        // ASSERT
        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "Proibido");
        Factory.UserManagementService.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateUserDto>()), Times.Never);
    }

    [Test(Description = "TUSERAPI6 - GET /api/users/search devolve ProblemDetails quando paginacao e invalida.")]
    public async Task SearchByName_Should_ReturnProblemDetails_When_PaginationIsInvalid()
    {
        // ARRANGE

        // ACT
        var response = await Client.GetAsync("/api/users/search?searchTerm=ana&page=0&pageSize=10");

        // ASSERT
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
        Factory.UserManagementService.Verify(
            s => s.SearchByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    [Test(Description = "TUSERAPI7 - PUT /api/users/{id} devolve 401 quando token nao contem utilizador.")]
    public async Task UpdateUser_Should_ReturnUnauthorized_When_UserClaimIsMissing()
    {
        // ARRANGE
        Client.AuthenticateAs(TestAuthHandler.MissingUserId, "GESTOR_PRODUCAO");

        // ACT
        var response = await Client.PutAsJsonAsync("/api/users/5", new { nome = "Novo Nome" });

        // ASSERT
        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "Nao autorizado");
        Factory.UserManagementService.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateUserDto>()), Times.Never);
    }

    [Test(Description = "TUSERAPI8 - PUT /api/users/{id} devolve 204 quando utilizador atualiza a propria conta.")]
    public async Task UpdateUser_Should_ReturnNoContent_When_UserUpdatesSelf()
    {
        // ARRANGE
        Client.AuthenticateAs("5", "GESTOR_PRODUCAO");
        Factory.UserManagementService
            .Setup(s => s.GetByIdAsync(5))
            .ReturnsAsync(new ResponseUserDto
            {
                User_id = 5,
                Nome = "Gestor Producao",
                Email = "gestor@tipmolde.pt",
                Role = "GESTOR_PRODUCAO"
            });
        Factory.UserManagementService
            .Setup(s => s.UpdateAsync(5, It.IsAny<UpdateUserDto>()))
            .Returns(Task.CompletedTask);

        // ACT
        var response = await Client.PutAsJsonAsync("/api/users/5", new { nome = "Nome Atualizado" });

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Factory.UserManagementService.Verify(s => s.UpdateAsync(5, It.IsAny<UpdateUserDto>()), Times.Once);
    }

    [Test(Description = "TUSERAPI9 - PUT /api/users/{id}/role devolve 204 quando admin altera perfil.")]
    public async Task ChangeRole_Should_ReturnNoContent_When_RequestIsValid()
    {
        // ARRANGE
        Factory.UserManagementService
            .Setup(s => s.ChangeRoleAsync(5, UserRole.GESTOR_DESENHO))
            .Returns(Task.CompletedTask);

        // ACT
        var response = await Client.PutAsJsonAsync("/api/users/5/role", new { role = UserRole.GESTOR_DESENHO });

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Factory.UserManagementService.Verify(s => s.ChangeRoleAsync(5, UserRole.GESTOR_DESENHO), Times.Once);
    }

    [Test(Description = "TUSERAPI10 - DELETE /api/users/{id} devolve 204 quando admin remove utilizador.")]
    public async Task DeleteUser_Should_ReturnNoContent_When_RequestIsValid()
    {
        // ARRANGE
        Factory.UserManagementService
            .Setup(s => s.DeleteAsync(5))
            .Returns(Task.CompletedTask);

        // ACT
        var response = await Client.DeleteAsync("/api/users/5");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Factory.UserManagementService.Verify(s => s.DeleteAsync(5), Times.Once);
    }
}

[TestFixture]
[Category("Integration")]
public sealed class UserPasswordControllerIntegrationTests : ControllerHttpTestBase
{
    [Test(Description = "TUSERPASSAPI1 - PUT /api/users/me/password devolve 204 quando request e valida.")]
    public async Task ChangePassword_Should_ReturnNoContent_When_RequestIsValid()
    {
        // ARRANGE
        Client.AuthenticateAs("7", "GESTOR_PRODUCAO");
        Factory.PasswordService
            .Setup(s => s.ChangePasswordAsync(7, "Atual123!", "Nova12345!"))
            .Returns(Task.CompletedTask);

        var payload = new
        {
            currentPassword = "Atual123!",
            newPassword = "Nova12345!"
        };

        // ACT
        var response = await Client.PutAsJsonAsync("/api/users/me/password", payload);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Factory.PasswordService.Verify(s => s.ChangePasswordAsync(7, "Atual123!", "Nova12345!"), Times.Once);
    }

    [Test(Description = "TUSERPASSAPI2 - PUT /api/users/me/password devolve 401 quando token nao contem utilizador.")]
    public async Task ChangePassword_Should_ReturnUnauthorized_When_UserClaimIsMissing()
    {
        // ARRANGE
        Client.AuthenticateAs(TestAuthHandler.MissingUserId, "GESTOR_PRODUCAO");

        var payload = new
        {
            currentPassword = "Atual123!",
            newPassword = "Nova12345!"
        };

        // ACT
        var response = await Client.PutAsJsonAsync("/api/users/me/password", payload);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        Factory.PasswordService.Verify(
            s => s.ChangePasswordAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test(Description = "TUSERPASSAPI3 - PUT /api/users/{id}/password/reset devolve 204 quando admin repoe password.")]
    public async Task ResetPassword_Should_ReturnNoContent_When_RequestIsValid()
    {
        // ARRANGE
        Factory.PasswordService
            .Setup(s => s.ResetPasswordAsync(9, "Nova12345!"))
            .Returns(Task.CompletedTask);

        // ACT
        var response = await Client.PutAsJsonAsync("/api/users/9/password/reset", new { newPassword = "Nova12345!" });

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Factory.PasswordService.Verify(s => s.ResetPasswordAsync(9, "Nova12345!"), Times.Once);
    }
}
