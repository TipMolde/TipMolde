using System.ComponentModel.DataAnnotations;

namespace TipMolde.Application.Dtos.FichaProducaoDto
{
    /// <summary>
    /// Representa os dados de criacao ou atualizacao de uma linha da ficha FOP.
    /// </summary>
    public class CreateFichaFopLinhaDto
    {
        [Required]
        public DateTime Data { get; set; }

        [Required, MaxLength(4000)]
        public required string Ocorrencia { get; set; }

        [MaxLength(4000)]
        public string? Correcao { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Responsavel_id { get; set; }

        [Range(1, int.MaxValue)]
        public int? Peca_id { get; set; }

        [Range(1, int.MaxValue)]
        public int? Molde_id { get; set; }
    }
}
