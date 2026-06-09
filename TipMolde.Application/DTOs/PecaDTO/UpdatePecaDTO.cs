using System.ComponentModel.DataAnnotations;

namespace TipMolde.Application.Dtos.PecaDto
{
    /// <summary>
    /// Representa os dados de atualizacao parcial de uma peca.
    /// </summary>
    /// <remarks>
    /// Campos omitidos devem preservar o valor atual da entidade.
    /// </remarks>
    public class UpdatePecaDto
    {
        [MaxLength(50)]
        public string? NumeroPeca { get; set; }

        [MinLength(2)]
        [MaxLength(100)]
        public string? Designacao { get; set; }

        [Range(1, 9999)]
        public int? Prioridade { get; set; }

        [Range(1, int.MaxValue)]
        public int? Quantidade { get; set; }

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

        public bool? MaterialRecebido { get; set; }

        [Range(1, int.MaxValue)]
        public int? ProximaFase_id { get; set; }
    }
}
