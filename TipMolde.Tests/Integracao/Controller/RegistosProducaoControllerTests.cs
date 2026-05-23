using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.RegistoProducaoDto;
using TipMolde.Application.Interface;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller
{
    /// <summary>
    /// Testes de integracao HTTP do controller de RegistosProducao.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public sealed class RegistosProducaoControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TRPAPI1 - GET /api/RegistosProducao devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetAll_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/RegistosProducao?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.RegistosProducaoService.Verify(
                s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TRPAPI2 - GET /api/RegistosProducao/historico envia paginacao recebida por query ao service.")]
        public async Task GetHistorico_Should_CallServiceWithQueryPagination_When_RequestIsValid()
        {
            // ARRANGE
            var responseItems = new[]
            {
                BuildResponse(id: 1, EstadoProducao.PREPARACAO)
            };

            Factory.RegistosProducaoService
                .Setup(s => s.GetHistoricoAsync(2, 3, 4, 5))
                .ReturnsAsync(new PagedResult<ResponseRegistosProducaoDto>(responseItems, 1, 4, 5));

            // ACT
            var response = await Client.GetAsync("/api/RegistosProducao/historico?faseId=2&pecaId=3&page=4&pageSize=5");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Factory.RegistosProducaoService.Verify(s => s.GetHistoricoAsync(2, 3, 4, 5), Times.Once);
        }

        [Test(Description = "TRPAPI3 - GET /api/RegistosProducao/ultimo devolve 404 quando nao existe historico.")]
        public async Task GetUltimo_Should_ReturnNotFound_When_NoHistoryExists()
        {
            // ARRANGE
            Factory.RegistosProducaoService
                .Setup(s => s.GetUltimoRegistoAsync(2, 3))
                .ReturnsAsync((ResponseRegistosProducaoDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/RegistosProducao/ultimo?faseId=2&pecaId=3");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TRPAPI4 - POST /api/RegistosProducao devolve 201 quando request e valida.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = BuildResponse(id: 10, EstadoProducao.PREPARACAO);

            Factory.RegistosProducaoService
                .Setup(s => s.CreateAsync(It.IsAny<CreateRegistosProducaoDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                peca_id = 3,
                fase_id = 2,
                maquina_id = 5,
                operador_id = 1,
                estado_producao = EstadoProducao.PREPARACAO
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/RegistosProducao", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await ReadBodyAsync<ResponseRegistosProducaoDto>(response);
            body.Should().BeEquivalentTo(created);
        }

        [Test(Description = "TRPAPI5 - GET /api/RegistosProducao/{id} devolve 404 quando registo nao existe.")]
        public async Task GetById_Should_ReturnProblemDetails_When_RegistoDoesNotExist()
        {
            // ARRANGE
            Factory.RegistosProducaoService
                .Setup(s => s.GetByIdAsync(44))
                .ReturnsAsync((ResponseRegistosProducaoDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/RegistosProducao/44");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TRPAPI6 - GET /api/RegistosProducao/historico devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetHistorico_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/RegistosProducao/historico?faseId=2&pecaId=3&page=1&pageSize=0");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.RegistosProducaoService.Verify(
                s => s.GetHistoricoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TRPAPI7 - GET /api/RegistosProducao/ultimo devolve 200 quando existe ultimo registo.")]
        public async Task GetUltimo_Should_ReturnOkJson_When_HistoryExists()
        {
            // ARRANGE
            var registo = BuildResponse(id: 12, EstadoProducao.CONCLUIDO);

            Factory.RegistosProducaoService
                .Setup(s => s.GetUltimoRegistoAsync(2, 3))
                .ReturnsAsync(registo);

            // ACT
            var response = await Client.GetAsync("/api/RegistosProducao/ultimo?faseId=2&pecaId=3");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await ReadBodyAsync<ResponseRegistosProducaoDto>(response);
            body.Should().BeEquivalentTo(registo);
        }

        private static ResponseRegistosProducaoDto BuildResponse(int id, EstadoProducao estado)
        {
            return new ResponseRegistosProducaoDto
            {
                Registo_Producao_id = id,
                Estado_producao = estado,
                Data_hora = DateTime.UtcNow,
                Fase_id = 2,
                Operador_id = 1,
                Peca_id = 3,
                Maquina_id = 5
            };
        }
    }
}
