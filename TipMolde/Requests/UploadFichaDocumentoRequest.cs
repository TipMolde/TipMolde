using System.ComponentModel.DataAnnotations;

namespace TipMolde.API.Requests
{
    /// <summary>
    /// Representa o request HTTP multipart usado para submeter um documento de ficha.
    /// </summary>
    public class UploadFichaDocumentoRequest
    {
        /// <summary>
        /// Ficheiro submetido pelo utilizador autenticado.
        /// </summary>
        [Required]
        public IFormFile File { get; set; } = default!;
    }
}
