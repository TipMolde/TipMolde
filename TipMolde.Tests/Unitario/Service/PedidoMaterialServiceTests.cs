using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TipMolde.Application.Dtos.PedidoMaterialDto;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface.Comercio.IFornecedor;
using TipMolde.Application.Interface.Comercio.IPedidoMaterial;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Application.Mappings;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class PedidoMaterialServiceTests
{
    private static readonly int[] ExpectedPecaIds = [1, 2];

    private Mock<IPedidoMaterialRepository> _pedidoRepository = null!;
    private Mock<IFornecedorRepository> _fornecedorRepository = null!;
    private Mock<IPecaRepository> _pecaRepository = null!;
    private Mock<IUserRepository> _userRepository = null!;
    private Mock<ILogger<PedidoMaterialService>> _logger = null!;
    private PedidoMaterialService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        // ARRANGE
        _pedidoRepository = new Mock<IPedidoMaterialRepository>();
        _fornecedorRepository = new Mock<IFornecedorRepository>();
        _pecaRepository = new Mock<IPecaRepository>();
        _userRepository = new Mock<IUserRepository>();
        _logger = new Mock<ILogger<PedidoMaterialService>>();

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<PedidoMaterialProfile>();
        });

        var mapper = mapperConfig.CreateMapper();

        _sut = new PedidoMaterialService(
            _pedidoRepository.Object,
            _fornecedorRepository.Object,
            _pecaRepository.Object,
            _userRepository.Object,
            mapper,
            _logger.Object);
    }

    private static CreatePedidoMaterialDto BuildCreateDto() => new()
    {
        Fornecedor_id = 10,
        Itens =
        {
            new CreateItemPedidoMaterialDto { Peca_id = 1, Quantidade = 2 },
            new CreateItemPedidoMaterialDto { Peca_id = 2, Quantidade = 4 }
        }
    };

    private static PedidoMaterial BuildPedido(int id = 1, EstadoPedido estado = EstadoPedido.PENDENTE) => new()
    {
        PedidoMaterial_id = id,
        Fornecedor_id = 10,
        DataPedido = new DateTime(2026, 4, 23, 10, 0, 0, DateTimeKind.Utc),
        Estado = estado,
        Itens =
        {
            new ItemPedidoMaterial { ItemPedidoMaterial_id = 1, Peca_id = 1, Quantidade = 2 },
            new ItemPedidoMaterial { ItemPedidoMaterial_id = 2, Peca_id = 2, Quantidade = 4 }
        }
    };

    private static IEnumerable<Peca> BuildPecas(params int[] ids) =>
        ids.Select(id => new Peca
        {
            Peca_id = id,
            Designacao = $"Peca {id}",
            Molde_id = 1
        });

    [Test(Description = "TPMSRV1 - Create deve persistir o agregado completo quando fornecedor e pecas existem.")]
    public async Task CreateAsync_Should_CreateAggregate_When_DataIsValid()
    {
        // ARRANGE
        var dto = BuildCreateDto();
        var created = BuildPedido(id: 55);

        _fornecedorRepository.Setup(r => r.GetByIdAsync(dto.Fornecedor_id)).ReturnsAsync(new Fornecedor
        {
            Fornecedor_id = dto.Fornecedor_id,
            Nome = "Fornecedor A",
            NIF = "123456789"
        });
        var pecas = BuildPecas(1, 2).ToList();
        _pecaRepository
            .Setup(r => r.GetByIdsAsync(
                It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(ExpectedPecaIds))))
            .ReturnsAsync(pecas);

        _pedidoRepository
            .Setup(r => r.AddAsync(It.IsAny<PedidoMaterial>()))
            .ReturnsAsync((PedidoMaterial entity) =>
            {
                entity.PedidoMaterial_id = 55;
                var index = 1;
                foreach (var item in entity.Itens)
                {
                    item.ItemPedidoMaterial_id = index++;
                }

                return entity;
            });

        _pedidoRepository
            .Setup(r => r.GetByIdWithItensAsync(55))
            .ReturnsAsync(created);

        // ACT
        var result = await _sut.CreateAsync(dto);

        // ASSERT
        result.PedidoMaterialId.Should().Be(55);
        result.Itens.Should().HaveCount(2);
        _pedidoRepository.Verify(r => r.AddAsync(It.Is<PedidoMaterial>(p =>
            p.Fornecedor_id == 10 &&
            p.Estado == EstadoPedido.PENDENTE &&
            p.Itens.Count == 2)), Times.Once);
    }

    [Test(Description = "TPMSRV2 - Create deve falhar quando o pedido contem pecas repetidas.")]
    public async Task CreateAsync_Should_ThrowArgumentException_When_PecaIsDuplicated()
    {
        // ARRANGE
        var dto = new CreatePedidoMaterialDto
        {
            Fornecedor_id = 10,
            Itens =
            {
                new CreateItemPedidoMaterialDto { Peca_id = 1, Quantidade = 2 },
                new CreateItemPedidoMaterialDto { Peca_id = 1, Quantidade = 5 }
            }
        };

        _fornecedorRepository.Setup(r => r.GetByIdAsync(dto.Fornecedor_id)).ReturnsAsync(new Fornecedor
        {
            Fornecedor_id = dto.Fornecedor_id,
            Nome = "Fornecedor A",
            NIF = "123456789"
        });

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*pecas repetidas*");
        _pedidoRepository.Verify(r => r.AddAsync(It.IsAny<PedidoMaterial>()), Times.Never);
    }

    [Test(Description = "TPMSRV3 - RegistarRececao deve falhar quando pedido ja se encontra recebido.")]
    public async Task RegistarRececaoAsync_Should_ThrowBusinessConflictException_When_PedidoAlreadyReceived()
    {
        // ARRANGE
        _pedidoRepository.Setup(r => r.GetByIdWithItensAsync(30))
            .ReturnsAsync(BuildPedido(id: 30, estado: EstadoPedido.RECEBIDO));

        _userRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new User
        {
            User_id = 7,
            Nome = "Operador",
            Email = "operador@tipmolde.pt",
            Password = "hash",
            Role = UserRole.GESTOR_PRODUCAO
        });

        // ACT
        Func<Task> act = () => _sut.RegistarRececaoAsync(30, 7);

        // ASSERT
        await act.Should().ThrowAsync<BusinessConflictException>();
        _pedidoRepository.Verify(r => r.RegistarRececaoAsync(It.IsAny<PedidoMaterial>(), It.IsAny<IEnumerable<Peca>>()), Times.Never);
    }

    [Test(Description = "TPMSRV4 - RegistarRececao deve desbloquear pecas e persistir rececao quando pedido e valido.")]
    public async Task RegistarRececaoAsync_Should_UpdatePedidoAndUnlockPecas_When_RequestIsValid()
    {
        // ARRANGE
        var pedido = BuildPedido(id: 41);
        var pecas = BuildPecas(1, 2).ToList();

        _pedidoRepository.Setup(r => r.GetByIdWithItensAsync(41)).ReturnsAsync(pedido);
        _userRepository.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(new User
        {
            User_id = 9,
            Nome = "Gestor",
            Email = "gestor@tipmolde.pt",
            Password = "hash",
            Role = UserRole.GESTOR_COMERCIAL
        });
        _pecaRepository
            .Setup(r => r.GetByIdsAsync(
                It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(ExpectedPecaIds))))
            .ReturnsAsync(pecas);

        // ACT
        await _sut.RegistarRececaoAsync(41, 9);

        // ASSERT
        pedido.Estado.Should().Be(EstadoPedido.RECEBIDO);
        pedido.UserConferente_id.Should().Be(9);
        pedido.DataRececao.Should().NotBeNull();
        pecas.Should().OnlyContain(p => p.MaterialRecebido);

        _pedidoRepository.Verify(r => r.RegistarRececaoAsync(
            It.Is<PedidoMaterial>(p => p.PedidoMaterial_id == 41 && p.Estado == EstadoPedido.RECEBIDO),
            It.Is<IEnumerable<Peca>>(list => list.All(p => p.MaterialRecebido))),
            Times.Once);
    }

    [Test(Description = "TPMSRV5 - Delete deve falhar quando pedido nao existe.")]
    public async Task DeleteAsync_Should_ThrowKeyNotFoundException_When_PedidoDoesNotExist()
    {
        // ARRANGE
        _pedidoRepository.Setup(r => r.GetByIdAsync(88)).ReturnsAsync((PedidoMaterial?)null);

        // ACT
        Func<Task> act = () => _sut.DeleteAsync(88);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
