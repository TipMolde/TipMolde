using System.ComponentModel.DataAnnotations;

namespace TipMolde.Application.Dtos.PecaDto
{
    /// <summary>
    /// Representa os dados de criacao de uma peca associada a um molde.
    /// </summary>
    /// <remarks>
    /// Este DTO pertence ao contrato de entrada da API e nao deve expor a entidade
    /// de dominio diretamente.
    /// </remarks>
    public class CreatePecaDto
    {
        [MaxLength(50)]
        public string? NumeroPeca { get; set; }

        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string Designacao { get; set; } = string.Empty;

        [Range(1, 9999)]
        public int Prioridade { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantidade { get; set; } = 1;

        [MaxLength(200)]
        public string? Referencia { get; set; }

        [MaxLength(100)]
        public string? MaterialDesignacao { get; set; }

        [MaxLength(100)]
        public string? TratamentoTermico { get; set; }

        [MaxLength(50)]
        public string? Massa { get; set; }

        [MaxLength(100)]
        public string? Observacao { get; set; }

        public bool MaterialRecebido { get; set; }

        [Range(1, int.MaxValue)]
        public int? ProximaFase_id { get; set; }

        [Range(1, int.MaxValue)]
        public int Molde_id { get; set; }
    }
}
