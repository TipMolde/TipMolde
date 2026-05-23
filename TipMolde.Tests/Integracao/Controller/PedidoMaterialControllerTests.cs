using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using TipMolde.Application.Dtos.PedidoMaterialDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Integracao.Controller
{
    [TestFixture]
    [Category("Integration")]
    public sealed class PedidoMaterialControllerTests : ControllerHttpTestBase
    {
        [Test(Description = "TPMAPI1 - GET /api/pedidos-material devolve ProblemDetails quando paginacao e invalida.")]
        public async Task GetAll_Should_ReturnProblemDetails_When_PaginationIsInvalid()
        {
            // ARRANGE

            // ACT
            var response = await Client.GetAsync("/api/pedidos-material?page=0&pageSize=10");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Pedido invalido");
            Factory.PedidoMaterialService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test(Description = "TPMAPI2 - GET /api/pedidos-material/{id} devolve ProblemDetails quando pedido nao existe.")]
        public async Task GetById_Should_ReturnProblemDetails_When_PedidoDoesNotExist()
        {
            // ARRANGE
            Factory.PedidoMaterialService
                .Setup(s => s.GetByIdAsync(44))
                .ReturnsAsync((ResponsePedidoMaterialDto?)null);

            // ACT
            var response = await Client.GetAsync("/api/pedidos-material/44");

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.NotFound, "Recurso nao encontrado");
        }

        [Test(Description = "TPMAPI3 - POST /api/pedidos-material devolve 201 e JSON do pedido criado quando request e valida.")]
        public async Task Create_Should_ReturnCreatedJson_When_RequestIsValid()
        {
            // ARRANGE
            var created = BuildPedidoMaterial(id: 99);
            Factory.PedidoMaterialService
                .Setup(s => s.CreateAsync(It.IsAny<CreatePedidoMaterialDto>()))
                .ReturnsAsync(created);

            var payload = new
            {
                fornecedor_id = 10,
                itens = new[]
                {
                new { peca_id = 100, quantidade = 3 }
            }
            };

            // ACT
            var response = await Client.PostAsJsonAsync("/api/pedidos-material", payload);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await ReadBodyAsync<ResponsePedidoMaterialDto>(response);
            body.Should().BeEquivalentTo(created);
        }

        [Test(Description = "TPMAPI4 - PUT /api/pedidos-material/{id}/rececao usa utilizador autenticado e devolve 204.")]
        public async Task RegistarRececao_Should_UseAuthenticatedUserId_When_RequestIsValid()
        {
            // ARRANGE
            Client.AuthenticateAs("7", "ADMIN");
            Factory.PedidoMaterialService
                .Setup(s => s.RegistarRececaoAsync(25, 7))
                .Returns(Task.CompletedTask);

            // ACT
            var response = await Client.PutAsync("/api/pedidos-material/25/rececao", null);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            Factory.PedidoMaterialService.Verify(s => s.RegistarRececaoAsync(25, 7), Times.Once);
        }

        [Test(Description = "TPMAPI5 - PUT /api/pedidos-material/{id}/rececao devolve ProblemDetails quando token nao tem utilizador.")]
        public async Task RegistarRececao_Should_ReturnProblemDetails_When_UserClaimIsMissing()
        {
            // ARRANGE
            Client.AuthenticateAs(TestAuthHandler.MissingUserId, "ADMIN");

            // ACT
            var response = await Client.PutAsync("/api/pedidos-material/25/rececao", null);

            // ASSERT
            await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "Nao autorizado");
            Factory.PedidoMaterialService.Verify(s => s.RegistarRececaoAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        private static ResponsePedidoMaterialDto BuildPedidoMaterial(int id = 1)
        {
            return new ResponsePedidoMaterialDto
            {
                PedidoMaterialId = id,
                DataPedido = DateTime.UtcNow,
                Estado = EstadoPedido.PENDENTE,
                FornecedorId = 10,
                Itens =
            {
                new ResponseItemPedidoMaterialDto
                {
                    ItemId = 1,
                    PecaId = 100,
                    Quantidade = 3
                }
            }
            };
        }
    }

}
