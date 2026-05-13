using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TipMolde.Application.Dtos.RegistoTempoProjetoDto;
using TipMolde.Application.Interface.Desenho.IProjeto;
using TipMolde.Application.Interface.Desenho.IRegistoTempoProjeto;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Application.Mappings;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Entities.Desenho;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service;

[TestFixture]
[Category("Unit")]
public class RegistoTempoProjetoServiceTests
{
    private Mock<IRegistoTempoProjetoRepository> _registoRepository = null!;
    private Mock<IProjetoRepository> _projetoRepository = null!;
    private Mock<IUserRepository> _userRepository = null!;
    private Mock<ILogger<RegistoTempoProjetoService>> _logger = null!;
    private IMapper _mapper = null!;
    private RegistoTempoProjetoService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _registoRepository = new Mock<IRegistoTempoProjetoRepository>();
        _projetoRepository = new Mock<IProjetoRepository>();
        _userRepository = new Mock<IUserRepository>();
        _logger = new Mock<ILogger<RegistoTempoProjetoService>>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<RegistoTempoProjetoProfile>());
        _mapper = config.CreateMapper();

        _sut = new RegistoTempoProjetoService(
            _registoRepository.Object,
            _projetoRepository.Object,
            _userRepository.Object,
            _mapper,
            _logger.Object);
    }

    private static CreateRegistoTempoProjetoDto BuildDto(EstadoTempoProjeto? estado = EstadoTempoProjeto.INICIADO)
    {
        return new CreateRegistoTempoProjetoDto
        {
            Estado_tempo = estado,
            Projeto_id = 10,
            Autor_id = 5,
        };
    }

    [Test(Description = "TRTPSRV3 - Create deve falhar quando o primeiro estado nao e INICIADO.")]
    public async Task CreateRegistoAsync_Should_ThrowArgumentException_When_FirstStateIsInvalid()
    {
        // ARRANGE
        var dto = BuildDto(EstadoTempoProjeto.PAUSADO);

        _projetoRepository.Setup(r => r.GetByIdAsync(dto.Projeto_id))
            .ReturnsAsync(new Projeto { Projeto_id = dto.Projeto_id, NomeProjeto = "Projeto", SoftwareUtilizado = "NX", CaminhoPastaServidor = "srv", Molde_id = 100 });

        _userRepository.Setup(r => r.GetByIdAsync(dto.Autor_id))
            .ReturnsAsync(new User { User_id = dto.Autor_id, Nome = "Ana", Email = "ana@tipmolde.pt", Password = "hash", Role = UserRole.ADMIN });

        _registoRepository.Setup(r => r.GetUltimoRegistoAsync(dto.Projeto_id, dto.Autor_id))
            .ReturnsAsync((RegistoTempoProjeto?)null);

        // ACT
        Func<Task> act = () => _sut.CreateRegistoAsync(dto);

        // ASSERT
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Primeiro estado deve ser INICIADO*");
    }

    [Test(Description = "TRTPSRV4 - Create deve persistir e devolver DTO quando os dados sao validos.")]
    public async Task CreateRegistoAsync_Should_PersistAndReturnDto_When_DataIsValid()
    {
        // ARRANGE
        var dto = BuildDto(EstadoTempoProjeto.INICIADO);

        _projetoRepository.Setup(r => r.GetByIdAsync(dto.Projeto_id))
            .ReturnsAsync(new Projeto { Projeto_id = dto.Projeto_id, NomeProjeto = "Projeto", SoftwareUtilizado = "NX", CaminhoPastaServidor = "srv", Molde_id = 100 });

        _userRepository.Setup(r => r.GetByIdAsync(dto.Autor_id))
            .ReturnsAsync(new User { User_id = dto.Autor_id, Nome = "Ana", Email = "ana@tipmolde.pt", Password = "hash", Role = UserRole.ADMIN });

        _registoRepository.Setup(r => r.GetUltimoRegistoAsync(dto.Projeto_id, dto.Autor_id))
            .ReturnsAsync((RegistoTempoProjeto?)null);
        _registoRepository.Setup(r => r.AddAsync(It.IsAny<RegistoTempoProjeto>()))
            .ReturnsAsync((RegistoTempoProjeto entity) =>
            {
                entity.Registo_Tempo_Projeto_id = 25;
                return entity;
            });


        // ACT
        var result = await _sut.CreateRegistoAsync(dto);

        // ASSERT
        result.Registo_Tempo_Projeto_id.Should().Be(25);
        result.Estado_tempo.Should().Be(EstadoTempoProjeto.INICIADO);
        result.Projeto_id.Should().Be(dto.Projeto_id);
        result.Autor_id.Should().Be(dto.Autor_id);
        _registoRepository.Verify(r => r.AddAsync(It.Is<RegistoTempoProjeto>(x =>
            x.Projeto_id == dto.Projeto_id &&
            x.Autor_id == dto.Autor_id &&
            x.Estado_tempo == EstadoTempoProjeto.INICIADO)), Times.Once);
    }
}
