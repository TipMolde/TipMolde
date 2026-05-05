using FluentAssertions;
using TipMolde.Domain.Entities.Producao;
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
    }

}
