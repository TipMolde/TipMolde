using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller
{
    /// <summary>
    /// Testes de integracao HTTP do controller de FichaProducao.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public sealed class FichaProducaoControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TFPAPI001 - GET /api/fichas-producao/by-encomendamolde devolve ProblemDetails quando a paginacao e invalida.")]
        public async Task GetByEncomendaMoldeId_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/fichas-producao/by-encomendamolde?encomendaMoldeId=3&page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.FichaProducaoService.Verify(
                s => s.GetByEncomendaMoldeIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TFPAPI001A - GET /api/fichas-producao/by-molde devolve ProblemDetails quando a paginacao e invalida.")]
        public async Task GetByMoldeId_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/fichas-producao/by-molde?moldeId=7&page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.FichaProducaoService.Verify(
                s => s.GetByMoldeIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TFPAPI001B - GET /api/fichas-producao/{id} devolve ProblemDetails quando a ficha nao existe.")]
        public async Task GetById_Should_ReturnProblemDetails_When_FichaDoesNotExist()
        {
            // ARRANGE
            Factory.FichaProducaoService
                .Setup(s => s.GetByIdAsync(88))
                .ReturnsAsync((ResponseFichaProducaoDetalheDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/fichas-producao/88");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TFPAPI001C - GET /api/fichas-producao/{id} devolve 200 com o detalhe da ficha quando existe.")]
        public async Task GetById_Should_ReturnOkJson_When_FichaExists()
        {
            // ARRANGE
            var detalhe = new ResponseFichaProducaoDetalheDto
            {
                FichaProducao_id = 8,
                Tipo = TipoFicha.FRM,
                DataCriacao = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
                EncomendaMolde_id = 4,
                NumeroMolde = "M-008",
                NomeCliente = "Cliente Teste"
            };

            Factory.FichaProducaoService
                .Setup(s => s.GetByIdAsync(8))
                .ReturnsAsync(detalhe);

            // ACT
            var response = await Client.GetAsync("/api/fichas-producao/8");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseFichaProducaoDetalheDto>();
            body.Should().BeEquivalentTo(detalhe);
        }

        [Test(Description = "TFPAPI002 - POST /api/fichas-producao/create devolve 201 com a ficha criada quando o request e valido.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = BuildFichaResponse(id: 12, tipo: TipoFicha.FRE);

            Factory.FichaProducaoService
                .Setup(s => s.CreateAsync(It.IsAny<CreateFichaProducaoDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                tipo = TipoFicha.FRE,
                encomendaMolde_id = 7
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/fichas-producao/create", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseFichaProducaoDto>();
            body.Should().BeEquivalentTo(created);
        }


        private static ResponseFichaProducaoDto BuildFichaResponse(
            int id,
            TipoFicha tipo) => new()
            {
                FichaProducao_id = id,
                Tipo = tipo,
                DataCriacao = DateTime.UtcNow,
                EncomendaMolde_id = 7
            };
    }
}
