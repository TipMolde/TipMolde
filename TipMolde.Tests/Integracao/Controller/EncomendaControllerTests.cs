using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.EncomendaDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class EncomendaControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TENCAPI1 - GET /api/encomendas devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetAllEncomendas_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/encomendas?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.EncomendaService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TENCAPI2 - GET /api/encomendas/por-numero-cliente devolve ProblemDetails quando numero e vazio.")]
        public async Task GetByNumeroCliente_Should_ReturnProblemDetails_When_NumeroIsBlank()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/encomendas/por-numero-cliente?numero=%20%20");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
        }

        [Test(Description = "TENCAPI3 - POST /api/encomendas devolve 201 e JSON da encomenda criada quando request e valida.")]
        public async Task CreateEncomenda_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = BuildEncomenda(id: 22);
            Factory.EncomendaService
                .Setup(s => s.CreateAsync(It.IsAny<CreateEncomendaDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                cliente_id = 3,
                numeroEncomendaCliente = "ENC-001"
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/encomendas", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await ReadBodyAsync<ResponseEncomendaDto>(response);
            body.Should().BeEquivalentTo(created);
        }

        [Test(Description = "TENCAPI4 - PATCH /api/encomendas/{id}/estado devolve 204 quando request e valida.")]
        public async Task UpdateEstado_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.EncomendaService
                .Setup(s => s.UpdateEstadoAsync(22, It.IsAny<UpdateEstadoEncomendaDto>()))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.PatchAsJsonAsync("/api/encomendas/22/estado", new { estado = EstadoEncomenda.EM_PRODUCAO });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.EncomendaService.Verify(s => s.UpdateEstadoAsync(22, It.IsAny<UpdateEstadoEncomendaDto>()), Times.Once);
        }

        [Test(Description = "TENCAPI5 - GET /api/encomendas/{id} devolve 404 quando encomenda nao existe.")]
        public async Task GetEncomendaById_Should_ReturnProblemDetails_When_EncomendaDoesNotExist()
        {
            // ARRANGE
            Factory.EncomendaService
                .Setup(s => s.GetByIdAsync(44))
                .ReturnsAsync((ResponseEncomendaDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/encomendas/44");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TENCAPI5B - GET /api/encomendas/{id} devolve 403 quando a role nao tem permissao para consultar detalhe comercial.")]
        public async Task GetEncomendaById_Should_ReturnForbidden_When_RoleIsNotAllowed()
        {
            // ARRANGE
            Client.AuthenticateAs("7", "GESTOR_PRODUCAO");

            // ACT
            var response = await Client.GetAsync("/api/encomendas/44");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Factory.EncomendaService.Verify(s => s.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TENCAPI6 - GET /api/encomendas/{id}/moldes devolve 404 quando encomenda nao existe.")]
        public async Task GetEncomendaWithMoldes_Should_ReturnProblemDetails_When_EncomendaDoesNotExist()
        {
            // ARRANGE
            Factory.EncomendaService
                .Setup(s => s.GetEncomendaWithMoldesAsync(44))
                .ReturnsAsync((ResponseEncomendaDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/encomendas/44/moldes");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TENCAPI7 - GET /api/encomendas/por-numero-cliente devolve 404 quando numero nao existe.")]
        public async Task GetByNumeroCliente_Should_ReturnProblemDetails_When_EncomendaDoesNotExist()
        {
            // ARRANGE
            Factory.EncomendaService
                .Setup(s => s.GetByNumeroEncomendaClienteAsync("ENC-404"))
                .ReturnsAsync((ResponseEncomendaDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/encomendas/por-numero-cliente?numero=ENC-404");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TENCAPI8 - GET /api/encomendas/por-concluir devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetEncomendasPorConcluir_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/encomendas/por-concluir?page=1&pageSize=0");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.EncomendaService.Verify(s => s.GetEncomendasPorConcluirAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TENCAPI8B - GET /api/encomendas/em-producao devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetEncomendasEmProducao_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ACT
            var response = await Client.GetAsync("/api/encomendas/em-producao?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.EncomendaService.Verify(s => s.GetEncomendasEmProducaoAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TENCAPI8C - GET /api/encomendas/em-producao/search devolve 403 quando a role nao tem permissao para a pesquisa comercial.")]
        public async Task SearchEncomendasEmProducao_Should_ReturnForbidden_When_RoleIsNotAllowed()
        {
            // ARRANGE
            Client.AuthenticateAs("9", "GESTOR_PRODUCAO");

            // ACT
            var response = await Client.GetAsync("/api/encomendas/em-producao/search?searchTerm=Molde&page=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Factory.EncomendaService.Verify(
                s => s.SearchEncomendasEmProducaoAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TENCAPI9 - PUT /api/encomendas/{id} devolve 204 quando request e valida.")]
        public async Task UpdateEncomenda_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.EncomendaService
                .Setup(s => s.UpdateAsync(22, It.IsAny<UpdateEncomendaDto>()))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.PutAsJsonAsync("/api/encomendas/22", new { nomeServicoCliente = "Servico atualizado" });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.EncomendaService.Verify(s => s.UpdateAsync(22, It.IsAny<UpdateEncomendaDto>()), Times.Once);
        }

        [Test(Description = "TENCAPI10 - DELETE /api/encomendas/{id} devolve 204 quando request e valida.")]
        public async Task DeleteEncomenda_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.EncomendaService
                .Setup(s => s.DeleteAsync(22))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.DeleteAsync("/api/encomendas/22");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.EncomendaService.Verify(s => s.DeleteAsync(22), Times.Once);
        }

        private static ResponseEncomendaDto BuildEncomenda(int id = 1)
        {
            return new ResponseEncomendaDto
            {
                Encomenda_id = id,
                Cliente_id = 3,
                NumeroEncomendaCliente = "ENC-001",
                DataRegisto = DateTime.UtcNow,
                Estado = EstadoEncomenda.CONFIRMADA
            };
        }
    }
}
