using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Dtos.OcorrenciaDto;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Application.Service;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Service
{
    /// <summary>
    /// Testes unitarios dos casos de uso de ocorrencias independentes.
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public class OcorrenciasServiceTests
    {
        private Mock<IFichaProducaoService> _fichaProducaoService = null!;
        private Mock<IEncomendaMoldeRepository> _encomendaMoldeRepository = null!;
        private Mock<IPecaRepository> _pecaRepository = null!;
        private Mock<IUserRepository> _userRepository = null!;
        private OcorrenciasService _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _fichaProducaoService = new Mock<IFichaProducaoService>();
            _encomendaMoldeRepository = new Mock<IEncomendaMoldeRepository>();
            _pecaRepository = new Mock<IPecaRepository>();
            _userRepository = new Mock<IUserRepository>();

            _sut = new OcorrenciasService(
                _fichaProducaoService.Object,
                _encomendaMoldeRepository.Object,
                _pecaRepository.Object,
                _userRepository.Object,
                NullLogger<OcorrenciasService>.Instance);
        }

        private static Peca BuildPeca(int id = 1, int moldeId = 7) => new()
        {
            Peca_id = id,
            Designacao = "Peca teste",
            Prioridade = 1,
            Molde_id = moldeId,
            MaterialRecebido = true
        };

        private static EncomendaMolde BuildEncomendaMolde(int id = 99, int moldeId = 7) => new()
        {
            EncomendaMolde_id = id,
            Encomenda_id = 12,
            Molde_id = moldeId,
            Quantidade = 1,
            Prioridade = 1,
            DataEntregaPrevista = DateTime.UtcNow
        };

        [Test(Description = "TOC001 - Registo de ocorrencia cria linha FOP sem depender de estado de producao.")]
        public async Task CreateAsync_Should_CreateFopLine_When_RequestIsValid()
        {
            // ARRANGE
            _encomendaMoldeRepository
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync(BuildEncomendaMolde());
            _pecaRepository
                .Setup(r => r.GetByIdAsync(3))
                .ReturnsAsync(BuildPeca(3, 7));
            _userRepository
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new User
                {
                    User_id = 1,
                    Nome = "Operador",
                    Email = "op@tipmolde.pt",
                    Password = "Hash123!",
                    Role = UserRole.GESTOR_PRODUCAO
                });

            _fichaProducaoService
                .Setup(s => s.EnsureAsync(It.Is<CreateFichaProducaoDto>(dto =>
                    dto.Tipo == TipoFicha.FOP &&
                    dto.EncomendaMolde_id == 99)))
                .ReturnsAsync(new ResponseFichaProducaoDto
                {
                    FichaProducao_id = 500,
                    Tipo = TipoFicha.FOP,
                    EncomendaMolde_id = 99,
                    DataCriacao = DateTime.UtcNow
                });

            _fichaProducaoService
                .Setup(s => s.CreateLinhaFopAsync(500, It.IsAny<CreateFichaFopLinhaDto>()))
                .ReturnsAsync(new ResponseFichaFopLinhaDto
                {
                    FichaFopLinha_id = 900,
                    FichaFop_id = 500,
                    Data = DateTime.UtcNow,
                    Ocorrencia = "Falha",
                    Correcao = "Ajuste",
                    Responsavel_id = 1,
                    Peca_id = 3,
                    Molde_id = 7,
                    CriadoEm = DateTime.UtcNow
                });

            var dto = new CreateOcorrenciaDto
            {
                EncomendaMolde_id = 99,
                Peca_id = 3,
                Responsavel_id = 1,
                Ocorrencia = "  Falha  ",
                Correcao = "  Ajuste  "
            };

            // ACT
            var result = await _sut.CreateAsync(dto);

            // ASSERT
            result.FichaFopLinha_id.Should().Be(900);
            _fichaProducaoService.Verify(s => s.EnsureAsync(It.Is<CreateFichaProducaoDto>(value =>
                value.Tipo == TipoFicha.FOP &&
                value.EncomendaMolde_id == 99)), Times.Once);
            _fichaProducaoService.Verify(s => s.CreateLinhaFopAsync(500, It.Is<CreateFichaFopLinhaDto>(value =>
                value.Ocorrencia == "Falha" &&
                value.Correcao == "Ajuste" &&
                value.Responsavel_id == 1 &&
                value.Peca_id == 3 &&
                value.Molde_id == 7)), Times.Once);
        }

        [Test(Description = "TOC002 - Registo de ocorrencia falha quando a peca nao pertence ao molde da encomenda.")]
        public async Task CreateAsync_Should_Throw_When_PecaDoesNotMatchEncomendaMolde()
        {
            // ARRANGE
            _encomendaMoldeRepository
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync(BuildEncomendaMolde(99, 7));
            _pecaRepository
                .Setup(r => r.GetByIdAsync(3))
                .ReturnsAsync(BuildPeca(3, 8));

            // ACT
            Func<Task> act = () => _sut.CreateAsync(new CreateOcorrenciaDto
            {
                EncomendaMolde_id = 99,
                Peca_id = 3,
                Responsavel_id = 1,
                Ocorrencia = "Falha"
            });

            // ASSERT
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*peca*molde*");
        }
    }
}
