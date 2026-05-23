using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.ProjetoDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class ProjetoControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TPROJAPI1 - GET /api/projetos devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetAll_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/projetos?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.ProjetoService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TPROJAPI2 - GET /api/projetos/{id}/com-revisoes devolve ProblemDetails quando projeto nao existe.")]
        public async Task GetWithRevisoes_Should_ReturnProblemDetails_When_ProjetoDoesNotExist()
        {
            // ARRANGE
            Factory.ProjetoService
                .Setup(s => s.GetWithRevisoesAsync(44))
                .ReturnsAsync((ResponseProjetoWithRevisoesDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/projetos/44/com-revisoes");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TPROJAPI3 - POST /api/projetos devolve 201 quando request e valida.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseProjetoDto
            {
                Projeto_id = 6,
                NomeProjeto = "Projeto M-001",
                SoftwareUtilizado = "SolidWorks",
                TipoProjeto = TipoProjeto.PROJETO_3D,
                CaminhoPastaServidor = "\\\\srv\\projetos\\M-001",
                Molde_id = 2
            };

            Factory.ProjetoService
                .Setup(s => s.CreateAsync(It.IsAny<CreateProjetoDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                nomeProjeto = "Projeto M-001",
                softwareUtilizado = "SolidWorks",
                tipoProjeto = TipoProjeto.PROJETO_3D,
                caminhoPastaServidor = "\\\\srv\\projetos\\M-001",
                molde_id = 2
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/projetos", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await ReadBodyAsync<ResponseProjetoDto>(response);
            body.Should().BeEquivalentTo(created);
        }

        [Test(Description = "TPROJAPI4 - GET /api/projetos/{id} devolve 404 quando projeto nao existe.")]
        public async Task GetById_Should_ReturnProblemDetails_When_ProjetoDoesNotExist()
        {
            // ARRANGE
            Factory.ProjetoService
                .Setup(s => s.GetByIdAsync(44))
                .ReturnsAsync((ResponseProjetoDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/projetos/44");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TPROJAPI5 - GET /api/projetos/por-molde/{id} devolve ProblemDetails quando moldeId e invalido.")]
        public async Task GetByMoldeId_Should_ReturnProblemDetails_When_MoldeIdIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/projetos/por-molde/0?page=1&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.ProjetoService.Verify(s => s.GetByMoldeIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TPROJAPI6 - GET /api/projetos/por-molde/{id} devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetByMoldeId_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/projetos/por-molde/5?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.ProjetoService.Verify(s => s.GetByMoldeIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TPROJAPI7 - PUT /api/projetos/{id} devolve 204 quando request e valida.")]
        public async Task Update_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.ProjetoService
                .Setup(s => s.UpdateAsync(6, It.IsAny<UpdateProjetoDto>()))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.PutAsJsonAsync("/api/projetos/6", new { nomeProjeto = "Projeto Atualizado" });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.ProjetoService.Verify(s => s.UpdateAsync(6, It.IsAny<UpdateProjetoDto>()), Times.Once);
        }

        [Test(Description = "TPROJAPI8 - DELETE /api/projetos/{id} devolve 204 quando request e valida.")]
        public async Task Delete_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.ProjetoService
                .Setup(s => s.DeleteAsync(6))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.DeleteAsync("/api/projetos/6");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.ProjetoService.Verify(s => s.DeleteAsync(6), Times.Once);
        }
    }
}
