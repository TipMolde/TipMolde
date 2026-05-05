using AutoMapper;
using FluentAssertions;
using TipMolde.Application.Dtos.FichaDocumentoDto;
using TipMolde.Application.Mappings;
using TipMolde.Domain.Entities.Fichas;

namespace TipMolde.Tests.Unitario.Mapping;

[TestFixture]
[Category("Unit")]
public class FichaDocumentoProfileTests
{
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<FichaDocumentoProfile>());
        _mapper = config.CreateMapper();
    }

    [Test(Description = "TFDOCMAP1 - Configuracao AutoMapper de FichaDocumentoProfile deve ser valida.")]
    public void MappingConfiguration_Should_BeValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<FichaDocumentoProfile>());

        Action act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    [Test(Description = "TFDOCMAP2 - CreateFichaDocumentoDto deve mapear para FichaDocumento sem popular navegacoes ignoradas.")]
    public void CreateFichaDocumentoDto_Should_MapTo_FichaDocumento()
    {
        var source = new CreateFichaDocumentoDto
        {
            FichaProducao_id = 9,
            CriadoPor_user_id = 17,
            Versao = 3,
            Origem = "UPLOAD",
            NomeFicheiro = "ficha_v3.pdf",
            TipoFicheiro = "application/pdf",
            CaminhoFicheiro = "Storage\\Fichas\\9\\ficha_v3.pdf",
            HashSha256 = "ABC123",
            Ativo = true
        };

        var result = _mapper.Map<FichaDocumento>(source);

        result.FichaDocumento_id.Should().Be(0);
        result.FichaProducao_id.Should().Be(9);
        result.CriadoPor_user_id.Should().Be(17);
        result.Versao.Should().Be(3);
        result.Origem.Should().Be("UPLOAD");
        result.NomeFicheiro.Should().Be("ficha_v3.pdf");
        result.TipoFicheiro.Should().Be("application/pdf");
        result.CaminhoFicheiro.Should().Be("Storage\\Fichas\\9\\ficha_v3.pdf");
        result.HashSha256.Should().Be("ABC123");
        result.Ativo.Should().BeTrue();
        result.FichaProducao.Should().BeNull();
        result.CriadoPor.Should().BeNull();
    }

    [Test(Description = "TFDOCMAP3 - FichaDocumento deve mapear para ResponseFichaDocumentoDto sem expor metadados internos.")]
    public void FichaDocumento_Should_MapTo_ResponseFichaDocumentoDto()
    {
        var source = new FichaDocumento
        {
            FichaDocumento_id = 25,
            FichaProducao_id = 4,
            CriadoPor_user_id = 8,
            Versao = 2,
            Origem = "SISTEMA",
            NomeFicheiro = "relatorio_v2.xlsx",
            TipoFicheiro = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            CaminhoFicheiro = "interno/nao/expor.xlsx",
            HashSha256 = "HASH-INTERNO",
            Ativo = false
        };

        var result = _mapper.Map<ResponseFichaDocumentoDto>(source);

        result.FichaDocumento_id.Should().Be(25);
        result.FichaProducao_id.Should().Be(4);
        result.CriadoPor_user_id.Should().Be(8);
        result.Versao.Should().Be(2);
        result.Origem.Should().Be("SISTEMA");
        result.NomeFicheiro.Should().Be("relatorio_v2.xlsx");
        result.TipoFicheiro.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Ativo.Should().BeFalse();
    }
}
