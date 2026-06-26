using System.ComponentModel.DataAnnotations;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.MaquinaDto
{
    /// <summary>
    /// Contrato de entrada para atualizacao parcial de uma maquina.
    /// </summary>
    /// <remarks>
    /// Campos omitidos devem preservar o valor atual, incluindo o estado operacional.
    /// </remarks>
    public class UpdateMaquinaDto
    {
        /// <summary>
        /// Novo numero fisico da maquina.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? Numero { get; set; }

        /// <summary>
        /// Novo nome/modelo do equipamento.
        /// </summary>
        [MinLength(2)]
        [MaxLength(100)]
        public string? NomeModelo { get; set; }

        /// <summary>
        /// Novo endereco IP opcional do equipamento.
        /// </summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Novo protocolo de comunicacao opcional da maquina.
        /// </summary>
        [MaxLength(30)]
        public string? ProtocoloComunicacao { get; set; }

        /// <summary>
        /// Novo estado operacional da maquina.
        /// </summary>
        public EstadoMaquina? Estado { get; set; }

        /// <summary>
        /// Nova fase de producao dedicada a esta maquina.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? FaseDedicada_id { get; set; }
    }
}
