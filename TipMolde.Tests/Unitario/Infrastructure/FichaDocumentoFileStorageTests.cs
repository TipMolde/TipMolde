using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using TipMolde.Infrastructure.Service;
using TipMolde.Infrastructure.Settings;

namespace TipMolde.Tests.Unitario.Infrastructure
{
    [TestFixture]
    [Category("Unit")]
    public sealed class FichaDocumentoFileStorageTests
    {
        [Test(Description = "TFDSTG001 - Save, Read e Delete devem gerir corretamente o ficheiro fisico da ficha.")]
        public async Task SaveReadAndDelete_Should_ManageDocumentLifecycle_OnFilesystem()
        {
            // ARRANGE
            var contentRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "tmp-storage", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(contentRoot);

            try
            {
                var environment = new Mock<IHostEnvironment>();
                environment.SetupGet(x => x.ContentRootPath).Returns(contentRoot);

                var options = Options.Create(new StorageOptions
                {
                    FichasRootPath = "fichas"
                });

                var sut = new FichaDocumentoFileStorage(options, environment.Object);
                var bytes = new byte[] { 1, 2, 3, 4 };

                // ACT
                var path = await sut.SaveAsync(7, "manual_v2.pdf", bytes);
                var readBytes = await sut.ReadAsync(path);
                await sut.DeleteIfExistsAsync(path);

                // ASSERT
                path.Should().Be(Path.Combine(contentRoot, "fichas", "7", "manual_v2.pdf"));
                readBytes.Should().Equal(bytes);
                sut.Exists(path).Should().BeFalse();
            }
            finally
            {
                if (Directory.Exists(contentRoot))
                    Directory.Delete(contentRoot, recursive: true);
            }
        }

        [Test(Description = "TFDSTG002 - O construtor deve falhar quando a raiz de storage nao esta configurada.")]
        public void Constructor_Should_Throw_When_FichasRootPathIsMissing()
        {
            // ARRANGE
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(x => x.ContentRootPath).Returns(TestContext.CurrentContext.WorkDirectory);

            var options = Options.Create(new StorageOptions
            {
                FichasRootPath = " "
            });

            // ACT
            Action act = () => new FichaDocumentoFileStorage(options, environment.Object);

            // ASSERT
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Storage:FichasRootPath nao configurado.");
        }
    }
}
