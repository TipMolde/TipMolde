using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TipMolde.Application.Dtos.RevisaoDto
{
    /// <summary>
    /// Representa o payload multipart da resposta do cliente a uma revisao.
    /// </summary>
    public class UpdateRespostaRevisaoFormDto : IValidatableObject
    {
        [Required]
        public bool? Aprovado { get; set; }

        [MaxLength(4000)]
        public string? FeedbackTexto { get; set; }

        public IFormFile? Anexo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Aprovado == true && Anexo is not null)
            {
                yield return new ValidationResult(
                    "Quando a revisao e aprovada, nao deve ser enviado anexo.",
                    new[] { nameof(Anexo) });
            }

            if (Aprovado == false
                && string.IsNullOrWhiteSpace(FeedbackTexto)
                && Anexo is null)
            {
                yield return new ValidationResult(
                    "Quando a revisao e rejeitada, deve ser enviado FeedbackTexto ou um anexo.",
                    new[] { nameof(FeedbackTexto), nameof(Anexo) });
            }
        }
    }
}
