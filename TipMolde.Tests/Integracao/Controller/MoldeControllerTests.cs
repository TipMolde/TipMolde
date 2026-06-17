
using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.MoldeDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class MoldeControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TMOLAPI1 - GET /api/moldes/por-numero devolve ProblemDetails quando numero e vazio.")]
        public async Task GetByNumero_Should_ReturnProblemDetails_When_NumeroIsBlank()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/moldes/por-numero?numero=%20%20");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.MoldeService.Verify(s => s.GetByNumeroAsync(It.IsAny<string>()), Times.Never);
        }

        [Test(Description = "TMOLAPI2 - POST /api/moldes devolve 201 quando request e valida.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseMoldeDto
            {
                MoldeId = 9,
                Numero = "M-001",
                Numero_cavidades = 2,
                TipoPedido = TipoPedido.NOVO_MOLDE
            };

            Factory.MoldeService
                .Setup(s => s.CreateAsync(It.IsAny<CreateMoldeDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                numero = "M-001",
                numero_cavidades = 2,
                tipoPedido = TipoPedido.NOVO_MOLDE
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/moldes", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await ReadBodyAsync<ResponseMoldeDto>(response);
            body.Should().BeEquivalentTo(created);
        }

        [Test(Description = "TMOLAPI3 - POST multipart /api/moldes devolve 201 quando request e valida.")]
        public async Task CreateMultipart_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseMoldeDto
            {
                MoldeId = 10,
                Numero = "M-002",
                Numero_cavidades = 3,
                TipoPedido = TipoPedido.NOVO_MOLDE,
                ImagemCapaPath = "Templates/image.png"
            };

            Factory.MoldeService
                .Setup(s => s.CreateAsync(It.IsAny<CreateMoldeDto>(), It.IsAny<byte[]?>(), It.IsAny<string?>()))
                .ReturnsAsync(created);

            using var content = new MultipartFormDataContent
            {
                { new StringContent("M-002"), "numero" },
                { new StringContent("3"), "numero_cavidades" },
                { new StringContent(TipoPedido.NOVO_MOLDE.ToString()), "tipoPedido" }
            };

            // ACT
            var response = await Client.PostAsync("/api/moldes", content);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await ReadBodyAsync<ResponseMoldeDto>(response);
            body.Should().BeEquivalentTo(created);
        }

        [Test(Description = "TMOLAPI3 - GET /api/moldes devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetAll_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/moldes?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.MoldeService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TMOLAPI4 - GET /api/moldes/{id} devolve 404 quando molde nao existe.")]
        public async Task GetById_Should_ReturnProblemDetails_When_MoldeDoesNotExist()
        {
            // ARRANGE
            Factory.MoldeService
                .Setup(s => s.GetByIdAsync(44))
                .ReturnsAsync((ResponseMoldeDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/moldes/44");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TMOLAPI5 - GET /api/moldes/por-encomenda/{id} devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetByEncomendaId_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/moldes/por-encomenda/5?page=1&pageSize=0");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.MoldeService.Verify(s => s.GetByEncomendaIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TMOLAPI6 - GET /api/moldes/{id}/ciclo-vida-pdf devolve ficheiro PDF.")]
        public async Task ExportCicloVidaPdf_Should_ReturnPdfFile_When_ServiceGeneratesReport()
        {
            // ARRANGE
            Factory.RelatorioService
                .Setup(s => s.GerarCicloVidaMoldePdfAsync(9))
                .ReturnsAsync(("PDF"u8.ToArray(), "molde-9.pdf"));

            // ACT
            var response = await Client.GetAsync("/api/moldes/9/ciclo-vida-pdf");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
            response.Content.Headers.ContentDisposition?.FileNameStar.Should().Be("molde-9.pdf");
        }

        [Test(Description = "TMOLAPI7 - PUT /api/moldes/{id} devolve 204 quando request e valida.")]
        public async Task Update_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.MoldeService
                .Setup(s => s.UpdateAsync(9, It.IsAny<UpdateMoldeDto>()))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.PutAsJsonAsync("/api/moldes/9", new { nome = "Molde Atualizado" });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.MoldeService.Verify(s => s.UpdateAsync(9, It.IsAny<UpdateMoldeDto>()), Times.Once);
        }

        [Test(Description = "TMOLAPI8 - DELETE /api/moldes/{id} devolve 204 quando request e valida.")]
        public async Task Delete_Should_ReturnNoContent_When_RequestIsValid()
        {
            // ARRANGE
            Factory.MoldeService
                .Setup(s => s.DeleteAsync(9))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.DeleteAsync("/api/moldes/9");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.MoldeService.Verify(s => s.DeleteAsync(9), Times.Once);
        }
    }
}
