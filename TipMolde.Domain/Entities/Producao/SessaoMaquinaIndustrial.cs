using TipMolde.Domain.Enums;

namespace TipMolde.Domain.Entities.Producao
{
    /// <summary>
    /// Mantem o contexto operacional atual ou historico de uma maquina industrial.
    /// </summary>
    /// <remarks>
    /// A sessao evita perguntar repetidamente operador/peca enquanto a maquina alterna
    /// entre RUNNING e STOPPED no mesmo trabalho.
    /// </remarks>
    public class SessaoMaquinaIndustrial
    {
        public int SessaoMaquinaIndustrial_id { get; set; }

        public int Maquina_id { get; set; }
        public Maquina? Maquina { get; set; }

        public int Operador_id { get; set; }
        public User? Operador { get; set; }

        public int Peca_id { get; set; }
        public Peca? Peca { get; set; }

        public int Fase_id { get; set; }
        public FasesProducao? Fase { get; set; }

        public int? RegistoProducaoInicio_id { get; set; }
        public RegistosProducao? RegistoProducaoInicio { get; set; }

        public int? RegistoProducaoConclusao_id { get; set; }
        public RegistosProducao? RegistoProducaoConclusao { get; set; }

        public EstadoSessaoMaquinaIndustrial EstadoSessao { get; set; } = EstadoSessaoMaquinaIndustrial.ATIVA;

        /// <summary>
        /// Ultimo estado tecnico recebido da maquina, por exemplo RUNNING, STOPPED ou IDLE.
        /// </summary>
        public string? UltimoEstadoMaquina { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime LastSeenAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
