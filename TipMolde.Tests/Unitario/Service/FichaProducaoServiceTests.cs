using AutoMapper;
using FluentAssertions;
using Moq;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Application.Mappings;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.TipoFichas;
using TipMolde.Domain.Entities.Fichas.TipoFichas.Linhas;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class FichaProducaoServiceTests
{
    private Mock<IFichaProducaoRepository> _fichaRepository = null!;
    private Mock<IEncomendaMoldeRepository> _encomendaMoldeRepository = null!;
    private Mock<IUserRepository> _userRepository = null!;
    private IMapper _mapper = null!;
    private FichaProducaoService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fichaRepository = new Mock<IFichaProducaoRepository>();
        _encomendaMoldeRepository = new Mock<IEncomendaMoldeRepository>();
        _userRepository = new Mock<IUserRepository>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<FichaProducaoProfile>());
        _mapper = config.CreateMapper();

        _sut = new FichaProducaoService(
            _fichaRepository.Object,
            _encomendaMoldeRepository.Object,
            _userRepository.Object,
            _mapper);
    }

    [Test(Description = "TFPSRV1 - Create deve rejeitar a criacao manual de fichas FLT.")]
    public async Task CreateAsync_Should_ThrowArgumentException_When_TipoIsFlt()
    {
        var dto = new CreateFichaProducaoDto
        {
            Tipo = TipoFicha.FLT,
            EncomendaMolde_id = 7
        };

        Func<Task> act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*FLT*");
    }

    [Test(Description = "TFPSRV2 - Create deve falhar quando a associacao EncomendaMolde nao existe.")]
    public async Task CreateAsync_Should_ThrowKeyNotFoundException_When_EncomendaMoldeDoesNotExist()
    {
        _encomendaMoldeRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((EncomendaMolde?)null);

        var dto = new CreateFichaProducaoDto
        {
            Tipo = TipoFicha.FRE,
            EncomendaMolde_id = 7
        };

        Func<Task> act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*7*");
    }

    [Test(Description = "TFPSRV3 - Submit deve falhar quando uma ficha FRM nao tem linhas.")]
    public async Task SubmitAsync_Should_ThrowArgumentException_When_FrmHasNoLines()
    {
        _fichaRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(BuildFichaFrm(10));
        _userRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(BuildUser(3));
        _fichaRepository.Setup(r => r.GetLinhasFrmByFichaIdAsync(10, 1, 1))
            .ReturnsAsync(new PagedResult<FichaFrmLinha>(Array.Empty<FichaFrmLinha>(), 0, 1, 1));

        Func<Task> act = () => _sut.SubmitAsync(10, 3);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*FRM*");
        _fichaRepository.Verify(r => r.UpdateAsync(It.IsAny<FichaProducao>()), Times.Never);
    }

    [Test(Description = "TFPSRV4 - Submit deve falhar quando uma ficha FRA nao tem linhas.")]
    public async Task SubmitAsync_Should_ThrowArgumentException_When_FraHasNoLines()
    {
        _fichaRepository.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(BuildFichaFra(11));
        _userRepository.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(BuildUser(4));
        _fichaRepository.Setup(r => r.GetLinhasFraByFichaIdAsync(11, 1, 1))
            .ReturnsAsync(new PagedResult<FichaFraLinha>(Array.Empty<FichaFraLinha>(), 0, 1, 1));

        Func<Task> act = () => _sut.SubmitAsync(11, 4);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*FRA*");
        _fichaRepository.Verify(r => r.UpdateAsync(It.IsAny<FichaProducao>()), Times.Never);
    }

    [Test(Description = "TFPSRV5 - Submit deve falhar quando uma ficha FOP nao tem linhas.")]
    public async Task SubmitAsync_Should_ThrowArgumentException_When_FopHasNoLines()
    {
        _fichaRepository.Setup(r => r.GetByIdAsync(12)).ReturnsAsync(BuildFichaFop(12));
        _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildUser(5));
        _fichaRepository.Setup(r => r.GetLinhasFopByFichaIdAsync(12, 1, 1))
            .ReturnsAsync(new PagedResult<FichaFopLinha>(Array.Empty<FichaFopLinha>(), 0, 1, 1));

        Func<Task> act = () => _sut.SubmitAsync(12, 5);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*FOP*");
        _fichaRepository.Verify(r => r.UpdateAsync(It.IsAny<FichaProducao>()), Times.Never);
    }

    [Test(Description = "TFPSRV6 - CreateLinhaFrm deve falhar quando o responsavel nao existe.")]
    public async Task CreateLinhaFrmAsync_Should_ThrowKeyNotFoundException_When_ResponsavelDoesNotExist()
    {
        _fichaRepository.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(BuildFichaFrm(30));
        _userRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        var dto = new CreateFichaFrmLinhaDto
        {
            Data = new DateTime(2026, 5, 1),
            Defeito = "Rebarba",
            Pormenor = "Face lateral",
            Verificado = false,
            Responsavel_id = 999
        };

        Func<Task> act = () => _sut.CreateLinhaFrmAsync(30, dto);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Test(Description = "TFPSRV7 - GetById deve carregar todas as linhas de uma ficha FRM no detalhe.")]
    public async Task GetByIdAsync_Should_LoadAllFrmLines_When_FichaIsFrm()
    {
        _fichaRepository.Setup(r => r.GetByIdDetalheAsync(40)).ReturnsAsync(BuildFichaFrmDetalhe(40));
        _fichaRepository.Setup(r => r.GetLinhasFrmByFichaIdAsync(40, 1, 1))
            .ReturnsAsync(new PagedResult<FichaFrmLinha>(
                new[] { BuildFrmLinha(1, 40, "Primeira") },
                2,
                1,
                1));
        _fichaRepository.Setup(r => r.GetLinhasFrmByFichaIdAsync(40, 1, 2))
            .ReturnsAsync(new PagedResult<FichaFrmLinha>(
                new[]
                {
                    BuildFrmLinha(1, 40, "Primeira"),
                    BuildFrmLinha(2, 40, "Segunda")
                },
                2,
                1,
                2));

        var result = await _sut.GetByIdAsync(40);

        result.Should().NotBeNull();
        result!.FichaProducao_id.Should().Be(40);
        result.Tipo.Should().Be(TipoFicha.FRM);
        result.NumeroMolde.Should().Be("MOL-040");
        result.NomeMolde.Should().Be("Molde FRM");
        result.NomeCliente.Should().Be("Cliente FRM");
        result.NumeroEncomendaCliente.Should().Be("ENC-040");
        result.LinhasFrm.Should().HaveCount(2);
        result.LinhasFrm.Select(x => x.Defeito).Should().Contain(new[] { "Primeira", "Segunda" });
        result.LinhasFra.Should().BeEmpty();
        result.LinhasFop.Should().BeEmpty();
    }

    [Test(Description = "TFPSRV8 - Cancel deve fazer delete logico e registar utilizador de cancelamento.")]
    public async Task CancelAsync_Should_MarkFichaAsCancelled_When_RequestIsValid()
    {
        var ficha = BuildFichaFra(50);

        _fichaRepository.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(ficha);
        _userRepository.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(BuildUser(6));
        _fichaRepository.Setup(r => r.UpdateAsync(It.IsAny<FichaProducao>())).Returns(Task.CompletedTask);

        var result = await _sut.CancelAsync(50, 6);

        result.FichaProducao_id.Should().Be(50);
        result.Ativa.Should().BeFalse();
        result.Estado.Should().Be(EstadoFichaProducao.CANCELADA);
        ficha.DesativadaPor_user_id.Should().Be(6);
        ficha.DesativadaEm.Should().NotBeNull();

        _fichaRepository.Verify(r => r.UpdateAsync(It.Is<FichaProducao>(x =>
            x.FichaProducao_id == 50 &&
            !x.Ativa &&
            x.Estado == EstadoFichaProducao.CANCELADA &&
            x.DesativadaPor_user_id == 6 &&
            x.DesativadaEm.HasValue)),
            Times.Once);
    }

    private static FichaFrm BuildFichaFrm(int id, EstadoFichaProducao estado = EstadoFichaProducao.RASCUNHO, bool ativa = true)
    {
        return new FichaFrm
        {
            FichaProducao_id = id,
            Tipo = TipoFicha.FRM,
            Estado = estado,
            Ativa = ativa,
            DataCriacao = new DateTime(2026, 4, 1),
            EncomendaMolde_id = 100 + id
        };
    }

    private static FichaFra BuildFichaFra(int id, EstadoFichaProducao estado = EstadoFichaProducao.RASCUNHO, bool ativa = true)
    {
        return new FichaFra
        {
            FichaProducao_id = id,
            Tipo = TipoFicha.FRA,
            Estado = estado,
            Ativa = ativa,
            DataCriacao = new DateTime(2026, 4, 1),
            EncomendaMolde_id = 100 + id
        };
    }

    private static FichaFop BuildFichaFop(int id, EstadoFichaProducao estado = EstadoFichaProducao.RASCUNHO, bool ativa = true)
    {
        return new FichaFop
        {
            FichaProducao_id = id,
            Tipo = TipoFicha.FOP,
            Estado = estado,
            Ativa = ativa,
            DataCriacao = new DateTime(2026, 4, 1),
            EncomendaMolde_id = 100 + id
        };
    }

    private static FichaFrm BuildFichaFrmDetalhe(int id)
    {
        return new FichaFrm
        {
            FichaProducao_id = id,
            Tipo = TipoFicha.FRM,
            Estado = EstadoFichaProducao.RASCUNHO,
            Ativa = true,
            DataCriacao = new DateTime(2026, 4, 10),
            EncomendaMolde_id = 200,
            EncomendaMolde = new EncomendaMolde
            {
                EncomendaMolde_id = 200,
                Molde_id = 40,
                Molde = new Molde
                {
                    Molde_id = 40,
                    Numero = "MOL-040",
                    Nome = "Molde FRM"
                },
                Encomenda_id = 90,
                Encomenda = new Encomenda
                {
                    Encomenda_id = 90,
                    NumeroEncomendaCliente = "ENC-040",
                    Cliente_id = 15,
                    Cliente = new Cliente
                    {
                        Cliente_id = 15,
                        Nome = "Cliente FRM",
                        NIF = "123456780",
                        Sigla = "CFRM"
                    }
                }
            }
        };
    }

    private static FichaFrmLinha BuildFrmLinha(int linhaId, int fichaId, string defeito)
    {
        return new FichaFrmLinha
        {
            FichaFrmLinha_id = linhaId,
            FichaFrm_id = fichaId,
            Data = new DateTime(2026, 5, 1).AddDays(linhaId),
            Defeito = defeito,
            Pormenor = $"Pormenor {linhaId}",
            Verificado = linhaId % 2 == 0,
            Responsavel_id = 10 + linhaId,
            CriadoEm = new DateTime(2026, 5, 1, 8, 0, 0).AddMinutes(linhaId)
        };
    }

    private static User BuildUser(int id)
    {
        return new User
        {
            User_id = id,
            Nome = $"Utilizador {id}",
            Email = $"user{id}@tipmolde.pt",
            Password = "hash",
            Role = UserRole.GESTOR_PRODUCAO
        };
    }
}
