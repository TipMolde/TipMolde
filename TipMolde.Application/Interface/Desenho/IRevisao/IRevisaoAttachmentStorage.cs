namespace TipMolde.Application.Interface.Desenho.IRevisao
{
    /// <summary>
    /// Abstrai o armazenamento fisico dos anexos associados a revisoes.
    /// </summary>
    public interface IRevisaoAttachmentStorage
    {
        /// <summary>
        /// Persiste o conteudo binario de um anexo na area de armazenamento dedicada a revisoes.
        /// </summary>
        /// <param name="revisaoId">Identificador da revisao dona do anexo.</param>
        /// <param name="fileName">Nome final do ficheiro a persistir.</param>
        /// <param name="content">Conteudo binario do ficheiro.</param>
        /// <returns>Caminho relativo guardado na base de dados.</returns>
        Task<string> SaveAsync(int revisaoId, string fileName, byte[] content);

        /// <summary>
        /// Remove um ficheiro guardado quando ele existe.
        /// </summary>
        /// <param name="path">Caminho relativo ou absoluto do ficheiro.</param>
        /// <returns>Task concluida quando a operacao termina.</returns>
        Task DeleteIfExistsAsync(string? path);
    }
}
