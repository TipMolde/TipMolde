using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TipMolde.Application.Interface.Desenho.IRevisao;
using TipMolde.Infrastructure.Settings;

namespace TipMolde.Infrastructure.Service
{
    /// <summary>
    /// Implementa o armazenamento fisico dos anexos das revisoes.
    /// </summary>
    public sealed class RevisaoAttachmentStorage : IRevisaoAttachmentStorage
    {
        private readonly string _rootPath;

        public RevisaoAttachmentStorage(
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

        public async Task<string> SaveAsync(int revisaoId, string fileName, byte[] content)
        {
            if (revisaoId <= 0)
                throw new ArgumentOutOfRangeException(nameof(revisaoId));

            if (content is null || content.Length == 0)
                throw new ArgumentException("O anexo da revisao e obrigatorio.", nameof(content));

            var safeFileName = BuildFileName(fileName);
            var relativePath = Path.Combine("Revisoes", revisaoId.ToString(), safeFileName);
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

            if (string.IsNullOrWhiteSpace(extension))
                throw new InvalidOperationException("O anexo da revisao deve ter extensao valida.");

            return $"anexo{extension}";
        }
    }
}
