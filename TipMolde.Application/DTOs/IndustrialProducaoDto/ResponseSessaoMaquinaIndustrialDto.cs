using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.IndustrialProducaoDto
{
    /// <summary>
    /// Resposta publica do contexto operacional ativo ou historico de uma maquina.
    /// </summary>
    public class ResponseSessaoMaquinaIndustrialDto
    {
        public int SessaoMaquinaIndustrial_id { get; set; }
        public int Maquina_id { get; set; }
        public int Operador_id { get; set; }
        public int Peca_id { get; set; }
        public int Fase_id { get; set; }
        public int? RegistoProducaoInicio_id { get; set; }
        public int? RegistoProducaoConclusao_id { get; set; }
        public EstadoSessaoMaquinaIndustrial EstadoSessao { get; set; }
        public string? UltimoEstadoMaquina { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
