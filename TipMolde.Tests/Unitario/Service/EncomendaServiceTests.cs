using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TipMolde.Application.Dtos.EncomendaDto;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.ICliente;
using TipMolde.Application.Interface.Comercio.IEncomenda;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class EncomendaServiceTests
{
    private static readonly int[] ExpectedEncomendaIds = [1, 2];

    private Mock<IEncomendaRepository> _encomendaRepository = null!;
    private Mock<IClienteRepository> _clienteRepository = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<ILogger<EncomendaService>> _logger = null!;
    private EncomendaService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _encomendaRepository = new Mock<IEncomendaRepository>();
        _clienteRepository = new Mock<IClienteRepository>();
        _mapper = new Mock<IMapper>();
        _logger = new Mock<ILogger<EncomendaService>>();

        _mapper.Setup(m => m.Map<ResponseEncomendaDto>(It.IsAny<Encomenda>()))
            .Returns((Encomenda e) => new ResponseEncomendaDto
            {
                Encomenda_id = e.Encomenda_id,
                NumeroEncomendaCliente = e.NumeroEncomendaCliente,
                NumeroProjetoCliente = e.NumeroProjetoCliente,
                NomeServicoCliente = e.NomeServicoCliente,
                NomeResponsavelCliente = e.NomeResponsavelCliente,
                DataRegisto = e.DataRegisto,
                Estado = e.Estado,
                Cliente_id = e.Cliente_id
            });

        _mapper.Setup(m => m.Map<IEnumerable<ResponseEncomendaDto>>(It.IsAny<IEnumerable<Encomenda>>()))
            .Returns((IEnumerable<Encomenda> list) => list.Select(e => new ResponseEncomendaDto
            {
                Encomenda_id = e.Encomenda_id,
                NumeroEncomendaCliente = e.NumeroEncomendaCliente,
                NumeroProjetoCliente = e.NumeroProjetoCliente,
                NomeServicoCliente = e.NomeServicoCliente,
                NomeResponsavelCliente = e.NomeResponsavelCliente,
                DataRegisto = e.DataRegisto,
                Estado = e.Estado,
                Cliente_id = e.Cliente_id
            }).ToList());

        _mapper.Setup(m => m.Map<Encomenda>(It.IsAny<CreateEncomendaDto>()))
            .Returns((CreateEncomendaDto dto) => new Encomenda
            {
                Cliente_id = dto.Cliente_id,
                NumeroEncomendaCliente = dto.NumeroEncomendaCliente,
                NumeroProjetoCliente = dto.NumeroProjetoCliente,
                NomeServicoCliente = dto.NomeServicoCliente,
                NomeResponsavelCliente = dto.NomeResponsavelCliente
            });

        _mapper.Setup(m => m.Map(It.IsAny<UpdateEncomendaDto>(), It.IsAny<Encomenda>()))
            .Callback((UpdateEncomendaDto dto, Encomenda entity) =>
            {
                if (!string.IsNullOrWhiteSpace(dto.NumeroEncomendaCliente))
                    entity.NumeroEncomendaCliente = dto.NumeroEncomendaCliente.Trim();

                if (!string.IsNullOrWhiteSpace(dto.NumeroProjetoCliente))
                    entity.NumeroProjetoCliente = dto.NumeroProjetoCliente.Trim();

                if (!string.IsNullOrWhiteSpace(dto.NomeServicoCliente))
                    entity.NomeServicoCliente = dto.NomeServicoCliente.Trim();

                if (!string.IsNullOrWhiteSpace(dto.NomeResponsavelCliente))
                    entity.NomeResponsavelCliente = dto.NomeResponsavelCliente.Trim();
            });

        _mapper.Setup(m => m.Map(It.IsAny<UpdateEstadoEncomendaDto>(), It.IsAny<Encomenda>()))
            .Callback((UpdateEstadoEncomendaDto dto, Encomenda entity) =>
            {
                entity.Estado = dto.Estado;
            });

        _sut = new EncomendaService(
            _encomendaRepository.Object,
            _clienteRepository.Object,
            _mapper.Object,
            _logger.Object);
    }

    private static Encomenda BuildEncomenda(int id = 1, string numero = "ENC-001", EstadoEncomenda estado = EstadoEncomenda.CONFIRMADA)
    {
        return new Encomenda
        {
            Encomenda_id = id,
            NumeroEncomendaCliente = numero,
            NumeroProjetoCliente = "PRJ-1",
            NomeServicoCliente = "Servico",
            NomeResponsavelCliente = "Maria",
            Cliente_id = 10,
            Estado = estado,
            DataRegisto = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc)
        };
    }

    [Test(Description = "TENCSRV1 - Create deve falhar quando numero de encomenda e vazio.")]
    public async Task CreateAsync_Should_ThrowArgumentException_When_NumeroIsBlank()
    {
        // ARRANGE
        var dto = new CreateEncomendaDto
        {
            Cliente_id = 10,
            NumeroEncomendaCliente = "   "
        };

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*numero de encomenda*");
    }

    [Test(Description = "TENCSRV2 - Create deve falhar quando cliente nao existe.")]
    public async Task CreateAsync_Should_ThrowKeyNotFoundException_When_ClienteDoesNotExist()
    {
        // ARRANGE
        var dto = new CreateEncomendaDto
        {
            Cliente_id = 10,
            NumeroEncomendaCliente = "ENC-001"
        };
        _clienteRepository.Setup(r => r.GetByIdAsync(dto.Cliente_id)).ReturnsAsync((Cliente?)null);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{dto.Cliente_id}*");
    }

    [Test(Description = "TENCSRV3 - Create deve falhar com conflito quando numero ja existe.")]
    public async Task CreateAsync_Should_ThrowBusinessConflictException_When_NumeroAlreadyExists()
    {
        // ARRANGE
        var dto = new CreateEncomendaDto
        {
            Cliente_id = 10,
            NumeroEncomendaCliente = " ENC-100 "
        };

        _clienteRepository.Setup(r => r.GetByIdAsync(dto.Cliente_id)).ReturnsAsync(new Cliente
        {
            Cliente_id = dto.Cliente_id,
            Nome = "Cliente",
            NIF = "123456789",
            Sigla = "CLI"
        });
        _encomendaRepository.Setup(r => r.ExistsNumeroEncomendaClienteAsync("ENC-100", null)).ReturnsAsync(true);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<BusinessConflictException>()
            .WithMessage("*ENC-100*");
    }

    [Test(Description = "TENCSRV4 - Create deve normalizar numero e definir estado inicial confirmada.")]
    public async Task CreateAsync_Should_SetDefaultsAndPersist_When_DataIsValid()
    {
        // ARRANGE
        var dto = new CreateEncomendaDto
        {
            Cliente_id = 10,
            NumeroEncomendaCliente = " ENC-200 ",
            NumeroProjetoCliente = "PRJ-200",
            NomeServicoCliente = "Servico",
            NomeResponsavelCliente = "Ana"
        };

        _clienteRepository.Setup(r => r.GetByIdAsync(dto.Cliente_id)).ReturnsAsync(new Cliente
        {
            Cliente_id = dto.Cliente_id,
            Nome = "Cliente",
            NIF = "123456789",
            Sigla = "CLI"
        });
        _encomendaRepository
            .Setup(r => r.ExistsNumeroEncomendaClienteAsync("ENC-200", It.IsAny<int?>()))
            .ReturnsAsync(false);
        _encomendaRepository.Setup(r => r.AddAsync(It.IsAny<Encomenda>()))
            .ReturnsAsync((Encomenda e) =>
            {
                e.Encomenda_id = 55;
                return e;
            });

        // ACT
        var result = await _sut.CreateAsync(dto);

        // ASSERT
        result.Encomenda_id.Should().Be(55);
        result.NumeroEncomendaCliente.Should().Be("ENC-200");
        result.Estado.Should().Be(EstadoEncomenda.CONFIRMADA);
        _encomendaRepository.Verify(r => r.AddAsync(It.IsAny<Encomenda>()), Times.Once);
    }

    [Test(Description = "TENCSRV5 - Update deve falhar quando encomenda nao existe.")]
    public async Task UpdateAsync_Should_ThrowKeyNotFoundException_When_EncomendaDoesNotExist()
    {
        // ARRANGE
        var dto = new UpdateEncomendaDto { NumeroEncomendaCliente = "ENC-500" };
        _encomendaRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Encomenda?)null);

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(99, dto);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TENCSRV6 - Update deve falhar com conflito quando novo numero ja existe noutra encomenda.")]
    public async Task UpdateAsync_Should_ThrowBusinessConflictException_When_NewNumeroAlreadyExists()
    {
        // ARRANGE
        var existing = BuildEncomenda(id: 10, numero: "ENC-010");
        var dto = new UpdateEncomendaDto { NumeroEncomendaCliente = " ENC-999 " };

        _encomendaRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(existing);
        _encomendaRepository.Setup(r => r.ExistsNumeroEncomendaClienteAsync("ENC-999", 10)).ReturnsAsync(true);

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(10, dto);

        // ASSERT
        await act.Should().ThrowAsync<BusinessConflictException>();
    }

    [Test(Description = "TENCSRV7 - Update deve atualizar campos permitidos e manter valores nao informados.")]
    public async Task UpdateAsync_Should_UpdatePartialFields_When_DataIsValid()
    {
        // ARRANGE
        var existing = BuildEncomenda(id: 20, numero: "ENC-020");
        existing.NumeroProjetoCliente = "PRJ-OLD";
        existing.NomeServicoCliente = "Servico Antigo";
        existing.NomeResponsavelCliente = "Ana";

        var dto = new UpdateEncomendaDto
        {
            NumeroEncomendaCliente = " ENC-021 ",
            NomeServicoCliente = "Servico Novo"
        };

        _encomendaRepository.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(existing);
        _encomendaRepository.Setup(r => r.ExistsNumeroEncomendaClienteAsync("ENC-021", 20)).ReturnsAsync(false);

        // ACT
        await _sut.UpdateAsync(20, dto);

        // ASSERT
        _encomendaRepository.Verify(r => r.UpdateAsync(It.Is<Encomenda>(e =>
            e.Encomenda_id == 20 &&
            e.NumeroEncomendaCliente == "ENC-021" &&
            e.NumeroProjetoCliente == "PRJ-OLD" &&
            e.NomeServicoCliente == "Servico Novo" &&
            e.NomeResponsavelCliente == "Ana")), Times.Once);
    }

    [Test(Description = "TENCSRV8 - UpdateEstado deve devolver erro quando encomenda nao existe.")]
    public async Task UpdateEstadoAsync_Should_ThrowKeyNotFoundException_When_EncomendaDoesNotExist()
    {
        // ARRANGE
        _encomendaRepository.Setup(r => r.GetByIdAsync(45)).ReturnsAsync((Encomenda?)null);

        // ACT
        Func<Task> act = () => _sut.UpdateEstadoAsync(45, new UpdateEstadoEncomendaDto { Estado = EstadoEncomenda.EM_PRODUCAO });

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TENCSRV9 - UpdateEstado deve falhar em transicao invalida.")]
    public async Task UpdateEstadoAsync_Should_ThrowArgumentException_When_TransitionIsInvalid()
    {
        // ARRANGE
        var encomenda = BuildEncomenda(id: 30, estado: EstadoEncomenda.CONCLUIDA);
        _encomendaRepository.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(encomenda);

        // ACT
        Func<Task> act = () => _sut.UpdateEstadoAsync(30, new UpdateEstadoEncomendaDto { Estado = EstadoEncomenda.EM_PRODUCAO });

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Transicao de estado invalida*");
    }

    [Test(Description = "TENCSRV10 - Delete deve apagar encomenda quando registo existe.")]
    public async Task DeleteAsync_Should_Delete_When_EncomendaExists()
    {
        // ARRANGE
        _encomendaRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(BuildEncomenda(id: 7));

        // ACT
        await _sut.DeleteAsync(7);

        // ASSERT
        _encomendaRepository.Verify(r => r.DeleteAsync(7), Times.Once);
    }

    [Test(Description = "TENCSRV11 - GetByNumero deve falhar quando numero e vazio.")]
    public async Task GetByNumeroEncomendaClienteAsync_Should_ThrowArgumentException_When_NumeroIsBlank()
    {
        // ARRANGE

        // ACT
        Func<Task> act = async () => await _sut.GetByNumeroEncomendaClienteAsync("   ");

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*obrigatorio*");
    }

    [Test(Description = "TENCSRV12 - GetAll deve mapear encomendas para DTO paginado.")]
    public async Task GetAllAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var paged = new PagedResult<Encomenda>(new[] { BuildEncomenda(id: 1), BuildEncomenda(id: 2, numero: "ENC-002") }, 2, 1, 10);
        _encomendaRepository.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync(paged);

        // ACT
        var result = await _sut.GetAllAsync(1, 10);

        // ASSERT
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.Encomenda_id).Should().Contain(ExpectedEncomendaIds);
    }

    [Test(Description = "TENCSRV13 - GetById deve devolver nulo quando encomenda nao existe.")]
    public async Task GetByIdAsync_Should_ReturnNull_When_EncomendaDoesNotExist()
    {
        // ARRANGE
        _encomendaRepository.Setup(r => r.GetByIdAsync(70)).ReturnsAsync((Encomenda?)null);

        // ACT
        var result = await _sut.GetByIdAsync(70);

        // ASSERT
        result.Should().BeNull();
    }

    [Test(Description = "TENCSRV14 - GetEncomendaWithMoldes deve mapear dto quando registo existe.")]
    public async Task GetEncomendaWithMoldesAsync_Should_MapResponse_When_EncomendaExists()
    {
        // ARRANGE
        var encomenda = BuildEncomenda(id: 30, numero: "ENC-030");
        _encomendaRepository.Setup(r => r.GetWithMoldesAsync(30)).ReturnsAsync(encomenda);

        // ACT
        var result = await _sut.GetEncomendaWithMoldesAsync(30);

        // ASSERT
        result.Should().NotBeNull();
        result!.Encomenda_id.Should().Be(30);
        result.NumeroEncomendaCliente.Should().Be("ENC-030");
    }

    [Test(Description = "TENCSRV15 - GetByEstado deve mapear payload paginado.")]
    public async Task GetByEstadoAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var paged = new PagedResult<Encomenda>(
            new[] { BuildEncomenda(id: 8, numero: "ENC-008", estado: EstadoEncomenda.EM_PRODUCAO) },
            1,
            2,
            10);
        _encomendaRepository.Setup(r => r.GetByEstadoAsync(EstadoEncomenda.EM_PRODUCAO, 2, 10)).ReturnsAsync(paged);

        // ACT
        var result = await _sut.GetByEstadoAsync(EstadoEncomenda.EM_PRODUCAO, 2, 5);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.Items.Single().Estado.Should().Be(EstadoEncomenda.EM_PRODUCAO);
    }

    [Test(Description = "TENCSRV16 - GetPorConcluir deve mapear payload paginado.")]
    public async Task GetEncomendasPorConcluirAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var paged = new PagedResult<Encomenda>(new[] { BuildEncomenda(id: 9, numero: "ENC-009") }, 1, 1, 10);
        _encomendaRepository.Setup(r => r.GetEncomendasPorConcluirAsync(1, 10)).ReturnsAsync(paged);

        // ACT
        var result = await _sut.GetEncomendasPorConcluirAsync(1, 10);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.Items.Single().Encomenda_id.Should().Be(9);
    }

    [Test(Description = "TENCSRV17 - GetByNumero deve devolver dto quando encomenda existe.")]
    public async Task GetByNumeroEncomendaClienteAsync_Should_ReturnResponse_When_EncomendaExists()
    {
        // ARRANGE
        _encomendaRepository
            .Setup(r => r.GetByNumeroEncomendaClienteAsync("ENC-050"))
            .ReturnsAsync(BuildEncomenda(id: 50, numero: "ENC-050"));

        // ACT
        var result = await _sut.GetByNumeroEncomendaClienteAsync("ENC-050");

        // ASSERT
        result.Should().NotBeNull();
        result!.Encomenda_id.Should().Be(50);
    }

    [Test(Description = "TENCSRV18 - Update deve falhar quando nenhum campo e enviado.")]
    public async Task UpdateAsync_Should_ThrowArgumentException_When_NoFieldsProvided()
    {
        // ARRANGE
        _encomendaRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(BuildEncomenda(id: 10));

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(10, new UpdateEncomendaDto());

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "TENCSRV19 - UpdateEstado deve persistir quando transicao e valida.")]
    public async Task UpdateEstadoAsync_Should_UpdateEntity_When_TransitionIsValid()
    {
        // ARRANGE
        var encomenda = BuildEncomenda(id: 60, estado: EstadoEncomenda.CONFIRMADA);
        _encomendaRepository.Setup(r => r.GetByIdAsync(60)).ReturnsAsync(encomenda);

        // ACT
        await _sut.UpdateEstadoAsync(60, new UpdateEstadoEncomendaDto { Estado = EstadoEncomenda.EM_PRODUCAO });

        // ASSERT
        _encomendaRepository.Verify(r => r.UpdateAsync(It.Is<Encomenda>(e =>
            e.Encomenda_id == 60 &&
            e.Estado == EstadoEncomenda.EM_PRODUCAO)), Times.Once);
    }
}
