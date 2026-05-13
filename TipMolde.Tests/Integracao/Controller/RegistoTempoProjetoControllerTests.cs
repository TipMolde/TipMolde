using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.RegistoTempoProjetoDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class RegistoTempoProjetoControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TRTPAPI1 - GET /api/registos-tempo-projeto devolve ProblemDetails quando query e invalida.")]
        public async Task GetHistorico_Should_ReturnProblemDetails_When_QueryIdsAreInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/registos-tempo-projeto?projetoId=0&autorId=1");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.RegistoTempoProjetoService.Verify(
                s => s.GetHistoricoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TRTPAPI2 - POST /api/registos-tempo-projeto devolve 201 quando request e valida.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseRegistoTempoProjetoDto
            {
                Registo_Tempo_Projeto_id = 10,
                Estado_tempo = EstadoTempoProjeto.INICIADO,
                Data_hora = DateTime.UtcNow,
                Projeto_id = 2,
                Autor_id = 1
            };

            Factory.RegistoTempoProjetoService
                .Setup(s => s.CreateRegistoAsync(It.IsAny<CreateRegistoTempoProjetoDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                estado_tempo = EstadoTempoProjeto.INICIADO,
                projeto_id = 2,
                autor_id = 1,
                peca_id = 8
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/registos-tempo-projeto", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseRegistoTempoProjetoDto>();
            body.Should().BeEquivalentTo(created);
        }
    }
}
