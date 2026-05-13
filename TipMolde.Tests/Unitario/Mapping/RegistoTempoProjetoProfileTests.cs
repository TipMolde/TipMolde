using AutoMapper;
using FluentAssertions;
using TipMolde.Application.Dtos.RegistoTempoProjetoDto;
using TipMolde.Application.Mappings;
using TipMolde.Domain.Entities.Desenho;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Mapping;

[TestFixture]
[Category("Unit")]
public class RegistoTempoProjetoProfileTests
{
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RegistoTempoProjetoProfile>());
        _mapper = config.CreateMapper();
    }

    [Test(Description = "TRTPMAP1 - Configuracao AutoMapper de RegistoTempoProjetoProfile deve ser valida.")]
    public void MappingConfiguration_Should_BeValid()
    {
        // ARRANGE
        var config = new MapperConfiguration(cfg => cfg.AddProfile<RegistoTempoProjetoProfile>());

        // ACT
        Action act = () => config.AssertConfigurationIsValid();

        // ASSERT
        act.Should().NotThrow();
    }

    [Test(Description = "TRTPMAP2 - CreateRegistoTempoProjetoDto deve mapear para entidade com Peca_id e estado informado.")]
    public void CreateDto_Should_MapTo_RegistoTempoProjeto()
    {
        // ARRANGE
        var dto = new CreateRegistoTempoProjetoDto
        {
            Estado_tempo = EstadoTempoProjeto.PAUSADO,
            Projeto_id = 10,
            Autor_id = 7
        };

        // ACT
        var result = _mapper.Map<RegistoTempoProjeto>(dto);

        // ASSERT
        result.Estado_tempo.Should().Be(EstadoTempoProjeto.PAUSADO);
        result.Projeto_id.Should().Be(10);
        result.Autor_id.Should().Be(7);
    }

    [Test(Description = "TRTPMAP3 - Entidade RegistoTempoProjeto deve mapear para ResponseRegistoTempoProjetoDto.")]
    public void RegistoTempoProjeto_Should_MapTo_ResponseDto()
    {
        // ARRANGE
        var source = new RegistoTempoProjeto
        {
            Registo_Tempo_Projeto_id = 15,
            Estado_tempo = EstadoTempoProjeto.RETOMADO,
            Data_hora = new DateTime(2026, 4, 24, 10, 15, 0, DateTimeKind.Utc),
            Projeto_id = 2,
            Autor_id = 4
        };

        // ACT
        var result = _mapper.Map<ResponseRegistoTempoProjetoDto>(source);

        // ASSERT
        result.Registo_Tempo_Projeto_id.Should().Be(15);
        result.Estado_tempo.Should().Be(EstadoTempoProjeto.RETOMADO);
        result.Projeto_id.Should().Be(2);
        result.Autor_id.Should().Be(4);
    }
}
