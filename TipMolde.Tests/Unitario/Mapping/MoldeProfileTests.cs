using AutoMapper;
using FluentAssertions;
using TipMolde.Application.Dtos.MoldeDto;
using TipMolde.Application.Mappings;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Mapping;

[TestFixture]
[Category("Unit")]
public class MoldeProfileTests
{
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MoldeProfile>());
        _mapper = config.CreateMapper();
    }

    [Test(Description = "TMOLDMAP1 - Configuracao AutoMapper de MoldeProfile deve ser valida.")]
    public void MappingConfiguration_Should_BeValid()
    {
        // ARRANGE
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MoldeProfile>());

        // ACT
        Action act = () => config.AssertConfigurationIsValid();

        // ASSERT
        act.Should().NotThrow();
    }

    [Test(Description = "TMOLDMAP2 - Entidade Molde deve mapear para ResponseMoldeDto com especificacoes tecnicas.")]
    public void Molde_Should_MapTo_ResponseMoldeDTO_WithTechnicalSpecs()
    {
        // ARRANGE
        var source = new Molde
        {
            Molde_id = 5,
            Numero = "MOL-005",
            NumeroMoldeCliente = "CLI-005",
            Nome = "Molde 5",
            Descricao = "Descricao",
            Numero_cavidades = 6,
            TipoPedido = TipoPedido.REPARACAO,
            ImagemCapaPath = "molde5.png",
            Especificacoes = new EspecificacoesTecnicas
            {
                Molde_id = 5,
                Largura = 100,
                Comprimento = 200,
                Altura = 300,
                PesoEstimado = 400,
                TipoInjecao = "Hot Runner",
                SistemaInjecao = "Canal Quente",
                Contracao = 1.5m,
                AcabamentoPeca = "Polido",
                Cor = CorMolde.BICOLOR,
                MaterialMacho = "P20",
                MaterialCavidade = "H13",
                MaterialMovimentos = "420",
                MaterialInjecao = "ABS"
            }
        };

        // ACT
        var result = _mapper.Map<ResponseMoldeDto>(source);

        // ASSERT
        result.MoldeId.Should().Be(5);
        result.Numero.Should().Be("MOL-005");
        result.NumeroMoldeCliente.Should().Be("CLI-005");
        result.Largura.Should().Be(100);
        result.MaterialInjecao.Should().Be("ABS");
        result.Cor.Should().Be(CorMolde.BICOLOR);
    }

    [Test(Description = "TMOLDMAP3 - CreateMoldeDto deve mapear para Molde sem acoplamento a encomendas.")]
    public void CreateMoldeDTO_Should_MapTo_Molde()
    {
        // ARRANGE
        var source = new CreateMoldeDto
        {
            Numero = " MOL-010 ",
            NumeroMoldeCliente = " CLI-010 ",
            Nome = " Molde 10 ",
            Numero_cavidades = 2,
            TipoPedido = TipoPedido.NOVO_MOLDE,
            Descricao = " Descricao "
        };

        // ACT
        var result = _mapper.Map<Molde>(source);

        // ASSERT
        result.Numero.Should().Be("MOL-010");
        result.NumeroMoldeCliente.Should().Be("CLI-010");
        result.Nome.Should().Be("Molde 10");
        result.Descricao.Should().Be("Descricao");
        result.Numero_cavidades.Should().Be(2);
        result.TipoPedido.Should().Be(TipoPedido.NOVO_MOLDE);
        result.EncomendasMoldes.Should().BeEmpty();
    }

    [Test(Description = "TMOLDMAP4 - UpdateMoldeDto deve atualizar apenas campos preenchidos e preservar os restantes.")]
    public void UpdateMoldeDTO_Should_MapOnlyProvidedFields_When_MappingToExistingMolde()
    {
        // ARRANGE
        var source = new UpdateMoldeDto
        {
            Nome = "Novo Nome",
            MaterialInjecao = "PP"
        };

        var destination = new Molde
        {
            Molde_id = 9,
            Numero = "MOL-009",
            NumeroMoldeCliente = "CLI-009",
            Nome = "Nome Antigo",
            Descricao = "Descricao Antiga",
            Numero_cavidades = 4,
            TipoPedido = TipoPedido.ALTERACAO,
            ImagemCapaPath = "antiga.png",
            Especificacoes = new EspecificacoesTecnicas
            {
                Molde_id = 9,
                MaterialInjecao = "ABS",
                MaterialMacho = "P20"
            }
        };

        // ACT
        _mapper.Map(source, destination);
        _mapper.Map(source, destination.Especificacoes!);

        // ASSERT
        destination.Nome.Should().Be("Novo Nome");
        destination.Numero.Should().Be("MOL-009");
        destination.TipoPedido.Should().Be(TipoPedido.ALTERACAO);
        destination.Especificacoes!.MaterialInjecao.Should().Be("PP");
        destination.Especificacoes.MaterialMacho.Should().Be("P20");
    }
}
