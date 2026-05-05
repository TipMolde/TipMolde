using AutoMapper;
using FluentAssertions;
using TipMolde.Application.Dtos.ClienteDto;
using TipMolde.Application.Mappings;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Mapping
{
    [TestFixture]
    [Category("Unit")]
    public class ClienteProfileTests
    {
        private IMapper _mapper = null!;

        [SetUp]
        public void SetUp()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ClienteProfile>();
                cfg.AddProfile<EncomendaProfile>();
            });

            _mapper = config.CreateMapper();
        }

        [Test(Description = "T1MAPCLI - Configuracao do AutoMapper para Cliente e valida.")]
        public void MappingConfiguration_Should_BeValid_When_ProfilesAreLoaded()
        {
            // ARRANGE
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ClienteProfile>();
                cfg.AddProfile<EncomendaProfile>();
            });

            // ACT
            Action act = () => config.AssertConfigurationIsValid();

            // ASSERT
            act.Should().NotThrow();
        }

        [Test(Description = "T2MAPCLI - CreateClienteDto aplica trim nos campos de texto.")]
        public void CreateClienteDTO_Should_MapWithTrim_When_FieldsContainOuterSpaces()
        {
            // ARRANGE
            var source = new CreateClienteDto
            {
                Nome = "  Cliente Teste  ",
                NIF = " 123456789 ",
                Sigla = "  CT  ",
                Pais = "  PT  ",
                Email = "  cliente@tipmolde.pt  ",
                Telefone = "  912345678  "
            };

            // ACT
            var result = _mapper.Map<Cliente>(source);

            // ASSERT
            result.Nome.Should().Be("Cliente Teste");
            result.NIF.Should().Be("123456789");
            result.Sigla.Should().Be("CT");
            result.Pais.Should().Be("PT");
            result.Email.Should().Be("cliente@tipmolde.pt");
            result.Telefone.Should().Be("912345678");
        }

        [Test(Description = "T3MAPCLI - UpdateClienteDto aplica trim e preserva valores quando campos sao nulos.")]
        public void UpdateClienteDTO_Should_TrimAndPreserveValues_When_NullAndWhitespaceFieldsAreProvided()
        {
            // ARRANGE
            var source = new UpdateClienteDto
            {
                Nome = "  Novo Nome  ",
                NIF = null,
                Sigla = "  NN  ",
                Pais = "  Espanha  ",
                Email = "  novo@tipmolde.pt  ",
                Telefone = null
            };

            var destination = new Cliente
            {
                Cliente_id = 10,
                Nome = "Nome Antigo",
                NIF = "123456789",
                Sigla = "OLD",
                Pais = "Portugal",
                Email = "antigo@tipmolde.pt",
                Telefone = "910000000"
            };

            // ACT
            _mapper.Map(source, destination);

            // ASSERT
            destination.Cliente_id.Should().Be(10);
            destination.Nome.Should().Be("Novo Nome");
            destination.NIF.Should().Be("123456789");
            destination.Sigla.Should().Be("NN");
            destination.Pais.Should().Be("Espanha");
            destination.Email.Should().Be("novo@tipmolde.pt");
            destination.Telefone.Should().Be("910000000");
        }

        [Test(Description = "T4MAPCLI - UpdateClienteDto ignora campos apenas com espacos para nao sobrescrever dados existentes.")]
        public void UpdateClienteDTO_Should_IgnoreWhitespaceOnlyValues_When_MappingToExistingEntity()
        {
            // ARRANGE
            var source = new UpdateClienteDto
            {
                Nome = "   ",
                NIF = "   ",
                Sigla = "   ",
                Pais = "   ",
                Email = "   ",
                Telefone = "   "
            };

            var destination = new Cliente
            {
                Cliente_id = 99,
                Nome = "Nome Atual",
                NIF = "123456789",
                Sigla = "AT",
                Pais = "Portugal",
                Email = "atual@tipmolde.pt",
                Telefone = "919999999"
            };

            // ACT
            _mapper.Map(source, destination);

            // ASSERT
            destination.Cliente_id.Should().Be(99);
            destination.Nome.Should().Be("Nome Atual");
            destination.NIF.Should().Be("123456789");
            destination.Sigla.Should().Be("AT");
            destination.Pais.Should().Be("Portugal");
            destination.Email.Should().Be("atual@tipmolde.pt");
            destination.Telefone.Should().Be("919999999");
        }

        [Test(Description = "T5MAPCLI - Cliente para ResponseClienteDto devolve campos normalizados com trim.")]
        public void Cliente_Should_MapToResponseClienteDTOWithTrim_When_SourceContainsOuterSpaces()
        {
            // ARRANGE
            var source = new Cliente
            {
                Cliente_id = 42,
                Nome = "  Cliente A  ",
                NIF = " 123456789 ",
                Sigla = "  CA ",
                Pais = " PT ",
                Email = " a@tipmolde.pt ",
                Telefone = " 911111111 "
            };

            // ACT
            var result = _mapper.Map<ResponseClienteDto>(source);

            // ASSERT
            result.Cliente_id.Should().Be(42);
            result.Nome.Should().Be("Cliente A");
            result.NIF.Should().Be("123456789");
            result.Sigla.Should().Be("CA");
            result.Pais.Should().Be("PT");
            result.Email.Should().Be("a@tipmolde.pt");
            result.Telefone.Should().Be("911111111");
        }

        [Test(Description = "T6MAPCLI - Cliente para ResponseClienteWithEncomendasDto normaliza campos e mapeia encomendas.")]
        public void Cliente_Should_MapToResponseClienteWithEncomendasDTO_When_HasRelatedEncomendas()
        {
            // ARRANGE
            var cliente = new Cliente
            {
                Cliente_id = 42,
                Nome = "  Cliente A  ",
                NIF = " 123456789 ",
                Sigla = "  CA ",
                Pais = " PT ",
                Email = " cliente@tipmolde.pt ",
                Telefone = " 910000000 ",
                Encomendas =
                [
                    new()
                    {
                        Encomenda_id = 1001,
                        NumeroEncomendaCliente = "ENC-1001",
                        NumeroProjetoCliente = "PRJ-77",
                        NomeServicoCliente = "Molde X",
                        NomeResponsavelCliente = "Maria",
                        DataRegisto = new DateTime(2026, 4, 1),
                        Estado = EstadoEncomenda.CONFIRMADA,
                        Cliente_id = 42,
                        Cliente = new Cliente
                        {
                            Cliente_id = 42,
                            Nome = "  Cliente A  ",
                            NIF = " 123456789 ",
                            Sigla = "  CA "
                        }
                    }
                ]
            };

            // ACT
            var result = _mapper.Map<ResponseClienteWithEncomendasDto>(cliente);

            // ASSERT
            result.ClienteId.Should().Be(42);
            result.Nome.Should().Be("Cliente A");
            result.NIF.Should().Be("123456789");
            result.Sigla.Should().Be("CA");
            result.Pais.Should().Be("PT");
            result.Email.Should().Be("cliente@tipmolde.pt");
            result.Telefone.Should().Be("910000000");

            var encomendas = result.Encomendas;
            encomendas.Should().NotBeNull();
            encomendas!.Should().HaveCount(1);

            var encomenda = encomendas!.First();
            encomenda.Encomenda_id.Should().Be(1001);
            encomenda.NumeroEncomendaCliente.Should().Be("ENC-1001");
            encomenda.NomeCliente.Should().Be("  Cliente A  ");
        }
    }
}
