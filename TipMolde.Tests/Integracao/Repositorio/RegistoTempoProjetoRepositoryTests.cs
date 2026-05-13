using FluentAssertions;
using TipMolde.Domain.Entities.Desenho;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.Repositorio;

namespace TipMolde.Tests.Integracao.Repositorio
{
    [TestFixture]
    [Category("Integration")]
    public sealed class RegistoTempoProjetoRepositoryTests : RepositoryIntegrationTestBase
    {
        [Test(Description = "TRTPREP1 - GetHistorico deve devolver registos ordenados por data e ID.")]
        public async Task GetHistoricoAsync_Should_ReturnOrderedHistory_When_ProjectAndAuthorMatch()
        {
            // ARRANGE
            await using var context = CreateContext();
            var older = new RegistoTempoProjeto
            {
                Projeto_id = 1,
                Autor_id = 7,
                Estado_tempo = EstadoTempoProjeto.INICIADO,
                Data_hora = new DateTime(2026, 4, 20, 8, 0, 0, DateTimeKind.Utc)
            };
            var newer = new RegistoTempoProjeto
            {
                Projeto_id = 1,
                Autor_id = 7,
                Estado_tempo = EstadoTempoProjeto.CONCLUIDO,
                Data_hora = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc)
            };

            await context.RegistosTempoProjeto.AddRangeAsync(newer, older);
            await context.SaveChangesAsync();

            var repository = new RegistoTempoProjetoRepository(context);

            // ACT
            var result = await repository.GetHistoricoAsync(1, 7, page: 1, pageSize: 10);

            // ASSERT
            result.Items.Select(r => r.Estado_tempo).Should().ContainInOrder(EstadoTempoProjeto.INICIADO, EstadoTempoProjeto.CONCLUIDO);
        }

        [Test(Description = "TRTPREP2 - GetUltimoRegisto deve devolver evento mais recente do autor no projeto.")]
        public async Task GetUltimoRegistoAsync_Should_ReturnLatestRecord_When_HistoryExists()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.RegistosTempoProjeto.AddRangeAsync(
                new RegistoTempoProjeto { Projeto_id = 1, Autor_id = 7, Estado_tempo = EstadoTempoProjeto.INICIADO, Data_hora = DateTime.UtcNow.AddHours(-2) },
                new RegistoTempoProjeto { Projeto_id = 1, Autor_id = 7, Estado_tempo = EstadoTempoProjeto.CONCLUIDO, Data_hora = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var repository = new RegistoTempoProjetoRepository(context);

            // ACT
            var result = await repository.GetUltimoRegistoAsync(1, 7);

            // ASSERT
            result.Should().NotBeNull();
            result!.Estado_tempo.Should().Be(EstadoTempoProjeto.CONCLUIDO);
        }
    }
}
