

using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.MaquinaDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class MaquinaControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TMAQAPI1 - GET /api/Maquina/por-estado devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetByEstado_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/Maquina/por-estado?estado=DISPONIVEL&page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.MaquinaService.Verify(
                s => s.GetByEstadoAsync(It.IsAny<EstadoMaquina>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TMAQAPI2 - POST /api/Maquina devolve 201 quando request e valida.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseMaquinaDto
            {
                Maquina_id = 4,
                Numero = 10,
                NomeModelo = "Makino",
                Estado = EstadoMaquina.DISPONIVEL,
                FaseDedicada_id = 2
            };

            Factory.MaquinaService
                .Setup(s => s.CreateAsync(It.IsAny<CreateMaquinaDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                maquina_id = 4,
                numero = 10,
                nomeModelo = "Makino",
                estado = EstadoMaquina.DISPONIVEL,
                faseDedicada_id = 2
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/Maquina", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await ReadBodyAsync<ResponseMaquinaDto>(response);
            body.Should().BeEquivalentTo(created);
        }

        [Test(Description = "TMAQAPI3 - GET /api/Maquina devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetAll_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/Maquina?page=1&pageSize=0");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.MaquinaService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TMAQAPI4 - GET /api/Maquina/{id} devolve 404 quando maquina nao existe.")]
        public async Task GetById_Should_ReturnProblemDetails_When_MaquinaDoesNotExist()
        {
            // ARRANGE
            Factory.MaquinaService
                .Setup(s => s.GetByIdAsync(44))
                .ReturnsAsync((ResponseMaquinaDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/Maquina/44");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TMAQAPI5 - PUT /api/Maquina/{id} devolve 204 quando request e valida.")]
        public async Task Update_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.MaquinaService
                .Setup(s => s.UpdateAsync(4, It.IsAny<UpdateMaquinaDto>()))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.PutAsJsonAsync("/api/Maquina/4", new { nomeModelo = "Makino Atualizada" });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.MaquinaService.Verify(s => s.UpdateAsync(4, It.IsAny<UpdateMaquinaDto>()), Times.Once);
        }

        [Test(Description = "TMAQAPI6 - DELETE /api/Maquina/{id} devolve 204 quando request e valida.")]
        public async Task Delete_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.MaquinaService
                .Setup(s => s.DeleteAsync(4))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.DeleteAsync("/api/Maquina/4");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.MaquinaService.Verify(s => s.DeleteAsync(4), Times.Once);
        }
    }
}
