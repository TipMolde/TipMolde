using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.FasesProducaoDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class FasesProducaoControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TFPAPI1 - GET /api/fases-producao devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetAll_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/fases-producao?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.FasesProducaoService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TFPAPI2 - GET /api/fases-producao/{id} devolve ProblemDetails quando fase nao existe.")]
        public async Task GetById_Should_ReturnProblemDetails_When_FaseDoesNotExist()
        {
            // ARRANGE
            Factory.FasesProducaoService
                .Setup(s => s.GetByIdAsync(44))
                .ReturnsAsync((ResponseFasesProducaoDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/fases-producao/44");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TFPAPI3 - POST /api/fases-producao devolve 201 quando request e valida.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = new ResponseFasesProducaoDto
            {
                FasesProducao_id = 3,
                Nome = NomeFases.MAQUINACAO,
                Descricao = "Maquinacao"
            };

            Factory.FasesProducaoService
                .Setup(s => s.CreateAsync(It.IsAny<CreateFasesProducaoDto>()))
                .ReturnsAsync(created);

            // ACT
            var response = await Client.PostAsJsonAsync("/api/fases-producao", new { nome = NomeFases.MAQUINACAO, descricao = "Maquinacao" });

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await ReadBodyAsync<ResponseFasesProducaoDto>(response);
            body.Should().BeEquivalentTo(created);
        }
    }
}
