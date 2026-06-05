using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TipMolde.Application.Dtos.EncomendaMoldeDto;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomenda;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

/// <summary>
/// Testes unitarios do servico de EncomendaMolde.
/// </summary>
/// <remarks>
/// Cobre validacoes da associacao, mapeamento paginado e integracao com a fila global.
/// </remarks>
[TestFixture]
[Category("Unit")]
public class EncomendaMoldeServiceTests
{
    private Mock<IEncomendaMoldeRepository> _repo = null!;
    private Mock<IEncomendaRepository> _encomendaRepo = null!;
    private Mock<IMoldeRepository> _moldeRepo = null!;
    private Mock<IPrioridadeGlobalMoldeService> _prioridadeGlobalMoldeService = null!;
    private Mock<IMapper> _mapper = null!;
    private Mock<ILogger<EncomendaMoldeService>> _logger = null!;
    private EncomendaMoldeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IEncomendaMoldeRepository>();
        _encomendaRepo = new Mock<IEncomendaRepository>();
        _moldeRepo = new Mock<IMoldeRepository>();
        _prioridadeGlobalMoldeService = new Mock<IPrioridadeGlobalMoldeService>();
        _mapper = new Mock<IMapper>();
        _logger = new Mock<ILogger<EncomendaMoldeService>>();

