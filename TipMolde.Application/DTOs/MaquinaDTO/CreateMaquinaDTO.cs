using System.ComponentModel.DataAnnotations;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.MaquinaDto
{
    /// <summary>
    /// Contrato de entrada para criacao de uma maquina.
    /// </summary>
    /// <remarks>
    /// O payload tem de transportar todos os atributos operacionais minimos
    /// para que a maquina fique rastreavel e utilizavel na producao.
    /// </remarks>
    public class CreateMaquinaDto
    {
        /// <summary>
        /// Identificador interno da maquina.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Maquina_id { get; set; }

        /// <summary>
        /// Numero fisico visivel na maquina.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Numero { get; set; }

        /// <summary>
        /// Nome/modelo do equipamento.
        /// </summary>
        [Required]
        [MinLength(2)]
        [MaxLength(100)]
        public string NomeModelo { get; set; } = string.Empty;

        /// <summary>
        /// Endereco IP opcional para integracao futura.
        /// </summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Protocolo de comunicacao opcional da maquina.
        /// </summary>
        [MaxLength(30)]
        public string? ProtocoloComunicacao { get; set; }

        /// <summary>
        /// Estado operacional inicial da maquina.
        /// </summary>
        [Required]
        public EstadoMaquina Estado { get; set; }

        /// <summary>
        /// Identificador da fase de producao dedicada a esta maquina.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int FaseDedicada_id { get; set; }
    }
}
