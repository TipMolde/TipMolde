using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.EncomendaMoldeDto;
using TipMolde.Application.Interface;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller
{
    /// <summary>
    /// Testes de integracao HTTP do controller de EncomendaMolde.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public sealed class EncomendaMoldeControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TENCMAPI1 - GET /api/encomenda-moldes/por-encomenda/{id} devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetByEncomendaId_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/encomenda-moldes/por-encomenda/1?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.EncomendaMoldeService.Verify(
                s => s.GetByEncomendaIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TENCMAPI2 - GET /api/encomenda-moldes/{id} devolve ProblemDetails quando associacao nao existe.")]
        public async Task GetById_Should_ReturnProblemDetails_When_LinkDoesNotExist()
        {
            // ARRANGE
            Factory.EncomendaMoldeService
                .Setup(s => s.GetByIdAsync(44))
                .ReturnsAsync((ResponseEncomendaMoldeDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/encomenda-moldes/44");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TENCMAPI3 - POST /api/encomenda-moldes devolve 201 quando request e valida.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseEncomendaMoldeDto
            {
                EncomendaMolde_id = 5,
                Encomenda_id = 2,
                Molde_id = 7,
                Quantidade = 1,
                Prioridade = 2,
                Estado = EstadoEncomendaMolde.PENDENTE,
                DataEntregaPrevista = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)
            };

            Factory.EncomendaMoldeService
                .Setup(s => s.CreateAsync(It.IsAny<CreateEncomendaMoldeDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                encomenda_id = 2,
                molde_id = 7,
                quantidade = 1,
                prioridade = 2,
                dataEntregaPrevista = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/encomenda-moldes", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await ReadBodyAsync<ResponseEncomendaMoldeDto>(response);
            body.Should().BeEquivalentTo(created);
        }

        [Test(Description = "TENCMAPI4 - GET /api/encomenda-moldes/por-molde/{id} devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetByMoldeId_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/encomenda-moldes/por-molde/3?page=1&pageSize=0");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.EncomendaMoldeService.Verify(
                s => s.GetByMoldeIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TENCMAPI4C - GET /api/encomenda-moldes/encomendas-confirmadas devolve ProblemDetails quando a paginacao e invalida.")]
        public async Task GetByEncomendasConfirmadas_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/encomenda-moldes/encomendas-confirmadas?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.EncomendaMoldeService.Verify(
                s => s.GetByEncomendasConfirmadasAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TENCMAPI4D - GET /api/encomenda-moldes/encomendas-confirmadas devolve 200 com a lista paginada de moldes de encomendas confirmadas.")]
        public async Task GetByEncomendasConfirmadas_Should_ReturnOkJson_When_RequestIsValid()
        {
            // ARRANGE
            var paged = new PagedResult<ResponseEncomendaMoldeDto>(
                new[]
                {
                    new ResponseEncomendaMoldeDto
                    {
                        EncomendaMolde_id = 8,
                        Encomenda_id = 4,
                        Molde_id = 12,
                        Quantidade = 2,
                        Prioridade = 1,
                        Estado = EstadoEncomendaMolde.PENDENTE,
                        DataEntregaPrevista = new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc),
                        NumeroEncomendaCliente = "ENC-004",
                        NumeroMolde = "M-012"
                    }
                },
                1,
                1,
                10);

            Factory.EncomendaMoldeService
                .Setup(s => s.GetByEncomendasConfirmadasAsync(1, 10))
                .ReturnsAsync(paged);

            // ACT
            var response = await Client.GetAsync("/api/encomenda-moldes/encomendas-confirmadas?page=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await ReadBodyAsync<PagedResult<ResponseEncomendaMoldeDto>>(response);
            body.Should().BeEquivalentTo(paged);
        }

        [Test(Description = "TENCMAPI4E1 - GET /api/encomenda-moldes/encomendas-confirmadas-para-desenho devolve 200 com a lista base de moldes aptos.")]
        public async Task GetByEncomendasConfirmadasParaDesenho_Should_ReturnOkJson_When_RequestIsValid()
        {
            // ARRANGE
            var paged = new PagedResult<ResponseEncomendaMoldeDto>(
                new[]
                {
                    new ResponseEncomendaMoldeDto
                    {
                        EncomendaMolde_id = 10,
                        Encomenda_id = 6,
                        Molde_id = 14,
                        Quantidade = 3,
                        Prioridade = 1,
                        Estado = EstadoEncomendaMolde.PENDENTE,
                        DataEntregaPrevista = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                        NumeroEncomendaCliente = "ENC-006",
                        NumeroMolde = "M-014",
                        NomeCliente = "Cliente Base",
                        NomeMolde = "Molde Base"
                    }
                },
                1,
                1,
                10);

            Factory.EncomendaMoldeService
                .Setup(s => s.GetByEncomendasConfirmadasParaDesenhoAsync(1, 10))
                .ReturnsAsync(paged);

            // ACT
            var response = await Client.GetAsync("/api/encomenda-moldes/encomendas-confirmadas-para-desenho?page=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await ReadBodyAsync<PagedResult<ResponseEncomendaMoldeDto>>(response);
            body.Should().BeEquivalentTo(paged);
            Factory.EncomendaMoldeService.Verify(
                s => s.GetByEncomendasConfirmadasParaDesenhoAsync(1, 10),
                Times.Once);
        }

        [Test(Description = "TENCMAPI4E - GET /api/encomenda-moldes/encomendas-confirmadas-para-desenho devolve 200 com resultados filtrados quando searchTerm e enviado.")]
        public async Task GetByEncomendasConfirmadasParaDesenho_Should_ReturnOkJson_When_SearchTermIsProvided()
        {
            // ARRANGE
            var paged = new PagedResult<ResponseEncomendaMoldeDto>(
                new[]
                {
                    new ResponseEncomendaMoldeDto
                    {
                        EncomendaMolde_id = 9,
                        Encomenda_id = 5,
                        Molde_id = 13,
                        Quantidade = 2,
                        Prioridade = 1,
                        Estado = EstadoEncomendaMolde.PENDENTE,
                        DataEntregaPrevista = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                        NumeroEncomendaCliente = "ENC-005",
                        NumeroMolde = "M-013",
                        NomeCliente = "Cliente Desenho",
                        NomeMolde = "Molde Desenho"
                    }
                },
                1,
                1,
                10);

            Factory.EncomendaMoldeService
                .Setup(s => s.SearchByTermForDesenhoAsync("cliente", 1, 10))
                .ReturnsAsync(paged);

            // ACT
            var response = await Client.GetAsync("/api/encomenda-moldes/encomendas-confirmadas-para-desenho?searchTerm=cliente&page=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await ReadBodyAsync<PagedResult<ResponseEncomendaMoldeDto>>(response);
            body.Should().BeEquivalentTo(paged);
            Factory.EncomendaMoldeService.Verify(
                s => s.SearchByTermForDesenhoAsync("cliente", 1, 10),
                Times.Once);
        }

        [Test(Description = "TENCMAPI4A - GET /api/encomenda-moldes/fila-global devolve ProblemDetails quando a paginacao e invalida.")]
        public async Task GetFilaGlobal_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/encomenda-moldes/fila-global?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.EncomendaMoldeService.Verify(
                s => s.GetFilaGlobalAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TENCMAPI4B - GET /api/encomenda-moldes/fila-global devolve 200 com a fila global paginada.")]
        public async Task GetFilaGlobal_Should_ReturnOkJson_When_RequestIsValid()
        {
            // ARRANGE
            var paged = new PagedResult<FilaGlobalMoldeItemDto>(
                new[]
                {
                    new FilaGlobalMoldeItemDto
                    {
                        EncomendaMolde_id = 7,
                        Encomenda_id = 3,
                        Molde_id = 11,
                        Prioridade = 1,
                        DataEntregaPrevista = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc),
                        Quantidade = 2,
                        NumeroEncomendaCliente = "ENC-003",
                        NomeCliente = "Cliente A",
                        NumeroMolde = "M-011",
                        NomeMolde = "Molde A",
                        EstadoEncomenda = "EM_PRODUCAO"
                    }
                },
                1,
                1,
                10);

            Factory.EncomendaMoldeService
                .Setup(s => s.GetFilaGlobalAsync(1, 10))
                .ReturnsAsync(paged);

            // ACT
            var response = await Client.GetAsync("/api/encomenda-moldes/fila-global?page=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await ReadBodyAsync<PagedResult<FilaGlobalMoldeItemDto>>(response);
            body.Should().BeEquivalentTo(paged);
        }

        [Test(Description = "TENCMAPI5 - PUT /api/encomenda-moldes/{id} devolve 204 quando o request e valido.")]
        public async Task Update_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            var payload = new
            {
                quantidade = 4,
                prioridade = 3,
                dataEntregaPrevista = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc)
            };

            // ACT
            var response = await Client.PutAsJsonAsync("/api/encomenda-moldes/8", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.EncomendaMoldeService.Verify(
                s => s.UpdateAsync(8, It.IsAny<UpdateEncomendaMoldeDto>()),
                Times.Once);
        }

        [Test(Description = "TENCMAPI5A - PATCH /api/encomenda-moldes/{id}/estado devolve 204 quando o request e valido.")]
        public async Task UpdateEstado_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            var payload = new
            {
                estado = EstadoEncomendaMolde.CONCLUIDO
            };

            // ACT
            var response = await Client.PatchAsJsonAsync("/api/encomenda-moldes/8/estado", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.EncomendaMoldeService.Verify(
                s => s.UpdateEstadoAsync(8, It.IsAny<UpdateEstadoEncomendaMoldeDto>()),
                Times.Once);
        }

        [Test(Description = "TENCMAPI6 - DELETE /api/encomenda-moldes/{id} devolve 204 quando a remocao e concluida.")]
        public async Task Delete_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE

            // ACT
            var response = await Client.DeleteAsync("/api/encomenda-moldes/12");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.EncomendaMoldeService.Verify(s => s.DeleteAsync(12), Times.Once);
        }
    }
}