        _sut = new EncomendaMoldeService(
            _repo.Object,
            _encomendaRepo.Object,
            _moldeRepo.Object,
            _prioridadeGlobalMoldeService.Object,
            _mapper.Object,
            _logger.Object);
    }

    [Test(Description = "TENCMSRV1 - Create deve falhar com conflito quando ja existe o par Encomenda-Molde.")]
    public async Task CreateAsync_Should_ThrowBusinessConflictException_When_AssociationAlreadyExists()
    {
        // ARRANGE
        var dto = new CreateEncomendaMoldeDto
        {
            Encomenda_id = 1,
            Molde_id = 2,
            Quantidade = 10,
            Prioridade = 1,
            DataEntregaPrevista = DateTime.UtcNow.AddDays(7)
        };

        _encomendaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Encomenda { Encomenda_id = 1, NumeroEncomendaCliente = "ENC-1" });
        _moldeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Molde { Molde_id = 2, Numero = "M-1" });
        _repo.Setup(r => r.ExistsAssociationAsync(1, 2, null)).ReturnsAsync(true);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<BusinessConflictException>();
    }

    [Test(Description = "TENCMSRV2 - Update deve falhar quando nenhum campo de patch e enviado.")]
    public async Task UpdateAsync_Should_ThrowArgumentException_When_NoFieldsProvided()
    {
        // ARRANGE
        var dto = new UpdateEncomendaMoldeDto();
        _repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new EncomendaMolde
        {
            EncomendaMolde_id = 10,
            Encomenda_id = 1,
            Molde_id = 1,
            Quantidade = 5,
            Prioridade = 1,
            DataEntregaPrevista = DateTime.UtcNow
        });

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(10, dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "TENCMSRV3 - GetById deve devolver nulo quando associacao nao existe.")]
    public async Task GetByIdAsync_Should_ReturnNull_When_LinkDoesNotExist()
    {
        // ARRANGE
        _repo.Setup(r => r.GetByIdAsync(77)).ReturnsAsync((EncomendaMolde?)null);

        // ACT
        var result = await _sut.GetByIdAsync(77);

        // ASSERT
        result.Should().BeNull();
    }

    [Test(Description = "TENCMSRV4 - GetById deve mapear DTO quando associacao existe.")]
    public async Task GetByIdAsync_Should_MapResponse_When_LinkExists()
    {
        // ARRANGE
        var entity = new EncomendaMolde { EncomendaMolde_id = 10, Encomenda_id = 1, Molde_id = 2, Quantidade = 5, Prioridade = 1 };
        var dto = new ResponseEncomendaMoldeDto { EncomendaMolde_id = 10, Encomenda_id = 1, Molde_id = 2, Quantidade = 5, Prioridade = 1 };
        _repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(entity);
        _mapper.Setup(m => m.Map<ResponseEncomendaMoldeDto>(entity)).Returns(dto);

        // ACT
        var result = await _sut.GetByIdAsync(10);

        // ASSERT
        result.Should().BeEquivalentTo(dto);
    }

    [Test(Description = "TENCMSRV5 - GetByEncomendaId deve mapear payload paginado.")]
    public async Task GetByEncomendaIdAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var entity = new EncomendaMolde { EncomendaMolde_id = 3, Encomenda_id = 1, Molde_id = 2, Quantidade = 8, Prioridade = 2 };
        var dto = new ResponseEncomendaMoldeDto { EncomendaMolde_id = 3, Encomenda_id = 1, Molde_id = 2, Quantidade = 8, Prioridade = 2 };
        _repo.Setup(r => r.GetByEncomendaIdAsync(1, 2, 10))
            .ReturnsAsync(new PagedResult<EncomendaMolde>(new[] { entity }, 1, 2, 10));
        _mapper.Setup(m => m.Map<IEnumerable<ResponseEncomendaMoldeDto>>(It.IsAny<IEnumerable<EncomendaMolde>>()))
            .Returns(new[] { dto });

        // ACT
        var result = await _sut.GetByEncomendaIdAsync(1, 2, 5);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.Items.Single().EncomendaMolde_id.Should().Be(3);
    }

    [Test(Description = "TENCMSRV6 - GetByMoldeId deve mapear payload paginado.")]
    public async Task GetByMoldeIdAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var entity = new EncomendaMolde { EncomendaMolde_id = 4, Encomenda_id = 1, Molde_id = 9, Quantidade = 6, Prioridade = 1 };
        var dto = new ResponseEncomendaMoldeDto { EncomendaMolde_id = 4, Encomenda_id = 1, Molde_id = 9, Quantidade = 6, Prioridade = 1 };
        _repo.Setup(r => r.GetByMoldeIdAsync(9, 1, 10))
            .ReturnsAsync(new PagedResult<EncomendaMolde>(new[] { entity }, 1, 1, 10));
        _mapper.Setup(m => m.Map<IEnumerable<ResponseEncomendaMoldeDto>>(It.IsAny<IEnumerable<EncomendaMolde>>()))
            .Returns(new[] { dto });

        // ACT
        var result = await _sut.GetByMoldeIdAsync(9, 1, 10);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.Items.Single().Molde_id.Should().Be(9);
    }

    [Test(Description = "TENCMSRV6B - GetByEncomendasConfirmadas deve mapear payload paginado.")]
    public async Task GetByEncomendasConfirmadasAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var entity = new EncomendaMolde
        {
            EncomendaMolde_id = 6,
            Encomenda_id = 2,
            Molde_id = 10,
            Quantidade = 4,
            Prioridade = 1
        };

        var dto = new ResponseEncomendaMoldeDto
        {
            EncomendaMolde_id = 6,
            Encomenda_id = 2,
            Molde_id = 10,
            Quantidade = 4,
            Prioridade = 1
        };

        _repo.Setup(r => r.GetByEncomendasConfirmadasAsync(1, 10))
            .ReturnsAsync(new PagedResult<EncomendaMolde>(new[] { entity }, 1, 1, 10));
        _mapper.Setup(m => m.Map<IEnumerable<ResponseEncomendaMoldeDto>>(It.IsAny<IEnumerable<EncomendaMolde>>()))
            .Returns(new[] { dto });

        // ACT
        var result = await _sut.GetByEncomendasConfirmadasAsync(1, 10);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.Items.Single().EncomendaMolde_id.Should().Be(6);
    }

    [Test(Description = "TENCMSRV6A - GetFilaGlobal deve delegar o carregamento ao servico de prioridade global.")]
    public async Task GetFilaGlobalAsync_Should_DelegateToPriorityService()
    {
        // ARRANGE
        var paged = new PagedResult<FilaGlobalMoldeItemDto>(
            new[]
            {
                new FilaGlobalMoldeItemDto
                {
                    EncomendaMolde_id = 9,
                    Encomenda_id = 2,
                    Molde_id = 5,
                    Prioridade = 1,
                    DataEntregaPrevista = new DateTime(2026, 6, 18),
                    Quantidade = 3,
                    NumeroEncomendaCliente = "ENC-002",
                    NomeCliente = "Cliente A",
                    NumeroMolde = "M-005",
                    NomeMolde = "Molde A",
                    EstadoEncomenda = "EM_PRODUCAO"
                }
            },
            1,
            2,
            10);

        _prioridadeGlobalMoldeService
            .Setup(s => s.GetFilaGlobalAsync(2, 10))
            .ReturnsAsync(paged);

        // ACT
        var result = await _sut.GetFilaGlobalAsync(2, 10);

        // ASSERT
        result.Should().BeEquivalentTo(paged);
    }

    [Test(Description = "TENCMSRV7 - Create deve falhar quando encomenda nao existe.")]
    public async Task CreateAsync_Should_ThrowKeyNotFoundException_When_EncomendaDoesNotExist()
    {
        // ARRANGE
        var dto = new CreateEncomendaMoldeDto { Encomenda_id = 1, Molde_id = 2, Quantidade = 10, Prioridade = 1 };
        _encomendaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Encomenda?)null);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TENCMSRV8 - Create deve falhar quando molde nao existe.")]
    public async Task CreateAsync_Should_ThrowKeyNotFoundException_When_MoldeDoesNotExist()
    {
        // ARRANGE
        var dto = new CreateEncomendaMoldeDto { Encomenda_id = 1, Molde_id = 2, Quantidade = 10, Prioridade = 1 };
        _encomendaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Encomenda { Encomenda_id = 1, NumeroEncomendaCliente = "ENC-1" });
        _moldeRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((Molde?)null);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TENCMSRV9 - Update deve persistir campos enviados quando pedido e valido.")]
    public async Task UpdateAsync_Should_UpdateEntity_When_RequestIsValid()
    {
        // ARRANGE
        var existente = new EncomendaMolde
        {
            EncomendaMolde_id = 10,
            Encomenda_id = 1,
            Molde_id = 1,
            Quantidade = 5,
            Prioridade = 1,
            DataEntregaPrevista = new DateTime(2026, 5, 1)
        };

        _repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(existente);

        var dto = new UpdateEncomendaMoldeDto
        {
            Quantidade = 9,
            Prioridade = 3,
            DataEntregaPrevista = new DateTime(2026, 5, 10)
        };

        // ACT
        await _sut.UpdateAsync(10, dto);

        // ASSERT
        _repo.Verify(r => r.UpdateAsync(It.Is<EncomendaMolde>(e =>
            e.EncomendaMolde_id == 10 &&
            e.Quantidade == 9 &&
            e.Prioridade == 3 &&
            e.DataEntregaPrevista == new DateTime(2026, 5, 10))), Times.Once);
    }

    [Test(Description = "TENCMSRV9A - UpdateEstado deve falhar quando a associacao nao existe.")]
    public async Task UpdateEstadoAsync_Should_ThrowKeyNotFoundException_When_LinkDoesNotExist()
    {
        // ARRANGE
        _repo.Setup(r => r.GetByIdWithEncomendaAsync(88)).ReturnsAsync((EncomendaMolde?)null);

        // ACT
        Func<Task> act = () => _sut.UpdateEstadoAsync(88, new UpdateEstadoEncomendaMoldeDto
        {
            Estado = EstadoEncomendaMolde.CONCLUIDO
        });

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TENCMSRV9B - UpdateEstado deve colocar a encomenda como parcialmente entregue quando ainda existem outros moldes por concluir.")]
    public async Task UpdateEstadoAsync_Should_SetEncomendaToParcialmenteEntregue_When_OtherMoldesRemain()
    {
        // ARRANGE
        var encomenda = new Encomenda
        {
            Encomenda_id = 3,
            NumeroEncomendaCliente = "ENC-003",
            Estado = EstadoEncomenda.EM_PRODUCAO
        };

        var link = new EncomendaMolde
        {
            EncomendaMolde_id = 15,
            Encomenda_id = 3,
            Encomenda = encomenda,
            Molde_id = 8,
            Estado = EstadoEncomendaMolde.EM_PRODUCAO
        };

        _repo.Setup(r => r.GetByIdWithEncomendaAsync(15)).ReturnsAsync(link);
        _repo.Setup(r => r.HasMoldesNaoConcluidosAsync(3, 15)).ReturnsAsync(true);

        // ACT
        await _sut.UpdateEstadoAsync(15, new UpdateEstadoEncomendaMoldeDto
        {
            Estado = EstadoEncomendaMolde.CONCLUIDO
        });

        // ASSERT
        encomenda.Estado.Should().Be(EstadoEncomenda.PARCIALMENTE_ENTREGUE);
        link.Estado.Should().Be(EstadoEncomendaMolde.CONCLUIDO);
        _repo.Verify(r => r.UpdateAsync(It.Is<EncomendaMolde>(e =>
            e.EncomendaMolde_id == 15 &&
            e.Estado == EstadoEncomendaMolde.CONCLUIDO &&
            e.Encomenda != null &&
            e.Encomenda.Estado == EstadoEncomenda.PARCIALMENTE_ENTREGUE)), Times.Once);
        _prioridadeGlobalMoldeService.Verify(s => s.RecalcularAsync(), Times.Once);
    }

    [Test(Description = "TENCMSRV9C - UpdateEstado deve concluir a encomenda quando o molde atualizado e o ultimo por concluir.")]
    public async Task UpdateEstadoAsync_Should_SetEncomendaToConcluida_When_LastMoldeIsDelivered()
    {
        // ARRANGE
        var encomenda = new Encomenda
        {
            Encomenda_id = 4,
            NumeroEncomendaCliente = "ENC-004",
            Estado = EstadoEncomenda.PARCIALMENTE_ENTREGUE
        };

        var link = new EncomendaMolde
        {
            EncomendaMolde_id = 21,
            Encomenda_id = 4,
            Encomenda = encomenda,
            Molde_id = 11,
            Estado = EstadoEncomendaMolde.EM_PRODUCAO
        };

        _repo.Setup(r => r.GetByIdWithEncomendaAsync(21)).ReturnsAsync(link);
        _repo.Setup(r => r.HasMoldesNaoConcluidosAsync(4, 21)).ReturnsAsync(false);

        // ACT
        await _sut.UpdateEstadoAsync(21, new UpdateEstadoEncomendaMoldeDto
        {
            Estado = EstadoEncomendaMolde.CONCLUIDO
        });

        // ASSERT
        encomenda.Estado.Should().Be(EstadoEncomenda.CONCLUIDA);
        _repo.Verify(r => r.UpdateAsync(It.Is<EncomendaMolde>(e =>
            e.EncomendaMolde_id == 21 &&
            e.Estado == EstadoEncomendaMolde.CONCLUIDO &&
            e.Encomenda != null &&
            e.Encomenda.Estado == EstadoEncomenda.CONCLUIDA)), Times.Once);
        _prioridadeGlobalMoldeService.Verify(s => s.RecalcularAsync(), Times.Once);
    }

    [Test(Description = "TENCMSRV9D - UpdateEstado deve colocar a encomenda em producao quando o primeiro molde arranca.")]
    public async Task UpdateEstadoAsync_Should_SetEncomendaToEmProducao_When_FirstMoldeStarts()
    {
        // ARRANGE
        var encomenda = new Encomenda
        {
            Encomenda_id = 5,
            NumeroEncomendaCliente = "ENC-005",
            Estado = EstadoEncomenda.CONFIRMADA
        };

        var link = new EncomendaMolde
        {
            EncomendaMolde_id = 30,
            Encomenda_id = 5,
            Encomenda = encomenda,
            Molde_id = 14,
            Estado = EstadoEncomendaMolde.PENDENTE
        };

        _repo.Setup(r => r.GetByIdWithEncomendaAsync(30)).ReturnsAsync(link);

        // ACT
        await _sut.UpdateEstadoAsync(30, new UpdateEstadoEncomendaMoldeDto
        {
            Estado = EstadoEncomendaMolde.EM_PRODUCAO
        });

        // ASSERT
        encomenda.Estado.Should().Be(EstadoEncomenda.EM_PRODUCAO);
        _repo.Verify(r => r.UpdateAsync(It.Is<EncomendaMolde>(e =>
            e.EncomendaMolde_id == 30 &&
            e.Estado == EstadoEncomendaMolde.EM_PRODUCAO &&
            e.Encomenda != null &&
            e.Encomenda.Estado == EstadoEncomenda.EM_PRODUCAO)), Times.Once);
        _prioridadeGlobalMoldeService.Verify(s => s.RecalcularAsync(), Times.Never);
    }

    [Test(Description = "TENCMSRV10 - Delete deve falhar quando associacao nao existe.")]
    public async Task DeleteAsync_Should_ThrowKeyNotFoundException_When_LinkDoesNotExist()
    {
        // ARRANGE
        _repo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync((EncomendaMolde?)null);

        // ACT
        Func<Task> act = () => _sut.DeleteAsync(50);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
