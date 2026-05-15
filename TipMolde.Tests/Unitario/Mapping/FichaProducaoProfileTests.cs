using AutoMapper;
using FluentAssertions;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Mappings;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.Linhas;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Mapping;

[TestFixture]
[Category("Unit")]
public class FichaProducaoProfileTests
{
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<FichaProducaoProfile>());
        _mapper = config.CreateMapper();
    }

    [Test(Description = "TFPMAP1 - Configuracao AutoMapper de FichaProducaoProfile deve ser valida.")]
    public void MappingConfiguration_Should_BeValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<FichaProducaoProfile>());

        Action act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    [Test(Description = "TFPMAP2 - CreateFichaProducaoDto deve mapear para FichaProducao preservando EncomendaMolde e ignorando metadados de sistema.")]
    public void CreateFichaProducaoDto_Should_MapTo_FichaProducao()
    {
        var source = new CreateFichaProducaoDto
        {
            Tipo = TipoFicha.FRE,
            EncomendaMolde_id = 12
        };

        var result = _mapper.Map<FichaProducao>(source);

        result.FichaProducao_id.Should().Be(0);
        result.Tipo.Should().Be(TipoFicha.FRE);
        result.EncomendaMolde_id.Should().Be(12);
        result.EncomendaMolde.Should().BeNull();
        result.Documentos.Should().BeEmpty();
    }

    [Test(Description = "TFPMAP3 - CreateFichaProducaoDto deve preservar o tipo documental na entidade unica.")]
    public void CreateFichaProducaoDto_Should_PreserveTipo()
    {
        var source = new CreateFichaProducaoDto
        {
            Tipo = TipoFicha.FOP,
            EncomendaMolde_id = 22
        };

        var result = _mapper.Map<FichaProducao>(source);

        result.Tipo.Should().Be(TipoFicha.FOP);
        result.EncomendaMolde_id.Should().Be(22);
        result.Documentos.Should().BeEmpty();
    }

    [Test(Description = "TFPMAP6 - DTOs de linha devem mapear para entidades de linha com os campos editaveis.")]
    public void LinhaDtos_Should_MapTo_LinhaEntities()
    {
        var frmDto = new CreateFichaFrmLinhaDto
        {
            Data = new DateTime(2026, 5, 1),
            Defeito = "Rebarba",
            Pormenor = "Zona lateral",
            Verificado = true,
            Responsavel_id = 5
        };
        var fraDto = new CreateFichaFraLinhaDto
        {
            Data = new DateTime(2026, 5, 2),
            Alteracoes = "Ajuste de cota",
            Verificado = false,
            Responsavel_id = 6
        };
        var fopDto = new CreateFichaFopLinhaDto
        {
            Data = new DateTime(2026, 5, 3),
            Ocorrencia = "Paragem de maquina",
            Correcao = "Rearme executado",
            Responsavel_id = 7
        };

        var frm = _mapper.Map<FichaFrmLinha>(frmDto);
        var fra = _mapper.Map<FichaFraLinha>(fraDto);
        var fop = _mapper.Map<FichaFopLinha>(fopDto);

        frm.Defeito.Should().Be("Rebarba");
        frm.Pormenor.Should().Be("Zona lateral");
        frm.Verificado.Should().BeTrue();
        frm.Responsavel_id.Should().Be(5);
        frm.FichaProducao.Should().BeNull();

        fra.Alteracoes.Should().Be("Ajuste de cota");
        fra.Verificado.Should().BeFalse();
        fra.Responsavel_id.Should().Be(6);
        fra.FichaProducao.Should().BeNull();

        fop.Ocorrencia.Should().Be("Paragem de maquina");
        fop.Correcao.Should().Be("Rearme executado");
        fop.Responsavel_id.Should().Be(7);
        fop.FichaProducao.Should().BeNull();
    }

    [Test(Description = "TFPMAP7 - FichaProducao deve mapear para detalhe com contexto comercial agregado.")]
    public void FichaProducao_Should_MapTo_ResponseFichaProducaoDetalheDto()
    {
        var source = new FichaProducao
        {
            FichaProducao_id = 40,
            Tipo = TipoFicha.FRM,
            DataCriacao = new DateTime(2026, 4, 29),
            EncomendaMolde_id = 14,
            EncomendaMolde = new EncomendaMolde
            {
                EncomendaMolde_id = 14,
                Molde = new Molde
                {
                    Molde_id = 3,
                    Numero = "MOL-003",
                    Nome = "Molde Tampa"
                },
                Encomenda = new Encomenda
                {
                    Encomenda_id = 9,
                    NumeroEncomendaCliente = "ENC-009",
                    Cliente_id = 4,
                    Cliente = new Cliente
                    {
                        Cliente_id = 4,
                        Nome = "Cliente XPTO",
                        NIF = "123456789",
                        Sigla = "XPTO"
                    }
                }
            }
        };

        var result = _mapper.Map<ResponseFichaProducaoDetalheDto>(source);

        result.FichaProducao_id.Should().Be(40);
        result.Tipo.Should().Be(TipoFicha.FRM);
        result.EncomendaMolde_id.Should().Be(14);
        result.NumeroMolde.Should().Be("MOL-003");
        result.NomeMolde.Should().Be("Molde Tampa");
        result.NomeCliente.Should().Be("Cliente XPTO");
        result.NumeroEncomendaCliente.Should().Be("ENC-009");
        result.LinhasFrm.Should().BeEmpty();
        result.LinhasFra.Should().BeEmpty();
        result.LinhasFop.Should().BeEmpty();
    }

    [Test(Description = "TFPMAP8 - Linha manual deve mapear para DTO publico correspondente.")]
    public void LinhaEntity_Should_MapTo_ResponseLinhaDto()
    {
        var linha = new FichaFopLinha
        {
            FichaFopLinha_id = 11,
            FichaProducao_id = 90,
            Data = new DateTime(2026, 5, 4),
            Ocorrencia = "Desalinhamento",
            Correcao = "Reposicionado",
            Responsavel_id = 12,
            CriadoEm = new DateTime(2026, 5, 4, 10, 30, 0)
        };

        var result = _mapper.Map<ResponseFichaFopLinhaDto>(linha);

        result.FichaFopLinha_id.Should().Be(11);
        result.FichaFop_id.Should().Be(90);
        result.Ocorrencia.Should().Be("Desalinhamento");
        result.Correcao.Should().Be("Reposicionado");
        result.Responsavel_id.Should().Be(12);
        result.CriadoEm.Should().Be(new DateTime(2026, 5, 4, 10, 30, 0));
    }
}
