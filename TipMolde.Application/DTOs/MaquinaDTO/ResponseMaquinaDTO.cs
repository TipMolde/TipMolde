using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.MaquinaDto
{
    /// <summary>
    /// Representa a resposta publica da feature Maquina.
    /// </summary>
    public class ResponseMaquinaDto
    {
        /// <summary>
        /// Identificador interno da maquina.
        /// </summary>
        public int Maquina_id { get; set; }

        /// <summary>
        /// Numero fisico da maquina.
        /// </summary>
        public int Numero { get; set; }

        /// <summary>
        /// Nome/modelo do equipamento.
        /// </summary>
        public string? NomeModelo { get; set; }

        /// <summary>
        /// Endereco IP da maquina.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Protocolo de comunicacao da maquina.
        /// </summary>
        public string? ProtocoloComunicacao { get; set; }

        /// <summary>
        /// Estado operacional atual da maquina.
        /// </summary>
        public EstadoMaquina Estado { get; set; }

        /// <summary>
        /// Identificador da fase dedicada desta maquina.
        /// </summary>
        public int FaseDedicada_id { get; set; }

        /// <summary>
        /// Nome de apresentacao da fase dedicada desta maquina.
        /// </summary>
        public string FaseDedicadaNome { get; set; } = string.Empty;
    }
}
