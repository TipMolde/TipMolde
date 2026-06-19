namespace TipMolde.Application.Dtos.PecaDto
{
    /// <summary>
    /// Representa um item da fila de trabalho das pecas da producao.
    /// </summary>
    public class ResponsePecaFilaTrabalhoDto
    {
        public int PecaId { get; set; }
        public int MoldeId { get; set; }
        public int PrioridadeMolde { get; set; }
        public int PrioridadePeca { get; set; }
        public int Quantidade { get; set; }
        public string NumeroMolde { get; set; } = string.Empty;
        public string NomeMolde { get; set; } = string.Empty;
        public string NumeroEncomendaCliente { get; set; } = string.Empty;
        public string NomeCliente { get; set; } = string.Empty;
        public string Designacao { get; set; } = string.Empty;
        public string NumeroPeca { get; set; } = string.Empty;
        public DateTime DataEntregaPrevista { get; set; }
        public string UltimoEstadoGlobal { get; set; } = string.Empty;
        public string UltimaFaseGlobal { get; set; } = string.Empty;
        public int? ProximaFaseId { get; set; }
        public string ProximaFaseNome { get; set; } = string.Empty;
        public string FaseTrabalho { get; set; } = string.Empty;
        public string ProximoPasso { get; set; } = string.Empty;
    }
}
