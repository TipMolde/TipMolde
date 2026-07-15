using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TipMolde.Application.Dtos.IndustrialProducaoDto;
using TipMolde.Application.Dtos.RegistoProducaoDto;
using TipMolde.Application.Interface.Producao.IIndustrial;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Producao.IRegistosProducao;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class IndustrialProducaoServiceTests
{
    private Mock<IEventoMaquinaIndustrialRepository> _eventoRepository = null!;
    private Mock<ISessaoMaquinaIndustrialRepository> _sessaoRepository = null!;
    private Mock<IMaquinaRepository> _maquinaRepository = null!;
    private Mock<IPecaRepository> _pecaRepository = null!;
    private Mock<IRegistosProducaoService> _registosProducaoService = null!;
    private IndustrialProducaoService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _eventoRepository = new Mock<IEventoMaquinaIndustrialRepository>();
        _sessaoRepository = new Mock<ISessaoMaquinaIndustrialRepository>();
        _maquinaRepository = new Mock<IMaquinaRepository>();
        _pecaRepository = new Mock<IPecaRepository>();
        _registosProducaoService = new Mock<IRegistosProducaoService>();

        _sut = new IndustrialProducaoService(
            _eventoRepository.Object,
            _sessaoRepository.Object,
            _maquinaRepository.Object,
            _pecaRepository.Object,
            _registosProducaoService.Object,
            NullLogger<IndustrialProducaoService>.Instance);
    }

    [Test]
    public async Task ProcessarTelemetriaAsync_RunningSemSessao_CriaEventoPendenteDeContexto()
    {
        var telemetry = BuildTelemetry("RUNNING");
        var capturedEvento = (EventoMaquinaIndustrial?)null;

        _maquinaRepository.Setup(r => r.GetByIpAddressAsync("192.168.1.111"))
            .ReturnsAsync(BuildMaquina());
        _sessaoRepository.Setup(r => r.GetAbertaPorMaquinaAsync(5))
            .ReturnsAsync((SessaoMaquinaIndustrial?)null);
        _eventoRepository.Setup(r => r.AddAsync(It.IsAny<EventoMaquinaIndustrial>()))
            .Callback<EventoMaquinaIndustrial>(e => capturedEvento = e)
            .ReturnsAsync((EventoMaquinaIndustrial e) => e);

        var result = await _sut.ProcessarTelemetriaAsync([telemetry]);

        result.Recebidos.Should().Be(1);
        result.Processados.Should().Be(1);
        result.Pendentes.Should().Be(1);
        capturedEvento.Should().NotBeNull();
        capturedEvento!.EstadoResolucao.Should().Be(EstadoResolucaoEventoMaquinaIndustrial.PENDENTE);
        capturedEvento.EstadoMaquina.Should().Be("RUNNING");
        capturedEvento.CamposEmFalta.Should().Be("Operador_id,Peca_id,Fase_id");
        capturedEvento.Maquina_id.Should().Be(5);
    }

    [Test]
    public async Task GetEventoPendenteMaquinaAsync_SessaoAguardarConfirmacao_DevePriorizarStoppedDaSessao()
    {
        var sessao = BuildSessao();
        sessao.EstadoSessao = EstadoSessaoMaquinaIndustrial.AGUARDAR_CONFIRMACAO_PARAGEM;
        var stopped = WithId(BuildEvento("STOPPED", new DateTime(2026, 6, 26, 8, 30, 0, DateTimeKind.Utc)), 71);
        stopped.SessaoMaquinaIndustrial_id = sessao.SessaoMaquinaIndustrial_id;

        _sessaoRepository.Setup(r => r.GetAbertaPorMaquinaAsync(5)).ReturnsAsync(sessao);
        _eventoRepository.Setup(r => r.GetUltimoStoppedPendenteAsync(100)).ReturnsAsync(stopped);

        var result = await _sut.GetEventoPendenteMaquinaAsync(5);

        result.Should().NotBeNull();
        result!.EventoMaquinaIndustrial_id.Should().Be(71);
        result.EstadoMaquina.Should().Be("STOPPED");
        _eventoRepository.Verify(r => r.GetMaisRecentePendentePorMaquinaAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetEventoPendenteMaquinaAsync_SessaoAtivaSemAcaoManual_DeveIgnorarPendentesAntigos()
    {
        _sessaoRepository.Setup(r => r.GetAbertaPorMaquinaAsync(5)).ReturnsAsync(BuildSessao());

        var result = await _sut.GetEventoPendenteMaquinaAsync(5);

        result.Should().BeNull();
        _eventoRepository.Verify(r => r.GetUltimoStoppedPendenteAsync(It.IsAny<int>()), Times.Never);
        _eventoRepository.Verify(r => r.GetMaisRecentePendentePorMaquinaAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetEventoPendenteMaquinaAsync_SemSessaoAberta_DeveUsarRunningMaisRecente()
    {
        var running = WithId(BuildEvento("RUNNING", new DateTime(2026, 6, 26, 8, 30, 0, DateTimeKind.Utc)), 81);

        _sessaoRepository.Setup(r => r.GetAbertaPorMaquinaAsync(5)).ReturnsAsync((SessaoMaquinaIndustrial?)null);
        _eventoRepository.Setup(r => r.GetMaisRecentePendentePorMaquinaAsync(5, "RUNNING")).ReturnsAsync(running);

        var result = await _sut.GetEventoPendenteMaquinaAsync(5);

        result.Should().NotBeNull();
        result!.EventoMaquinaIndustrial_id.Should().Be(81);
        result.EstadoMaquina.Should().Be("RUNNING");
    }

    [Test]
    public async Task CompletarContextoAsync_EventoRunningPendente_CriaSessaoEIniciaProducao()
    {
        var evento = BuildEvento("RUNNING");
        var estadosCriados = new List<EstadoProducao?>();
        var sessaoCriada = (SessaoMaquinaIndustrial?)null;
        var sessaoAtualizada = (SessaoMaquinaIndustrial?)null;
        var eventoAtualizado = (EventoMaquinaIndustrial?)null;

        _eventoRepository.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(evento);
        _sessaoRepository.Setup(r => r.GetAbertaPorMaquinaAsync(5))
            .ReturnsAsync((SessaoMaquinaIndustrial?)null);
        _maquinaRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildMaquina());
        _pecaRepository.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(BuildPeca());
        _sessaoRepository.Setup(r => r.AddAsync(It.IsAny<SessaoMaquinaIndustrial>()))
            .Callback<SessaoMaquinaIndustrial>(s =>
            {
                s.SessaoMaquinaIndustrial_id = 100;
                sessaoCriada = s;
            })
            .ReturnsAsync((SessaoMaquinaIndustrial s) => s);
        _sessaoRepository.Setup(r => r.UpdateAsync(It.IsAny<SessaoMaquinaIndustrial>()))
            .Callback<SessaoMaquinaIndustrial>(s => sessaoAtualizada = s)
            .Returns(Task.CompletedTask);
        _eventoRepository.Setup(r => r.UpdateAsync(It.IsAny<EventoMaquinaIndustrial>()))
            .Callback<EventoMaquinaIndustrial>(e => eventoAtualizado = e)
            .Returns(Task.CompletedTask);
        SetupRegistoIndustrial(estadosCriados);

        var result = await _sut.CompletarContextoAsync(20, new CompletarContextoEventoIndustrialDto
        {
            Operador_id = 3,
            Peca_id = 30,
            Fase_id = 7
        });

        result.SessaoMaquinaIndustrial_id.Should().Be(100);
        sessaoCriada.Should().NotBeNull();
        sessaoCriada!.Operador_id.Should().Be(3);
        sessaoCriada.Peca_id.Should().Be(30);
        sessaoCriada.Fase_id.Should().Be(7);
        sessaoAtualizada!.RegistoProducaoInicio_id.Should().Be(1001);
        estadosCriados.Should().Equal(EstadoProducao.EM_CURSO);
        eventoAtualizado!.EstadoResolucao.Should().Be(EstadoResolucaoEventoMaquinaIndustrial.RESOLVIDO);
        eventoAtualizado.ResolvidoComoEstadoProducao.Should().Be(EstadoProducao.EM_CURSO);
        eventoAtualizado.SessaoMaquinaIndustrial_id.Should().Be(100);
    }

    [Test]
    public async Task CompletarContextoAsync_PecaForaDaFaseDaMaquina_DeveFalhar()
    {
        var evento = BuildEvento("RUNNING");

        _eventoRepository.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(evento);
        _sessaoRepository.Setup(r => r.GetAbertaPorMaquinaAsync(5))
            .ReturnsAsync((SessaoMaquinaIndustrial?)null);
        _maquinaRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(BuildMaquina());
        _pecaRepository.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(BuildPeca(proximaFaseId: 8));

        var act = async () => await _sut.CompletarContextoAsync(20, new CompletarContextoEventoIndustrialDto
        {
            Operador_id = 3,
            Peca_id = 30,
            Fase_id = 7
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*nao pertence a fase dedicada desta maquina*");
    }

    [Test]
    public async Task ConfirmarParagemAsync_TrabalhoConcluido_CriaRegistoConcluidoEFechaSessao()
    {
        var stoppedAt = new DateTime(2026, 6, 26, 8, 30, 0, DateTimeKind.Utc);
        var evento = WithId(BuildEvento("STOPPED", stoppedAt), 40);
        evento.SessaoMaquinaIndustrial_id = 100;
        evento.CreatedAt = stoppedAt;
        var sessao = BuildSessao();
        sessao.RegistoProducaoInicio_id = 250;
        var sessaoAtualizada = (SessaoMaquinaIndustrial?)null;
        var eventoAtualizado = (EventoMaquinaIndustrial?)null;
        CreateRegistosProducaoDto? dtoCriado = null;
        DateTime? dataCriada = null;

        _eventoRepository.Setup(r => r.GetByIdAsync(40)).ReturnsAsync(evento);
        _sessaoRepository.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(sessao);
        _registosProducaoService.Setup(s => s.GetUltimoRegistoAsync(sessao.Fase_id, sessao.Peca_id))
            .ReturnsAsync(new ResponseRegistosProducaoDto { Estado_producao = EstadoProducao.EM_CURSO });
        _registosProducaoService.Setup(s => s.CreateFromIndustrialEventAsync(It.IsAny<CreateRegistosProducaoDto>(), It.IsAny<DateTime>(), It.IsAny<bool>()))
            .Callback<CreateRegistosProducaoDto, DateTime, bool>((dto, data, _) =>
            {
                dtoCriado = dto;
                dataCriada = data;
            })
            .ReturnsAsync(new ResponseRegistosProducaoDto { Registo_Producao_id = 300, Estado_producao = EstadoProducao.CONCLUIDO });
        _sessaoRepository.Setup(r => r.UpdateAsync(It.IsAny<SessaoMaquinaIndustrial>()))
            .Callback<SessaoMaquinaIndustrial>(s => sessaoAtualizada = s)
            .Returns(Task.CompletedTask);
        _eventoRepository.Setup(r => r.UpdateAsync(It.IsAny<EventoMaquinaIndustrial>()))
            .Callback<EventoMaquinaIndustrial>(e => eventoAtualizado = e)
            .Returns(Task.CompletedTask);

        var result = await _sut.ConfirmarParagemAsync(40, new ConfirmarParagemIndustrialDto
        {
            TrabalhoConcluido = true,
            ProximaFase_id = 12
        });

        result.ResolvidoComoEstadoProducao.Should().Be(EstadoProducao.CONCLUIDO);
        dtoCriado!.Estado_producao.Should().Be(EstadoProducao.CONCLUIDO);
        dtoCriado.ProximaFase_id.Should().Be(12);
        dataCriada.Should().Be(stoppedAt);
        sessaoAtualizada!.EstadoSessao.Should().Be(EstadoSessaoMaquinaIndustrial.FECHADA);
        sessaoAtualizada.ClosedAt.Should().Be(stoppedAt);
        sessaoAtualizada.RegistoProducaoConclusao_id.Should().Be(300);
        eventoAtualizado!.RegistoProducao_id.Should().Be(300);
    }

    [Test]
    public async Task ConfirmarParagemAsync_TrabalhoConcluidoJaPersistido_FechaSessaoSemNovoRegisto()
    {
        var stoppedAt = new DateTime(2026, 6, 26, 8, 30, 0, DateTimeKind.Utc);
        var evento = WithId(BuildEvento("STOPPED", stoppedAt), 40);
        evento.SessaoMaquinaIndustrial_id = 100;
        evento.CreatedAt = stoppedAt;
        var sessao = BuildSessao();
        sessao.RegistoProducaoInicio_id = 250;
        var sessaoAtualizada = (SessaoMaquinaIndustrial?)null;
        var eventoAtualizado = (EventoMaquinaIndustrial?)null;

        _eventoRepository.Setup(r => r.GetByIdAsync(40)).ReturnsAsync(evento);
        _sessaoRepository.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(sessao);
        _registosProducaoService.Setup(s => s.GetUltimoRegistoAsync(sessao.Fase_id, sessao.Peca_id))
            .ReturnsAsync(new ResponseRegistosProducaoDto
            {
                Registo_Producao_id = 300,
                Estado_producao = EstadoProducao.CONCLUIDO
            });
        _sessaoRepository.Setup(r => r.UpdateAsync(It.IsAny<SessaoMaquinaIndustrial>()))
            .Callback<SessaoMaquinaIndustrial>(s => sessaoAtualizada = s)
            .Returns(Task.CompletedTask);
        _eventoRepository.Setup(r => r.UpdateAsync(It.IsAny<EventoMaquinaIndustrial>()))
            .Callback<EventoMaquinaIndustrial>(e => eventoAtualizado = e)
            .Returns(Task.CompletedTask);

        var result = await _sut.ConfirmarParagemAsync(40, new ConfirmarParagemIndustrialDto
        {
            TrabalhoConcluido = true,
            ProximaFase_id = 12
        });

        result.RegistoProducao_id.Should().Be(300);
        result.ResolvidoComoEstadoProducao.Should().Be(EstadoProducao.CONCLUIDO);
        sessaoAtualizada!.EstadoSessao.Should().Be(EstadoSessaoMaquinaIndustrial.FECHADA);
        sessaoAtualizada.RegistoProducaoConclusao_id.Should().Be(300);
        sessaoAtualizada.ClosedAt.Should().Be(stoppedAt);
        eventoAtualizado!.FonteResolucao.Should().Be("UTILIZADOR_CONFIRMOU_CONCLUIDO_IDEMPOTENTE");
        _registosProducaoService.Verify(
            s => s.CreateFromIndustrialEventAsync(It.IsAny<CreateRegistosProducaoDto>(), It.IsAny<DateTime>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Test]
    public async Task ConfirmarParagemAsync_SessaoSemRegistoInicial_ReconstroiInicioAntesDePausar()
    {
        var stoppedAt = new DateTime(2026, 6, 26, 8, 30, 0, DateTimeKind.Utc);
        var evento = WithId(BuildEvento("STOPPED", stoppedAt), 40);
        evento.SessaoMaquinaIndustrial_id = 100;
        var sessao = BuildSessao();
        var sessaoAtualizada = (SessaoMaquinaIndustrial?)null;
        var eventoAtualizado = (EventoMaquinaIndustrial?)null;
        var estadosCriados = new List<EstadoProducao?>();
        var datasCriadas = new List<DateTime>();

        _eventoRepository.Setup(r => r.GetByIdAsync(40)).ReturnsAsync(evento);
        _sessaoRepository.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(sessao);
        _registosProducaoService.Setup(s => s.GetUltimoRegistoAsync(sessao.Fase_id, sessao.Peca_id))
            .ReturnsAsync(new ResponseRegistosProducaoDto
            {
                Registo_Producao_id = 200,
                Estado_producao = EstadoProducao.CONCLUIDO,
                Data_hora = new DateTime(2026, 6, 23, 14, 47, 4, DateTimeKind.Utc)
            });
        _registosProducaoService.Setup(s => s.CreateFromIndustrialEventAsync(It.IsAny<CreateRegistosProducaoDto>(), It.IsAny<DateTime>(), It.IsAny<bool>()))
            .Callback<CreateRegistosProducaoDto, DateTime, bool>((dto, data, _) =>
            {
                estadosCriados.Add(dto.Estado_producao);
                datasCriadas.Add(data);
            })
            .ReturnsAsync((CreateRegistosProducaoDto dto, DateTime data, bool _) => new ResponseRegistosProducaoDto
            {
                Registo_Producao_id = 300 + estadosCriados.Count,
                Estado_producao = dto.Estado_producao!.Value,
                Data_hora = data
            });
        _sessaoRepository.Setup(r => r.UpdateAsync(It.IsAny<SessaoMaquinaIndustrial>()))
            .Callback<SessaoMaquinaIndustrial>(s => sessaoAtualizada = s)
            .Returns(Task.CompletedTask);
        _eventoRepository.Setup(r => r.UpdateAsync(It.IsAny<EventoMaquinaIndustrial>()))
            .Callback<EventoMaquinaIndustrial>(e => eventoAtualizado = e)
            .Returns(Task.CompletedTask);

        var result = await _sut.ConfirmarParagemAsync(40, new ConfirmarParagemIndustrialDto
        {
            TrabalhoConcluido = false
        });

        result.ResolvidoComoEstadoProducao.Should().Be(EstadoProducao.PAUSADO);
        estadosCriados.Should().Equal(EstadoProducao.EM_CURSO, EstadoProducao.PAUSADO);
        datasCriadas.Should().Equal(sessao.StartedAt, stoppedAt);
        sessaoAtualizada!.RegistoProducaoInicio_id.Should().Be(301);
        sessaoAtualizada.EstadoSessao.Should().Be(EstadoSessaoMaquinaIndustrial.ATIVA);
        eventoAtualizado!.RegistoProducao_id.Should().Be(302);
    }

    [Test]
    public async Task ConfirmarParagemAsync_SessaoComPreparacaoSemEmCurso_RecuperaEmCursoAntesDePausar()
    {
        var stoppedAt = new DateTime(2026, 6, 26, 8, 30, 0, DateTimeKind.Utc);
        var evento = WithId(BuildEvento("STOPPED", stoppedAt), 40);
        evento.SessaoMaquinaIndustrial_id = 100;
        var sessao = BuildSessao();
        var sessaoAtualizada = (SessaoMaquinaIndustrial?)null;
        var eventoAtualizado = (EventoMaquinaIndustrial?)null;
        var estadosCriados = new List<EstadoProducao?>();
        var datasCriadas = new List<DateTime>();

        _eventoRepository.Setup(r => r.GetByIdAsync(40)).ReturnsAsync(evento);
        _sessaoRepository.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(sessao);
        _registosProducaoService.Setup(s => s.GetUltimoRegistoAsync(sessao.Fase_id, sessao.Peca_id))
            .ReturnsAsync(new ResponseRegistosProducaoDto
            {
                Registo_Producao_id = 200,
                Estado_producao = EstadoProducao.PREPARACAO,
                Data_hora = sessao.StartedAt
            });
        _registosProducaoService.Setup(s => s.CreateFromIndustrialEventAsync(It.IsAny<CreateRegistosProducaoDto>(), It.IsAny<DateTime>(), It.IsAny<bool>()))
            .Callback<CreateRegistosProducaoDto, DateTime, bool>((dto, data, _) =>
            {
                estadosCriados.Add(dto.Estado_producao);
                datasCriadas.Add(data);
            })
            .ReturnsAsync((CreateRegistosProducaoDto dto, DateTime data, bool _) => new ResponseRegistosProducaoDto
            {
                Registo_Producao_id = 300 + estadosCriados.Count,
                Estado_producao = dto.Estado_producao!.Value,
                Data_hora = data
            });
        _sessaoRepository.Setup(r => r.UpdateAsync(It.IsAny<SessaoMaquinaIndustrial>()))
            .Callback<SessaoMaquinaIndustrial>(s => sessaoAtualizada = s)
            .Returns(Task.CompletedTask);
        _eventoRepository.Setup(r => r.UpdateAsync(It.IsAny<EventoMaquinaIndustrial>()))
            .Callback<EventoMaquinaIndustrial>(e => eventoAtualizado = e)
            .Returns(Task.CompletedTask);

        var result = await _sut.ConfirmarParagemAsync(40, new ConfirmarParagemIndustrialDto
        {
            TrabalhoConcluido = false
        });

        result.ResolvidoComoEstadoProducao.Should().Be(EstadoProducao.PAUSADO);
        estadosCriados.Should().Equal(EstadoProducao.EM_CURSO, EstadoProducao.PAUSADO);
        datasCriadas.Should().Equal(sessao.StartedAt, stoppedAt);
        sessaoAtualizada!.RegistoProducaoInicio_id.Should().Be(301);
        eventoAtualizado!.RegistoProducao_id.Should().Be(302);
    }

    [Test]
    public async Task ConfirmarParagemAsync_ConclusaoSemProximaFase_DeveFalhar()
    {
        var evento = WithId(BuildEvento("STOPPED"), 40);
        evento.SessaoMaquinaIndustrial_id = 100;

        _eventoRepository.Setup(r => r.GetByIdAsync(40)).ReturnsAsync(evento);
        _sessaoRepository.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(BuildSessao());

        var act = async () => await _sut.ConfirmarParagemAsync(40, new ConfirmarParagemIndustrialDto
        {
            TrabalhoConcluido = true
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*obrigatorio indicar a proxima fase*");
    }

    [Test]
    public async Task ProcessarTelemetriaAsync_RunningComStoppedPendente_CriaPausaNaHoraDoStoppedERetomaNaHoraDoRunning()
    {
        var stoppedAt = new DateTime(2026, 6, 26, 8, 0, 0, DateTimeKind.Utc);
        var runningAt = new DateTime(2026, 6, 26, 8, 10, 0, DateTimeKind.Utc);
        var stoppedPendente = BuildEvento("STOPPED", stoppedAt);
        stoppedPendente.EventoMaquinaIndustrial_id = 70;
        stoppedPendente.SessaoMaquinaIndustrial_id = 100;
        var sessao = BuildSessao();
        var estadosCriados = new List<EstadoProducao?>();
        var datasCriadas = new List<DateTime>();
        var runningCriado = (EventoMaquinaIndustrial?)null;
        var stoppedAtualizado = (EventoMaquinaIndustrial?)null;

        _maquinaRepository.Setup(r => r.GetByIpAddressAsync("192.168.1.111"))
            .ReturnsAsync(BuildMaquina());
        _sessaoRepository.Setup(r => r.GetAbertaPorMaquinaAsync(5))
            .ReturnsAsync(sessao);
        _eventoRepository.Setup(r => r.GetUltimoStoppedPendenteAsync(100))
            .ReturnsAsync(stoppedPendente);
        _registosProducaoService.Setup(s => s.CreateFromIndustrialEventAsync(It.IsAny<CreateRegistosProducaoDto>(), It.IsAny<DateTime>(), It.IsAny<bool>()))
            .Callback<CreateRegistosProducaoDto, DateTime, bool>((dto, data, _) =>
            {
                estadosCriados.Add(dto.Estado_producao);
                datasCriadas.Add(data);
            })
            .ReturnsAsync((CreateRegistosProducaoDto dto, DateTime data, bool _) => new ResponseRegistosProducaoDto
            {
                Registo_Producao_id = estadosCriados.Count,
                Estado_producao = dto.Estado_producao!.Value,
                Data_hora = data
            });
        _eventoRepository.Setup(r => r.UpdateAsync(It.IsAny<EventoMaquinaIndustrial>()))
            .Callback<EventoMaquinaIndustrial>(e => stoppedAtualizado = e)
            .Returns(Task.CompletedTask);
        _eventoRepository.Setup(r => r.AddAsync(It.IsAny<EventoMaquinaIndustrial>()))
            .Callback<EventoMaquinaIndustrial>(e => runningCriado = e)
            .ReturnsAsync((EventoMaquinaIndustrial e) => e);
        _sessaoRepository.Setup(r => r.UpdateAsync(It.IsAny<SessaoMaquinaIndustrial>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ProcessarTelemetriaAsync([BuildTelemetry("RUNNING", runningAt)]);

        result.Resolvidos.Should().Be(1);
        estadosCriados.Should().Equal(EstadoProducao.PAUSADO, EstadoProducao.EM_CURSO);
        datasCriadas.Should().Equal(stoppedAt, runningAt);
        stoppedAtualizado!.ResolvidoComoEstadoProducao.Should().Be(EstadoProducao.PAUSADO);
        runningCriado!.ResolvidoComoEstadoProducao.Should().Be(EstadoProducao.EM_CURSO);
        sessao.UltimoEstadoMaquina.Should().Be("RUNNING");
    }

    [Test]
    public async Task ProcessarTelemetriaAsync_RunningComSessaoAtivaSemRegistoInicio_NaoCriaRegistoAutomatico()
    {
        var runningAt = new DateTime(2026, 6, 26, 8, 10, 0, DateTimeKind.Utc);
        var sessao = BuildSessao();
        sessao.UltimoEstadoMaquina = "IDLE";
        sessao.RegistoProducaoInicio_id = null;
        var eventoCriado = (EventoMaquinaIndustrial?)null;

        _maquinaRepository.Setup(r => r.GetByIpAddressAsync("192.168.1.111"))
            .ReturnsAsync(BuildMaquina());
        _sessaoRepository.Setup(r => r.GetAbertaPorMaquinaAsync(5))
            .ReturnsAsync(sessao);
        _eventoRepository.Setup(r => r.AddAsync(It.IsAny<EventoMaquinaIndustrial>()))
            .Callback<EventoMaquinaIndustrial>(e => eventoCriado = e)
            .ReturnsAsync((EventoMaquinaIndustrial e) => e);

        var beforeProcessing = DateTime.UtcNow;
        var result = await _sut.ProcessarTelemetriaAsync([BuildTelemetry("RUNNING", runningAt)]);
        var afterProcessing = DateTime.UtcNow;

        result.Resolvidos.Should().Be(1);
        eventoCriado.Should().NotBeNull();
        eventoCriado!.FonteResolucao.Should().Be("RUNNING_SEM_REGISTO_INICIAL");
        eventoCriado.ResolvidoComoEstadoProducao.Should().BeNull();
        _registosProducaoService.Verify(
            s => s.CreateFromIndustrialEventAsync(
                It.IsAny<CreateRegistosProducaoDto>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool>()),
            Times.Never);
        sessao.UltimoEstadoMaquina.Should().Be("RUNNING");
        sessao.LastSeenAt.Should().BeOnOrAfter(beforeProcessing);
        sessao.LastSeenAt.Should().BeOnOrBefore(afterProcessing);
        sessao.EstadoSessao.Should().Be(EstadoSessaoMaquinaIndustrial.ATIVA);
    }

    private static IndustrialTelemetryDto BuildTelemetry(string state, DateTime? occurredAt = null) => new()
    {
        MachineIp = "192.168.1.111",
        Protocol = "OPC-UA",
        State = state,
        OccurredAt = occurredAt ?? new DateTime(2026, 6, 26, 8, 0, 0, DateTimeKind.Utc),
        Program = "OPC_SIM_A",
        PieceCounter = 1,
        RawPayload = "STATE=RUNNING"
    };

    private static Maquina BuildMaquina() => new()
    {
        Maquina_id = 5,
        Numero = 5,
        NomeModelo = "CNC Teste",
        FaseDedicada_id = 7,
        IpAddress = "192.168.1.111",
        Estado = EstadoMaquina.DISPONIVEL
    };

    private static SessaoMaquinaIndustrial BuildSessao() => new()
    {
        SessaoMaquinaIndustrial_id = 100,
        Maquina_id = 5,
        Operador_id = 3,
        Peca_id = 30,
        Fase_id = 7,
        EstadoSessao = EstadoSessaoMaquinaIndustrial.ATIVA,
        UltimoEstadoMaquina = "STOPPED",
        StartedAt = new DateTime(2026, 6, 26, 7, 0, 0, DateTimeKind.Utc),
        LastSeenAt = new DateTime(2026, 6, 26, 8, 0, 0, DateTimeKind.Utc)
    };

    private static Peca BuildPeca(int proximaFaseId = 7) => new()
    {
        Peca_id = 30,
        Designacao = "Peca teste",
        ProximaFase_id = proximaFaseId,
        Molde_id = 1,
        MaterialRecebido = true
    };

    private static EventoMaquinaIndustrial BuildEvento(string state, DateTime? occurredAt = null) => new()
    {
        EventoMaquinaIndustrial_id = 20,
        Maquina_id = 5,
        IpMaquina = "192.168.1.111",
        Protocolo = "OPC-UA",
        EstadoMaquina = state,
        OccurredAt = occurredAt ?? new DateTime(2026, 6, 26, 8, 0, 0, DateTimeKind.Utc),
        EstadoResolucao = EstadoResolucaoEventoMaquinaIndustrial.PENDENTE
    };

    private static EventoMaquinaIndustrial WithId(EventoMaquinaIndustrial evento, int id)
    {
        evento.EventoMaquinaIndustrial_id = id;
        return evento;
    }

    private void SetupRegistoIndustrial(List<EstadoProducao?> estadosCriados)
    {
        _registosProducaoService.Setup(s => s.CreateFromIndustrialEventAsync(It.IsAny<CreateRegistosProducaoDto>(), It.IsAny<DateTime>(), It.IsAny<bool>()))
            .Returns((CreateRegistosProducaoDto dto, DateTime data, bool _) =>
            {
                estadosCriados.Add(dto.Estado_producao);

                return Task.FromResult(new ResponseRegistosProducaoDto
                {
                    Registo_Producao_id = 1000 + estadosCriados.Count,
                    Estado_producao = dto.Estado_producao!.Value,
                    Data_hora = data,
                    Fase_id = dto.Fase_id,
                    Operador_id = dto.Operador_id,
                    Peca_id = dto.Peca_id,
                    Maquina_id = dto.Maquina_id
                });
            });
    }
}
