using FluentAssertions;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.Repositorio;

namespace TipMolde.Tests.Integracao.Repositorio
{
    [TestFixture]
    [Category("Integration")]
    public sealed class EncomendaRepositoryTests : RepositoryIntegrationTestBase
    {
        [Test(Description = "TENCREP1 - GetEncomendasPorConcluir deve excluir encomendas concluidas e canceladas.")]
        public async Task GetEncomendasPorConcluirAsync_Should_ExcludeClosedStates()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.Encomendas.AddRangeAsync(
                new Encomenda { NumeroEncomendaCliente = "ENC-001", Estado = EstadoEncomenda.CONFIRMADA },
                new Encomenda { NumeroEncomendaCliente = "ENC-002", Estado = EstadoEncomenda.CONCLUIDA },
                new Encomenda { NumeroEncomendaCliente = "ENC-003", Estado = EstadoEncomenda.CANCELADA });
            await context.SaveChangesAsync();

            var repository = new EncomendaRepository(context);

            // ACT
            var result = await repository.GetEncomendasPorConcluirAsync(page: 1, pageSize: 10);

            // ASSERT
            result.Items.Should().ContainSingle(e => e.NumeroEncomendaCliente == "ENC-001");
        }

        [Test(Description = "TENCREP1B - GetEncomendasEmProducao deve excluir apenas concluidas e carregar cliente.")]
        public async Task GetEncomendasEmProducaoAsync_Should_IncludeCliente_And_ExcludeOnlyConcluidas()
        {
            // ARRANGE
            await using var context = CreateContext();
            var cliente = new Cliente
            {
                Nome = "Cliente Operacional",
                NIF = "509111111",
                Sigla = "COP"
            };

            await context.Clientes.AddAsync(cliente);
            await context.SaveChangesAsync();

            await context.Encomendas.AddRangeAsync(
                new Encomenda { NumeroEncomendaCliente = "ENC-101", Estado = EstadoEncomenda.EM_PRODUCAO, Cliente_id = cliente.Cliente_id },
                new Encomenda { NumeroEncomendaCliente = "ENC-102", Estado = EstadoEncomenda.CANCELADA, Cliente_id = cliente.Cliente_id },
                new Encomenda { NumeroEncomendaCliente = "ENC-103", Estado = EstadoEncomenda.CONCLUIDA, Cliente_id = cliente.Cliente_id });
            await context.SaveChangesAsync();

            var repository = new EncomendaRepository(context);

            // ACT
            var result = await repository.GetEncomendasEmProducaoAsync(page: 1, pageSize: 10);

            // ASSERT
            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(e => e.Estado != EstadoEncomenda.CONCLUIDA);
            result.Items.Should().OnlyContain(e => e.Cliente != null && e.Cliente.Nome == "Cliente Operacional");
        }

        [Test(Description = "TENCREP2 - GetByNumeroEncomendaCliente deve procurar numero normalizado.")]
        public async Task GetByNumeroEncomendaClienteAsync_Should_ReturnEncomenda_When_NumeroHasSpaces()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.Encomendas.AddAsync(new Encomenda { NumeroEncomendaCliente = "ENC-001" });
            await context.SaveChangesAsync();

            var repository = new EncomendaRepository(context);

            // ACT
            var result = await repository.GetByNumeroEncomendaClienteAsync(" ENC-001 ");

            // ASSERT
            result.Should().NotBeNull();
            result!.NumeroEncomendaCliente.Should().Be("ENC-001");
        }

        [Test(Description = "TENCREP3 - GetByEstado deve filtrar e ordenar encomendas por data decrescente.")]
        public async Task GetByEstadoAsync_Should_ReturnPagedResultsOrderedByData_When_EstadoMatches()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.Encomendas.AddRangeAsync(
                new Encomenda { NumeroEncomendaCliente = "ENC-OLD", Estado = EstadoEncomenda.CONFIRMADA, DataRegisto = DateTime.UtcNow.AddDays(-2) },
                new Encomenda { NumeroEncomendaCliente = "ENC-NEW", Estado = EstadoEncomenda.CONFIRMADA, DataRegisto = DateTime.UtcNow.AddDays(-1) },
                new Encomenda { NumeroEncomendaCliente = "ENC-CAN", Estado = EstadoEncomenda.CANCELADA, DataRegisto = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var repository = new EncomendaRepository(context);

            // ACT
            var result = await repository.GetByEstadoAsync(EstadoEncomenda.CONFIRMADA, page: 1, pageSize: 10);

            // ASSERT
            result.TotalCount.Should().Be(2);
            result.Items.Select(e => e.NumeroEncomendaCliente).Should().ContainInOrder("ENC-NEW", "ENC-OLD");
        }

        [Test(Description = "TENCREP4 - ExistsNumeroEncomendaCliente deve respeitar exclusao de encomenda.")]
        public async Task ExistsNumeroEncomendaClienteAsync_Should_RespectExcludedEncomendaId()
        {
            // ARRANGE
            await using var context = CreateContext();
            var encomenda = new Encomenda { NumeroEncomendaCliente = "ENC-001" };
            await context.Encomendas.AddAsync(encomenda);
            await context.SaveChangesAsync();

            var repository = new EncomendaRepository(context);

            // ACT
            var existsWithoutExclude = await repository.ExistsNumeroEncomendaClienteAsync(" ENC-001 ");
            var existsWithExclude = await repository.ExistsNumeroEncomendaClienteAsync(" ENC-001 ", encomenda.Encomenda_id);

            // ASSERT
            existsWithoutExclude.Should().BeTrue();
            existsWithExclude.Should().BeFalse();
        }

        [Test(Description = "TENCREP5 - GetWithMoldes deve carregar associacoes e moldes da encomenda.")]
        public async Task GetWithMoldesAsync_Should_LoadMoldes_When_EncomendaExists()
        {
            // ARRANGE
            await using var context = CreateContext();
            var encomenda = new Encomenda { NumeroEncomendaCliente = "ENC-REL" };
            var molde = new TipMolde.Domain.Entities.Producao.Molde
            {
                Numero = "M-REL",
                Numero_cavidades = 1,
                TipoPedido = TipoPedido.NOVO_MOLDE
            };

            await context.Encomendas.AddAsync(encomenda);
            await context.Moldes.AddAsync(molde);
            await context.SaveChangesAsync();

            await context.EncomendasMoldes.AddAsync(new EncomendaMolde
            {
                Encomenda_id = encomenda.Encomenda_id,
                Molde_id = molde.Molde_id,
                Quantidade = 1,
                Prioridade = 1,
                DataEntregaPrevista = DateTime.UtcNow.AddDays(5)
            });
            await context.SaveChangesAsync();

            var repository = new EncomendaRepository(context);

            // ACT
            var result = await repository.GetWithMoldesAsync(encomenda.Encomenda_id);

            // ASSERT
            result.Should().NotBeNull();
            result!.EncomendasMoldes.Should().ContainSingle();
            result.EncomendasMoldes.Single().Molde!.Numero.Should().Be("M-REL");
        }
    }
}
