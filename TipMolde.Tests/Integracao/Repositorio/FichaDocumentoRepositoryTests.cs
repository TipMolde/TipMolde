using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.TipoFichas;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;
using TipMolde.Infrastructure.Repositorio;

namespace TipMolde.Tests.Integracao.Repositorio
{
    [TestFixture]
    [Category("Integration")]
    public sealed class FichaDocumentoRepositoryTests : RepositoryIntegrationTestBase
    {
        [Test(Description = "TFDREP001 - Queries documentais devem respeitar existencia, versao, ativo e paginacao.")]
        public async Task QueryMethods_Should_ReturnExpectedDocumentState()
        {
            // ARRANGE
            await using var context = CreateContext();
            var ficha = await SeedFichaFreAsync(context);

            await context.FichasDocumentos.AddRangeAsync(
                CreateDocumento(ficha.FichaProducao_id, versao: 1, ativo: false, nomeFicheiro: "doc_v1.pdf"),
                CreateDocumento(ficha.FichaProducao_id, versao: 2, ativo: false, nomeFicheiro: "doc_v2.pdf"),
                CreateDocumento(ficha.FichaProducao_id, versao: 3, ativo: true, nomeFicheiro: "doc_v3.pdf"));
            await context.SaveChangesAsync();

            var repository = new FichaDocumentoRepository(context);

            // ACT
            var existe = await repository.FichaExisteAsync(ficha.FichaProducao_id);
            var proximaVersao = await repository.GetProximaVersaoAsync(ficha.FichaProducao_id);
            var ativo = await repository.GetAtivoByFichaIdAsync(ficha.FichaProducao_id);
            var detalhe = await repository.GetByIdAndFichaIdAsync(ficha.FichaProducao_id, ativo!.FichaDocumento_id);
            var paged = await repository.GetByFichaIdAsync(ficha.FichaProducao_id, 1, 2);

            // ASSERT
            existe.Should().BeTrue();
            proximaVersao.Should().Be(4);
            ativo.Versao.Should().Be(3);
            detalhe.Should().NotBeNull();
            paged.TotalCount.Should().Be(3);
            paged.Items.Select(x => x.Versao).Should().Equal(3, 2);
        }

        [Test(Description = "TFDREP002 - Add e DesativarVersoesAtivas devem persistir nova versao e desativar as ativas.")]
        public async Task AddAndDeactivateMethods_Should_UpdateDocumentVersions()
        {
            // ARRANGE
            await using var context = CreateContext();
            var ficha = await SeedFichaFreAsync(context);

            await context.FichasDocumentos.AddAsync(CreateDocumento(ficha.FichaProducao_id, versao: 1, ativo: true, nomeFicheiro: "doc_v1.pdf"));
            await context.SaveChangesAsync();

            var repository = new FichaDocumentoRepository(context);
            var novoDocumento = CreateDocumento(ficha.FichaProducao_id, versao: 2, ativo: true, nomeFicheiro: "doc_v2.pdf");

            // ACT
            await repository.AddAsync(novoDocumento);
            await repository.DesativarVersoesAtivasAsync(ficha.FichaProducao_id);

            // ASSERT
            var persisted = await repository.GetByIdAsync(novoDocumento.FichaDocumento_id);
            persisted.Should().NotBeNull();

            var ativos = await context.FichasDocumentos
                .Where(x => x.FichaProducao_id == ficha.FichaProducao_id && x.Ativo)
                .ToListAsync();
            ativos.Should().BeEmpty();
        }

        private static async Task<FichaFre> SeedFichaFreAsync(ApplicationDbContext context)
        {
            var ficha = new FichaFre
            {
                Tipo = TipoFicha.FRE,
                DataCriacao = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
                EncomendaMolde_id = 1
            };

            await context.FichasFre.AddAsync(ficha);
            await context.SaveChangesAsync();
            return ficha;
        }

        private static FichaDocumento CreateDocumento(int fichaId, int versao, bool ativo, string nomeFicheiro)
        {
            return new FichaDocumento
            {
                FichaProducao_id = fichaId,
                CriadoPor_user_id = 1,
                Versao = versao,
                Origem = "SISTEMA",
                NomeFicheiro = nomeFicheiro,
                TipoFicheiro = "application/pdf",
                CaminhoFicheiro = $@"C:\storage\{nomeFicheiro}",
                Ativo = ativo
            };
        }
    }
}
