using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Dtos.OcorrenciaDto;

namespace TipMolde.Tests.Integracao.Controller
{
    /// <summary>
    /// Testes de integracao HTTP do controller de ocorrencias.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public sealed class OcorrenciasControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TOCAPI1 - POST /api/ocorrencias devolve 201 com a linha FOP criada.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseFichaFopLinhaDto
            {
                FichaFopLinha_id = 31,
                FichaFop_id = 5,
                Data = DateTime.UtcNow.Date,
                Ocorrencia = "Paragem",
                Correcao = "Rearranque",
                Responsavel_id = 1,
                Peca_id = 3,
                Molde_id = 7,
                CriadoEm = DateTime.UtcNow
            };

            Factory.OcorrenciasService
                .Setup(s => s.CreateAsync(It.IsAny<CreateOcorrenciaDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                encomendaMolde_id = 99,
                peca_id = 3,
                responsavel_id = 1,
                ocorrencia = "Paragem",
                correcao = "Rearranque"
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/ocorrencias", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseFichaFopLinhaDto>();
            body.Should().BeEquivalentTo(created);
            Factory.OcorrenciasService.Verify(
                s => s.CreateAsync(It.IsAny<CreateOcorrenciaDto>()),
                Times.Once);
        }
    }
}
