using FluentAssertions;
using Moq;
using System.Net;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class RelatorioControllerTests : ControllerHttpTestBase
    {
        [TestCase("/api/fichas-producao/encomendas-moldes/4/export/flt", "FLT", 4, Description = "TRLAPI001 - Export FLT devolve Excel quando o request e valido.")]
        [TestCase("/api/fichas-producao/5/export/fre", "FRE", 5, Description = "TRLAPI002 - Export FRE devolve Excel quando o request e valido.")]
        [TestCase("/api/fichas-producao/6/export/frm", "FRM", 6, Description = "TRLAPI003 - Export FRM devolve Excel quando o request e valido.")]
        [TestCase("/api/fichas-producao/7/export/fra", "FRA", 7, Description = "TRLAPI004 - Export FRA devolve Excel quando o request e valido.")]
        [TestCase("/api/fichas-producao/8/export/fop", "FOP", 8, Description = "TRLAPI005 - Export FOP devolve Excel quando o request e valido.")]
        public async Task Export_Should_ReturnExcelFile_When_RequestIsValid(string route, string tipo, int id)
        {
            // ARRANGE
            var bytes = new byte[] { 1, 3, 5, 7 };
            var fileName = $"ficha_{tipo}_{id}.xlsx";
            SetupExport(tipo, id, bytes, fileName);

            // ACT
            var response = await Client.GetAsync(route);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            (await response.Content.ReadAsByteArrayAsync()).Should().Equal(bytes);

            var returnedFileName =
                response.Content.Headers.ContentDisposition?.FileNameStar ??
                response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            returnedFileName.Should().Be(fileName);
        }

        [Test(Description = "TRLAPI006 - Export devolve ProblemDetails quando o token nao identifica utilizador.")]
        public async Task Export_Should_ReturnUnauthorizedProblem_When_UserIdIsMissing()
        {
            // ARRANGE
            Client.AuthenticateAs(TestAuthHandler.MissingUserId, "ADMIN");

            // ACT
            var response = await Client.GetAsync("/api/fichas-producao/6/export/frm");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "Token invalido");
            Factory.RelatorioService.Verify(
                s => s.GerarFichaExcelFRMAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        [Test(Description = "TRLAPI007 - Export devolve 403 quando o utilizador autenticado nao tem role ADMIN.")]
        public async Task Export_Should_ReturnForbidden_When_UserIsNotAdmin()
        {
            // ARRANGE
            Client.AuthenticateAs("1", "GESTOR_PRODUCAO");

            // ACT
            var response = await Client.GetAsync("/api/fichas-producao/6/export/frm");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Factory.RelatorioService.Verify(
                s => s.GerarFichaExcelFRMAsync(It.IsAny<int>(), It.IsAny<int>()),
                Times.Never);
        }

        private void SetupExport(string tipo, int id, byte[] bytes, string fileName)
        {
            switch (tipo)
            {
                case "FLT":
                    Factory.RelatorioService.Setup(s => s.GerarFichaExcelFLTAsync(id, 1)).ReturnsAsync((bytes, fileName));
                    break;
                case "FRE":
                    Factory.RelatorioService.Setup(s => s.GerarFichaExcelFREAsync(id, 1)).ReturnsAsync((bytes, fileName));
                    break;
                case "FRM":
                    Factory.RelatorioService.Setup(s => s.GerarFichaExcelFRMAsync(id, 1)).ReturnsAsync((bytes, fileName));
                    break;
                case "FRA":
                    Factory.RelatorioService.Setup(s => s.GerarFichaExcelFRAAsync(id, 1)).ReturnsAsync((bytes, fileName));
                    break;
                case "FOP":
                    Factory.RelatorioService.Setup(s => s.GerarFichaExcelFOPAsync(id, 1)).ReturnsAsync((bytes, fileName));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de exportacao nao suportado.");
            }
        }
    }
}
