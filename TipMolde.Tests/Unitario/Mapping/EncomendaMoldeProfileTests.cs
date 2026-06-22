using AutoMapper;
using FluentAssertions;
using TipMolde.Application.Dtos.EncomendaMoldeDto;
using TipMolde.Application.Mappings;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Mapping;

[TestFixture]
[Category("Unit")]
public class EncomendaMoldeProfileTests
{
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EncomendaMoldeProfile>());
        _mapper = config.CreateMapper();
    }

    [Test(Description = "TENCMMAP1 - Configuracao AutoMapper de EncomendaMoldeProfile deve ser valida.")]
    public void MappingConfiguration_Should_BeValid()
    {
        // ARRANGE
        var config = new MapperConfiguration(cfg => cfg.AddProfile<EncomendaMoldeProfile>());

        // ACT
        Action act = () => config.AssertConfigurationIsValid();

        // ASSERT
        act.Should().NotThrow();
    }

    [Test(Description = "TENCMMAP2 - Entidade EncomendaMolde deve mapear para DTO de resposta com campos de navegacao.")]
    public void EncomendaMolde_Should_MapTo_ResponseEncomendaMoldeDto()
    {
        // ARRANGE
        var source = new EncomendaMolde
        {
            EncomendaMolde_id = 5,
            Encomenda_id = 10,
            Molde_id = 20,
            Quantidade = 30,
            Prioridade = 1,
            Estado = EstadoEncomendaMolde.PENDENTE,
            DataEntregaPrevista = new DateTime(2026, 5, 1),
            Encomenda = new Encomenda
            {
                Encomenda_id = 10,
                NumeroEncomendaCliente = "ENC-10",
                Cliente = new Cliente { Nome = "Cliente Desenho", NIF = "123456789", Sigla = "DES" }
            },
            Molde = new Molde
            {
                Molde_id = 20,
                Numero = "M-20",
                Nome = "Molde Teste",
                Descricao = "Descricao do molde",
                ImagemCapaPath = "capa.png"
            }
        };

        // ACT
        var result = _mapper.Map<ResponseEncomendaMoldeDto>(source);

        // ASSERT
        result.EncomendaMolde_id.Should().Be(5);
        result.Estado.Should().Be(EstadoEncomendaMolde.PENDENTE);
        result.NumeroEncomendaCliente.Should().Be("ENC-10");
        result.NumeroMolde.Should().Be("M-20");
        result.NomeCliente.Should().Be("Cliente Desenho");
        result.NomeMolde.Should().Be("Molde Teste");
        result.DescricaoMolde.Should().Be("Descricao do molde");
        result.ImagemCapaPath.Should().Be("capa.png");
    }
}
