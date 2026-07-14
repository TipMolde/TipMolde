using System.ComponentModel.DataAnnotations;

namespace TipMolde.Application.Dtos.IndustrialProducaoDto
{
    /// <summary>
    /// Resposta do utilizador ao pedido de confirmacao de paragem de uma maquina.
    /// </summary>
    public class ConfirmarParagemIndustrialDto
    {
        [Required]
        public bool TrabalhoConcluido { get; set; }

        public int? ProximaFase_id { get; set; }
    }
}
