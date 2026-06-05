using FluentAssertions;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.Repositorio;

namespace TipMolde.Tests.Integracao.Repositorio
{
    [TestFixture]
    [Category("Integration")]
    public sealed class MoldeRepositoryTests : RepositoryIntegrationTestBase
    {
        [Test(Description = "TMOLREP1 - GetByNumero deve carregar especificacoes tecnicas do molde.")]
        public async Task GetByNumeroAsync_Should_LoadEspecificacoes_When_MoldeExists()
        {
            // ARRANGE
            await using var context = CreateContext();
            var molde = new Molde { Numero = "M-001", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };
            await context.Moldes.AddAsync(molde);
            await context.SaveChangesAsync();

            await context.EspecificacoesTecnicas.AddAsync(new EspecificacoesTecnicas
            {
                Molde_id = molde.Molde_id,
                MaterialMacho = "AISI P20"
            });
            await context.SaveChangesAsync();

            var repository = new MoldeRepository(context);

            // ACT
            var result = await repository.GetByNumeroAsync("M-001");

            // ASSERT
            result.Should().NotBeNull();
            result!.Especificacoes.Should().NotBeNull();
            result.Especificacoes!.MaterialMacho.Should().Be("AISI P20");
        }

        [Test(Description = "TMOLREP2 - GetByEncomendaId deve devolver moldes associados a encomenda.")]
        public async Task GetByEncomendaIdAsync_Should_ReturnMoldes_When_LinkExists()
        {
            // ARRANGE
            await using var context = CreateContext();
            var encomenda = new Encomenda { NumeroEncomendaCliente = "ENC-001" };
            var molde = new Molde { Numero = "M-001", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };

            await context.Encomendas.AddAsync(encomenda);
            await context.Moldes.AddAsync(molde);
            await context.SaveChangesAsync();

            await context.EncomendasMoldes.AddAsync(new EncomendaMolde
            {
                Encomenda_id = encomenda.Encomenda_id,
                Molde_id = molde.Molde_id,
                Quantidade = 1,
                Prioridade = 1,
                DataEntregaPrevista = DateTime.UtcNow.AddDays(10)
            });
            await context.SaveChangesAsync();

            var repository = new MoldeRepository(context);

            // ACT
            var result = await repository.GetByEncomendaIdAsync(encomenda.Encomenda_id, page: 1, pageSize: 10);

            // ASSERT
            result.Items.Should().ContainSingle(m => m.Numero == "M-001");
        }

        [Test(Description = "TMOLREP3 - GetAll deve carregar especificacoes e paginar moldes por ID.")]
        public async Task GetAllAsync_Should_LoadEspecificacoesAndReturnRequestedPage()
        {
            // ARRANGE
            await using var context = CreateContext();
            var molde1 = new Molde { Numero = "M-001", Numero_cavidades = 1, TipoPedido = TipoPedido.NOVO_MOLDE };
            var molde2 = new Molde { Numero = "M-002", Numero_cavidades = 2, TipoPedido = TipoPedido.ALTERACAO };
            await context.Moldes.AddRangeAsync(molde1, molde2);
            await context.SaveChangesAsync();

            await context.EspecificacoesTecnicas.AddRangeAsync(
                new EspecificacoesTecnicas { Molde_id = molde1.Molde_id, MaterialMacho = "P20" },
                new EspecificacoesTecnicas { Molde_id = molde2.Molde_id, MaterialMacho = "H13" });
            await context.SaveChangesAsync();

            var repository = new MoldeRepository(context);

            // ACT
            var result = await repository.GetAllAsync(page: 2, pageSize: 1);

            // ASSERT
            result.TotalCount.Should().Be(2);
            result.Items.Should().ContainSingle(m => m.Numero == "M-002");
            result.Items.Single().Especificacoes!.MaterialMacho.Should().Be("H13");
        }

        [Test(Description = "TMOLREP4 - GetById deve carregar especificacoes do molde.")]
        public async Task GetByIdAsync_Should_LoadEspecificacoes_When_MoldeExists()
        {
            // ARRANGE
            await using var context = CreateContext();
            var molde = new Molde { Numero = "M-ID", Numero_cavidades = 1, TipoPedido = TipoPedido.REPARACAO };
            await context.Moldes.AddAsync(molde);
            await context.SaveChangesAsync();

            await context.EspecificacoesTecnicas.AddAsync(new EspecificacoesTecnicas
            {
                Molde_id = molde.Molde_id,
                MaterialCavidade = "H13"
            });
            await context.SaveChangesAsync();

            var repository = new MoldeRepository(context);

            // ACT
            var result = await repository.GetByIdAsync(molde.Molde_id);

            // ASSERT
            result.Should().NotBeNull();
            result!.Especificacoes!.MaterialCavidade.Should().Be("H13");
        }

        [Test(Description = "TMOLREP5 - AddMoldeWithSpecs deve persistir molde e especificacoes.")]
        public async Task AddMoldeWithSpecsAsync_Should_PersistAggregateParts()
        {
            // ARRANGE
            await using var context = CreateContext();

            var molde = new Molde { Numero = "M-ADD", Numero_cavidades = 2, TipoPedido = TipoPedido.NOVO_MOLDE };
            var specs = new EspecificacoesTecnicas { MaterialMacho = "P20" };

            var repository = new MoldeRepository(context);

            // ACT
            await repository.AddMoldeWithSpecsAsync(molde, specs);

            // ASSERT
            molde.Molde_id.Should().BeGreaterThan(0);
            context.EspecificacoesTecnicas.Should().ContainSingle(e => e.Molde_id == molde.Molde_id && e.MaterialMacho == "P20");
            context.EncomendasMoldes.Should().BeEmpty();
        }
    }
}
