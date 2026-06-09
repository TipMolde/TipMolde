using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using TipMolde.Application.Mappings;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;
using TipMolde.Infrastructure.Repositorio;

namespace TipMolde.Tests.Integracao;

[TestFixture]
[Category("Integration")]
public class PecaImportCsvServiceTests
{
    private const string MockDirectory = @"C:\Users\HP\Documents\TipMolde\CsvMock";
    private static readonly string InputCsvPath = Path.Combine(MockDirectory, "PecasImportCsvMock.csv");
    private static readonly string OutputCsvPath = Path.Combine(MockDirectory, "PecasImportCsvMock_ComPrioridade.csv");

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static PecaService CreateSut(ApplicationDbContext ctx)
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PecaProfile>();
        });

        var pecaRepository = new PecaRepository(ctx);
        var moldeRepository = new MoldeRepository(ctx);
        var fasesRepository = new FasesProducaoRepository(ctx);
        var logger = new Mock<ILogger<PecaService>>();

        return new PecaService(
            pecaRepository,
            moldeRepository,
            fasesRepository,
            mapperConfig.CreateMapper(),
            logger.Object);
    }

    private static async Task<int> SeedMoldeAsync(ApplicationDbContext ctx)
    {
        var molde = new Molde
        {
            Numero = "294.26",
            Nome = "Molde 294.26",
            NumeroMoldeCliente = "294.26",
            Descricao = "Molde usado no teste de importacao CSV com dados reais.",
            Numero_cavidades = 1,
            TipoPedido = TipoPedido.NOVO_MOLDE
        };

        await ctx.Moldes.AddAsync(molde);
        await ctx.SaveChangesAsync();

        return molde.Molde_id;
    }

    private static UTF8Encoding CreateMockOutputEncoding()
    {
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    }

    private static async Task WriteImportResultCsvAsync(IEnumerable<Peca> pecas)
    {
        Directory.CreateDirectory(MockDirectory);

        var linhas = new List<string>
        {
            "Prioridade;NumeroPeca;Designacao;Quantidade;Referencia;Material;TratamentoTermico;Massa;Observacao"
        };

        linhas.AddRange(pecas
            .OrderBy(x => x.Prioridade)
            .Select(x => string.Join(';',
                x.Prioridade,
                x.NumeroPeca,
                x.Designacao,
                x.Quantidade,
                x.Referencia,
                x.MaterialDesignacao,
                x.TratamentoTermico,
                x.Massa,
                x.Observacao)));

        await using var outputStream = new FileStream(OutputCsvPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(outputStream, CreateMockOutputEncoding());

        foreach (var linha in linhas)
        {
            await writer.WriteLineAsync(linha);
        }
    }

    [Test(Description = "TPECAIMPORTCSV1 - Importar CSV deve ler dados reais e criar CSV de pecas com prioridade.")]
    [Explicit("Depende do CSV real e deve ser executado manualmente depois de resolver linhas repetidas com dados contraditorios.")]
    public async Task ImportarCsvAsync_ComDadosReais_DevePersistirPecasConsolidadasECriarCsvComPrioridade()
    {
        // ARRANGE
        await using var ctx = CreateContext();
        var moldeId = await SeedMoldeAsync(ctx);
        var sut = CreateSut(ctx);

        File.Exists(InputCsvPath).Should().BeTrue($"o CSV real deve existir em '{InputCsvPath}'");
        await using var stream = new FileStream(InputCsvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // ACT
        var result = await sut.ImportarCsvAsync(moldeId, stream);
        var pecasPersistidas = await ctx.Pecas
            .AsNoTracking()
            .Where(x => x.Molde_id == moldeId)
            .OrderBy(x => x.Prioridade)
            .ToListAsync();

        await WriteImportResultCsvAsync(pecasPersistidas);

        // ASSERT
        result.MoldeId.Should().Be(moldeId);
        result.ReferenciaMolde.Should().Be("Molde");
        result.TotalLinhasPecaLidas.Should().BeGreaterThan(0);
        result.TotalPecasConsolidadas.Should().Be(pecasPersistidas.Count);
        result.TotalQuantidadeConsolidada.Should().Be(pecasPersistidas.Sum(x => x.Quantidade));
        result.PecasImportadas.Should().HaveCount(pecasPersistidas.Count);

        pecasPersistidas.Should().NotBeEmpty();
        pecasPersistidas.Select(x => x.Prioridade)
            .Should()
            .Equal(Enumerable.Range(1, pecasPersistidas.Count));

        File.Exists(OutputCsvPath).Should().BeTrue();
        File.ReadLines(OutputCsvPath).First().Should().StartWith("Prioridade;NumeroPeca");
    }
}
