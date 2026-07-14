using FluentAssertions;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.Repositorio;

namespace TipMolde.Tests.Integracao.Repositorio
{
    /// <summary>
    /// Testes de integracao do repositorio de RegistosProducao.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public sealed class RegistosProducaoRepositoryTests : RepositoryIntegrationTestBase
    {
        [Test(Description = "TRPREP1 - GetHistorico devolve registos filtrados por fase e peca em ordem cronologica.")]
        public async Task GetHistoricoAsync_Should_ReturnOrderedHistory_When_FaseAndPecaMatch()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.RegistosProducao.AddRangeAsync(
                BuildRegisto(faseId: 1, pecaId: 1, EstadoProducao.EM_CURSO, DateTime.UtcNow.AddHours(-1)),
                BuildRegisto(faseId: 1, pecaId: 1, EstadoProducao.PREPARACAO, DateTime.UtcNow.AddHours(-2)),
                BuildRegisto(faseId: 2, pecaId: 1, EstadoProducao.PREPARACAO, DateTime.UtcNow.AddHours(-3)));
            await context.SaveChangesAsync();

            var repository = new RegistosProducaoRepository(context);

            // ACT
            var result = await repository.GetHistoricoAsync(faseId: 1, pecaId: 1, page: 1, pageSize: 10);

            // ASSERT
            result.TotalCount.Should().Be(2);
            result.Items.Select(r => r.Estado_producao)
                .Should().ContainInOrder(EstadoProducao.PREPARACAO, EstadoProducao.EM_CURSO);
        }

        [Test(Description = "TRPREP2 - GetUltimoRegisto devolve o registo mais recente da peca na fase.")]
        public async Task GetUltimoRegistoAsync_Should_ReturnLatestRecord_When_HistoryExists()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.RegistosProducao.AddRangeAsync(
                BuildRegisto(faseId: 1, pecaId: 1, EstadoProducao.PREPARACAO, DateTime.UtcNow.AddHours(-2)),
                BuildRegisto(faseId: 1, pecaId: 1, EstadoProducao.EM_CURSO, DateTime.UtcNow.AddHours(-1)),
                BuildRegisto(faseId: 1, pecaId: 2, EstadoProducao.CONCLUIDO, DateTime.UtcNow));
            await context.SaveChangesAsync();

            var repository = new RegistosProducaoRepository(context);

            // ACT
            var result = await repository.GetUltimoRegistoAsync(faseId: 1, pecaId: 1);

