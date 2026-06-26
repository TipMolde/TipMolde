using AutoMapper;
using FluentAssertions;
using TipMolde.Application.Dtos.MaquinaDto;
using TipMolde.Application.Mappings;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Mapping;

[TestFixture]
[Category("Unit")]
public class MaquinaProfileTests
{
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MaquinaProfile>());
        _mapper = config.CreateMapper();
    }

    [Test(Description = "TMAQMAP1 - Configuracao AutoMapper de MaquinaProfile deve ser valida.")]
    public void MappingConfiguration_Should_BeValid()
    {
        // ARRANGE
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MaquinaProfile>());

        // ACT
        Action act = () => config.AssertConfigurationIsValid();

        // ASSERT
        act.Should().NotThrow();
    }

    [Test(Description = "TMAQMAP2 - CreateMaquinaDto deve mapear para entidade com trim nos campos de texto.")]
    public void CreateDto_Should_MapTo_Entity_WithTrimmedFields()
    {
        // ARRANGE
        var source = new CreateMaquinaDto
        {
            Maquina_id = 9,
            Numero = 101,
            NomeModelo = "  Makino V33  ",
            IpAddress = " 10.0.0.9 ",
            ProtocoloComunicacao = " OPC-UA ",
            Estado = EstadoMaquina.DISPONIVEL,
            FaseDedicada_id = 4
        };

        // ACT
        var result = _mapper.Map<Maquina>(source);

        // ASSERT
        result.Maquina_id.Should().Be(9);
        result.Numero.Should().Be(101);
        result.NomeModelo.Should().Be("Makino V33");
        result.IpAddress.Should().Be("10.0.0.9");
        result.ProtocoloComunicacao.Should().Be("OPC-UA");
        result.Estado.Should().Be(EstadoMaquina.DISPONIVEL);
        result.FaseDedicada_id.Should().Be(4);
    }

    [Test(Description = "TMAQMAP3 - Entidade deve mapear para ResponseMaquinaDto.")]
    public void Entity_Should_MapTo_ResponseDto()
    {
        // ARRANGE
        var source = new Maquina
        {
            Maquina_id = 12,
            Numero = 220,
            NomeModelo = "Haas VF2",
            IpAddress = "10.0.0.12",
            ProtocoloComunicacao = "MTConnect",
            Estado = EstadoMaquina.EM_USO,
            FaseDedicada_id = 6
        };

        // ACT
        var result = _mapper.Map<ResponseMaquinaDto>(source);

        // ASSERT
        result.Maquina_id.Should().Be(12);
        result.Numero.Should().Be(220);
        result.NomeModelo.Should().Be("Haas VF2");
        result.IpAddress.Should().Be("10.0.0.12");
        result.ProtocoloComunicacao.Should().Be("MTConnect");
        result.Estado.Should().Be(EstadoMaquina.EM_USO);
        result.FaseDedicada_id.Should().Be(6);
    }

    [Test(Description = "TMAQMAP4 - UpdateMaquinaDto deve atualizar apenas os campos enviados.")]
    public void UpdateDto_Should_MapOnlyProvidedFields()
    {
        // ARRANGE
        var source = new UpdateMaquinaDto
        {
            NomeModelo = "  Novo Modelo  ",
            FaseDedicada_id = 9
        };

        var destination = new Maquina
        {
            Maquina_id = 3,
            Numero = 33,
            NomeModelo = "Modelo Antigo",
            IpAddress = "10.0.0.3",
            ProtocoloComunicacao = "SODICK",
            Estado = EstadoMaquina.EM_USO,
            FaseDedicada_id = 7
        };

        // ACT
        _mapper.Map(source, destination);

        // ASSERT
        destination.Maquina_id.Should().Be(3);
        destination.Numero.Should().Be(33);
        destination.NomeModelo.Should().Be("Novo Modelo");
        destination.IpAddress.Should().Be("10.0.0.3");
        destination.ProtocoloComunicacao.Should().Be("SODICK");
        destination.Estado.Should().Be(EstadoMaquina.EM_USO);
        destination.FaseDedicada_id.Should().Be(9);
    }
}
