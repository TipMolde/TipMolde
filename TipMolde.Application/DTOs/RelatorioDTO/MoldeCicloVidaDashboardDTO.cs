namespace TipMolde.Application.Dtos.RelatorioDto
{
    public class MoldeCicloVidaDashboardDto
    {
        public int MoldeId { get; set; }
        public string NumeroMolde { get; set; } = string.Empty;
        public int TotalPecas { get; set; }

        public int Maquinacao { get; set; }
        public int Erosao { get; set; }
        public int Montagem { get; set; }
        public int EmEspera { get; set; }

        public int EmTrabalho { get; set; }      // PREPARACAO/EM_CURSO/PAUSADO
        public int Concluidas { get; set; }      // regra: concluiu MONTAGEM
        public int MaterialPendente { get; set; } // MaterialRecebido == false

        public decimal PercentagemConclusao { get; set; }
    }
}
