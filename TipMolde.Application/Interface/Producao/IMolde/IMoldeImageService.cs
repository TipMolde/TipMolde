namespace TipMolde.Application.Interface.Producao.IMolde
{
    /// <summary>
    /// Abstrai o armazenamento fisico das imagens de capa dos moldes.
    /// </summary>
    public interface IMoldeImageService
    {
        /// <summary>
        /// Persiste uma imagem de capa no armazenamento dedicado aos uploads.
        /// </summary>
        /// <param name="moldeId">Identificador do molde dono da imagem.</param>
        /// <param name="fileName">Nome original do ficheiro submetido.</param>
        /// <param name="content">Conteudo binario da imagem.</param>
        /// <returns>Caminho relativo guardado na base de dados.</returns>
        Task<string> SaveAsync(int moldeId, string fileName, byte[] content);

        /// <summary>
        /// Remove um ficheiro guardado quando ele existe.
        /// </summary>
        /// <param name="path">Caminho relativo ou absoluto do ficheiro.</param>
        /// <returns>Task concluida quando a operacao termina.</returns>
        Task DeleteIfExistsAsync(string? path);
    }
}
