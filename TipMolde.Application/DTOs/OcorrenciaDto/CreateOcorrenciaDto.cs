using System.ComponentModel.DataAnnotations;

namespace TipMolde.Application.Dtos.OcorrenciaDto
{
    /// <summary>
    /// Contrato de entrada para registar uma ocorrencia ou correcao independente da producao.
    /// </summary>
    public class CreateOcorrenciaDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int EncomendaMolde_id { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Peca_id { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Responsavel_id { get; set; }

        [Required]
        [MaxLength(4000)]
        public required string Ocorrencia { get; set; }

        [MaxLength(4000)]
        public string? Correcao { get; set; }
    }
}
