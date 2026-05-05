using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TipMolde.Application.Dtos.ProjetoDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Desenho.IProjeto;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Application.Mappings;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities.Desenho;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class ProjetoServiceTests
{
    private static readonly int[] ExpectedProjetoIds = [1, 2];

    private Mock<IProjetoRepository> _projetoRepository = null!;
    private Mock<IMoldeRepository> _moldeRepository = null!;
    private Mock<ILogger<ProjetoService>> _logger = null!;
    private IMapper _mapper = null!;
    private ProjetoService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _projetoRepository = new Mock<IProjetoRepository>();
        _moldeRepository = new Mock<IMoldeRepository>();
        _logger = new Mock<ILogger<ProjetoService>>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProjetoProfile>();
            cfg.AddProfile<RevisaoProfile>();
        });
        _mapper = config.CreateMapper();

        _sut = new ProjetoService(
            _projetoRepository.Object,
            _moldeRepository.Object,
            _mapper,
            _logger.Object);
    }

    private static CreateProjetoDto BuildCreateDto(string caminho = @" \\srv\projetos\molde-01 ")
    {
        return new CreateProjetoDto
        {
            NomeProjeto = " Projeto Base ",
            SoftwareUtilizado = " SolidWorks ",
            TipoProjeto = TipoProjeto.PROJETO_3D,
            CaminhoPastaServidor = caminho,
            Molde_id = 7
        };
    }

    private static Projeto BuildProjeto(int id = 1, TipoProjeto tipoProjeto = TipoProjeto.PROJETO_3D)
    {
        return new Projeto
        {
            Projeto_id = id,
            NomeProjeto = "Projeto Atual",
            SoftwareUtilizado = "NX",
            TipoProjeto = tipoProjeto,
            CaminhoPastaServidor = @"\\srv\projetos\origem",
            Molde_id = 9
        };
    }

    [Test(Description = "TPROJSRV1 - Create deve falhar quando caminho da pasta e vazio.")]
    public async Task CreateAsync_Should_ThrowArgumentException_When_CaminhoPastaServidorIsBlank()
    {
        // ARRANGE
        var dto = BuildCreateDto("   ");

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Caminho da pasta no servidor e obrigatorio*");
    }

    [Test(Description = "TPROJSRV2 - Create deve falhar quando o molde referenciado nao existe.")]
    public async Task CreateAsync_Should_ThrowKeyNotFoundException_When_MoldeDoesNotExist()
    {
        // ARRANGE
        var dto = BuildCreateDto();
        _moldeRepository.Setup(r => r.GetByIdAsync(dto.Molde_id)).ReturnsAsync((Molde?)null);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{dto.Molde_id}*");
    }

    [Test(Description = "TPROJSRV3 - Create deve persistir projeto e devolver DTO com caminho do servidor.")]
    public async Task CreateAsync_Should_PersistProjetoAndReturnResponse_When_DataIsValid()
    {
        // ARRANGE
        var dto = BuildCreateDto();

        _moldeRepository.Setup(r => r.GetByIdAsync(dto.Molde_id))
            .ReturnsAsync(new Molde
            {
                Molde_id = dto.Molde_id,
                Numero = "MOL-007",
                Numero_cavidades = 4,
                TipoPedido = TipoPedido.NOVO_MOLDE
            });

        _projetoRepository.Setup(r => r.AddAsync(It.IsAny<Projeto>()))
            .Callback<Projeto>(projeto => projeto.Projeto_id = 25)
            .ReturnsAsync((Projeto projeto) => projeto);

        // ACT
        var result = await _sut.CreateAsync(dto);

        // ASSERT
        result.Projeto_id.Should().Be(25);
        result.NomeProjeto.Should().Be("Projeto Base");
        result.SoftwareUtilizado.Should().Be("SolidWorks");
        result.CaminhoPastaServidor.Should().Be(@"\\srv\projetos\molde-01");
        result.TipoProjeto.Should().Be(TipoProjeto.PROJETO_3D);

        _projetoRepository.Verify(r => r.AddAsync(It.Is<Projeto>(p =>
            p.NomeProjeto == "Projeto Base" &&
            p.SoftwareUtilizado == "SolidWorks" &&
            p.CaminhoPastaServidor == @"\\srv\projetos\molde-01" &&
            p.TipoProjeto == TipoProjeto.PROJETO_3D &&
            p.Molde_id == dto.Molde_id)), Times.Once);
    }

    [Test(Description = "TPROJSRV4 - Update deve falhar quando nenhum campo e enviado.")]
    public async Task UpdateAsync_Should_ThrowArgumentException_When_NoFieldsProvided()
    {
        // ARRANGE
        var existente = BuildProjeto(id: 11);
        var dto = new UpdateProjetoDto();

        _projetoRepository.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(existente);

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(11, dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Pelo menos um campo*");
    }

    [Test(Description = "TPROJSRV5 - Update deve preservar TipoProjeto quando o campo nao e enviado.")]
    public async Task UpdateAsync_Should_PreserveTipoProjeto_When_FieldIsNotSent()
    {
        // ARRANGE
        var existente = BuildProjeto(id: 12, tipoProjeto: TipoProjeto.PROJETO_3D);
        var dto = new UpdateProjetoDto
        {
            NomeProjeto = "Projeto Atualizado"
        };

        _projetoRepository.Setup(r => r.GetByIdAsync(12)).ReturnsAsync(existente);

        // ACT
        await _sut.UpdateAsync(12, dto);

        // ASSERT
        _projetoRepository.Verify(r => r.UpdateAsync(It.Is<Projeto>(p =>
            p.Projeto_id == 12 &&
            p.NomeProjeto == "Projeto Atualizado" &&
            p.TipoProjeto == TipoProjeto.PROJETO_3D &&
            p.CaminhoPastaServidor == @"\\srv\projetos\origem")), Times.Once);
    }

    [Test(Description = "TPROJSRV6 - Delete deve remover projeto quando o registo existe.")]
    public async Task DeleteAsync_Should_Delete_When_ProjetoExists()
    {
        // ARRANGE
        _projetoRepository.Setup(r => r.GetByIdAsync(13)).ReturnsAsync(BuildProjeto(id: 13));

        // ACT
        await _sut.DeleteAsync(13);

        // ASSERT
        _projetoRepository.Verify(r => r.DeleteAsync(13), Times.Once);
    }

    [Test(Description = "TPROJSRV7 - GetAll deve mapear projetos para DTO paginado.")]
    public async Task GetAllAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var paged = new PagedResult<Projeto>(new[] { BuildProjeto(id: 1), BuildProjeto(id: 2) }, 2, 1, 10);
        _projetoRepository.Setup(r => r.GetAllAsync(1, 10)).ReturnsAsync(paged);

        // ACT
        var result = await _sut.GetAllAsync(1, 10);

        // ASSERT
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(x => x.Projeto_id).Should().Contain(ExpectedProjetoIds);
    }

    [Test(Description = "TPROJSRV8 - GetById deve devolver nulo quando projeto nao existe.")]
    public async Task GetByIdAsync_Should_ReturnNull_When_ProjetoDoesNotExist()
    {
        // ARRANGE
        _projetoRepository.Setup(r => r.GetByIdAsync(90)).ReturnsAsync((Projeto?)null);

        // ACT
        var result = await _sut.GetByIdAsync(90);

        // ASSERT
        result.Should().BeNull();
    }

    [Test(Description = "TPROJSRV9 - GetWithRevisoes deve devolver nulo quando projeto nao existe.")]
    public async Task GetWithRevisoesAsync_Should_ReturnNull_When_ProjetoDoesNotExist()
    {
        // ARRANGE
        _projetoRepository.Setup(r => r.GetWithRevisoesAsync(91)).ReturnsAsync((Projeto?)null);

        // ACT
        var result = await _sut.GetWithRevisoesAsync(91);

        // ASSERT
        result.Should().BeNull();
    }

    [Test(Description = "TPROJSRV10 - GetWithRevisoes deve mapear dto quando projeto existe.")]
    public async Task GetWithRevisoesAsync_Should_MapResponse_When_ProjetoExists()
    {
        // ARRANGE
        var projeto = BuildProjeto(id: 20);
        projeto.Revisoes = new List<Revisao>
        {
            new() { Revisao_id = 1, NumRevisao = 2, DescricaoAlteracoes = "Rev 2", Projeto_id = 20 }
        };

        _projetoRepository.Setup(r => r.GetWithRevisoesAsync(20)).ReturnsAsync(projeto);

        // ACT
        var result = await _sut.GetWithRevisoesAsync(20);

        // ASSERT
        result.Should().NotBeNull();
        result!.Projeto_id.Should().Be(20);
        result.Revisoes.Should().ContainSingle();
    }

    [Test(Description = "TPROJSRV11 - GetByMoldeId deve mapear payload paginado.")]
    public async Task GetByMoldeIdAsync_Should_MapPagedResult_When_RepositoryReturnsItems()
    {
        // ARRANGE
        var paged = new PagedResult<Projeto>(new[] { BuildProjeto(id: 30) }, 1, 2, 10);
        _projetoRepository.Setup(r => r.GetByMoldeIdAsync(7, 2, 10)).ReturnsAsync(paged);

        // ACT
        var result = await _sut.GetByMoldeIdAsync(7, 2, 5);

        // ASSERT
        result.TotalCount.Should().Be(1);
        result.Items.Single().Projeto_id.Should().Be(30);
    }

    [Test(Description = "TPROJSRV12 - Create deve falhar quando nome do projeto e vazio.")]
    public async Task CreateAsync_Should_ThrowArgumentException_When_NomeProjetoIsBlank()
    {
        // ARRANGE
        var dto = BuildCreateDto();
        dto.NomeProjeto = "   ";

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "TPROJSRV13 - Create deve falhar quando software utilizado e vazio.")]
    public async Task CreateAsync_Should_ThrowArgumentException_When_SoftwareUtilizadoIsBlank()
    {
        // ARRANGE
        var dto = BuildCreateDto();
        dto.SoftwareUtilizado = "   ";

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "TPROJSRV14 - Update deve falhar quando projeto nao existe.")]
    public async Task UpdateAsync_Should_ThrowKeyNotFoundException_When_ProjetoDoesNotExist()
    {
        // ARRANGE
        _projetoRepository.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Projeto?)null);

        // ACT
        Func<Task> act = () => _sut.UpdateAsync(404, new UpdateProjetoDto { NomeProjeto = "Novo Projeto" });

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TPROJSRV15 - Delete deve falhar quando projeto nao existe.")]
    public async Task DeleteAsync_Should_ThrowKeyNotFoundException_When_ProjetoDoesNotExist()
    {
        // ARRANGE
        _projetoRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Projeto?)null);

        // ACT
        Func<Task> act = () => _sut.DeleteAsync(99);

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
