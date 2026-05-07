using FluentAssertions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using TipMolde.Application.Dtos.FichaDocumentoDto;
using TipMolde.Application.Interface.Fichas.IFichaDocumento;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Desenho;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.TipoFichas;
using TipMolde.Domain.Entities.Fichas.TipoFichas.Linhas;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;
using TipMolde.Infrastructure.Repositorio;
using TipMolde.Infrastructure.Settings;
using TipMolde.Infrastructure.Service;
using TipMolde.Domain.Entities;

namespace TipMolde.Tests.Integracao
{
    /// <summary>
    /// Testes de integracao do RelatorioService com repositorio e contexto em memoria.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public class RelatorioServiceTests
    {
        private const string MockOutputDirectory = @"C:\Users\HP\Documents\TipMolde\RelatoriosMock";
        private const string TemplatesRootDirectory = @"C:\Users\HP\Documents\TipMolde\Templates";

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private static RelatorioService CreateSut(ApplicationDbContext ctx)
        {
            var repo = new RelatorioRepository(ctx);
            var templateOptions = Options.Create(new TemplateOptions
            {
                RootPath = TemplatesRootDirectory,
                FichaFLT = "FLT.xlsx",
                FichaFRE = "FRE.xlsx",
                FichaFRM = "FRM.xlsx",
                FichaFRA = "FRA.xlsx",
                FichaFOP = "FOP.xlsx",
                FolhaFLT = "FLT - TM.04.05",
                FolhaFRE = "FRE - TM.08.05",
                FolhaFRM = "FRM - TM.09.05",
                FolhaFRA = "FRA - TM.010.05",
                FolhaFOP = "FOP - TM.07.05"
            });
            var storageOptions = Options.Create(new StorageOptions
            {
                FichasRootPath = @"C:\Users\HP\Documents\TipMolde\Storage\Fichas",
                UploadsRootPath = @"C:\Users\HP\Documents\TipMolde\Storage\Uploads"
            });
            var environmentMock = new Mock<IHostEnvironment>();
            environmentMock.SetupGet(e => e.ContentRootPath).Returns(@"C:\Users\HP\Documents\TipMolde\Aplicacao\TipMolde\TipMolde");

            var fichaDocServiceMock = new Mock<IFichaDocumentoService>();

            fichaDocServiceMock
            .Setup(s => s.GuardarGeradoAsync(
                It.IsAny<int>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync((int fichaId, byte[] content, string fileName, string tipoFicheiro, int userId, string origem) =>
                new ResponseFichaDocumentoDto
                {
                    FichaProducao_id = fichaId,
                    NomeFicheiro = fileName,
                    TipoFicheiro = tipoFicheiro,
                    CriadoPor_user_id = userId,
                    Origem = origem,
                    Versao = 1,
                    Ativo = true
                });


            return new RelatorioService(
                repo,
                fichaDocServiceMock.Object,
                templateOptions,
                storageOptions,
                environmentMock.Object);
        }

        private static async Task<int> SeedMoldeAsync(ApplicationDbContext ctx, string numero = "M-001")
        {
            var molde = new Molde
            {
                Numero = numero,
                Nome = "Molde Teste",
                NumeroMoldeCliente = "Molde Teste - 001",
                Descricao = "Descricao teste",
                Numero_cavidades = 4,
                TipoPedido = TipoPedido.NOVO_MOLDE
            };

            await ctx.Moldes.AddAsync(molde);
            await ctx.SaveChangesAsync();
            return molde.Molde_id;
        }

        private static async Task<int> SeedMoldeCicloVidaCompletoAsync(ApplicationDbContext ctx, string numero = "M-001")
        {
            var cliente = new Cliente
            {
                Nome = "Cliente Validacao",
                NIF = "509999990",
                Sigla = "CV"
            };
            await ctx.Clientes.AddAsync(cliente);
            await ctx.SaveChangesAsync();

            var encomenda = new Encomenda
            {
                NumeroEncomendaCliente = "ENC-VAL-2026-001",
                NumeroProjetoCliente = "PRJ-CLIENTE-77",
                NomeServicoCliente = "Ferramenta tampas premium",
                NomeResponsavelCliente = "Ana Martins",
                DataRegisto = new DateTime(2026, 03, 12, 0, 0, 0, DateTimeKind.Utc),
                Cliente_id = cliente.Cliente_id
            };
            await ctx.Encomendas.AddAsync(encomenda);
            await ctx.SaveChangesAsync();

            var molde = new Molde
            {
                Numero = numero,
                Nome = "Molde Tampa Premium 4C",
                NumeroMoldeCliente = "CLI-MOLDE-9001",
                Descricao = "Molde piloto para validacao do relatorio de ciclo de vida",
                Numero_cavidades = 4,
                TipoPedido = TipoPedido.NOVO_MOLDE
            };
            await ctx.Moldes.AddAsync(molde);
            await ctx.SaveChangesAsync();

            var encomendaMolde = new EncomendaMolde
            {
                Encomenda_id = encomenda.Encomenda_id,
                Molde_id = molde.Molde_id,
                Quantidade = 1,
                Prioridade = 1,
                DataEntregaPrevista = new DateTime(2026, 05, 30, 0, 0, 0, DateTimeKind.Utc)
            };
            await ctx.EncomendasMoldes.AddAsync(encomendaMolde);
            await ctx.SaveChangesAsync();

            var pecas = new[]
            {
                new Peca
                {
                    Molde_id = molde.Molde_id,
                    NumeroPeca = "P-001",
                    Designacao = "Cavidade A",
                    Prioridade = 1,
                    Quantidade = 1,
                    MaterialDesignacao = "1.2311",
                    MaterialRecebido = true
                },
                new Peca
                {
                    Molde_id = molde.Molde_id,
                    NumeroPeca = "P-002",
                    Designacao = "Cavidade B",
                    Prioridade = 2,
                    Quantidade = 1,
                    MaterialDesignacao = "1.2311",
                    MaterialRecebido = false
                },
                new Peca
                {
                    Molde_id = molde.Molde_id,
                    NumeroPeca = "P-003",
                    Designacao = "Extrator Central",
                    Prioridade = 3,
                    Quantidade = 2,
                    MaterialDesignacao = "1.2083",
                    MaterialRecebido = true
                },
                new Peca
                {
                    Molde_id = molde.Molde_id,
                    NumeroPeca = "P-004",
                    Designacao = "Placa Apoio",
                    Prioridade = 4,
                    Quantidade = 1,
                    MaterialDesignacao = "C45",
                    MaterialRecebido = true
                }
            };
            await ctx.Pecas.AddRangeAsync(pecas);
            await ctx.SaveChangesAsync();

            var projeto3D = new Projeto
            {
                Molde_id = molde.Molde_id,
                NomeProjeto = "Conceito 3D",
                SoftwareUtilizado = "Siemens NX",
                TipoProjeto = TipoProjeto.PROJETO_3D,
                CaminhoPastaServidor = @"\\srv\projetos\molde_9001\3d"
            };
            var projeto2D = new Projeto
            {
                Molde_id = molde.Molde_id,
                NomeProjeto = "Detalhamento 2D",
                SoftwareUtilizado = "AutoCAD",
                TipoProjeto = TipoProjeto.PROJETO_2D,
                CaminhoPastaServidor = @"\\srv\projetos\molde_9001\2d"
            };
            await ctx.Projetos.AddRangeAsync(projeto3D, projeto2D);
            await ctx.SaveChangesAsync();

            await ctx.Revisoes.AddRangeAsync(
                new Revisao
                {
                    Projeto_id = projeto3D.Projeto_id,
                    NumRevisao = 1,
                    DescricaoAlteracoes = "Primeira proposta enviada ao cliente",
                    DataEnvioCliente = new DateTime(2026, 03, 15, 0, 0, 0, DateTimeKind.Utc),
                    Aprovado = false,
                    DataResposta = new DateTime(2026, 03, 17, 0, 0, 0, DateTimeKind.Utc),
                    FeedbackTexto = "Ajustar extracao lateral"
                },
                new Revisao
                {
                    Projeto_id = projeto3D.Projeto_id,
                    NumRevisao = 2,
                    DescricaoAlteracoes = "Reforco na zona de extracao",
                    DataEnvioCliente = new DateTime(2026, 03, 22, 0, 0, 0, DateTimeKind.Utc),
                    Aprovado = true,
                    DataResposta = new DateTime(2026, 03, 24, 0, 0, 0, DateTimeKind.Utc),
                    FeedbackTexto = "Aprovado para producao"
                },
                new Revisao
                {
                    Projeto_id = projeto2D.Projeto_id,
                    NumRevisao = 1,
                    DescricaoAlteracoes = "Cotas finais de maquinação",
                    DataEnvioCliente = new DateTime(2026, 04, 10, 0, 0, 0, DateTimeKind.Utc),
                    Aprovado = true,
                    DataResposta = new DateTime(2026, 04, 12, 0, 0, 0, DateTimeKind.Utc),
                    FeedbackTexto = "Liberado"
                });
            await ctx.SaveChangesAsync();

            var maquinacao = new FasesProducao { Nome = NomeFases.MAQUINACAO, Descricao = "Maquinacao CNC" };
            var erosao = new FasesProducao { Nome = NomeFases.EROSAO, Descricao = "Erosao" };
            var montagem = new FasesProducao { Nome = NomeFases.MONTAGEM, Descricao = "Montagem final" };
            await ctx.Fases_Producao.AddRangeAsync(maquinacao, erosao, montagem);
            await ctx.SaveChangesAsync();

            await ctx.RegistosProducao.AddRangeAsync(
                new RegistosProducao
                {
                    Peca_id = pecas[0].Peca_id,
                    Fase_id = maquinacao.Fases_producao_id,
                    Operador_id = 1,
                    Estado_producao = EstadoProducao.CONCLUIDO,
                    Data_hora = new DateTime(2026, 04, 01, 8, 0, 0, DateTimeKind.Utc)
                },
                new RegistosProducao
                {
                    Peca_id = pecas[0].Peca_id,
                    Fase_id = erosao.Fases_producao_id,
                    Operador_id = 1,
                    Estado_producao = EstadoProducao.PREPARACAO,
                    Data_hora = new DateTime(2026, 04, 03, 10, 0, 0, DateTimeKind.Utc)
                },
                new RegistosProducao
                {
                    Peca_id = pecas[0].Peca_id,
                    Fase_id = montagem.Fases_producao_id,
                    Operador_id = 1,
                    Estado_producao = EstadoProducao.CONCLUIDO,
                    Data_hora = new DateTime(2026, 04, 18, 15, 0, 0, DateTimeKind.Utc)
                },
                new RegistosProducao
                {
                    Peca_id = pecas[1].Peca_id,
                    Fase_id = maquinacao.Fases_producao_id,
                    Operador_id = 2,
                    Estado_producao = EstadoProducao.EM_CURSO,
                    Data_hora = new DateTime(2026, 04, 08, 11, 30, 0, DateTimeKind.Utc)
                },
                new RegistosProducao
                {
                    Peca_id = pecas[2].Peca_id,
                    Fase_id = maquinacao.Fases_producao_id,
                    Operador_id = 3,
                    Estado_producao = EstadoProducao.CONCLUIDO,
                    Data_hora = new DateTime(2026, 04, 05, 14, 0, 0, DateTimeKind.Utc)
                },
                new RegistosProducao
                {
                    Peca_id = pecas[2].Peca_id,
                    Fase_id = erosao.Fases_producao_id,
                    Operador_id = 3,
                    Estado_producao = EstadoProducao.CONCLUIDO,
                    Data_hora = new DateTime(2026, 04, 09, 9, 0, 0, DateTimeKind.Utc)
                },
                new RegistosProducao
                {
                    Peca_id = pecas[2].Peca_id,
                    Fase_id = montagem.Fases_producao_id,
                    Operador_id = 4,
                    Estado_producao = EstadoProducao.EM_CURSO,
                    Data_hora = new DateTime(2026, 04, 20, 16, 30, 0, DateTimeKind.Utc)
                });
            await ctx.SaveChangesAsync();

            return molde.Molde_id;
        }

        private static async Task WriteMockArtifactAsync(string fileName, byte[] content)
        {
            Directory.CreateDirectory(MockOutputDirectory);
            await File.WriteAllBytesAsync(Path.Combine(MockOutputDirectory, fileName), content);
        }

        private static async Task<int> SeedFichaAsync(ApplicationDbContext ctx, TipoFicha tipo, int fichaId = 1)
        {
            var cliente = new Cliente
            {
                Nome = "Cliente X",
                NIF = $"123456{fichaId:000}",
                Sigla = $"CX{fichaId}"
            };
            await ctx.Clientes.AddAsync(cliente);
            await ctx.SaveChangesAsync();

            var encomenda = new Encomenda
            {
                NumeroEncomendaCliente = $"ENC-{fichaId:000}",
                NomeServicoCliente = $"Serviço Teste",
                NumeroProjetoCliente = $"PRJ-{fichaId:000}",
                NomeResponsavelCliente = $"Responsável Gonçalo",
                Cliente_id = cliente.Cliente_id
            };
            await ctx.Encomendas.AddAsync(encomenda);
            await ctx.SaveChangesAsync();

            var molde = new Molde
            {
                Numero = $"M-{fichaId:000}",
                Nome = $"Molde {fichaId:000}",
                NumeroMoldeCliente = "Molde Teste - 001",
                ImagemCapaPath = @"C:\Users\HP\Documents\TipMolde\Templates\Imagem_Template.png",
                Numero_cavidades = 2,
                TipoPedido = TipoPedido.NOVO_MOLDE
            };
            await ctx.Moldes.AddAsync(molde);
            await ctx.SaveChangesAsync();

            var specs = new EspecificacoesTecnicas
            {
                Molde_id = molde.Molde_id,
                TipoInjecao = "Hot Runner",
                SistemaInjecao = "Canal Quente",
                MaterialInjecao = "ABS",
                MaterialMacho = "AISI P20",
                MaterialCavidade = "H13",
                MaterialMovimentos = "AISI 420",
                AcabamentoPeca = "Polido",
                Contracao = 1.20m,
                Cor = CorMolde.BICOLOR,
                LadoFixo = true,
                LadoMovel = true
            };
            await ctx.EspecificacoesTecnicas.AddAsync(specs);
            await ctx.SaveChangesAsync();

            var link = new EncomendaMolde
            {
                Encomenda_id = encomenda.Encomenda_id,
                Molde_id = molde.Molde_id,
                Quantidade = 1,
                Prioridade = 1,
                DataEntregaPrevista = DateTime.UtcNow.Date.AddDays(10)
            };
            await ctx.EncomendasMoldes.AddAsync(link);
            await ctx.SaveChangesAsync();

            FichaProducao ficha = tipo switch
            {
                TipoFicha.FLT => new FichaFlt(),
                TipoFicha.FRE => new FichaFre(),
                TipoFicha.FRM => new FichaFrm(),
                TipoFicha.FRA => new FichaFra(),
                TipoFicha.FOP => new FichaFop(),
                _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de ficha nao suportado no seed de testes.")
            };

            ficha.FichaProducao_id = fichaId;
            ficha.Tipo = tipo;
            ficha.DataCriacao = DateTime.UtcNow;
            ficha.EncomendaMolde_id = link.EncomendaMolde_id;

            await ctx.FichasProducao.AddAsync(ficha);
            await ctx.SaveChangesAsync();

            return ficha.FichaProducao_id;
        }

        private static async Task SeedResponsavelAsync(ApplicationDbContext ctx, int userId, string nome)
        {
            if (await ctx.Users.AnyAsync(u => u.User_id == userId))
                return;

            await ctx.Users.AddAsync(new User
            {
                User_id = userId,
                Nome = nome,
                Email = $"user{userId}@tipmolde.test",
                Password = "hashed-password",
                Role = UserRole.ADMIN
            });

            await ctx.SaveChangesAsync();
        }

        private static async Task SeedFrmLinhasAsync(ApplicationDbContext ctx, int fichaId)
        {
            await SeedResponsavelAsync(ctx, 101, "Rita FRM");
            await SeedResponsavelAsync(ctx, 102, "Tiago FRM");

            await ctx.FichasFrmLinhas.AddRangeAsync(
                new FichaFrmLinha
                {
                    FichaFrm_id = fichaId,
                    Data = new DateTime(2026, 05, 01),
                    Defeito = "Risco na gaveta",
                    Pormenor = "Zona junto ao extrator",
                    Verificado = true,
                    Responsavel_id = 101
                },
                new FichaFrmLinha
                {
                    FichaFrm_id = fichaId,
                    Data = new DateTime(2026, 05, 03),
                    Defeito = "Folga lateral",
                    Pormenor = "Confirmar ajuste final",
                    Verificado = false,
                    Responsavel_id = 102
                });

            await ctx.SaveChangesAsync();
        }

        private static async Task SeedFraLinhasAsync(ApplicationDbContext ctx, int fichaId)
        {
            await SeedResponsavelAsync(ctx, 201, "Rita FRA");
            await SeedResponsavelAsync(ctx, 202, "Tiago FRA");

            await ctx.FichasFraLinhas.AddRangeAsync(
                new FichaFraLinha
                {
                    FichaFra_id = fichaId,
                    Data = new DateTime(2026, 05, 02),
                    Alteracoes = "Reforco da zona de apoio",
                    Verificado = true,
                    Responsavel_id = 201
                },
                new FichaFraLinha
                {
                    FichaFra_id = fichaId,
                    Data = new DateTime(2026, 05, 04),
                    Alteracoes = "Ajuste de cota no macho",
                    Verificado = false,
                    Responsavel_id = 202
                });

            await ctx.SaveChangesAsync();
        }

        private static async Task SeedFopLinhasAsync(ApplicationDbContext ctx, int fichaId)
        {
            await SeedResponsavelAsync(ctx, 301, "Rita FOP");
            await SeedResponsavelAsync(ctx, 302, "Tiago FOP");

            await ctx.FichasFopLinhas.AddRangeAsync(
                new FichaFopLinha
                {
                    FichaFop_id = fichaId,
                    Data = new DateTime(2026, 05, 02),
                    Ocorrencia = "Paragem por afinacao",
                    Correcao = "Reiniciado com nova parametrizacao",
                    Responsavel_id = 301
                },
                new FichaFopLinha
                {
                    FichaFop_id = fichaId,
                    Data = new DateTime(2026, 05, 05),
                    Ocorrencia = "Batida na extracao",
                    Correcao = "Revisto curso do cilindro",
                    Responsavel_id = 302
                });

            await ctx.SaveChangesAsync();
        }

        private static Task<(byte[] Content, string FileName)> GerarFichaExcelAsync(RelatorioService sut, TipoFicha tipo, int fichaId)
        {
            return tipo switch
            {
                TipoFicha.FRM => sut.GerarFichaExcelFRMAsync(fichaId, 1),
                TipoFicha.FRA => sut.GerarFichaExcelFRAAsync(fichaId, 1),
                TipoFicha.FOP => sut.GerarFichaExcelFOPAsync(fichaId, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de ficha nao suportado para este helper.")
            };
        }

        private static string GetWorksheetName(TipoFicha tipo)
        {
            return tipo switch
            {
                TipoFicha.FRM => "FRM - TM.09.05",
                TipoFicha.FRA => "FRA - TM.010.05",
                TipoFicha.FOP => "FOP - TM.07.05",
                _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de ficha nao suportado para este helper.")
            };
        }

        private static XLWorkbook OpenWorkbook(byte[] content)
        {
            return new XLWorkbook(new MemoryStream(content));
        }

        [Test(Description = "TRLI001 - Agrega dados comerciais, desenho e producao no ciclo de vida do molde.")]
        public async Task ObterMoldeCicloVidaAsync_Should_Return_CompleteLifecycleData()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var moldeId = await SeedMoldeCicloVidaCompletoAsync(ctx, "M-010");
            var repo = new RelatorioRepository(ctx);

            // ACT
            var relatorio = await repo.ObterMoldeCicloVidaAsync(moldeId);

            // ASSERT
            relatorio.Should().NotBeNull();
            relatorio!.NumeroMolde.Should().Be("M-010");
            relatorio.NumeroMoldeCliente.Should().Be("CLI-MOLDE-9001");
            relatorio.NomeMolde.Should().Be("Molde Tampa Premium 4C");
            relatorio.DescricaoMolde.Should().Contain("validacao");
            relatorio.ClienteNome.Should().Be("Cliente Validacao");
            relatorio.NumeroEncomendaCliente.Should().Be("ENC-VAL-2026-001");
            relatorio.NumeroProjetoCliente.Should().Be("PRJ-CLIENTE-77");
            relatorio.NomeResponsavelCliente.Should().Be("Ana Martins");
            relatorio.DataRegistoEncomenda.Should().Be(new DateTime(2026, 03, 12, 0, 0, 0, DateTimeKind.Utc));
            relatorio.DataEntregaPrevista.Should().Be(new DateTime(2026, 05, 30, 0, 0, 0, DateTimeKind.Utc));
            relatorio.TotalPecas.Should().Be(4);
            relatorio.MaterialPendente.Should().Be(1);
            relatorio.TotalProjetos.Should().Be(2);
            relatorio.TotalRevisoes.Should().Be(3);
            relatorio.UltimaRevisaoEm.Should().Be(new DateTime(2026, 04, 12, 0, 0, 0, DateTimeKind.Utc));
            relatorio.Maquinacao.Should().Be(3);
            relatorio.Erosao.Should().Be(2);
            relatorio.Montagem.Should().Be(2);
            relatorio.EmTrabalho.Should().Be(3);
            relatorio.Concluidas.Should().Be(1);
            relatorio.PercentagemConclusao.Should().Be(50.00m);
            relatorio.Projetos.Should().HaveCount(2);
            relatorio.Fases.Should().HaveCount(3);
        }

        [Test(Description = "TRLI002 - Gera PDF do ciclo de vida com dados suficientes para validacao funcional.")]
        public async Task GerarCicloVidaMoldePdfAsync_Should_ReturnPdf_When_MoldeHasLifecycleData()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var moldeId = await SeedMoldeCicloVidaCompletoAsync(ctx, "M-010");
            var sut = CreateSut(ctx);

            // ACT
            var dashboard = await sut.ObterDashboardMoldeAsync(moldeId);
            var result = await sut.GerarCicloVidaMoldePdfAsync(moldeId);
            await WriteMockArtifactAsync(result.FileName, result.Content);

            // ASSERT
            dashboard.NumeroMolde.Should().Be("M-010");
            dashboard.TotalPecas.Should().Be(4);
            dashboard.MaterialPendente.Should().Be(1);
            dashboard.Maquinacao.Should().Be(3);
            dashboard.Erosao.Should().Be(2);
            dashboard.Montagem.Should().Be(2);
            dashboard.EmTrabalho.Should().Be(3);
            dashboard.Concluidas.Should().Be(1);
            dashboard.PercentagemConclusao.Should().Be(50.00m);
            result.Content.Should().NotBeNullOrEmpty();
            result.FileName.Should().Be($"ciclo_vida_molde_{moldeId}.pdf");
            result.Content.Length.Should().BeGreaterThan(500);
        }

        [Test(Description = "TRLI003 - Gera excecao quando o molde nao existe para o relatorio PDF.")]
        public async Task GerarCicloVidaMoldePdfAsync_Should_Throw_When_MoldeDoesNotExist()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var sut = CreateSut(ctx);

            // ACT
            Func<Task> act = () => sut.GerarCicloVidaMoldePdfAsync(999);

            // ASSERT
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Test(Description = "TRLI004 - Gera a ficha FLT quando a relacao Encomenda-Molde existe.")]
        public async Task GerarFichaExcelFLTAsync_Should_ReturnExcel_When_FichaExists()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var fichaId = await SeedFichaAsync(ctx, TipoFicha.FLT, 30);
            var encomendaMoldeId = await ctx.FichasProducao
                .Where(f => f.FichaProducao_id == fichaId)
                .Select(f => f.EncomendaMolde_id)
                .SingleAsync();
            var sut = CreateSut(ctx);

            // ACT
            var result = await sut.GerarFichaExcelFLTAsync(encomendaMoldeId, 1);
            await WriteMockArtifactAsync(result.FileName, result.Content);

            // ASSERT
            result.Content.Should().NotBeNullOrEmpty();
            result.FileName.Should().EndWith(".xlsx");
            result.FileName.Should().StartWith("ficha_");
            result.Content.Length.Should().BeGreaterThan(50);

            using var workbook = OpenWorkbook(result.Content);
            var worksheet = workbook.Worksheet("FLT - TM.04.05");

            worksheet.Cell("C6").GetString().Should().Be("Molde 030");
            worksheet.Cell("J6").GetString().Should().Be("M-030");
            worksheet.Cell("D28").GetValue<int>().Should().Be(2);
            worksheet.Cell("G28").GetString().Should().Be("ABS");
            worksheet.Cell("E29").GetString().Should().Be("Hot Runner");
            worksheet.Cell("D43").GetString().Should().Be("Cliente X");
            worksheet.Cell("D44").GetString().Should().Be("Serviço Teste");
            worksheet.Cell("E45").GetString().Should().Be("PRJ-030");
            worksheet.Cell("I45").GetString().Should().Be("Molde Teste - 001");
            worksheet.Cell("E46").GetString().Should().Be("Responsável Gonçalo");

        }

