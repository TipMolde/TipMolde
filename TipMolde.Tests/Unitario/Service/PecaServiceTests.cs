using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using TipMolde.Application.Dtos.PecaDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Mappings;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class PecaServiceTests
{
    private static readonly int[] ExpectedPecaIds = [3, 4];

    private Mock<IPecaRepository> _pecaRepository = null!;
    private Mock<IMoldeRepository> _moldeRepository = null!;
    private Mock<ILogger<PecaService>> _logger = null!;
    private PecaService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        // ARRANGE
        _pecaRepository = new Mock<IPecaRepository>();
        _moldeRepository = new Mock<IMoldeRepository>();
        _logger = new Mock<ILogger<PecaService>>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PecaProfile>();
        });

        var mapper = mapperConfig.CreateMapper();

        _sut = new PecaService(
            _pecaRepository.Object,
            _moldeRepository.Object,
            mapper,
            _logger.Object);
    }

    private static Molde BuildMolde(int id = 1) => new()
    {
        Molde_id = id,
        Numero = $"M-{id}",
        Numero_cavidades = 1,
        TipoPedido = TipoPedido.NOVO_MOLDE
    };

    private static Peca BuildPeca(int id = 1, int moldeId = 1, string designacao = "Extrator", string? numeroPeca = "100A") => new()
    {
        Peca_id = id,
        NumeroPeca = numeroPeca,
        Designacao = designacao,
        Prioridade = 1,
        Quantidade = 1,
        Referencia = "REF-1",
        MaterialDesignacao = "Aco",
        TratamentoTermico = "Temperado",
        Massa = "0,20kg",
        Observacao = "34,92",
        MaterialRecebido = false,
        Molde_id = moldeId
    };

    private static MemoryStream BuildCsvStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    [Test(Description = "TPECASRV1 - Create deve persistir peca e devolver DTO quando os dados sao validos.")]
    public async Task CreateAsync_Should_CreatePeca_When_DataIsValid()
    {
        // ARRANGE
        var dto = new CreatePecaDto
        {
            NumeroPeca = "  100A  ",
            Designacao = "  Extrator  ",
            Prioridade = 1,
            Quantidade = 2,
            Referencia = "  REF-1  ",
            MaterialDesignacao = "Aco",
            TratamentoTermico = "  Temperado  ",
            Massa = "  0,20kg  ",
            Observacao = "  34,92  ",
            MaterialRecebido = false,
            Molde_id = 1
        };

        _moldeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildMolde());
        _pecaRepository.Setup(r => r.GetByNumeroPecaAsync("100A", 1)).ReturnsAsync((Peca?)null);
        _pecaRepository.Setup(r => r.AddAsync(It.IsAny<Peca>()))
            .ReturnsAsync((Peca entity) =>
            {
                entity.Peca_id = 12;
                return entity;
            });

        // ACT
        var result = await _sut.CreateAsync(dto);

        // ASSERT
        result.PecaId.Should().Be(12);
        result.NumeroPeca.Should().Be("100A");
        result.Designacao.Should().Be("Extrator");
        result.Quantidade.Should().Be(2);
        result.Referencia.Should().Be("REF-1");
        result.TratamentoTermico.Should().Be("Temperado");
        result.Massa.Should().Be("0,20kg");
        result.Observacao.Should().Be("34,92");
        result.Molde_id.Should().Be(1);
        _pecaRepository.Verify(r => r.AddAsync(It.Is<Peca>(p =>
            p.NumeroPeca == "100A" &&
            p.Designacao == "Extrator" &&
            p.Prioridade == 1 &&
            p.Quantidade == 2 &&
            p.Referencia == "REF-1" &&
            p.TratamentoTermico == "Temperado" &&
            p.Massa == "0,20kg" &&
            p.Observacao == "34,92" &&
            p.Molde_id == 1)), Times.Once);
    }

    [Test(Description = "TPECASRV2 - Create deve falhar quando o molde nao existe.")]
    public async Task CreateAsync_Should_ThrowKeyNotFoundException_When_MoldeDoesNotExist()
    {
        // ARRANGE
        var dto = new CreatePecaDto
        {
            Designacao = "Extrator",
            Prioridade = 1,
            Molde_id = 7
        };

        _moldeRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((Molde?)null);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TPECASRV3 - GetAll deve devolver Dtos paginados com PecaId preenchido.")]
    public async Task GetAllAsync_Should_ReturnPagedDtos_When_RequestIsValid()
    {
        // ARRANGE
        var items = new[] { BuildPeca(id: 3), BuildPeca(id: 4, designacao: "Coluna") };
        var paged = new PagedResult<Peca>(items, 2, 1, 10);

        _pecaRepository.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync(paged);

        // ACT
        var result = await _sut.GetAllAsync(1, 10);

        // ASSERT
        result.TotalCount.Should().Be(2);
        result.Items.Select(x => x.PecaId).Should().BeEquivalentTo(ExpectedPecaIds);
    }

    [Test(Description = "TPECASRV4 - Update deve preservar MaterialRecebido quando o campo nao e enviado.")]
    public async Task UpdateAsync_Should_PreserveMaterialRecebido_When_FieldIsOmitted()
    {
        // ARRANGE
        var existing = BuildPeca(id: 1, designacao: "Original");
        existing.MaterialRecebido = true;

        _pecaRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _pecaRepository.Setup(r => r.GetByNumeroPecaAsync("100A", 1)).ReturnsAsync(existing);

        var dto = new UpdatePecaDto
        {
            Designacao = "Atualizada"
        };

        // ACT
        await _sut.UpdateAsync(1, dto);

        // ASSERT
        _pecaRepository.Verify(r => r.UpdateAsync(It.Is<Peca>(p =>
            p.Peca_id == 1 &&
            p.Designacao == "Atualizada" &&
            p.MaterialRecebido)), Times.Once);
    }

    [Test(Description = "TPECASRV5 - Update deve rejeitar designacao duplicada dentro do mesmo molde.")]
    public async Task UpdateAsync_Should_ThrowArgumentException_When_DesignacaoAlreadyExistsInMolde()
    {
        // ARRANGE
        var existing = BuildPeca(id: 1, moldeId: 7, designacao: "Extrator", numeroPeca: null);
        var duplicate = BuildPeca(id: 2, moldeId: 7, designacao: "Coluna", numeroPeca: null);

        _pecaRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _pecaRepository.Setup(r => r.GetByDesignacaoAsync("Coluna", 7)).ReturnsAsync(duplicate);

        var dto = new UpdatePecaDto
        {
            Designacao = "Coluna"
        };

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(1, dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Ja existe uma peca*");
        _pecaRepository.Verify(r => r.UpdateAsync(It.IsAny<Peca>()), Times.Never);
    }

    [Test(Description = "TPECASRV6 - Delete deve falhar quando a peca nao existe.")]
    public async Task DeleteAsync_Should_ThrowKeyNotFoundException_When_PecaDoesNotExist()
    {
        // ARRANGE
        _pecaRepository.Setup(r => r.GetByIdAsync(88)).ReturnsAsync((Peca?)null);

        // ACT
        Func<Task> act = () => _sut.DeleteAsync(88);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TPECASRV7 - GetByDesignacao deve devolver DTO quando a peca existe.")]
    public async Task GetByDesignacaoAsync_Should_ReturnMappedResponse_When_PecaExists()
    {
        // ARRANGE
        _pecaRepository.Setup(r => r.GetByDesignacaoAsync("Extrator", 2))
            .ReturnsAsync(BuildPeca(id: 5, moldeId: 2));

        // ACT
        var result = await _sut.GetByDesignacaoAsync("Extrator", 2);

        // ASSERT
        result.Should().NotBeNull();
        result!.PecaId.Should().Be(5);
        result.Molde_id.Should().Be(2);
    }

    [Test(Description = "TPECASRV8 - ImportarCsvAsync deve consolidar quantidades quando o NumeroPeca tem dados equivalentes.")]
    public async Task ImportarCsvAsync_Should_ConsolidateRows_When_NumeroPecaHasEquivalentData()
    {
        // ARRANGE
        const string csv =
            "Nº Peça;Designação;Qtd.;Ref.;Material;Trat. Térmico;Mass;Obs.\n" +
            ";;1;Molde;;;433,73kg;\n" +
            "100A;Postico das Cavidades;4;E 1710 / 4 x 40;Meusburger;;0,00kg;34,92\n" +
            "100A;Postico das Cavidades;1;E 1710 / 4 x 40;Meusburger;;0,00kg;34,92\n" +
            "101;Extrator;2;REF-2;Aco;Temperado;0,20kg;\n";

        _moldeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildMolde());
        _pecaRepository.Setup(r => r.GetByNumeroPecaAsync(It.IsAny<string>(), 1)).ReturnsAsync((Peca?)null);

        var nextId = 25;
        _pecaRepository.Setup(r => r.AddAsync(It.IsAny<Peca>()))
            .ReturnsAsync((Peca entity) =>
            {
                entity.Peca_id = nextId++;
                return entity;
            });

        await using var stream = BuildCsvStream(csv);

        // ACT
        var result = await _sut.ImportarCsvAsync(1, stream);

        // ASSERT
        result.MoldeId.Should().Be(1);
        result.ReferenciaMolde.Should().Be("Molde");
        result.MassaMolde.Should().Be("433,73kg");
        result.TotalLinhasPecaLidas.Should().Be(3);
        result.TotalPecasConsolidadas.Should().Be(2);
        result.TotalQuantidadeConsolidada.Should().Be(7);
        result.PecasImportadas.Should().HaveCount(2);
        result.PecasImportadas.Should().ContainSingle(x =>
            x.NumeroPeca == "100A" &&
            x.Designacao == "Postico das Cavidades" &&
            x.Quantidade == 5 &&
            x.Prioridade == 1);
        result.PecasImportadas.Should().ContainSingle(x =>
            x.NumeroPeca == "101" &&
            x.Designacao == "Extrator" &&
            x.Quantidade == 2 &&
            x.Prioridade == 2);

        _pecaRepository.Verify(r => r.AddAsync(It.Is<Peca>(p =>
            p.NumeroPeca == "100A" &&
            p.Designacao == "Postico das Cavidades" &&
            p.Quantidade == 5 &&
            p.Referencia == "E 1710 / 4 x 40" &&
            p.MaterialDesignacao == "Meusburger" &&
            p.Massa == "0,00kg" &&
            p.Observacao == "34,92" &&
            p.Molde_id == 1 &&
            p.Prioridade == 1)), Times.Once);
    }

    [Test(Description = "TPECASRV9 - ImportarCsvAsync deve atribuir prioridades por pecas base, variantes, 0 a 011 e restantes.")]
    public async Task ImportarCsvAsync_Should_AssignPriorities_By_ClientPriorityBlocks()
    {
        // ARRANGE
        const string csv =
            "N PECA;DESIGNACAO;QTD;REF;MATERIAL;TRAT TERMICO;MASS;OBS\n" +
            ";;1;Molde;;;433,73kg;\n" +
            "12;Suporte;1;REF-12;Aco;;1kg;\n" +
            "200A;Postico Grupo 200;1;REF-200;Aco;;1kg;\n" +
            "080C;Postico Grupo 080;1;REF-080;Aco;;1kg;\n" +
            "100B;Postico Grupo 100 B;1;REF-100B;Aco;;1kg;\n" +
            "80;Postico Grupo 80;1;REF-80;Aco;;1kg;\n" +
            "011;Peca Onze;1;REF-011;Aco;;1kg;\n" +
            "300A;Peca Restante;1;REF-300;Aco;;1kg;\n" +
            "100A;Postico Grupo 100 A;1;REF-100A;Aco;;1kg;\n" +
            "009;Peca Nove;1;REF-009;Aco;;1kg;\n" +
            "2;Peca Dois;1;REF-2;Aco;;1kg;\n" +
            "201;Peca 201 Restante;1;REF-201;Aco;;1kg;\n" +
            "200;Base Grupo 200;1;REF-BASE-200;Aco;;1kg;\n" +
            "100;Base Grupo 100;1;REF-BASE-100;Aco;;1kg;\n" +
            "080A;Postico Grupo 080 A;1;REF-080A;Aco;;1kg;\n";

        _moldeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildMolde());
        _pecaRepository.Setup(r => r.GetByNumeroPecaAsync(It.IsAny<string>(), 1)).ReturnsAsync((Peca?)null);
        _pecaRepository.Setup(r => r.AddAsync(It.IsAny<Peca>()))
            .ReturnsAsync((Peca entity) => entity);

        await using var stream = BuildCsvStream(csv);

        // ACT
        var result = await _sut.ImportarCsvAsync(1, stream);

        // ASSERT
        result.PecasImportadas
            .OrderBy(x => x.Prioridade)
            .Select(x => x.NumeroPeca)
            .Should()
            .Equal("100", "200", "80", "100B", "100A", "200A", "080C", "080A", "011", "009", "2", "12", "300A", "201");

        result.PecasImportadas
            .OrderBy(x => x.Prioridade)
            .Select(x => x.Prioridade)
            .Should()
            .Equal(Enumerable.Range(1, 14));
    }

    [Test(Description = "TPECASRV10 - ImportarCsvAsync deve rejeitar NumeroPeca repetido quando os restantes campos divergem.")]
    public async Task ImportarCsvAsync_Should_ThrowArgumentException_When_RepeatedNumeroPecaHasConflictingData()
    {
        // ARRANGE
        const string csv =
            "Nº Peça;Designação;Qtd.;Ref.;Material;Trat. Térmico;Mass;Obs.\n" +
            ";;1;Molde;;;433,73kg;\n" +
            "100A;Postico das Cavidades;4;E 1710 / 4 x 40;Meusburger;;0,00kg;34,92\n" +
            "100A;Postico das Cavidades;1;E 1710 / 4 x 40;Aco;;0,00kg;34,92\n";

        _moldeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildMolde());

        await using var stream = BuildCsvStream(csv);

        // ACT
        Func<Task> act = () => _sut.ImportarCsvAsync(1, stream);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*dados contraditorios*");
        _pecaRepository.Verify(r => r.AddAsync(It.IsAny<Peca>()), Times.Never);
    }
}
