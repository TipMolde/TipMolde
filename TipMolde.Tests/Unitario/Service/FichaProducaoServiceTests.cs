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
using TipMolde.Domain.Entities.Fichas.Linhas;
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

    private static FichaProducao BuildFichaFrm(int id)
    {
        return new FichaProducao
        {
            FichaProducao_id = id,
            Tipo = TipoFicha.FRM,
            DataCriacao = new DateTime(2026, 4, 1),
            EncomendaMolde_id = 100 + id
        };
    }

    private static FichaProducao BuildFichaFra(int id)
    {
        return new FichaProducao
        {
            FichaProducao_id = id,
            Tipo = TipoFicha.FRA,
            DataCriacao = new DateTime(2026, 4, 1),
            EncomendaMolde_id = 100 + id
        };
    }

    private static FichaProducao BuildFichaFop(int id)
    {
        return new FichaProducao
        {
            FichaProducao_id = id,
            Tipo = TipoFicha.FOP,
            DataCriacao = new DateTime(2026, 4, 1),
            EncomendaMolde_id = 100 + id
        };
    }

    private static FichaProducao BuildFichaFrmDetalhe(int id)
    {
        return new FichaProducao
        {
            FichaProducao_id = id,
            Tipo = TipoFicha.FRM,
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
            FichaProducao_id = fichaId,
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
