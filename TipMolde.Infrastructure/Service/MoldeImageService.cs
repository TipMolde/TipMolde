using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Infrastructure.Settings;

namespace TipMolde.Infrastructure.Service
{
    /// <summary>
    /// Implementa o armazenamento fisico das imagens de capa dos moldes.
    /// </summary>
    public sealed class MoldeImageService : IMoldeImageService
    {
        private readonly string _rootPath;

        public MoldeImageService(
            IOptions<StorageOptions> options,
            IHostEnvironment environment)
        {
            var configuredPath = options.Value.UploadsRootPath;
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new InvalidOperationException("Storage:UploadsRootPath nao configurado.");

            _rootPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
        }

        public async Task<string> SaveAsync(int moldeId, string fileName, byte[] content)
        {
            if (moldeId <= 0)
                throw new ArgumentOutOfRangeException(nameof(moldeId));

            if (content is null || content.Length == 0)
                throw new ArgumentException("A imagem do molde e obrigatoria.", nameof(content));

            var safeFileName = BuildFileName(fileName);
            var relativePath = Path.Combine("Moldes", moldeId.ToString(), safeFileName);
            var physicalPath = GetPhysicalPath(relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);
            await File.WriteAllBytesAsync(physicalPath, content);

            return relativePath.Replace(Path.DirectorySeparatorChar, '/');
        }

        public Task DeleteIfExistsAsync(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return Task.CompletedTask;

            var physicalPath = Path.IsPathRooted(path)
                ? path
                : GetPhysicalPath(path);

            if (File.Exists(physicalPath))
                File.Delete(physicalPath);

            return Task.CompletedTask;
        }

        private string GetPhysicalPath(string relativePath) =>
            Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

        private static string BuildFileName(string fileName)
        {
            var safeOriginalFileName = Path.GetFileName(fileName);
            var extension = Path.GetExtension(safeOriginalFileName).ToLowerInvariant();

            if (extension is not ".png" and not ".jpg" and not ".jpeg")
                throw new InvalidOperationException("Formato de imagem nao suportado.");

            return $"capa{extension}";
        }
    }
}
