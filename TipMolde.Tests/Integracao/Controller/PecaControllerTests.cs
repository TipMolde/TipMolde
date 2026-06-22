

using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.PecaDto;
using TipMolde.Application.Interface;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class PecaControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TPECAAPI1 - GET /api/pecas/por-designacao devolve ProblemDetails quando designacao e vazia.")]
        public async Task GetByDesignacao_Should_ReturnProblemDetails_When_DesignacaoIsBlank()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/pecas/por-designacao?designacao=%20%20&moldeId=1");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.PecaService.Verify(s => s.GetByDesignacaoAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TPECAAPI2 - POST /api/pecas devolve 201 quando request e valida.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponsePecaDto
            {
                PecaId = 11,
                Designacao = "Placa",
                Prioridade = 1,
                Quantidade = 2,
                Molde_id = 5
            };

            Factory.PecaService
                .Setup(s => s.CreateAsync(It.IsAny<CreatePecaDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                designacao = "Placa",
                prioridade = 1,
                quantidade = 2,
                molde_id = 5
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/pecas", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponsePecaDto>();
            body.Should().BeEquivalentTo(created);
        }

        [Test(Description = "TPECAAPI3 - POST /api/pecas/por-molde/{id}/importacao-csv devolve ProblemDetails quando ficheiro falta.")]
        public async Task ImportarCsv_Should_ReturnProblemDetails_When_FileIsMissing()
        {
            // ARRANGE
            using var form = new MultipartFormDataContent();
            using var emptyFile = new ByteArrayContent(Array.Empty<byte>());
            form.Add(emptyFile, "file", "pecas.csv");

            // ACT
            var response = await Client.PostAsync("/api/pecas/por-molde/5/importacao-csv", form);

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.PecaService.Verify(s => s.ImportarCsvAsync(It.IsAny<int>(), It.IsAny<Stream>()), Times.Never);
        }

        [Test(Description = "TPECAAPI4 - GET /api/pecas devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetAll_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/pecas?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.PecaService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TPECAAPI5 - GET /api/pecas/{id} devolve 404 quando peca nao existe.")]
        public async Task GetById_Should_ReturnProblemDetails_When_PecaDoesNotExist()
        {
            // ARRANGE
            Factory.PecaService
                .Setup(s => s.GetByIdAsync(44))
                .ReturnsAsync((ResponsePecaDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/pecas/44");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TPECAAPI6 - GET /api/pecas/por-molde/{id} devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetByMoldeId_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/pecas/por-molde/5?page=1&pageSize=0");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.PecaService.Verify(s => s.GetByMoldeIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TPECAAPI6B - GET /api/pecas/por-molde/{id}/sem-pedido-material encaminha o termo de pesquisa ao servico.")]
        public async Task GetByMoldeIdWithoutPedidoMaterial_Should_ReturnOkJson_When_SearchTermIsProvided()
        {
            // ARRANGE
            var result = new PagedResult<ResponsePecaDto>(
                new[]
                {
                    new ResponsePecaDto
                    {
                        PecaId = 11,
                        Designacao = "Base",
                        Molde_id = 5
                    }
                },
                1,
                2,
                3);

            Factory.PecaService
                .Setup(s => s.GetByMoldeIdWithoutPedidoMaterialAsync(5, 2, 3, "Base"))
                .ReturnsAsync(result);

            // ACT
            var response = await Client.GetAsync("/api/pecas/por-molde/5/sem-pedido-material?page=2&pageSize=3&searchTerm=Base");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Factory.PecaService.Verify(s => s.GetByMoldeIdWithoutPedidoMaterialAsync(5, 2, 3, "Base"), Times.Once);
        }

        [Test(Description = "TPECAAPI7 - GET /api/pecas/por-designacao devolve 404 quando peca nao existe.")]
        public async Task GetByDesignacao_Should_ReturnProblemDetails_When_PecaDoesNotExist()
        {
            // ARRANGE
            Factory.PecaService
                .Setup(s => s.GetByDesignacaoAsync("Placa", 5))
                .ReturnsAsync((ResponsePecaDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/pecas/por-designacao?designacao=Placa&moldeId=5");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TPECAAPI8 - PUT /api/pecas/{id} devolve 204 quando request e valida.")]
        public async Task Update_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.PecaService
                .Setup(s => s.UpdateAsync(11, It.IsAny<UpdatePecaDto>()))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.PutAsJsonAsync("/api/pecas/11", new { designacao = "Placa Atualizada" });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.PecaService.Verify(s => s.UpdateAsync(11, It.IsAny<UpdatePecaDto>()), Times.Once);
        }

        [Test(Description = "TPECAAPI9 - DELETE /api/pecas/{id} devolve 204 quando request e valida.")]
        public async Task Delete_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.PecaService
                .Setup(s => s.DeleteAsync(11))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.DeleteAsync("/api/pecas/11");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.PecaService.Verify(s => s.DeleteAsync(11), Times.Once);
        }
    }

}
