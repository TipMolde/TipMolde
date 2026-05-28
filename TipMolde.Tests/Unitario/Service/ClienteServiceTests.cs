using AutoMapper;
using FluentAssertions;
using Moq;
using TipMolde.Application.Dtos.ClienteDto;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.ICliente;
using TipMolde.Application.Mappings;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities.Comercio;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class ClienteServiceTests
{
    private static readonly int[] ExpectedClienteIds = [1, 2];

    private Mock<IClienteRepository> _clienteRepository = null!;
    private ClienteService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        // ARRANGE
        _clienteRepository = new Mock<IClienteRepository>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ClienteProfile>();
            cfg.AddProfile<EncomendaProfile>();
        });

        var mapper = mapperConfig.CreateMapper();
        _sut = new ClienteService(_clienteRepository.Object, mapper);
    }

    private static Cliente BuildCliente(
        int id = 1,
        string nome = "Cliente A",
        string nif = "123456789",
        string sigla = "CLA") => new()
        {
            Cliente_id = id,
            Nome = nome,
            NIF = nif,
            Sigla = sigla,
            Pais = "PT",
            Email = "cliente@a.pt",
            Telefone = "910000000"
        };

    private static CreateClienteDto BuildCreateDto(
        string nome = "Cliente A",
        string nif = "123456789",
        string sigla = "CLA") => new()
        {
            Nome = nome,
            NIF = nif,
            Sigla = sigla,
            Pais = "PT",
            Email = "cliente@a.pt",
            Telefone = "910000000"
        };

    private static UpdateClienteDto BuildUpdateDto(
        string? nome = "Cliente A",
        string? nif = "123456789",
        string? sigla = "CLA") => new()
        {
            Nome = nome,
            NIF = nif,
            Sigla = sigla,
            Pais = "PT",
            Email = "cliente@a.pt",
            Telefone = "910000000"
        };

    [Test(Description = "T1CLI - Create deve normalizar campos e criar cliente quando dados sao validos.")]
    public async Task CreateAsync_Should_TrimAndCreateCliente_When_DataIsValid()
    {
        // ARRANGE
        var dto = BuildCreateDto(nome: "  Cliente A  ", nif: " 123456789 ", sigla: " cla ");
        dto.Pais = "  PT  ";
        dto.Email = "  cliente@a.pt  ";
        dto.Telefone = " 910000000 ";

        _clienteRepository.Setup(r => r.GetByNifAsync("123456789")).ReturnsAsync((Cliente?)null);
        _clienteRepository.Setup(r => r.GetBySiglaAsync("cla")).ReturnsAsync((Cliente?)null);

        // ACT
        var result = await _sut.CreateAsync(dto);

        // ASSERT
        result.Nome.Should().Be("Cliente A");
        result.NIF.Should().Be("123456789");
        result.Sigla.Should().Be("cla");

        _clienteRepository.Verify(r => r.AddAsync(It.Is<Cliente>(c =>
            c.Nome == "Cliente A" &&
            c.NIF == "123456789" &&
            c.Sigla == "cla" &&
            c.Pais == "PT" &&
            c.Email == "cliente@a.pt" &&
            c.Telefone == "910000000")), Times.Once);
    }

    [Test(Description = "T2CLI - Create deve falhar quando NIF ja existe.")]
    public async Task CreateAsync_Should_ThrowBusinessConflictException_When_NifAlreadyExists()
    {
        // ARRANGE
        var dto = BuildCreateDto();
        _clienteRepository.Setup(r => r.GetByNifAsync(dto.NIF)).ReturnsAsync(BuildCliente(id: 7));

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<BusinessConflictException>();
    }

    [Test(Description = "T3CLI - Create deve falhar quando sigla ja existe.")]
    public async Task CreateAsync_Should_ThrowBusinessConflictException_When_SiglaAlreadyExists()
    {
        // ARRANGE
        var dto = BuildCreateDto();
        _clienteRepository.Setup(r => r.GetByNifAsync(dto.NIF)).ReturnsAsync((Cliente?)null);
        _clienteRepository.Setup(r => r.GetBySiglaAsync(dto.Sigla)).ReturnsAsync(BuildCliente(id: 7));

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<BusinessConflictException>();
    }

    [TestCase("", "123456789", "SIG", Description = "T4CLI-A - Create deve falhar quando nome obrigatorio esta em falta.")]
    [TestCase("Cliente", "", "SIG", Description = "T4CLI-B - Create deve falhar quando NIF obrigatorio esta em falta.")]
    [TestCase("Cliente", "123456789", "", Description = "T4CLI-C - Create deve falhar quando sigla obrigatoria esta em falta.")]
    public async Task CreateAsync_Should_ThrowArgumentException_When_RequiredFieldIsMissing(string nome, string nif, string sigla)
    {
        // ARRANGE
        var dto = BuildCreateDto(nome: nome, nif: nif, sigla: sigla);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "T5CLI - Update deve falhar quando cliente nao existe.")]
    public async Task UpdateAsync_Should_ThrowKeyNotFoundException_When_ClienteDoesNotExist()
    {
        // ARRANGE
        _clienteRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Cliente?)null);
        var dto = BuildUpdateDto();

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(99, dto);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "T6CLI - Update deve persistir dados normalizados quando cliente existe.")]
    public async Task UpdateAsync_Should_UpdateExistingCliente_When_DataIsValid()
    {
        // ARRANGE
        var existing = BuildCliente(id: 1, nome: "Old Name", nif: "123456789", sigla: "OLD");
        _clienteRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        _clienteRepository.Setup(r => r.GetByNifAsync("987654321")).ReturnsAsync((Cliente?)null);
        _clienteRepository.Setup(r => r.GetBySiglaAsync("NEW")).ReturnsAsync((Cliente?)null);

        var dto = BuildUpdateDto(nome: "  New Name  ", nif: "987654321", sigla: "NEW");
        dto.Pais = "  Portugal  ";
        dto.Email = "  novo@tipmolde.pt  ";
        dto.Telefone = " 919999999 ";

        // ACT
        await _sut.UpdateAsync(1, dto);

        // ASSERT
        _clienteRepository.Verify(r => r.UpdateAsync(It.Is<Cliente>(c =>
            c.Cliente_id == 1 &&
            c.Nome == "New Name" &&
            c.NIF == "987654321" &&
            c.Sigla == "NEW" &&
            c.Pais == "Portugal" &&
            c.Email == "novo@tipmolde.pt" &&
            c.Telefone == "919999999")), Times.Once);
    }

    [Test(Description = "T7CLI - Delete deve falhar quando cliente nao existe.")]
    public async Task DeleteAsync_Should_ThrowKeyNotFoundException_When_ClienteDoesNotExist()
    {
        // ARRANGE
        _clienteRepository.Setup(r => r.GetByIdAsync(50)).ReturnsAsync((Cliente?)null);

        // ACT
        Func<Task> act = () => _sut.DeleteAsync(50);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "T8CLI - Delete deve remover cliente quando registo existe.")]
    public async Task DeleteAsync_Should_DeleteCliente_When_ClienteExists()
    {
        // ARRANGE
        _clienteRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildCliente());

        // ACT
        await _sut.DeleteAsync(1);

        // ASSERT
        _clienteRepository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Test(Description = "T9CLI - Search por nome deve devolver vazio quando termo e branco.")]
    public async Task SearchByNameAsync_Should_ReturnEmpty_When_SearchTermIsBlank()
    {
        // ARRANGE

        // ACT
        var result = await _sut.SearchByNameAsync("   ", 1, 10);

        // ASSERT
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _clienteRepository.Verify(r => r.SearchByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test(Description = "T10CLI - Search por sigla deve devolver vazio quando termo e branco.")]
    public async Task SearchBySiglaAsync_Should_ReturnEmpty_When_SearchTermIsBlank()
    {
        // ARRANGE

        // ACT
        var result = await _sut.SearchBySiglaAsync(string.Empty, 1, 10);

        // ASSERT
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _clienteRepository.Verify(r => r.SearchBySiglaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test(Description = "T11CLI - Search por nome deve mapear clientes para DTO paginado quando ha resultados.")]
    public async Task SearchByNameAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var clientes = new[] { BuildCliente(id: 10, nome: " Cliente X ", nif: "111111111", sigla: " CX ") };
        var paged = new PagedResult<Cliente>(clientes, 1, 2, 10);

        _clienteRepository
            .Setup(r => r.SearchByNameAsync("Cliente", 2, 10))
            .ReturnsAsync(paged);

        // ACT
        var result = await _sut.SearchByNameAsync(" Cliente ", 2, 5);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.CurrentPage.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Should().ContainSingle();
        result.Items.Single().Cliente_id.Should().Be(10);
        result.Items.Single().Nome.Should().Be("Cliente X");
        result.Items.Single().Sigla.Should().Be("CX");
    }

    [Test(Description = "T12CLI - GetAll deve mapear clientes para DTO paginado.")]
    public async Task GetAllAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var clientes = new[] { BuildCliente(id: 1), BuildCliente(id: 2, nome: "Cliente B", nif: "987654321", sigla: "CLB") };
        var paged = new PagedResult<Cliente>(clientes, 2, 1, 10);
        _clienteRepository.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync(paged);

        // ACT
        var result = await _sut.GetAllAsync(1, 10);

        // ASSERT
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.Cliente_id).Should().Contain(ExpectedClienteIds);
    }

    [Test(Description = "T13CLI - GetById deve devolver nulo quando cliente nao existe.")]
    public async Task GetByIdAsync_Should_ReturnNull_When_ClienteDoesNotExist()
    {
        // ARRANGE
        _clienteRepository.Setup(r => r.GetByIdAsync(77)).ReturnsAsync((Cliente?)null);

        // ACT
        var result = await _sut.GetByIdAsync(77);

        // ASSERT
        result.Should().BeNull();
    }

    [Test(Description = "T14CLI - GetById deve mapear cliente para DTO quando registo existe.")]
    public async Task GetByIdAsync_Should_MapCliente_When_ClienteExists()
    {
        // ARRANGE
        _clienteRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(BuildCliente(id: 7, nome: " Cliente 7 ", nif: " 111111111 ", sigla: " C7 "));

        // ACT
        var result = await _sut.GetByIdAsync(7);

        // ASSERT
        result.Should().NotBeNull();
        result!.Cliente_id.Should().Be(7);
        result.Nome.Should().Be("Cliente 7");
        result.Sigla.Should().Be("C7");
    }

    [Test(Description = "T15CLI - Search por sigla deve mapear clientes para DTO paginado quando ha resultados.")]
    public async Task SearchBySiglaAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var clientes = new[] { BuildCliente(id: 12, nome: "Cliente Y", nif: "222222222", sigla: " CY ") };
        var paged = new PagedResult<Cliente>(clientes, 1, 3, 10);

        _clienteRepository
            .Setup(r => r.SearchBySiglaAsync("CY", 3, 10))
            .ReturnsAsync(paged);

        // ACT
        var result = await _sut.SearchBySiglaAsync(" CY ", 3, 4);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.CurrentPage.Should().Be(3);
        result.PageSize.Should().Be(10);
        result.Items.Single().Cliente_id.Should().Be(12);
        result.Items.Single().Sigla.Should().Be("CY");
    }

    [Test(Description = "T16CLI - GetClienteWithEncomendas deve devolver nulo quando cliente nao existe.")]
    public async Task GetClienteWithEncomendasAsync_Should_ReturnNull_When_ClienteDoesNotExist()
    {
        // ARRANGE
        _clienteRepository.Setup(r => r.GetClienteWithEncomendasAsync(90)).ReturnsAsync((Cliente?)null);

        // ACT
        var result = await _sut.GetClienteWithEncomendasAsync(90);

        // ASSERT
        result.Should().BeNull();
    }

    [Test(Description = "T17CLI - GetClienteWithEncomendas deve mapear cliente e encomendas quando registo existe.")]
    public async Task GetClienteWithEncomendasAsync_Should_MapCliente_When_ClienteExists()
    {
        // ARRANGE
        var cliente = BuildCliente(id: 15, nome: " Cliente Z ", nif: "333333333", sigla: " CZ ");
        cliente.Encomendas = new List<Encomenda>
        {
            new()
            {
                Encomenda_id = 5,
                NumeroEncomendaCliente = "ENC-005",
                Cliente_id = 15,
                Estado = TipMolde.Domain.Enums.EstadoEncomenda.CONFIRMADA,
                DataRegisto = new DateTime(2026, 4, 20, 9, 0, 0, DateTimeKind.Utc)
            }
        };
        _clienteRepository.Setup(r => r.GetClienteWithEncomendasAsync(15)).ReturnsAsync(cliente);

        // ACT
        var result = await _sut.GetClienteWithEncomendasAsync(15);

        // ASSERT
        result.Should().NotBeNull();
        result!.ClienteId.Should().Be(15);
        result.Nome.Should().Be("Cliente Z");
        result.Encomendas.Should().ContainSingle();
        result.Encomendas!.Single().Encomenda_id.Should().Be(5);
    }
}

