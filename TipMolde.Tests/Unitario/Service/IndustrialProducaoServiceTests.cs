using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TipMolde.Application.Dtos.IndustrialProducaoDto;
using TipMolde.Application.Dtos.RegistoProducaoDto;
using TipMolde.Application.Interface.Producao.IIndustrial;
using TipMolde.Application.Interface.Producao.IMaquina;
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
    private Mock<IRegistosProducaoService> _registosProducaoService = null!;
    private IndustrialProducaoService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _eventoRepository = new Mock<IEventoMaquinaIndustrialRepository>();
        _sessaoRepository = new Mock<ISessaoMaquinaIndustrialRepository>();
        _maquinaRepository = new Mock<IMaquinaRepository>();
        _registosProducaoService = new Mock<IRegistosProducaoService>();

        _sut = new IndustrialProducaoService(
            _eventoRepository.Object,
            _sessaoRepository.Object,
            _maquinaRepository.Object,
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
        sessaoAtualizada!.RegistoProducaoInicio_id.Should().Be(1003);
        estadosCriados.Should().Equal(EstadoProducao.EM_CURSO, EstadoProducao.PREPARACAO, EstadoProducao.EM_CURSO);
        eventoAtualizado!.EstadoResolucao.Should().Be(EstadoResolucaoEventoMaquinaIndustrial.RESOLVIDO);
        eventoAtualizado.ResolvidoComoEstadoProducao.Should().Be(EstadoProducao.EM_CURSO);
        eventoAtualizado.SessaoMaquinaIndustrial_id.Should().Be(100);
    }

    [Test]
    public async Task ConfirmarParagemAsync_TrabalhoConcluido_CriaRegistoConcluidoEFechaSessao()
    {
        var stoppedAt = new DateTime(2026, 6, 26, 8, 30, 0, DateTimeKind.Utc);
        var evento = WithId(BuildEvento("STOPPED", stoppedAt), 40);
        evento.SessaoMaquinaIndustrial_id = 100;
        var sessao = BuildSessao();
        var sessaoAtualizada = (SessaoMaquinaIndustrial?)null;
        var eventoAtualizado = (EventoMaquinaIndustrial?)null;
        CreateRegistosProducaoDto? dtoCriado = null;
        DateTime? dataCriada = null;

        _eventoRepository.Setup(r => r.GetByIdAsync(40)).ReturnsAsync(evento);
        _sessaoRepository.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(sessao);
        _registosProducaoService.Setup(s => s.CreateFromIndustrialEventAsync(It.IsAny<CreateRegistosProducaoDto>(), It.IsAny<DateTime>()))
            .Callback<CreateRegistosProducaoDto, DateTime>((dto, data) =>
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

        var result = await _sut.ConfirmarParagemAsync(40, new ConfirmarParagemIndustrialDto { TrabalhoConcluido = true });

        result.ResolvidoComoEstadoProducao.Should().Be(EstadoProducao.CONCLUIDO);
        dtoCriado!.Estado_producao.Should().Be(EstadoProducao.CONCLUIDO);
        dataCriada.Should().Be(stoppedAt);
        sessaoAtualizada!.EstadoSessao.Should().Be(EstadoSessaoMaquinaIndustrial.FECHADA);
        sessaoAtualizada.ClosedAt.Should().Be(stoppedAt);
        sessaoAtualizada.RegistoProducaoConclusao_id.Should().Be(300);
        eventoAtualizado!.RegistoProducao_id.Should().Be(300);
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
        _registosProducaoService.Setup(s => s.CreateFromIndustrialEventAsync(It.IsAny<CreateRegistosProducaoDto>(), It.IsAny<DateTime>()))
            .Callback<CreateRegistosProducaoDto, DateTime>((dto, data) =>
            {
                estadosCriados.Add(dto.Estado_producao);
                datasCriadas.Add(data);
            })
            .ReturnsAsync((CreateRegistosProducaoDto dto, DateTime data) => new ResponseRegistosProducaoDto
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
        _registosProducaoService.Setup(s => s.CreateFromIndustrialEventAsync(It.IsAny<CreateRegistosProducaoDto>(), It.IsAny<DateTime>()))
            .Returns((CreateRegistosProducaoDto dto, DateTime data) =>
            {
                estadosCriados.Add(dto.Estado_producao);
                if (estadosCriados.Count == 1 && dto.Estado_producao == EstadoProducao.EM_CURSO)
                    throw new ArgumentException("Primeiro estado deve ser PREPARACAO.");

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
