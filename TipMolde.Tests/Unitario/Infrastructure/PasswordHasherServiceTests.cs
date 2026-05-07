using FluentAssertions;
using TipMolde.Infrastructure.Service;

namespace TipMolde.Tests.Unitario.Infrastructure
{
    [TestFixture]
    [Category("Unit")]
    public sealed class PasswordHasherServiceTests
    {
        [Test(Description = "TPWH001 - Hash deve gerar um valor verificavel e reconhecido como hash.")]
        public void Hash_Should_ReturnVerifiableHash()
        {
            // ARRANGE
            var sut = new PasswordHasherService();

            // ACT
            var hash = sut.Hash("Password123!");

            // ASSERT
            hash.Should().NotBeNullOrWhiteSpace();
            hash.Should().NotBe("Password123!");
            sut.IsHash(hash).Should().BeTrue();
            sut.Verify("Password123!", hash).Should().BeTrue();
        }

        [Test(Description = "TPWH002 - Verify deve falhar para password errada ou input vazio.")]
        public void Verify_Should_ReturnFalse_When_PasswordOrHashIsInvalid()
        {
            // ARRANGE
            var sut = new PasswordHasherService();
            var hash = sut.Hash("Password123!");

            // ACT
            var wrongPassword = sut.Verify("OutraPassword!", hash);
            var emptyPassword = sut.Verify(" ", hash);
            var emptyHash = sut.Verify("Password123!", " ");
            var invalidFormat = sut.Verify("Password123!", "nao-e-hash");

            // ASSERT
            wrongPassword.Should().BeFalse();
            emptyPassword.Should().BeFalse();
            emptyHash.Should().BeFalse();
            invalidFormat.Should().BeFalse();
        }

        [Test(Description = "TPWH003 - Hash deve lancar excecao quando a password e vazia.")]
        public void Hash_Should_Throw_When_PasswordIsEmpty()
        {
            // ARRANGE
            var sut = new PasswordHasherService();

            // ACT
            Action act = () => sut.Hash(" ");

            // ASSERT
            act.Should().Throw<ArgumentException>()
                .WithMessage("Password e obrigatoria.*");
        }
    }
}
