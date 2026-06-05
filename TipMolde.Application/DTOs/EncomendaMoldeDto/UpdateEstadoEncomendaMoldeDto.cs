using System.ComponentModel.DataAnnotations;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.EncomendaMoldeDto
{
    public class UpdateEstadoEncomendaMoldeDto
    {
        [Required]
        public required EstadoEncomendaMolde Estado { get; set; }
    }
}
