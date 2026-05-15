using FluentAssertions;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.Linhas;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;
using TipMolde.Infrastructure.Repositorio;

namespace TipMolde.Tests.Integracao.Repositorio
{
    [TestFixture]
    [Category("Integration")]
    public sealed class FichaProducaoRepositoryTests : RepositoryIntegrationTestBase
    {
        [Test(Description = "TFPREP001 - As queries de fichas devem filtrar por contexto, ordenar por data e carregar o detalhe completo.")]
        public async Task QueryMethods_Should_FilterSortAndLoadDetail()
        {
            // ARRANGE
            await using var context = CreateContext();
            var link = await SeedEncomendaMoldeAsync(context);
            var outroLink = await SeedEncomendaMoldeAsync(context, "ALT");

            var fichaAntiga = new FichaProducao
            {
                Tipo = TipoFicha.FRE,
                DataCriacao = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
                EncomendaMolde_id = link.EncomendaMolde_id
            };

            var fichaDetalhe = new FichaProducao
            {
                Tipo = TipoFicha.FRM,
                DataCriacao = new DateTime(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc),
                EncomendaMolde_id = link.EncomendaMolde_id,
                Documentos =
                {
                    new FichaDocumento
                    {
                        CriadoPor_user_id = 1,
                        Versao = 1,
                        Origem = "SISTEMA",
                        NomeFicheiro = "frm.xlsx",
                        TipoFicheiro = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        CaminhoFicheiro = @"C:\storage\frm.xlsx",
                        Ativo = true
                    }
                }
            };

            var fichaOutroContexto = new FichaProducao
            {
                Tipo = TipoFicha.FRA,
                DataCriacao = new DateTime(2026, 5, 4, 8, 0, 0, DateTimeKind.Utc),
                EncomendaMolde_id = outroLink.EncomendaMolde_id,
            };

            await context.FichasProducao.AddAsync(fichaAntiga);
            await context.FichasProducao.AddAsync(fichaDetalhe);
            await context.FichasProducao.AddAsync(fichaOutroContexto);
            await context.SaveChangesAsync();

            var repository = new FichaProducaoRepository(context);

            // ACT
            var byEncomenda = await repository.GetByEncomendaMoldeIdAsync(link.EncomendaMolde_id, 1, 10);
            var byMolde = await repository.GetByMoldeIdAsync(link.Molde_id, 1, 10);
            var detalhe = await repository.GetByIdDetalheAsync(fichaDetalhe.FichaProducao_id);

            // ASSERT
            byEncomenda.TotalCount.Should().Be(2);
            byEncomenda.Items.Select(x => x.FichaProducao_id).Should().Equal(fichaDetalhe.FichaProducao_id, fichaAntiga.FichaProducao_id);

            byMolde.TotalCount.Should().Be(2);
            byMolde.Items.Select(x => x.FichaProducao_id).Should().Equal(fichaDetalhe.FichaProducao_id, fichaAntiga.FichaProducao_id);

            detalhe.Should().NotBeNull();
            detalhe!.EncomendaMolde.Should().NotBeNull();
            detalhe.EncomendaMolde!.Encomenda.Should().NotBeNull();
            detalhe.EncomendaMolde.Encomenda!.Cliente.Should().NotBeNull();
            detalhe.EncomendaMolde.Encomenda.Cliente!.Nome.Should().Be("Cliente Repo");
            detalhe.EncomendaMolde.Molde.Should().NotBeNull();
            detalhe.EncomendaMolde.Molde!.Numero.Should().Be("M-REP-01");
            detalhe.Documentos.Should().ContainSingle(x => x.NomeFicheiro == "frm.xlsx");
        }

        [Test(Description = "TFPREP002 - As operacoes FRM devem adicionar, listar, obter e atualizar linhas manualmente.")]
        public async Task FrmLineMethods_Should_AddListGetAndUpdateLines()
        {
            // ARRANGE
            await using var context = CreateContext();
            var ficha = await SeedFichaFrmAsync(context);
            var repository = new FichaProducaoRepository(context);

            var primeira = await repository.AddLinhaFrmAsync(new FichaFrmLinha
            {
                FichaProducao_id = ficha.FichaProducao_id,
                Data = new DateTime(2026, 5, 3),
                Defeito = "Rebarba",
                Pormenor = "Zona lateral",
                Verificado = true,
                Responsavel_id = 1
            });

            var segunda = await repository.AddLinhaFrmAsync(new FichaFrmLinha
            {
                FichaProducao_id = ficha.FichaProducao_id,
                Data = new DateTime(2026, 5, 1),
                Defeito = "Batida",
                Pormenor = "Canto superior",
                Verificado = false,
                Responsavel_id = 2
            });

            // ACT
            var paged = await repository.GetLinhasFrmByFichaIdAsync(ficha.FichaProducao_id, 1, 10);
            var detalhe = await repository.GetLinhaFrmByIdAsync(ficha.FichaProducao_id, primeira.FichaFrmLinha_id);

            primeira.Pormenor = "Zona lateral revista";
            await repository.UpdateLinhaFrmAsync(primeira);

            // ASSERT
            paged.Items.Select(x => x.FichaFrmLinha_id).Should().Equal(segunda.FichaFrmLinha_id, primeira.FichaFrmLinha_id);
            detalhe.Should().NotBeNull();
            (await repository.GetLinhaFrmByIdAsync(ficha.FichaProducao_id, primeira.FichaFrmLinha_id))!
                .Pormenor.Should().Be("Zona lateral revista");
        }

