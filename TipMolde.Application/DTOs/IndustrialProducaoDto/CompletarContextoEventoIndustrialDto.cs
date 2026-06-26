using System.ComponentModel.DataAnnotations;

namespace TipMolde.Application.Dtos.IndustrialProducaoDto
{
    /// <summary>
    /// Dados fornecidos pelo utilizador quando a maquina envia RUNNING sem contexto suficiente.
    /// </summary>
    public class CompletarContextoEventoIndustrialDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int Operador_id { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Peca_id { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Fase_id { get; set; }
    }
}
