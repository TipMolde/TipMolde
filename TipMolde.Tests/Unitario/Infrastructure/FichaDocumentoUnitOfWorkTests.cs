using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TipMolde.Infrastructure.DB;
using TipMolde.Infrastructure.Service;

namespace TipMolde.Tests.Unitario.Infrastructure
{
    [TestFixture]
    [Category("Unit")]
    public sealed class FichaDocumentoUnitOfWorkTests
    {
        [Test(Description = "TFDUOW001 - ExecuteInTransaction deve devolver o resultado produzido pela operacao.")]
        public async Task ExecuteInTransactionAsync_Should_ReturnActionResult()
        {
            // ARRANGE
            await using var context = CreateContext();
            var sut = new FichaDocumentoUnitOfWork(context);

            // ACT
            var result = await sut.ExecuteInTransactionAsync(async () =>
            {
                await Task.Yield();
                return 42;
            });

            // ASSERT
            result.Should().Be(42);
        }

        [Test(Description = "TFDUOW002 - ExecuteInTransaction deve propagar a excecao da operacao.")]
        public async Task ExecuteInTransactionAsync_Should_Rethrow_When_ActionFails()
        {
            // ARRANGE
            await using var context = CreateContext();
            var sut = new FichaDocumentoUnitOfWork(context);

            // ACT
            Func<Task> act = async () => await sut.ExecuteInTransactionAsync<int>(() =>
                throw new InvalidOperationException("falha transacional"));

            // ASSERT
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("falha transacional");
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