        [Test(Description = "TRLI005 - Gera a ficha FRE quando a ficha existe.")]
        public async Task GerarFichaExcelFREAsync_Should_ReturnExcel_When_FichaExists()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var fichaId = await SeedFichaAsync(ctx, TipoFicha.FRE, 31);
            var sut = CreateSut(ctx);

            // ACT
            var result = await sut.GerarFichaExcelFREAsync(fichaId, 1);
            await WriteMockArtifactAsync(result.FileName, result.Content);

            // ASSERT
            result.Content.Should().NotBeNullOrEmpty();
            result.FileName.Should().EndWith(".xlsx");
            result.FileName.Should().StartWith("ficha_");
            result.Content.Length.Should().BeGreaterThan(50);

            using var workbook = OpenWorkbook(result.Content);
            var worksheet = workbook.Worksheet("FRE - TM.08.05");

            worksheet.Cell("C6").GetString().Should().Be("Molde 031");
            worksheet.Cell("J6").GetString().Should().Be("M-031");
            worksheet.Cell("D20").GetValue<int>().Should().Be(2);
            worksheet.Cell("G20").GetString().Should().Be("ABS");
            worksheet.Cell("E21").GetString().Should().Be("Hot Runner");
            worksheet.Cell("D26").GetString().Should().Be("Cliente X");
            worksheet.Cell("D27").GetString().Should().Be("Serviço Teste");
            worksheet.Cell("E28").GetString().Should().Be("PRJ-031");
            worksheet.Cell("I28").GetString().Should().Be("Molde Teste - 001");
            worksheet.Cell("E29").GetString().Should().Be("Responsável Gonçalo");
        }

        [Test(Description = "TRLI006 - Gera a ficha FRM quando a ficha existe.")]
        public async Task GerarFichaExcelFRMAsync_Should_ReturnExcel_When_FichaExists()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var fichaId = await SeedFichaAsync(ctx, TipoFicha.FRM, 32);
            var sut = CreateSut(ctx);

            // ACT
            var result = await sut.GerarFichaExcelFRMAsync(fichaId, 1);
            await WriteMockArtifactAsync(result.FileName, result.Content);

            // ASSERT
            result.Content.Should().NotBeNullOrEmpty();
            result.FileName.Should().EndWith(".xlsx");
            result.FileName.Should().StartWith("ficha_");
            result.Content.Length.Should().BeGreaterThan(50);
        }

        [Test(Description = "TRLI007 - Gera a ficha FRA quando a ficha existe.")]
        public async Task GerarFichaExcelFRAAsync_Should_ReturnExcel_When_FichaExists()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var fichaId = await SeedFichaAsync(ctx, TipoFicha.FRA, 33);
            var sut = CreateSut(ctx);

            // ACT
            var result = await sut.GerarFichaExcelFRAAsync(fichaId, 1);
            await WriteMockArtifactAsync(result.FileName, result.Content);

            // ASSERT
            result.Content.Should().NotBeNullOrEmpty();
            result.FileName.Should().EndWith(".xlsx");
            result.FileName.Should().StartWith("ficha_");
            result.Content.Length.Should().BeGreaterThan(50);
        }

        [Test(Description = "TRLI008 - Gera a ficha FOP quando a ficha existe.")]
        public async Task GerarFichaExcelFOPAsync_Should_ReturnExcel_When_FichaExists()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var fichaId = await SeedFichaAsync(ctx, TipoFicha.FOP, 34);
            var sut = CreateSut(ctx);

            // ACT
            var result = await sut.GerarFichaExcelFOPAsync(fichaId, 1);
            await WriteMockArtifactAsync(result.FileName, result.Content);

            // ASSERT
            result.Content.Should().NotBeNullOrEmpty();
            result.FileName.Should().EndWith(".xlsx");
            result.FileName.Should().StartWith("ficha_");
            result.Content.Length.Should().BeGreaterThan(50);
        }

        [TestCase(TipoFicha.FRM, Description = "TRLI008A - FRM sem linhas deve manter a primeira linha do template em branco.")]
        [TestCase(TipoFicha.FRA, Description = "TRLI008B - FRA sem linhas deve manter a primeira linha do template em branco.")]
        [TestCase(TipoFicha.FOP, Description = "TRLI008C - FOP sem linhas deve manter a primeira linha do template em branco.")]
        public async Task GerarFichaExcelAsync_Should_KeepTemplateRowsBlank_When_ThereAreNoManualLines(TipoFicha tipo)
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var fichaId = await SeedFichaAsync(ctx, tipo, 80 + (int)tipo);
            var sut = CreateSut(ctx);

            // ACT
            var result = await GerarFichaExcelAsync(sut, tipo, fichaId);

            // ASSERT
            using var workbook = OpenWorkbook(result.Content);
            var worksheet = workbook.Worksheet(GetWorksheetName(tipo));

            worksheet.Cell("B14").GetString().Should().BeEmpty();
            worksheet.Cell("C14").GetString().Should().BeEmpty();
        }

        [Test(Description = "TRLI008D - FRM com linhas deve preencher apenas as linhas existentes e deixar as seguintes livres.")]
        public async Task GerarFichaExcelFRMAsync_Should_FillExistingRows_And_KeepFollowingRowsBlank()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var fichaId = await SeedFichaAsync(ctx, TipoFicha.FRM, 132);
            await SeedFrmLinhasAsync(ctx, fichaId);
            var sut = CreateSut(ctx);

            // ACT
            var result = await sut.GerarFichaExcelFRMAsync(fichaId, 1);
            await WriteMockArtifactAsync(result.FileName, result.Content);

            // ASSERT
            using var workbook = OpenWorkbook(result.Content);
            var worksheet = workbook.Worksheet("FRM - TM.09.05");

            worksheet.Cell("B14").GetString().Should().Be("01/05/2026");
            worksheet.Cell("C14").GetString().Should().Be("Risco na gaveta");
            worksheet.Cell("F14").GetString().Should().Be("Zona junto ao extrator");
            worksheet.Cell("I14").GetString().Should().Be("Sim");
            worksheet.Cell("J14").GetString().Should().Be("Rita FRM");

            worksheet.Cell("B15").GetString().Should().BeEmpty();
            worksheet.Cell("C15").GetString().Should().BeEmpty();
            worksheet.Cell("F15").GetString().Should().BeEmpty();

            worksheet.Cell("B16").GetString().Should().Be("03/05/2026");
            worksheet.Cell("C16").GetString().Should().Be("Folga lateral");
            worksheet.Cell("F16").GetString().Should().Be("Confirmar ajuste final");
            worksheet.Cell("I16").GetString().Should().Be("Nao");
            worksheet.Cell("J16").GetString().Should().Be("Tiago FRM");
        }

        [Test(Description = "TRLI008E - FRA com linhas deve preencher apenas as linhas existentes e deixar as seguintes livres.")]
        public async Task GerarFichaExcelFRAAsync_Should_FillExistingRows_And_KeepFollowingRowsBlank()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var fichaId = await SeedFichaAsync(ctx, TipoFicha.FRA, 133);
            await SeedFraLinhasAsync(ctx, fichaId);
            var sut = CreateSut(ctx);

            // ACT
            var result = await sut.GerarFichaExcelFRAAsync(fichaId, 1);
            await WriteMockArtifactAsync(result.FileName, result.Content);

            // ASSERT
            using var workbook = OpenWorkbook(result.Content);
            var worksheet = workbook.Worksheet("FRA - TM.010.05");

            worksheet.Cell("B14").GetString().Should().Be("02/05/2026");
            worksheet.Cell("C14").GetString().Should().Be("Reforco da zona de apoio");
            worksheet.Cell("I14").GetString().Should().Be("Sim");
            worksheet.Cell("J14").GetString().Should().Be("Rita FRA");

            worksheet.Cell("B15").GetString().Should().BeEmpty();
            worksheet.Cell("C15").GetString().Should().BeEmpty();
            worksheet.Cell("I15").GetString().Should().BeEmpty();

            worksheet.Cell("B16").GetString().Should().Be("04/05/2026");
            worksheet.Cell("C16").GetString().Should().Be("Ajuste de cota no macho");
            worksheet.Cell("I16").GetString().Should().Be("Nao");
            worksheet.Cell("J16").GetString().Should().Be("Tiago FRA");
        }

        [Test(Description = "TRLI008F - FOP com linhas deve preencher apenas as linhas existentes e deixar as seguintes livres.")]
        public async Task GerarFichaExcelFOPAsync_Should_FillExistingRows_And_KeepFollowingRowsBlank()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var fichaId = await SeedFichaAsync(ctx, TipoFicha.FOP, 134);
            await SeedFopLinhasAsync(ctx, fichaId);
            var sut = CreateSut(ctx);

            // ACT
            var result = await sut.GerarFichaExcelFOPAsync(fichaId, 1);
            await WriteMockArtifactAsync(result.FileName, result.Content);

            // ASSERT
            using var workbook = OpenWorkbook(result.Content);
            var worksheet = workbook.Worksheet("FOP - TM.07.05");

            worksheet.Cell("B14").GetString().Should().Be("02/05/2026");
            worksheet.Cell("C14").GetString().Should().Be("Paragem por afinacao");
            worksheet.Cell("G14").GetString().Should().Be("Reiniciado com nova parametrizacao");
            worksheet.Cell("J14").GetString().Should().Be("Rita FOP");

            worksheet.Cell("B15").GetString().Should().BeEmpty();
            worksheet.Cell("C15").GetString().Should().BeEmpty();
            worksheet.Cell("G15").GetString().Should().BeEmpty();

            worksheet.Cell("B16").GetString().Should().Be("05/05/2026");
            worksheet.Cell("C16").GetString().Should().Be("Batida na extracao");
            worksheet.Cell("G16").GetString().Should().Be("Revisto curso do cilindro");
            worksheet.Cell("J16").GetString().Should().Be("Tiago FOP");
        }

        [Test(Description = "TRLI009 - Gera excecao quando a ficha nao existe para a exportacao FLT.")]
        public async Task GerarFichaExcelFLTAsync_Should_Throw_When_FichaDoesNotExist()
        {
            // ARRANGE
            await using var ctx = CreateContext();
            var sut = CreateSut(ctx);

            // ACT
            Func<Task> act = () => sut.GerarFichaExcelFLTAsync(999, 1);

            // ASSERT
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
    }
}