            // ASSERT
            result.Should().NotBeNull();
            result!.Estado_producao.Should().Be(EstadoProducao.EM_CURSO);
        }

        [Test(Description = "TRPREP3 - GetByMaquina devolve registos filtrados por maquina e paginados.")]
        public async Task GetByMaquinaAsync_Should_ReturnPagedResults_When_MaquinaMatches()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.RegistosProducao.AddRangeAsync(
                BuildRegisto(faseId: 1, pecaId: 1, EstadoProducao.PREPARACAO, DateTime.UtcNow.AddHours(-3), maquinaId: 7),
                BuildRegisto(faseId: 1, pecaId: 1, EstadoProducao.EM_CURSO, DateTime.UtcNow.AddHours(-2), maquinaId: 7),
                BuildRegisto(faseId: 1, pecaId: 2, EstadoProducao.PREPARACAO, DateTime.UtcNow.AddHours(-1), maquinaId: 8));
            await context.SaveChangesAsync();

            var repository = new RegistosProducaoRepository(context);

            // ACT
            var result = await repository.GetByMaquinaAsync(maquinaId: 7, page: 1, pageSize: 1);

            // ASSERT
            result.TotalCount.Should().Be(2);
            result.Items.Should().ContainSingle();
            result.Items.Single().Estado_producao.Should().Be(EstadoProducao.EM_CURSO);
        }

        [Test(Description = "TRPREP4 - AddWithMachineState persiste registo e estado da maquina na mesma operacao.")]
        public async Task AddWithMachineStateAsync_Should_PersistRecordAndMachineState_When_MachineIsProvided()
        {
            // ARRANGE
            await using var context = CreateContext();
            var maquina = new Maquina
            {
                Numero = 10,
                NomeModelo = "Makino",
                Estado = EstadoMaquina.DISPONIVEL,
                FaseDedicada_id = 1
            };

            await context.Maquinas.AddAsync(maquina);
            await context.SaveChangesAsync();

            maquina.Estado = EstadoMaquina.EM_USO;
            var registo = BuildRegisto(
                faseId: 1,
                pecaId: 1,
                EstadoProducao.PREPARACAO,
                DateTime.UtcNow,
                maquina.Maquina_id);

            var repository = new RegistosProducaoRepository(context);

            // ACT
            var result = await repository.AddWithMachineStateAsync(registo, maquina, pecaToUpdate: null);

            // ASSERT
            result.Registo_Producao_id.Should().BeGreaterThan(0);
            context.RegistosProducao.Should().ContainSingle(r => r.Maquina_id == maquina.Maquina_id);
            context.Maquinas.Single(m => m.Maquina_id == maquina.Maquina_id).Estado.Should().Be(EstadoMaquina.EM_USO);
        }

        [Test(Description = "TRPREP4B - AddWithMachineState ignora navegacoes de fase destacadas para evitar conflitos de tracking.")]
        public async Task AddWithMachineStateAsync_Should_Persist_When_DetachedPhaseNavigationsShareKeys()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.Fases_Producao.AddRangeAsync(
                new FasesProducao { Fases_producao_id = 1, Nome = NomeFases.MAQUINACAO, Descricao = "Maquinacao" },
                new FasesProducao { Fases_producao_id = 2, Nome = NomeFases.EROSAO, Descricao = "Erosao" });
            await context.Maquinas.AddAsync(new Maquina
            {
                Maquina_id = 10,
                Numero = 10,
                NomeModelo = "Makino",
                Estado = EstadoMaquina.DISPONIVEL,
                FaseDedicada_id = 1
            });
            await context.Pecas.AddAsync(new Peca
            {
                Peca_id = 20,
                Designacao = "Peca 20",
                Prioridade = 1,
                Quantidade = 1,
                MaterialRecebido = true,
                Molde_id = 1,
                ProximaFase_id = 1
            });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            _ = await context.Maquinas.FindAsync(10);
            _ = await context.Fases_Producao.FindAsync(1);

            var maquina = new Maquina
            {
                Maquina_id = 10,
                Numero = 10,
                NomeModelo = "Makino",
                Estado = EstadoMaquina.EM_USO,
                FaseDedicada_id = 1,
                FaseDedicada = new FasesProducao { Fases_producao_id = 1, Nome = NomeFases.MAQUINACAO, Descricao = "Detached" }
            };
            var peca = new Peca
            {
                Peca_id = 20,
                Designacao = "Peca 20",
                Prioridade = 1,
                Quantidade = 1,
                MaterialRecebido = true,
                Molde_id = 1,
                ProximaFase_id = 1,
                ProximaFase = new FasesProducao { Fases_producao_id = 1, Nome = NomeFases.MAQUINACAO, Descricao = "Detached duplicate" }
            };
            var registo = BuildRegisto(
                faseId: 1,
                pecaId: 20,
                EstadoProducao.CONCLUIDO,
                DateTime.UtcNow,
                maquinaId: 10);

            var repository = new RegistosProducaoRepository(context);

            // ACT
            var result = await repository.AddWithMachineStateAsync(registo, maquina, peca);

            // ASSERT
            result.Registo_Producao_id.Should().BeGreaterThan(0);
            context.Maquinas.Single(m => m.Maquina_id == 10).Estado.Should().Be(EstadoMaquina.EM_USO);
            context.Pecas.Single(p => p.Peca_id == 20).ProximaFase_id.Should().Be(1);
        }

        [Test(Description = "TRPREP5 - GetUltimoRegisto devolve null quando nao existe historico.")]
        public async Task GetUltimoRegistoAsync_Should_ReturnNull_When_HistoryDoesNotExist()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.RegistosProducao.AddAsync(
                BuildRegisto(faseId: 1, pecaId: 2, EstadoProducao.PREPARACAO, DateTime.UtcNow));
            await context.SaveChangesAsync();

            var repository = new RegistosProducaoRepository(context);

            // ACT
            var result = await repository.GetUltimoRegistoAsync(faseId: 1, pecaId: 1);

            // ASSERT
            result.Should().BeNull();
        }

        [Test(Description = "TRPREP6 - AddWithMachineState persiste registo mesmo sem maquina para atualizar.")]
        public async Task AddWithMachineStateAsync_Should_PersistRecord_When_MachineIsNull()
        {
            // ARRANGE
            await using var context = CreateContext();
            var registo = BuildRegisto(
                faseId: 1,
                pecaId: 1,
                EstadoProducao.PAUSADO,
                DateTime.UtcNow);

            var repository = new RegistosProducaoRepository(context);

            // ACT
            var result = await repository.AddWithMachineStateAsync(registo, maquinaToUpdate: null, pecaToUpdate: null);

            // ASSERT
            result.Registo_Producao_id.Should().BeGreaterThan(0);
            context.RegistosProducao.Should().ContainSingle(r => r.Maquina_id == null && r.Estado_producao == EstadoProducao.PAUSADO);
        }

        private static RegistosProducao BuildRegisto(
            int faseId,
            int pecaId,
            EstadoProducao estado,
            DateTime dataHora,
            int? maquinaId = null)
        {
            return new RegistosProducao
            {
                Fase_id = faseId,
                Peca_id = pecaId,
                Operador_id = 1,
                Maquina_id = maquinaId,
                Estado_producao = estado,
                Data_hora = dataHora
            };
        }
    }
}
