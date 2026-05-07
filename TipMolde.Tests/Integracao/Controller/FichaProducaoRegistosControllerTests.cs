using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Interface;
using TipMolde.Application.Dtos.FichaProducaoDto;

namespace TipMolde.Tests.Integracao.Controller
{
    /// <summary>
    /// Testes de integracao HTTP do controller de linhas manuais das fichas de producao.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public sealed class FichaProducaoRegistosControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TFPAPI005 - GET /api/fichas-producao/{fichaId}/linhas-frm devolve ProblemDetails quando a paginacao e invalida.")]
        public async Task GetLinhasFrm_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/fichas-producao/5/linhas-frm?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.FichaProducaoService.Verify(
                s => s.GetLinhasFrmAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TFPAPI006 - POST /api/fichas-producao/{fichaId}/linhas-frm devolve 201 com a linha criada.")]
        public async Task CreateLinhaFrm_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseFichaFrmLinhaDto
            {
                FichaFrmLinha_id = 11,
                FichaFrm_id = 5,
                Data = DateTime.UtcNow.Date,
                Defeito = "Rebarba",
                Pormenor = "Rebarba na zona lateral",
                Responsavel_id = 1,
                CriadoEm = DateTime.UtcNow
            };

            Factory.FichaProducaoService
                .Setup(s => s.CreateLinhaFrmAsync(5, It.IsAny<CreateFichaFrmLinhaDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                data = DateTime.UtcNow.Date,
                defeito = "Rebarba",
                pormenor = "Rebarba na zona lateral",
                responsavel_id = 1
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/fichas-producao/5/linhas-frm", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseFichaFrmLinhaDto>();
            body.Should().BeEquivalentTo(created);
            Factory.FichaProducaoService.Verify(
                s => s.CreateLinhaFrmAsync(5, It.IsAny<CreateFichaFrmLinhaDto>()),
                Times.Once);
        }

        [Test(Description = "TFPAPI007 - GET /api/fichas-producao/{fichaId}/linhas-fra devolve 200 com a pagina de linhas.")]
        public async Task GetLinhasFra_Should_ReturnOkJson_When_RequestIsValid()
        {
            // ARRANGE
            var result = new PagedResult<ResponseFichaFraLinhaDto>(
                new[]
                {
                    new ResponseFichaFraLinhaDto
                    {
                        FichaFraLinha_id = 21,
                        FichaFra_id = 5,
                        Data = new DateTime(2026, 5, 2),
                        Alteracoes = "Ajuste no macho",
                        Verificado = true,
                        Responsavel_id = 1,
                        CriadoEm = new DateTime(2026, 5, 2, 8, 0, 0, DateTimeKind.Utc)
                    }
                },
                1,
                1,
                10);

            Factory.FichaProducaoService
                .Setup(s => s.GetLinhasFraAsync(5, 1, 10))
                .ReturnsAsync(result);

            // ACT
            var response = await Client.GetAsync("/api/fichas-producao/5/linhas-fra?page=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Factory.FichaProducaoService.Verify(s => s.GetLinhasFraAsync(5, 1, 10), Times.Once);
        }

        [Test(Description = "TFPAPI008 - POST /api/fichas-producao/{fichaId}/linhas-fra devolve 201 com a linha criada.")]
        public async Task CreateLinhaFra_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseFichaFraLinhaDto
            {
                FichaFraLinha_id = 22,
                FichaFra_id = 5,
                Data = DateTime.UtcNow.Date,
                Alteracoes = "Ajuste de cota",
                Verificado = false,
                Responsavel_id = 1,
                CriadoEm = DateTime.UtcNow
            };

            Factory.FichaProducaoService
                .Setup(s => s.CreateLinhaFraAsync(5, It.IsAny<CreateFichaFraLinhaDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                data = DateTime.UtcNow.Date,
                alteracoes = "Ajuste de cota",
                verificado = false,
                responsavel_id = 1
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/fichas-producao/5/linhas-fra", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseFichaFraLinhaDto>();
            body.Should().BeEquivalentTo(created);
            Factory.FichaProducaoService.Verify(
                s => s.CreateLinhaFraAsync(5, It.IsAny<CreateFichaFraLinhaDto>()),
                Times.Once);
        }

        [Test(Description = "TFPAPI009 - GET /api/fichas-producao/{fichaId}/linhas-fop devolve 200 com a pagina de linhas.")]
        public async Task GetLinhasFop_Should_ReturnOkJson_When_RequestIsValid()
        {
            // ARRANGE
            var result = new PagedResult<ResponseFichaFopLinhaDto>(
                new[]
                {
                    new ResponseFichaFopLinhaDto
                    {
                        FichaFopLinha_id = 31,
                        FichaFop_id = 5,
                        Data = new DateTime(2026, 5, 3),
                        Ocorrencia = "Paragem",
                        Correcao = "Rearranque",
                        Responsavel_id = 1,
                        CriadoEm = new DateTime(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc)
                    }
                },
                1,
                1,
                10);

            Factory.FichaProducaoService
                .Setup(s => s.GetLinhasFopAsync(5, 1, 10))
                .ReturnsAsync(result);

            // ACT
            var response = await Client.GetAsync("/api/fichas-producao/5/linhas-fop?page=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Factory.FichaProducaoService.Verify(s => s.GetLinhasFopAsync(5, 1, 10), Times.Once);
        }

        [Test(Description = "TFPAPI010 - POST /api/fichas-producao/{fichaId}/linhas-fop devolve 201 com a linha criada.")]
        public async Task CreateLinhaFop_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseFichaFopLinhaDto
            {
                FichaFopLinha_id = 32,
                FichaFop_id = 5,
                Data = DateTime.UtcNow.Date,
                Ocorrencia = "Batida",
                Correcao = "Curso revisto",
                Responsavel_id = 1,
                CriadoEm = DateTime.UtcNow
            };

            Factory.FichaProducaoService
                .Setup(s => s.CreateLinhaFopAsync(5, It.IsAny<CreateFichaFopLinhaDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                data = DateTime.UtcNow.Date,
                ocorrencia = "Batida",
                correcao = "Curso revisto",
                responsavel_id = 1
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/fichas-producao/5/linhas-fop", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseFichaFopLinhaDto>();
            body.Should().BeEquivalentTo(created);
            Factory.FichaProducaoService.Verify(
                s => s.CreateLinhaFopAsync(5, It.IsAny<CreateFichaFopLinhaDto>()),
                Times.Once);
        }
    }
}
