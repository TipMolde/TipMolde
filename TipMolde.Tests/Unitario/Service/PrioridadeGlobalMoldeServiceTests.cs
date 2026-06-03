using FluentAssertions;
using Moq;
using TipMolde.Application.Dtos.EncomendaMoldeDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

/// <summary>
/// Testes unitarios do servico de prioridade global de moldes.
/// </summary>
/// <remarks>
/// Garante a ordenacao operacional, o rebalanceamento de prioridades e o mapeamento paginado da fila global.
/// </remarks>
[TestFixture]
[Category("Unit")]
public sealed class PrioridadeGlobalMoldeServiceTests
{
    private Mock<IEncomendaMoldeRepository> _repo = null!;
    private PrioridadeGlobalMoldeService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<IEncomendaMoldeRepository>();
        _sut = new PrioridadeGlobalMoldeService(_repo.Object);
    }

    /// <summary>
    /// Cria uma associacao Encomenda-Molde com navegacoes minimas para os testes.
    /// </summary>
    /// <param name="id">Identificador da associacao.</param>
    /// <param name="prioridade">Prioridade atual.</param>
    /// <param name="dataEntrega">Data prevista de entrega.</param>
    /// <param name="numeroEncomenda">Numero funcional da encomenda.</param>
    /// <param name="numeroMolde">Numero funcional do molde.</param>
    /// <returns>Entidade pronta para compor cenarios de fila global.</returns>
    private static EncomendaMolde BuildLink(
        int id,
        int prioridade,
        DateTime dataEntrega,
        string numeroEncomenda,
        string numeroMolde) => new()
        {
            EncomendaMolde_id = id,
            Encomenda_id = id + 100,
            Molde_id = id + 200,
            Prioridade = prioridade,
            Quantidade = 1,
            DataEntregaPrevista = dataEntrega,
            Encomenda = new Encomenda
            {
                Encomenda_id = id + 100,
                NumeroEncomendaCliente = numeroEncomenda,
                Estado = EstadoEncomenda.EM_PRODUCAO,
                Cliente = new Cliente
                {
                    Cliente_id = id + 300,
                    Nome = $"Cliente {id}",
                    NIF = $"10000000{id}",
                    Sigla = $"C{id}"
                }
            },
            Molde = new Molde
            {
                Molde_id = id + 200,
                Numero = numeroMolde,
                Nome = $"Molde {id}",
                Numero_cavidades = 2,
                TipoPedido = TipoPedido.NOVO_MOLDE
            }
        };

    [Test(Description = "TPGMSRV1 - Recalcular deve ordenar por data de entrega e ID quando as prioridades estao desatualizadas.")]
    public async Task RecalcularAsync_Should_ReorderByDateThenId_When_PrioritiesAreOutdated()
    {
        // ARRANGE
        var items = new List<EncomendaMolde>
        {
            BuildLink(30, 9, new DateTime(2026, 7, 1), "ENC-030", "M-030"),
            BuildLink(20, 8, new DateTime(2026, 6, 18), "ENC-020", "M-020"),
            BuildLink(10, 7, new DateTime(2026, 6, 18), "ENC-010", "M-010")
        };

        _repo.Setup(r => r.GetFilaGlobalAbertosAsync()).ReturnsAsync(items);

        // ACT
        await _sut.RecalcularAsync();

        // ASSERT
        _repo.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<EncomendaMolde>>(links =>
            links.OrderBy(x => x.Prioridade).Select(x => x.EncomendaMolde_id).SequenceEqual(new[] { 10, 20, 30 }) &&
            links.Single(x => x.EncomendaMolde_id == 10).Prioridade == 1 &&
            links.Single(x => x.EncomendaMolde_id == 20).Prioridade == 2 &&
            links.Single(x => x.EncomendaMolde_id == 30).Prioridade == 3)),
            Times.Once);
    }

    [Test(Description = "TPGMSRV2 - Recalcular nao deve persistir quando a fila global ja esta ordenada.")]
    public async Task RecalcularAsync_Should_NotPersist_When_PrioritiesAlreadyMatch()
    {
        // ARRANGE
        var items = new List<EncomendaMolde>
        {
            BuildLink(10, 1, new DateTime(2026, 6, 18), "ENC-010", "M-010"),
            BuildLink(20, 2, new DateTime(2026, 6, 18), "ENC-020", "M-020"),
            BuildLink(30, 3, new DateTime(2026, 7, 1), "ENC-030", "M-030")
        };

        _repo.Setup(r => r.GetFilaGlobalAbertosAsync()).ReturnsAsync(items);

        // ACT
        await _sut.RecalcularAsync();

        // ASSERT
        _repo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<EncomendaMolde>>()), Times.Never);
    }

    [Test(Description = "TPGMSRV3 - GetFilaGlobal deve normalizar paginacao e mapear o resultado do repositorio para DTO.")]
    public async Task GetFilaGlobalAsync_Should_NormalizePaginationAndMapRepositoryResult()
    {
        // ARRANGE
        var entity = BuildLink(15, 1, new DateTime(2026, 6, 18), "ENC-015", "M-015");
        _repo.Setup(r => r.GetFilaGlobalAsync(1, 200))
            .ReturnsAsync(new PagedResult<EncomendaMolde>(new[] { entity }, 1, 1, 200));

        // ACT
        var result = await _sut.GetFilaGlobalAsync(page: 0, pageSize: 999);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(200);
        result.Items.Should().ContainSingle();

        var item = result.Items.Single();
        item.EncomendaMolde_id.Should().Be(15);
        item.NumeroEncomendaCliente.Should().Be("ENC-015");
        item.NomeCliente.Should().Be("Cliente 15");
        item.NumeroMolde.Should().Be("M-015");
        item.NomeMolde.Should().Be("Molde 15");
        item.EstadoEncomenda.Should().Be("EM_PRODUCAO");
    }
}
