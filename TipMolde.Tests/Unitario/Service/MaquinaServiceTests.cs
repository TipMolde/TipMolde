using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TipMolde.Application.Dtos.MaquinaDto;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class MaquinaServiceTests
{
    private static readonly int[] ExpectedMaquinaIds = [1, 2];

    private Mock<IMaquinaRepository> _maquinaRepository = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<ILogger<MaquinaService>> _logger = null!;
    private MaquinaService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _maquinaRepository = new Mock<IMaquinaRepository>();
        _mapper = new Mock<IMapper>();
        _logger = new Mock<ILogger<MaquinaService>>();

        _mapper.Setup(m => m.Map<Maquina>(It.IsAny<CreateMaquinaDto>()))
            .Returns((CreateMaquinaDto dto) => new Maquina
            {
                Maquina_id = dto.Maquina_id,
                Numero = dto.Numero,
                NomeModelo = dto.NomeModelo.Trim(),
                IpAddress = string.IsNullOrWhiteSpace(dto.IpAddress) ? null : dto.IpAddress.Trim(),
                Estado = dto.Estado,
                FaseDedicada_id = dto.FaseDedicada_id
            });

        _mapper.Setup(m => m.Map<ResponseMaquinaDto>(It.IsAny<Maquina>()))
            .Returns((Maquina maquina) => new ResponseMaquinaDto
            {
                Maquina_id = maquina.Maquina_id,
                Numero = maquina.Numero,
                NomeModelo = maquina.NomeModelo,
                IpAddress = maquina.IpAddress,
                Estado = maquina.Estado,
                FaseDedicada_id = maquina.FaseDedicada_id
            });

        _mapper.Setup(m => m.Map<IEnumerable<ResponseMaquinaDto>>(It.IsAny<IEnumerable<Maquina>>()))
            .Returns((IEnumerable<Maquina> items) => items.Select(maquina => new ResponseMaquinaDto
            {
                Maquina_id = maquina.Maquina_id,
                Numero = maquina.Numero,
                NomeModelo = maquina.NomeModelo,
                IpAddress = maquina.IpAddress,
                Estado = maquina.Estado,
                FaseDedicada_id = maquina.FaseDedicada_id
            }).ToList());

        _mapper.Setup(m => m.Map(It.IsAny<UpdateMaquinaDto>(), It.IsAny<Maquina>()))
            .Callback((UpdateMaquinaDto dto, Maquina entity) =>
            {
                if (dto.Numero.HasValue)
                    entity.Numero = dto.Numero.Value;

                if (!string.IsNullOrWhiteSpace(dto.NomeModelo))
                    entity.NomeModelo = dto.NomeModelo.Trim();

                if (!string.IsNullOrWhiteSpace(dto.IpAddress))
                    entity.IpAddress = dto.IpAddress.Trim();

                if (dto.Estado.HasValue)
                    entity.Estado = dto.Estado.Value;

                if (dto.FaseDedicada_id.HasValue)
                    entity.FaseDedicada_id = dto.FaseDedicada_id.Value;
            });

        _sut = new MaquinaService(
            _maquinaRepository.Object,
            _mapper.Object,
            _logger.Object);
    }

    private static Maquina BuildMaquina(
        int id = 1,
        int numero = 10,
        string nomeModelo = "Makino V33",
        string? ipAddress = "192.168.1.10",
        EstadoMaquina estado = EstadoMaquina.DISPONIVEL,
        int faseDedicadaId = 5)
    {
        return new Maquina
        {
            Maquina_id = id,
            Numero = numero,
            NomeModelo = nomeModelo,
            IpAddress = ipAddress,
            Estado = estado,
            FaseDedicada_id = faseDedicadaId
        };
    }

    private static CreateMaquinaDto BuildCreateDto(
        int id = 1,
        int numero = 10,
        string nomeModelo = "Makino V33",
        string? ipAddress = "192.168.1.10",
        EstadoMaquina estado = EstadoMaquina.DISPONIVEL,
        int faseDedicadaId = 5)
    {
        return new CreateMaquinaDto
        {
            Maquina_id = id,
            Numero = numero,
            NomeModelo = nomeModelo,
            IpAddress = ipAddress,
            Estado = estado,
            FaseDedicada_id = faseDedicadaId
        };
    }

    [Test(Description = "TMAQSERV1 - Create deve falhar com conflito quando numero ja existe.")]
    public async Task CreateAsync_Should_ThrowBusinessConflictException_When_NumeroAlreadyExists()
    {
        // ARRANGE
        var dto = BuildCreateDto(id: 30, numero: 7);
        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(dto.Maquina_id))
            .ReturnsAsync((Maquina?)null);
        _maquinaRepository.Setup(r => r.ExistsNumeroAsync(dto.Numero, null))
            .ReturnsAsync(true);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<BusinessConflictException>()
            .WithMessage("*numero*7*");
    }

    [Test(Description = "TMAQSERV2 - Create deve falhar quando a fase dedicada nao existe.")]
    public async Task CreateAsync_Should_ThrowKeyNotFoundException_When_FaseDedicadaDoesNotExist()
    {
        // ARRANGE
        var dto = BuildCreateDto(id: 31, numero: 8, faseDedicadaId: 99);
        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(dto.Maquina_id))
            .ReturnsAsync((Maquina?)null);
        _maquinaRepository.Setup(r => r.ExistsNumeroAsync(dto.Numero, null))
            .ReturnsAsync(false);
        _maquinaRepository.Setup(r => r.ExistsFaseDedicadaAsync(dto.FaseDedicada_id))
            .ReturnsAsync(false);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*99*");
    }

    [Test(Description = "TMAQSERV3 - Create deve devolver response criada quando os dados sao validos.")]
    public async Task CreateAsync_Should_ReturnCreatedResponse_When_DataIsValid()
    {
        // ARRANGE
        var dto = BuildCreateDto(
            id: 32,
            numero: 9,
            nomeModelo: "  Haas VF2  ",
            ipAddress: " 10.0.0.9 ",
            estado: EstadoMaquina.DISPONIVEL,
            faseDedicadaId: 6);

        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(dto.Maquina_id))
            .ReturnsAsync((Maquina?)null);
        _maquinaRepository.Setup(r => r.ExistsNumeroAsync(dto.Numero, null))
            .ReturnsAsync(false);
        _maquinaRepository.Setup(r => r.ExistsFaseDedicadaAsync(dto.FaseDedicada_id))
            .ReturnsAsync(true);
        _maquinaRepository.Setup(r => r.CreateAsync(It.IsAny<Maquina>()))
            .ReturnsAsync((Maquina maquina) => maquina);

        // ACT
        var result = await _sut.CreateAsync(dto);

        // ASSERT
        result.Maquina_id.Should().Be(32);
        result.Numero.Should().Be(9);
        result.NomeModelo.Should().Be("Haas VF2");
        result.IpAddress.Should().Be("10.0.0.9");
        result.Estado.Should().Be(EstadoMaquina.DISPONIVEL);
        result.FaseDedicada_id.Should().Be(6);

        _maquinaRepository.Verify(r => r.CreateAsync(It.Is<Maquina>(m =>
            m.Maquina_id == 32 &&
            m.Numero == 9 &&
            m.NomeModelo == "Haas VF2" &&
            m.IpAddress == "10.0.0.9" &&
            m.Estado == EstadoMaquina.DISPONIVEL &&
            m.FaseDedicada_id == 6)), Times.Once);
    }

    [Test(Description = "TMAQSERV4 - Update deve preservar estado quando o campo e omitido.")]
    public async Task UpdateAsync_Should_PreserveEstado_When_EstadoIsOmitted()
    {
        // ARRANGE
        var existing = BuildMaquina(
            id: 40,
            numero: 15,
            nomeModelo: "Makino Antiga",
            ipAddress: "192.168.0.15",
            estado: EstadoMaquina.EM_USO,
            faseDedicadaId: 8);

        var dto = new UpdateMaquinaDto
        {
            NomeModelo = "  Makino Atualizada  ",
            IpAddress = " 10.10.10.15 "
        };

        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(40))
            .ReturnsAsync(existing);

        // ACT
        await _sut.UpdateAsync(40, dto);

        // ASSERT
        _maquinaRepository.Verify(r => r.UpdateExistingAsync(It.Is<Maquina>(m =>
            m.Maquina_id == 40 &&
            m.Numero == 15 &&
            m.NomeModelo == "Makino Atualizada" &&
            m.IpAddress == "10.10.10.15" &&
            m.Estado == EstadoMaquina.EM_USO &&
            m.FaseDedicada_id == 8)), Times.Once);
    }

    [Test(Description = "TMAQSERV5 - Update deve falhar com conflito quando o numero ja existe noutra maquina.")]
    public async Task UpdateAsync_Should_ThrowBusinessConflictException_When_NumeroAlreadyExistsInAnotherMaquina()
    {
        // ARRANGE
        var existing = BuildMaquina(id: 41, numero: 20, estado: EstadoMaquina.DISPONIVEL);
        var dto = new UpdateMaquinaDto { Numero = 99 };

        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(41))
            .ReturnsAsync(existing);
        _maquinaRepository.Setup(r => r.ExistsNumeroAsync(99, 41))
            .ReturnsAsync(true);

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(41, dto);

        // ASSERT
        await act.Should().ThrowAsync<BusinessConflictException>()
            .WithMessage("*99*");
    }

    [Test(Description = "TMAQSERV6 - Update deve falhar quando nenhum campo e enviado.")]
    public async Task UpdateAsync_Should_ThrowArgumentException_When_NoFieldsProvided()
    {
        // ARRANGE
        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(42))
            .ReturnsAsync(BuildMaquina(id: 42));

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(42, new UpdateMaquinaDto());

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Pelo menos um campo*");
    }

    [Test(Description = "TMAQSERV7 - GetAll deve mapear o resultado paginado para response dto.")]
    public async Task GetAllAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var paged = new PagedResult<Maquina>(
            new[] { BuildMaquina(id: 1), BuildMaquina(id: 2, numero: 20) },
            2,
            1,
            10);

        _maquinaRepository.Setup(r => r.GetAllAsync(1, 10))
            .ReturnsAsync(paged);

        // ACT
        var result = await _sut.GetAllAsync(1, 10);

        // ASSERT
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(m => m.Maquina_id).Should().Contain(ExpectedMaquinaIds);
    }

    [Test(Description = "TMAQSERV8 - GetByEstado deve mapear o resultado paginado por estado.")]
    public async Task GetByEstadoAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var paged = new PagedResult<Maquina>(
            new[] { BuildMaquina(id: 9, estado: EstadoMaquina.EM_USO) },
            1,
            2,
            10);

        _maquinaRepository.Setup(r => r.GetByEstadoAsync(EstadoMaquina.EM_USO, 2, 10))
            .ReturnsAsync(paged);

        // ACT
        var result = await _sut.GetByEstadoAsync(EstadoMaquina.EM_USO, 2, 5);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.CurrentPage.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Single().Estado.Should().Be(EstadoMaquina.EM_USO);
    }

    [Test(Description = "TMAQSERV9 - Search deve devolver pagina vazia quando o termo e blank.")]
    public async Task SearchAsync_Should_ReturnEmptyPage_When_SearchTermIsBlank()
    {
        // ARRANGE

        // ACT
        var result = await _sut.SearchAsync("   ", 2, 5);

        // ASSERT
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
        _maquinaRepository.Verify(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Test(Description = "TMAQSERV10 - Search deve normalizar o termo e mapear o resultado paginado.")]
    public async Task SearchAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var paged = new PagedResult<Maquina>(
            new[] { BuildMaquina(id: 11, numero: 33, nomeModelo: "Erosao 1", estado: EstadoMaquina.MANUTENCAO, faseDedicadaId: 9) },
            1,
            1,
            10);

        _maquinaRepository.Setup(r => r.SearchAsync("erosao", 1, 10))
            .ReturnsAsync(paged);

        // ACT
        var result = await _sut.SearchAsync(" erosao ", 1, 10);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.Items.Single().Numero.Should().Be(33);
        result.Items.Single().NomeModelo.Should().Be("Erosao 1");
        result.Items.Single().FaseDedicada_id.Should().Be(9);

        _maquinaRepository.Verify(r => r.SearchAsync("erosao", 1, 10), Times.Once);
    }
}
