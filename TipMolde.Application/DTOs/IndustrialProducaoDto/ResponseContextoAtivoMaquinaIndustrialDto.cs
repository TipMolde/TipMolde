namespace TipMolde.Application.Dtos.IndustrialProducaoDto
{
    /// <summary>
    /// Resumo do contexto industrial atualmente ativo numa maquina.
    /// </summary>
    public class ResponseContextoAtivoMaquinaIndustrialDto
    {
        public int SessaoMaquinaIndustrial_id { get; set; }
        public int Maquina_id { get; set; }
        public int Operador_id { get; set; }
        public string OperadorNome { get; set; } = string.Empty;
        public int Peca_id { get; set; }
        public string NumeroPeca { get; set; } = string.Empty;
        public string DesignacaoPeca { get; set; } = string.Empty;
        public int Molde_id { get; set; }
        public string NumeroMolde { get; set; } = string.Empty;
        public int Fase_id { get; set; }
        public string FaseNome { get; set; } = string.Empty;
        public int? ProximaFasePlaneada_id { get; set; }
        public string ProximaFasePlaneadaNome { get; set; } = string.Empty;
        public string EstadoSessao { get; set; } = string.Empty;
        public string UltimoEstadoMaquina { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
