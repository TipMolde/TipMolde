using TipMolde.Domain.Enums;

namespace TipMolde.Domain.Entities.Producao
{
    /// <summary>
    /// Representa um equipamento de produção (torno CNC, máquina de erosão, etc.).
    /// Associada a uma fase específica para validação de compatibilidade.
    /// </summary>
    /// <remarks>
    /// O IpAddress permite futura integração via protocolos MTConnect ou OPC UA
    /// conforme previsto no requisito RF-IT-01.
    /// </remarks>
    public class Maquina
    {
        /// <summary>
        /// Identificador único da máquina (PK).
        /// Diferente de Numero para permitir reutilização de números físicos.
        /// </summary>
        public int Maquina_id { get; set; }

        /// <summary>
        /// Número físico da máquina (ex: estampado na máquina).
        /// Indexado para pesquisas rápidas por operadores.
        /// </summary>
        public int Numero { get; set; }

        public required string NomeModelo { get; set; }

        /// <summary>
        /// Endereço IP para integração futura com MES/MTConnect.
        /// Opcional porque nem todas as máquinas têm conectividade.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Protocolo de comunicacao detetado/configurado para a maquina.
        /// Ex: OPC-UA, MTConnect, SODICK.
        /// Opcional enquanto a maquina ainda nao foi integrada.
        /// </summary>
        public string? ProtocoloComunicacao { get; set; }

        /// <summary>
        /// Estado atual da máquina.
        /// Atualizado automaticamente durante registos de produção (RF-PR-02).
        /// </summary>
        public EstadoMaquina Estado { get; set; } = EstadoMaquina.DISPONIVEL;

        /// <summary>
        /// Identificador da fase dedicada a esta maquina.
        /// </summary>
        /// <remarks>
        /// A fase deve existir e nao pode ser removida enquanto houver maquinas a referi-la.
        /// </remarks>
        public int FaseDedicada_id { get; set; }

        /// <summary>
        /// Navegacao para a fase dedicada da maquina.
        /// </summary>
        public FasesProducao? FaseDedicada { get; set; }
    }
}