        [Test(Description = "TFPREP003 - As operacoes FRA devem adicionar, listar, obter e atualizar linhas manualmente.")]
        public async Task FraLineMethods_Should_AddListGetAndUpdateLines()
        {
            // ARRANGE
            await using var context = CreateContext();
            var ficha = await SeedFichaFraAsync(context);
            var repository = new FichaProducaoRepository(context);

            var linha = await repository.AddLinhaFraAsync(new FichaFraLinha
            {
                FichaProducao_id = ficha.FichaProducao_id,
                Data = new DateTime(2026, 5, 2),
                Alteracoes = "Ajuste do macho",
                Verificado = false,
                Responsavel_id = 1
            });

            // ACT
            var paged = await repository.GetLinhasFraByFichaIdAsync(ficha.FichaProducao_id, 1, 10);
            var detalhe = await repository.GetLinhaFraByIdAsync(ficha.FichaProducao_id, linha.FichaFraLinha_id);

            linha.Verificado = true;
            await repository.UpdateLinhaFraAsync(linha);

            // ASSERT
            paged.TotalCount.Should().Be(1);
            detalhe.Should().NotBeNull();
            (await repository.GetLinhaFraByIdAsync(ficha.FichaProducao_id, linha.FichaFraLinha_id))!
                .Verificado.Should().BeTrue();
        }

        [Test(Description = "TFPREP004 - As operacoes FOP devem adicionar, listar, obter e atualizar linhas manualmente.")]
        public async Task FopLineMethods_Should_AddListGetAndUpdateLines()
        {
            // ARRANGE
            await using var context = CreateContext();
            var ficha = await SeedFichaFopAsync(context);
            var repository = new FichaProducaoRepository(context);

            var linha = await repository.AddLinhaFopAsync(new FichaFopLinha
            {
                FichaProducao_id = ficha.FichaProducao_id,
                Data = new DateTime(2026, 5, 4),
                Ocorrencia = "Paragem",
                Correcao = "Rearranque",
                Responsavel_id = 1
            });

            // ACT
            var paged = await repository.GetLinhasFopByFichaIdAsync(ficha.FichaProducao_id, 1, 10);
            var detalhe = await repository.GetLinhaFopByIdAsync(ficha.FichaProducao_id, linha.FichaFopLinha_id);

            linha.Correcao = "Rearranque controlado";
            await repository.UpdateLinhaFopAsync(linha);

            // ASSERT
            paged.TotalCount.Should().Be(1);
            detalhe.Should().NotBeNull();
            (await repository.GetLinhaFopByIdAsync(ficha.FichaProducao_id, linha.FichaFopLinha_id))!
                .Correcao.Should().Be("Rearranque controlado");
        }

        private static async Task<EncomendaMolde> SeedEncomendaMoldeAsync(ApplicationDbContext context, string suffix = "REP")
        {
            var clienteNome = suffix == "REP" ? "Cliente Repo" : $"Cliente {suffix}";
            var numeroEncomenda = suffix == "REP" ? "ENC-REP-01" : $"ENC-{suffix}-01";
            var numeroMolde = suffix == "REP" ? "M-REP-01" : $"M-{suffix}-01";

            var cliente = new Cliente
            {
                Nome = clienteNome,
                NIF = suffix == "REP" ? "123456789" : "123456788",
                Sigla = suffix == "REP" ? "CR" : "CA"
            };

            var encomenda = new Encomenda
            {
                NumeroEncomendaCliente = numeroEncomenda,
                Cliente = cliente
            };

            var molde = new Molde
            {
                Numero = numeroMolde,
                Numero_cavidades = 2,
                TipoPedido = TipoPedido.NOVO_MOLDE
            };

            var link = new EncomendaMolde
            {
                Encomenda = encomenda,
                Molde = molde,
                Quantidade = 1,
                Prioridade = 1,
                DataEntregaPrevista = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            await context.EncomendasMoldes.AddAsync(link);
            await context.SaveChangesAsync();
            return link;
        }

        private static async Task<FichaProducao> SeedFichaFrmAsync(ApplicationDbContext context)
        {
            var link = await SeedEncomendaMoldeAsync(context);
            var ficha = new FichaProducao
            {
                Tipo = TipoFicha.FRM,
                DataCriacao = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
                EncomendaMolde_id = link.EncomendaMolde_id
            };

            await context.FichasProducao.AddAsync(ficha);
            await context.SaveChangesAsync();
            return ficha;
        }

        private static async Task<FichaProducao> SeedFichaFraAsync(ApplicationDbContext context)
        {
            var link = await SeedEncomendaMoldeAsync(context);
            var ficha = new FichaProducao
            {
                Tipo = TipoFicha.FRA,
                DataCriacao = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
                EncomendaMolde_id = link.EncomendaMolde_id
            };

            await context.FichasProducao.AddAsync(ficha);
            await context.SaveChangesAsync();
            return ficha;
        }

        private static async Task<FichaProducao> SeedFichaFopAsync(ApplicationDbContext context)
        {
            var link = await SeedEncomendaMoldeAsync(context);
            var ficha = new FichaProducao
            {
                Tipo = TipoFicha.FOP,
                DataCriacao = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
                EncomendaMolde_id = link.EncomendaMolde_id
            };

            await context.FichasProducao.AddAsync(ficha);
            await context.SaveChangesAsync();
            return ficha;
        }
    }
}
