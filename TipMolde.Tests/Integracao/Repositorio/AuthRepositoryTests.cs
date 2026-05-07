using FluentAssertions;
using TipMolde.Domain.Entities;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.Repositorio;

namespace TipMolde.Tests.Integracao.Repositorio
{
    [TestFixture]
    [Category("Integration")]
    public sealed class AuthRepositoryTests : RepositoryIntegrationTestBase
    {
        [Test(Description = "TAUTHREP001 - GetByEmail deve devolver o utilizador correto quando o email existe.")]
        public async Task GetByEmailAsync_Should_ReturnUser_When_EmailExists()
        {
            // ARRANGE
            await using var context = CreateContext();
            await context.Users.AddAsync(new User
            {
                Nome = "Ana Auth",
                Email = "ana@tipmolde.pt",
                Password = "hash",
                Role = UserRole.ADMIN
            });
            await context.SaveChangesAsync();

            var repository = new AuthRepository(context);

            // ACT
            var result = await repository.GetByEmailAsync("ana@tipmolde.pt");

            // ASSERT
            result.Should().NotBeNull();
            result!.Nome.Should().Be("Ana Auth");
        }

        [Test(Description = "TAUTHREP002 - Update deve persistir alteracoes do utilizador.")]
        public async Task UpdateAsync_Should_PersistUserChanges()
        {
            // ARRANGE
            await using var context = CreateContext();
            var user = new User
            {
                Nome = "Bruno Auth",
                Email = "bruno@tipmolde.pt",
                Password = "hash-antigo",
                Role = UserRole.GESTOR_PRODUCAO
            };

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            var repository = new AuthRepository(context);
            user.Password = "hash-novo";

            // ACT
            await repository.UpdateAsync(user);

            // ASSERT
            var persisted = await context.Users.FindAsync(user.User_id);
            persisted!.Password.Should().Be("hash-novo");
        }
    }
}
