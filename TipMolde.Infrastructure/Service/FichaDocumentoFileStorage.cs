using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TipMolde.Application.Interface.Fichas.IFichaDocumento;
using TipMolde.Infrastructure.Settings;

namespace TipMolde.Infrastructure.Service
{
    /// <summary>
    /// Implementa o armazenamento fisico dos documentos das fichas em filesystem local.
    /// </summary>
    public class FichaDocumentoFileStorage : IFichaDocumentoStorage
    {
        private readonly string _rootPath;

        /// <summary>
        /// Construtor de FichaDocumentoFileStorage.
        /// </summary>
        /// <param name="options">Options com a raiz fisica de armazenamento das fichas.</param>
        /// <param name="environment">Ambiente da aplicacao usado para resolver paths relativos.</param>
        public FichaDocumentoFileStorage(
            IOptions<StorageOptions> options,
            IHostEnvironment environment)
        {
            var configuredPath = options.Value.FichasRootPath;
            if (string.IsNullOrWhiteSpace(configuredPath))
                throw new InvalidOperationException("Storage:FichasRootPath nao configurado.");

            _rootPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
        }

        /// <summary>
        /// Persiste o conteudo binario de um documento na area de armazenamento da ficha.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha dona do documento.</param>
        /// <param name="fileName">Nome final do ficheiro a persistir.</param>
        /// <param name="content">Conteudo binario do ficheiro.</param>
        /// <returns>Caminho fisico final onde o ficheiro ficou guardado.</returns>
        public async Task<string> SaveAsync(int fichaId, string fileName, byte[] content)
        {
            var dir = Path.Combine(_rootPath, fichaId.ToString());
            Directory.CreateDirectory(dir);

            var finalPath = Path.Combine(dir, fileName);
            await File.WriteAllBytesAsync(finalPath, content);
            return finalPath;
        }

        /// <summary>
        /// Carrega o conteudo binario de um documento previamente persistido.
        /// </summary>
        /// <param name="path">Caminho fisico do ficheiro.</param>
        /// <returns>Conteudo binario do ficheiro.</returns>
        public Task<byte[]> ReadAsync(string path) => File.ReadAllBytesAsync(path);

        /// <summary>
        /// Verifica se o ficheiro existe no armazenamento fisico.
        /// </summary>
        /// <param name="path">Caminho fisico do ficheiro.</param>
        /// <returns>True quando o ficheiro existe.</returns>
        public bool Exists(string path) => File.Exists(path);

        /// <summary>
        /// Remove um ficheiro do armazenamento fisico quando ele existe.
        /// </summary>
        /// <param name="path">Caminho fisico do ficheiro.</param>
        public Task DeleteIfExistsAsync(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }
    }
}
