using FluentAssertions;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.Repositorio;

namespace TipMolde.Tests.Integracao.Repositorio
{
    [TestFixture]
    [Category("Integration")]
    public sealed class PecaRepositoryTests : RepositoryIntegrationTestBase
    {
        private static readonly string[] ExpectedDesignacoes = ["Placa", "Extrator"];

        [Test(Description = "TPECAREP1 - GetByMoldeId deve devolver pecas paginadas do molde indicado.")]
        public async Task GetByMoldeIdAsync_Should_ReturnPecas_When_MoldeMatches()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.Pecas.AddRangeAsync(
                new Peca { Designacao = "Placa", Molde_id = 1, Prioridade = 1 },
                new Peca { Designacao = "Extrator", Molde_id = 1, Prioridade = 2 },
                new Peca { Designacao = "Postico", Molde_id = 2, Prioridade = 1 });
            await context.SaveChangesAsync();

            var repository = new PecaRepository(context);

            // ACT
            var result = await repository.GetByMoldeIdAsync(1, page: 1, pageSize: 10);

            // ASSERT
            result.TotalCount.Should().Be(2);
            result.Items.Select(p => p.Designacao).Should().Contain(ExpectedDesignacoes);
        }

        [Test(Description = "TPECAREP2 - GetByIds deve remover duplicados e devolver pecas ordenadas por ID.")]
        public async Task GetByIdsAsync_Should_ReturnDistinctOrderedPecas_When_IdsHaveDuplicates()
        {
            // ARRANGE
            await using var context = CreateContext();
            var peca1 = new Peca { Designacao = "Placa", Molde_id = 1 };
            var peca2 = new Peca { Designacao = "Extrator", Molde_id = 1 };
            await context.Pecas.AddRangeAsync(peca1, peca2);
            await context.SaveChangesAsync();

            var repository = new PecaRepository(context);

            // ACT
            var result = await repository.GetByIdsAsync(new[] { peca2.Peca_id, peca1.Peca_id, peca1.Peca_id });

            // ASSERT
            result.Select(p => p.Peca_id).Should().ContainInOrder(peca1.Peca_id, peca2.Peca_id);
        }

        [Test(Description = "TPECAREP3 - GetByMoldeIdWithoutPedidoMaterial deve filtrar por termo de pesquisa e excluir pecas ja pedidas ou com material recebido.")]
        public async Task GetByMoldeIdWithoutPedidoMaterialAsync_Should_FilterBySearchTerm_When_SearchTermMatches()
        {
            // ARRANGE
            await using var context = CreateContext();

            var pecaCorrespondente = new Peca
            {
                Designacao = "Base",
                NumeroPeca = "P-011",
                Referencia = "REF-BASE",
                MaterialDesignacao = "Aco",
                TratamentoTermico = "TT",
                Massa = "0,20kg",
                Observacao = "Observacao base",
                Molde_id = 1,
                Prioridade = 1,
                Quantidade = 1
            };

            var pecaSemMatch = new Peca
            {
                Designacao = "Tampa",
                NumeroPeca = "P-012",
                Referencia = "REF-TAMPA",
                MaterialDesignacao = "Aco",
                Molde_id = 1,
                Prioridade = 2,
                Quantidade = 1
            };

            var pecaJaPedida = new Peca
            {
                Designacao = "Base bloqueada",
                NumeroPeca = "P-013",
                Referencia = "REF-BLOQUEADA",
                Molde_id = 1,
                Prioridade = 3,
                Quantidade = 1
            };

            var pecaMaterialRecebido = new Peca
            {
                Designacao = "Base recebida",
                NumeroPeca = "P-014",
                Referencia = "REF-RECEBIDA",
                Molde_id = 1,
                Prioridade = 4,
                Quantidade = 1,
                MaterialRecebido = true
            };

            await context.Pecas.AddRangeAsync(pecaCorrespondente, pecaSemMatch, pecaJaPedida, pecaMaterialRecebido);
            await context.SaveChangesAsync();

            var pedido = new PedidoMaterial
            {
                DataPedido = DateTime.UtcNow,
                Estado = EstadoPedido.PENDENTE,
                Fornecedor_id = 1
            };

            await context.PedidosMaterial.AddAsync(pedido);
            await context.SaveChangesAsync();

            await context.ItensPedidoMaterial.AddAsync(new ItemPedidoMaterial
            {
                PedidoMaterial_id = pedido.PedidoMaterial_id,
                Peca_id = pecaJaPedida.Peca_id,
                Quantidade = 1
            });
            await context.SaveChangesAsync();

            var repository = new PecaRepository(context);

            // ACT
            var result = await repository.GetByMoldeIdWithoutPedidoMaterialAsync(1, page: 1, pageSize: 10, searchTerm: "Base");

            // ASSERT
            result.TotalCount.Should().Be(1);
            result.Items.Should().ContainSingle(item => item.Peca_id == pecaCorrespondente.Peca_id);
            result.Items.Select(item => item.Designacao).Should().Contain("Base");
        }
    }

}
