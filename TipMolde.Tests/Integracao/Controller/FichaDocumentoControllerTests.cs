using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.FichaDocumentoDto;
using TipMolde.Application.Interface;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class FichaDocumentoControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TFDOCAPI001 - GET /api/fichas/{id}/documentos devolve ProblemDetails quando a paginacao e invalida.")]
        public async Task Listar_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/fichas/5/documentos?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.FichaDocumentoService.Verify(
                s => s.ListarAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TFDOCAPI002 - POST /api/fichas/{id}/documentos/upload devolve ProblemDetails quando o token nao identifica utilizador.")]
        public async Task Upload_Should_ReturnUnauthorizedProblem_When_UserIdIsMissing()
        {
            // ARRANGE
            Client.AuthenticateAs(TestAuthHandler.MissingUserId, "ADMIN");
            using var form = BuildMultipartForm("manual.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new byte[] { 1, 2, 3 });

            // ACT
            var response = await Client.PostAsync("/api/fichas/5/documentos/upload", form);

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "Token invalido");
            Factory.FichaDocumentoService.Verify(
                s => s.UploadAsync(It.IsAny<int>(), It.IsAny<UploadFichaDocumentoDto>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TFDOCAPI003 - POST /api/fichas/{id}/documentos/upload devolve 200 com o documento criado quando o pedido e valido.")]
        public async Task Upload_Should_ReturnOkJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseFichaDocumentoDto
            {
                FichaDocumento_id = 9,
                FichaProducao_id = 5,
                Versao = 3,
                Origem = "MANUAL",
                NomeFicheiro = "manual_v3.xlsx",
                TipoFicheiro = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                CriadoPor_user_id = 1,
                Ativo = true
            };

            Factory.FichaDocumentoService
                .Setup(s => s.UploadAsync(5, It.IsAny<UploadFichaDocumentoDto>(), 1))
                .ReturnsAsync(created);

            using var form = BuildMultipartForm("manual.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", new byte[] { 10, 20, 30, 40 });

            // ACT
            var response = await Client.PostAsync("/api/fichas/5/documentos/upload", form);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ResponseFichaDocumentoDto>();
            body.Should().BeEquivalentTo(created);
            Factory.FichaDocumentoService.Verify(
                s => s.UploadAsync(
                    5,
                    It.Is<UploadFichaDocumentoDto>(dto =>
                        dto.FileName == "manual.xlsx" &&
                        dto.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" &&
                        dto.Content.SequenceEqual(new byte[] { 10, 20, 30, 40 })),
                    1),
                Times.Once);
        }

        [Test(Description = "TFDOCAPI004 - GET /api/fichas/{id}/documentos/{docId}/download devolve o ficheiro pedido.")]
        public async Task Download_Should_ReturnFile_When_RequestIsValid()
        {
            // ARRANGE
            var bytes = new byte[] { 7, 8, 9 };

            Factory.FichaDocumentoService
                .Setup(s => s.DownloadAsync(5, 11))
                .ReturnsAsync(new FichaDocumentoDownloadResultDto
                {
                    Content = bytes,
                    FileName = "manual_v3.pdf",
                    TipoFicheiro = "application/pdf"
                });

            // ACT
            var response = await Client.GetAsync("/api/fichas/5/documentos/11/download");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
            (await response.Content.ReadAsByteArrayAsync()).Should().Equal(bytes);

            var fileName =
                response.Content.Headers.ContentDisposition?.FileNameStar ??
                response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            fileName.Should().Be("manual_v3.pdf");
        }

        [Test(Description = "TFDOCAPI005 - GET /api/fichas/{id}/documentos devolve 200 com a pagina quando o pedido e valido.")]
        public async Task Listar_Should_ReturnOkJson_When_RequestIsValid()
        {
            // ARRANGE
            var paged = new PagedResult<ResponseFichaDocumentoDto>(
                new[]
                {
            new ResponseFichaDocumentoDto
            {
                FichaDocumento_id = 11,
                FichaProducao_id = 5,
                Versao = 2,
                NomeFicheiro = "manual_v2.pdf",
                TipoFicheiro = "application/pdf",
                Origem = "UPLOAD",
                CriadoPor_user_id = 1,
                Ativo = true
            }
                },
                TotalCount: 1,
                CurrentPage: 1,
                PageSize: 10);

            Factory.FichaDocumentoService
                .Setup(s => s.ListarAsync(5, 1, 10))
                .ReturnsAsync(paged);

            // ACT
            var response = await Client.GetAsync("/api/fichas/5/documentos?page=1&pageSize=10");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            Factory.FichaDocumentoService.Verify(s => s.ListarAsync(5, 1, 10), Times.Once);
        }

        [Test(Description = "TFDOCAPI006 - GET /api/fichas/{id}/documentos/{docId}/download devolve ProblemDetails quando o documento nao existe.")]
        public async Task Download_Should_ReturnProblemDetails_When_DocumentDoesNotExist()
        {
            // ARRANGE
            Factory.FichaDocumentoService
                .Setup(s => s.DownloadAsync(5, 404))
                .ThrowsAsync(new KeyNotFoundException("Documento nao encontrado para a ficha indicada."));

            // ACT
            var response = await Client.GetAsync("/api/fichas/5/documentos/404/download");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        private static MultipartFormDataContent BuildMultipartForm(string fileName, string contentType, byte[] fileBytes)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);
            return form;
        }
    }
}
