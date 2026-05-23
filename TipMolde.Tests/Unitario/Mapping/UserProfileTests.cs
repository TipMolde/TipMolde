using AutoMapper;
using FluentAssertions;
using TipMolde.Application.Dtos.UserDto;
using TipMolde.Application.Mappings;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Enums;

namespace TipMolde.Tests.Unitario.Mapping
{
    [TestFixture]
    [Category("Unit")]
    public class UserProfileTests
    {
        private IMapper _mapper = null!;

        [SetUp]
        public void SetUp()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<UserProfile>();
            });

            _mapper = config.CreateMapper();
        }

        [Test]
        public void shouldHaveValidAutoMapperConfiguration()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<UserProfile>();
            });

            config.AssertConfigurationIsValid();
        }

        [Test]
        public void shouldMapCreateUserDtoToUserWithTrimmedFields()
        {
            var source = new CreateUserDto
            {
                Nome = "  Ana  ",
                Email = " ana@tipmolde.pt ",
                Password = "Valida123!",
                Role = UserRole.ADMIN
            };

            var result = _mapper.Map<User>(source);

            result.Nome.Should().Be("Ana");
            result.Email.Should().Be("ana@tipmolde.pt");
            result.Password.Should().Be("Valida123!");
            result.Role.Should().Be(UserRole.ADMIN);
        }

        [Test]
        public void shouldMapUpdateUserDtoToExistingUserWithoutOverwritingNulls()
        {
            var source = new UpdateUserDto { Nome = "  Novo Nome  ", Email = null };
            var destination = new User
            {
                User_id = 11,
                Nome = "Nome Antigo",
                Email = "antigo@tipmolde.pt",
                Password = "hash",
                Role = UserRole.GESTOR_PRODUCAO,
                CreatedAt = DateTime.UtcNow
            };

            _mapper.Map(source, destination);

            destination.Nome.Should().Be("Novo Nome");
            destination.Email.Should().Be("antigo@tipmolde.pt");
        }

        [Test]
        public void shouldMapUserToResponseUserDto()
        {
            var source = new User
            {
                User_id = 7,
                Nome = "Bruno",
                Email = "bruno@tipmolde.pt",
                Password = "hash",
                Role = UserRole.ADMIN,
                CreatedAt = DateTime.UtcNow
            };

            var result = _mapper.Map<ResponseUserDto>(source);

            result.User_id.Should().Be(7);
            result.Nome.Should().Be("Bruno");
            result.Email.Should().Be("bruno@tipmolde.pt");
            result.Role.Should().Be("ADMIN");
        }

        [Test]
        public void shouldMapChangeUserRoleDtoToExistingUserChangingOnlyRole()
        {
            var source = new ChangeUserRoleDto
            {
                Role = UserRole.ADMIN
            };

            var destination = new User
            {
                User_id = 15,
                Nome = "Operador",
                Email = "operador@tipmolde.pt",
                Password = "hash-existente",
                Role = UserRole.GESTOR_PRODUCAO,
                CreatedAt = new DateTime(2026, 1, 10)
            };

            _mapper.Map(source, destination);

            destination.User_id.Should().Be(15);
            destination.Nome.Should().Be("Operador");
            destination.Email.Should().Be("operador@tipmolde.pt");
            destination.Password.Should().Be("hash-existente");
            destination.CreatedAt.Should().Be(new DateTime(2026, 1, 10));
            destination.Role.Should().Be(UserRole.ADMIN);
        }
    }
}
