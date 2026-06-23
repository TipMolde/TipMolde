using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Dtos.RegistoProducaoDto;
using TipMolde.Application.Interface.Producao.IFasesProducao;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Producao.IRegistosProducao;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Application.Mappings;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

/// <summary>
/// Testes unitarios dos casos de uso de RegistosProducao.
/// </summary>
[TestFixture]
[Category("Unit")]
public class RegistosProducaoServiceTests
{
    private Mock<IRegistosProducaoRepository> _registosRepository = null!;
    private Mock<IFasesProducaoRepository> _fasesRepository = null!;
    private Mock<IUserRepository> _userRepository = null!;
    private Mock<IMaquinaRepository> _maquinaRepository = null!;
    private Mock<IPecaRepository> _pecaRepository = null!;
    private Mock<IFichaProducaoService> _fichaProducaoService = null!;
    private IMapper _mapper = null!;
    private RegistosProducaoService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _registosRepository = new Mock<IRegistosProducaoRepository>();
        _fasesRepository = new Mock<IFasesProducaoRepository>();
        _userRepository = new Mock<IUserRepository>();
        _maquinaRepository = new Mock<IMaquinaRepository>();
        _pecaRepository = new Mock<IPecaRepository>();
        _fichaProducaoService = new Mock<IFichaProducaoService>();

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<RegistosProducaoProfile>());
        _mapper = mapperConfig.CreateMapper();

        _sut = new RegistosProducaoService(
            _registosRepository.Object,
            _fasesRepository.Object,
            _userRepository.Object,
            _maquinaRepository.Object,
            _pecaRepository.Object,
            _fichaProducaoService.Object,
            _mapper,
            NullLogger<RegistosProducaoService>.Instance);
    }

    private static FasesProducao BuildFase(int id = 1, NomeFases nome = NomeFases.MAQUINACAO) => new()
    {
        Fases_producao_id = id,
        Nome = nome,
        Descricao = "Fase teste"
    };

    private static User BuildOperador(int id = 1) => new()
    {
        User_id = id,
        Nome = "Operador",
        Email = "op@tipmolde.pt",
        Password = "Hash123!",
        Role = UserRole.GESTOR_PRODUCAO
    };

    private static Peca BuildPeca(int id = 1, bool materialRecebido = true) => new()
    {
        Peca_id = id,
        Designacao = "Peca",
        Prioridade = 1,
        MaterialDesignacao = "Aco",
        MaterialRecebido = materialRecebido,
        Molde_id = 1
    };

    private static Maquina BuildMaquina(int id = 1, int faseId = 1, EstadoMaquina estado = EstadoMaquina.DISPONIVEL) => new()
    {
        Maquina_id = id,
        Numero = id,
        NomeModelo = "CNC",
        FaseDedicada_id = faseId,
        Estado = estado
    };

    private static CreateRegistosProducaoDto BuildDto(
        int faseId = 1,
        int operadorId = 1,
        int pecaId = 1,
        EstadoProducao? estado = EstadoProducao.PREPARACAO,
        int? maquinaId = 1) => new()
        {
            Fase_id = faseId,
            Operador_id = operadorId,
            Peca_id = pecaId,
            Estado_producao = estado,
            Maquina_id = maquinaId
        };

    private void SetupValidDependencies(
        int faseId = 1,
        NomeFases nomeFase = NomeFases.MAQUINACAO,
        int operadorId = 1,
        int pecaId = 1,
        bool materialRecebido = true)
    {
        _fasesRepository.Setup(r => r.GetByIdAsync(faseId)).ReturnsAsync(BuildFase(faseId, nomeFase));
        _userRepository.Setup(r => r.GetByIdAsync(operadorId)).ReturnsAsync(BuildOperador(operadorId));
        _pecaRepository.Setup(r => r.GetByIdAsync(pecaId)).ReturnsAsync(BuildPeca(pecaId, materialRecebido));
    }

    private void SetupPersistCreated()
    {
        _registosRepository
            .Setup(r => r.AddWithMachineStateAsync(It.IsAny<RegistosProducao>(), It.IsAny<Maquina?>(), It.IsAny<Peca?>()))
            .ReturnsAsync((RegistosProducao registo, Maquina? _, Peca? __) =>
            {
                registo.Registo_Producao_id = 10;
                return registo;
            });
    }

    [Test(Description = "TRP001 - Criacao falha quando a fase nao existe.")]
    public async Task CreateAsync_Should_Throw_When_FaseDoesNotExist()
    {
        // ARRANGE
        _fasesRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((FasesProducao?)null);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(BuildDto(faseId: 99));

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TRP002 - Criacao falha quando o operador nao existe.")]
    public async Task CreateAsync_Should_Throw_When_OperadorDoesNotExist()
    {
        // ARRANGE
        _fasesRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildFase());
        _userRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(BuildDto(operadorId: 99));

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TRP003 - Criacao falha quando a peca nao existe.")]
    public async Task CreateAsync_Should_Throw_When_PecaDoesNotExist()
    {
        // ARRANGE
        _fasesRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildFase());
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildOperador());
        _pecaRepository.Setup(r => r.GetByIdAsync(88)).ReturnsAsync((Peca?)null);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(BuildDto(pecaId: 88));

        // ASSERT
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test(Description = "TRP004 - Criacao falha quando a peca ainda nao tem material recebido.")]
    public async Task CreateAsync_Should_Throw_When_MaterialWasNotReceived()
    {
        // ARRANGE
        _fasesRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildFase());
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildOperador());
        _pecaRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(BuildPeca(materialRecebido: false));

        // ACT
        Func<Task> act = () => _sut.CreateAsync(BuildDto());

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "TRP004A - Criacao falha quando existe correcao sem ocorrencia.")]
    public async Task CreateAsync_Should_Throw_When_CorrecaoExistsWithoutOcorrencia()
    {
        // ARRANGE
        var dto = BuildDto();
        dto.Correcao = "Ajuste efetuado";

        // ACT
        Func<Task> act = () => _sut.CreateAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*correcao*ocorrencia*");
    }

    [Test(Description = "TRP005 - Primeira transicao tem de ser PREPARACAO.")]
    public async Task CreateAsync_Should_Throw_When_FirstTransitionIsNotPreparacao()
    {
        // ARRANGE
        SetupValidDependencies();
        _registosRepository.Setup(r => r.GetUltimoRegistoAsync(1, 1)).ReturnsAsync((RegistosProducao?)null);

        // ACT
        Func<Task> act = () => _sut.CreateAsync(BuildDto(estado: EstadoProducao.EM_CURSO));

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "TRP006 - PREPARACAO cria registo e coloca maquina em uso numa unica operacao de persistencia.")]
    public async Task CreateAsync_Should_CreateRegistoAndSetMachineInUse_When_TransitionIsPreparacao()
    {
        // ARRANGE
        SetupValidDependencies();
        SetupPersistCreated();
        _registosRepository.Setup(r => r.GetUltimoRegistoAsync(1, 1)).ReturnsAsync((RegistosProducao?)null);

        var maquina = BuildMaquina(id: 1, faseId: 1, estado: EstadoMaquina.DISPONIVEL);
        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(1)).ReturnsAsync(maquina);

        // ACT
        var result = await _sut.CreateAsync(BuildDto(estado: EstadoProducao.PREPARACAO, maquinaId: 1));

        // ASSERT
        result.Estado_producao.Should().Be(EstadoProducao.PREPARACAO);
        result.Data_hora.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        maquina.Estado.Should().Be(EstadoMaquina.EM_USO);
        _maquinaRepository.Verify(r => r.UpdateAsync(It.IsAny<Maquina>()), Times.Never);
        _registosRepository.Verify(r => r.AddWithMachineStateAsync(
            It.IsAny<RegistosProducao>(),
            It.Is<Maquina>(m => m.Estado == EstadoMaquina.EM_USO),
            It.IsAny<Peca?>()), Times.Once);
    }

    [Test(Description = "TRP007 - PREPARACAO falha quando a maquina nao pertence a fase.")]
    public async Task CreateAsync_Should_Throw_When_MachinePhaseDoesNotMatchRegistoPhase()
    {
        // ARRANGE
        SetupValidDependencies();
        _registosRepository.Setup(r => r.GetUltimoRegistoAsync(1, 1)).ReturnsAsync((RegistosProducao?)null);
        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(1)).ReturnsAsync(BuildMaquina(id: 1, faseId: 2));

        // ACT
        Func<Task> act = () => _sut.CreateAsync(BuildDto(estado: EstadoProducao.PREPARACAO, maquinaId: 1));

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "TRP008 - PREPARACAO falha quando a maquina esta indisponivel.")]
    public async Task CreateAsync_Should_Throw_When_MachineIsUnavailable()
    {
        // ARRANGE
        SetupValidDependencies();
        _registosRepository.Setup(r => r.GetUltimoRegistoAsync(1, 1)).ReturnsAsync((RegistosProducao?)null);
        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(1)).ReturnsAsync(BuildMaquina(id: 1, faseId: 1, estado: EstadoMaquina.EM_USO));

        // ACT
        Func<Task> act = () => _sut.CreateAsync(BuildDto(estado: EstadoProducao.PREPARACAO, maquinaId: 1));

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "TRP009 - CONCLUIDO liberta a maquina associada ao ultimo registo.")]
    public async Task CreateAsync_Should_ReleaseMachine_When_TransitionIsConcluido()
    {
        // ARRANGE
        SetupValidDependencies();
        SetupPersistCreated();
        _registosRepository.Setup(r => r.GetUltimoRegistoAsync(1, 1)).ReturnsAsync(new RegistosProducao
        {
            Fase_id = 1,
            Peca_id = 1,
            Maquina_id = 1,
            Estado_producao = EstadoProducao.EM_CURSO,
            Data_hora = DateTime.UtcNow.AddMinutes(-5)
        });

        var maquina = BuildMaquina(id: 1, faseId: 1, estado: EstadoMaquina.EM_USO);
        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(1)).ReturnsAsync(maquina);

        // ACT
        await _sut.CreateAsync(BuildDto(estado: EstadoProducao.CONCLUIDO, maquinaId: null));

        // ASSERT
        maquina.Estado.Should().Be(EstadoMaquina.DISPONIVEL);
        _maquinaRepository.Verify(r => r.UpdateAsync(It.IsAny<Maquina>()), Times.Never);
        _registosRepository.Verify(r => r.AddWithMachineStateAsync(
            It.Is<RegistosProducao>(rp => rp.Maquina_id == 1),
            It.Is<Maquina>(m => m.Estado == EstadoMaquina.DISPONIVEL),
            It.IsAny<Peca?>()), Times.Once);
    }

    [Test(Description = "TRP010 - Criacao falha quando a transicao de estado e invalida.")]
    public async Task CreateAsync_Should_Throw_When_TransitionIsInvalid()
    {
        // ARRANGE
        SetupValidDependencies();
        _registosRepository.Setup(r => r.GetUltimoRegistoAsync(1, 1)).ReturnsAsync(new RegistosProducao
        {
            Fase_id = 1,
            Peca_id = 1,
            Estado_producao = EstadoProducao.PREPARACAO,
            Data_hora = DateTime.UtcNow.AddMinutes(-5)
        });

        // ACT
        Func<Task> act = () => _sut.CreateAsync(BuildDto(estado: EstadoProducao.CONCLUIDO));

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test(Description = "TRP010A - Uma ocorrencia deve criar a respetiva linha FOP com correcao associada quando existir.")]
    public async Task CreateAsync_Should_CreateFopLine_When_OcorrenciaExists()
    {
        // ARRANGE
        SetupValidDependencies();
        SetupPersistCreated();
        _registosRepository.Setup(r => r.GetUltimoRegistoAsync(1, 1)).ReturnsAsync((RegistosProducao?)null);

        var maquina = BuildMaquina(id: 1, faseId: 1, estado: EstadoMaquina.DISPONIVEL);
        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(1)).ReturnsAsync(maquina);
        _pecaRepository.Setup(r => r.GetMoldeIdByPecaIdAsync(1)).ReturnsAsync(1);

        _fichaProducaoService
            .Setup(s => s.EnsureAsync(It.Is<CreateFichaProducaoDto>(dto =>
                dto.Tipo == TipoFicha.FOP &&
                dto.EncomendaMolde_id == 77)))
            .ReturnsAsync(new ResponseFichaProducaoDto
            {
                FichaProducao_id = 500,
                Tipo = TipoFicha.FOP,
                EncomendaMolde_id = 77,
                DataCriacao = DateTime.UtcNow
            });

        _fichaProducaoService
            .Setup(s => s.CreateLinhaFopAsync(500, It.IsAny<CreateFichaFopLinhaDto>()))
            .ReturnsAsync(new ResponseFichaFopLinhaDto
            {
                FichaFopLinha_id = 900,
                FichaFop_id = 500,
                Data = DateTime.UtcNow,
                Ocorrencia = "Falha no sensor",
                Correcao = "Sensor reajustado",
                Responsavel_id = 1,
                Peca_id = 1,
                Molde_id = 1,
                CriadoEm = DateTime.UtcNow
            });

        var dto = BuildDto(estado: EstadoProducao.PREPARACAO, maquinaId: 1);
        dto.Ocorrencia = "  Falha no sensor  ";
        dto.Correcao = "  Sensor reajustado  ";
        dto.EncomendaMolde_id = 77;

        // ACT
        var result = await _sut.CreateAsync(dto);

        // ASSERT
        result.Estado_producao.Should().Be(EstadoProducao.PREPARACAO);
        _fichaProducaoService.Verify(s => s.EnsureAsync(It.Is<CreateFichaProducaoDto>(value =>
            value.Tipo == TipoFicha.FOP &&
            value.EncomendaMolde_id == 77)), Times.Once);
        _fichaProducaoService.Verify(s => s.CreateLinhaFopAsync(500, It.Is<CreateFichaFopLinhaDto>(value =>
            value.Ocorrencia == "Falha no sensor" &&
            value.Correcao == "Sensor reajustado" &&
            value.Responsavel_id == 1 &&
            value.Peca_id == 1 &&
            value.Molde_id == 1)), Times.Once);
        _pecaRepository.Verify(r => r.GetMoldeIdByPecaIdAsync(1), Times.Once);
    }

    [Test(Description = "TRP011 - MONTAGEM aceita PENDENTE como primeiro estado para assinalar pronta para montar.")]
    public async Task CreateAsync_Should_AcceptPendenteAsFirstState_When_PhaseIsMontagem()
    {
        // ARRANGE
        SetupValidDependencies(faseId: 2, nomeFase: NomeFases.MONTAGEM);
        SetupPersistCreated();
        _registosRepository.Setup(r => r.GetUltimoRegistoAsync(2, 1)).ReturnsAsync((RegistosProducao?)null);

        // ACT
        var result = await _sut.CreateAsync(BuildDto(faseId: 2, estado: EstadoProducao.PENDENTE, maquinaId: null));

        // ASSERT
        result.Estado_producao.Should().Be(EstadoProducao.PENDENTE);
        _registosRepository.Verify(r => r.AddWithMachineStateAsync(
            It.Is<RegistosProducao>(rp => rp.Fase_id == 2 && rp.Estado_producao == EstadoProducao.PENDENTE),
            null,
            It.IsAny<Peca?>()), Times.Once);
    }

    [Test(Description = "TRP012 - MONTAGEM EM_CURSO falha quando nem todas as pecas do molde ja estao em montagem.")]
    public async Task CreateAsync_Should_Throw_When_MontagemStartsBeforeAllPiecesReachMontagem()
    {
        // ARRANGE
        SetupValidDependencies(faseId: 2, nomeFase: NomeFases.MONTAGEM);
        _registosRepository.Setup(r => r.GetUltimoRegistoAsync(2, 1)).ReturnsAsync(new RegistosProducao
        {
            Fase_id = 2,
            Peca_id = 1,
            Estado_producao = EstadoProducao.PENDENTE,
            Data_hora = DateTime.UtcNow.AddMinutes(-5)
        });

        _pecaRepository
            .Setup(r => r.GetAllByMoldeIdAsync(1))
            .ReturnsAsync(new List<Peca>
            {
                BuildPeca(1),
                BuildPeca(2)
            });

        _registosRepository
            .Setup(r => r.GetUltimosRegistosGlobaisAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new List<RegistosProducao>
            {
                new()
                {
                    Peca_id = 1,
                    Fase_id = 2,
                    Estado_producao = EstadoProducao.PENDENTE,
                    Data_hora = DateTime.UtcNow.AddMinutes(-5)
                },
                new()
                {
                    Peca_id = 2,
                    Fase_id = 1,
                    Estado_producao = EstadoProducao.EM_CURSO,
                    Data_hora = DateTime.UtcNow.AddMinutes(-2)
                }
            });

        // ACT
        Func<Task> act = () => _sut.CreateAsync(BuildDto(faseId: 2, estado: EstadoProducao.EM_CURSO, maquinaId: 1));

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*todas as pecas do molde devem estar primeiro na fase MONTAGEM*");
    }

    [Test(Description = "TRP013 - MONTAGEM EM_CURSO e permitida quando todas as pecas do molde ja estao em montagem.")]
    public async Task CreateAsync_Should_StartMontagem_When_AllPiecesAreAlreadyInMontagem()
    {
        // ARRANGE
        SetupValidDependencies(faseId: 2, nomeFase: NomeFases.MONTAGEM);
        SetupPersistCreated();
        _registosRepository.Setup(r => r.GetUltimoRegistoAsync(2, 1)).ReturnsAsync(new RegistosProducao
        {
            Fase_id = 2,
            Peca_id = 1,
            Estado_producao = EstadoProducao.PENDENTE,
            Maquina_id = 7,
            Data_hora = DateTime.UtcNow.AddMinutes(-5)
        });

        _pecaRepository
            .Setup(r => r.GetAllByMoldeIdAsync(1))
            .ReturnsAsync(new List<Peca>
            {
                BuildPeca(1),
                BuildPeca(2)
            });

        _registosRepository
            .Setup(r => r.GetUltimosRegistosGlobaisAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new List<RegistosProducao>
            {
                new()
                {
                    Peca_id = 1,
                    Fase_id = 2,
                    Estado_producao = EstadoProducao.PENDENTE,
                    Data_hora = DateTime.UtcNow.AddMinutes(-5)
                },
                new()
                {
                    Peca_id = 2,
                    Fase_id = 2,
                    Estado_producao = EstadoProducao.PENDENTE,
                    Data_hora = DateTime.UtcNow.AddMinutes(-3)
                }
            });

        var maquina = BuildMaquina(id: 1, faseId: 2, estado: EstadoMaquina.DISPONIVEL);
        _maquinaRepository.Setup(r => r.GetByIdUnicoAsync(1)).ReturnsAsync(maquina);

        // ACT
        var result = await _sut.CreateAsync(BuildDto(faseId: 2, estado: EstadoProducao.EM_CURSO, maquinaId: 1));

        // ASSERT
        result.Estado_producao.Should().Be(EstadoProducao.EM_CURSO);
        maquina.Estado.Should().Be(EstadoMaquina.EM_USO);
    }
}
