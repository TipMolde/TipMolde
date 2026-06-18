using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.RevisaoDto;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class RevisaoControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TREVAPI1 - GET /api/revisoes devolve ProblemDetails quando projetoId e invalido.")]
        public async Task GetByProjeto_Should_ReturnProblemDetails_When_ProjetoIdIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/revisoes?projetoId=0");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.RevisaoService.Verify(
                s => s.GetByProjetoIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TREVAPI2 - POST /api/revisoes devolve 201 quando request e valida.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseRevisaoDto
            {
                Revisao_id = 3,
                NumRevisao = 1,
                DescricaoAlteracoes = "Ajustar extratores",
                DataEnvioCliente = DateTime.UtcNow,
                Projeto_id = 2
            };

            Factory.RevisaoService
                .Setup(s => s.CreateAsync(It.IsAny<CreateRevisaoDto>()))
                .ReturnsAsync(created);

            // ACT
            var response = await Client.PostAsJsonAsync("/api/revisoes", new { descricaoAlteracoes = "Ajustar extratores", projeto_id = 2 });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ResponseRevisaoDto>();
            body.Should().BeEquivalentTo(created);
        }

        [Test(Description = "TREVAPI3 - PUT /api/revisoes/{id}/resposta-cliente devolve ProblemDetails quando rejeicao nao tem feedback.")]
        public async Task UpdateRespostaCliente_Should_ReturnProblemDetails_When_RejectionHasNoFeedback()
        {
            // ARRANGE

            // ACT
            var response = await Client.PutAsJsonAsync("/api/revisoes/3/resposta-cliente", new { aprovado = false });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            Factory.RevisaoService.Verify(
                s => s.UpdateRespostaClienteAsync(It.IsAny<int>(), It.IsAny<UpdateRespostaRevisaoDto>()),
                Times.Never);
        }

        [Test(Description = "TREVAPI4 - GET /api/revisoes devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetByProjeto_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/revisoes?projetoId=2&page=1&pageSize=0");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.RevisaoService.Verify(
                s => s.GetByProjetoIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TREVAPI5 - GET /api/revisoes/{id} devolve 404 quando revisao nao existe.")]
        public async Task GetById_Should_ReturnProblemDetails_When_RevisaoDoesNotExist()
        {
            // ARRANGE
            Factory.RevisaoService
                .Setup(s => s.GetByIdAsync(44))
                .ReturnsAsync((ResponseRevisaoDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/revisoes/44");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TREVAPI6 - PUT /api/revisoes/{id}/resposta-cliente devolve 204 quando request e valida.")]
        public async Task UpdateRespostaCliente_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.RevisaoService
                .Setup(s => s.UpdateRespostaClienteAsync(3, It.IsAny<UpdateRespostaRevisaoDto>()))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.PutAsJsonAsync("/api/revisoes/3/resposta-cliente", new { aprovado = true });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.RevisaoService.Verify(s => s.UpdateRespostaClienteAsync(3, It.IsAny<UpdateRespostaRevisaoDto>()), Times.Once);
        }

        [Test(Description = "TREVAPI6A - PUT multipart /api/revisoes/{id}/resposta-cliente devolve 204 quando a revisao e rejeitada com anexo.")]
        public async Task UpdateRespostaClienteComAnexo_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.RevisaoService
                .Setup(s => s.UpdateRespostaClienteAsync(
                    3,
                    It.IsAny<UpdateRespostaRevisaoDto>(),
                    It.IsAny<byte[]?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()))
                .Returns(Task.CompletedTask);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("false"), "Aprovado");
            content.Add(new StringContent("Feedback com anexo"), "FeedbackTexto");
            var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            content.Add(fileContent, "Anexo", "feedback.pdf");

            // ACT
            var response = await Client.PutAsync("/api/revisoes/3/resposta-cliente", content);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.RevisaoService.Verify(
                s => s.UpdateRespostaClienteAsync(
                    3,
                    It.Is<UpdateRespostaRevisaoDto>(dto =>
                        dto.Aprovado == false &&
                        dto.FeedbackTexto == "Feedback com anexo"),
                    It.Is<byte[]>(bytes => bytes.Length == 3),
                    "feedback.pdf",
                    "application/pdf"),
                Times.Once);
        }

        [Test(Description = "TREVAPI7 - DELETE /api/revisoes/{id} devolve 204 quando request e valida.")]
        public async Task Delete_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.RevisaoService
                .Setup(s => s.DeleteAsync(3))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.DeleteAsync("/api/revisoes/3");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.RevisaoService.Verify(s => s.DeleteAsync(3), Times.Once);
        }
    }
}
