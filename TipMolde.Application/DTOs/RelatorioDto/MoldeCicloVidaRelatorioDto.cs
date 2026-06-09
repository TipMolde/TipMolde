using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.RelatorioDto
{
    public sealed class MoldeCicloVidaRelatorioDto
    {
        public int MoldeId { get; set; }
        public string NumeroMolde { get; set; } = string.Empty;
        public string? NumeroMoldeCliente { get; set; }
        public string? NomeMolde { get; set; }
        public string? DescricaoMolde { get; set; }
        public int NumeroCavidades { get; set; }
        public TipoPedido TipoPedido { get; set; }
        public string? ClienteNome { get; set; }
        public string? NumeroEncomendaCliente { get; set; }
        public string? NumeroProjetoCliente { get; set; }
        public string? NomeResponsavelCliente { get; set; }
        public DateTime? DataRegistoEncomenda { get; set; }
        public DateTime? DataEntregaPrevista { get; set; }
        public int TotalPecas { get; set; }
        public int MaterialPendente { get; set; }
        public int TotalProjetos { get; set; }
        public int TotalRevisoes { get; set; }
        public DateTime? UltimaRevisaoEm { get; set; }
        public int Maquinacao { get; set; }
        public int Erosao { get; set; }
        public int Montagem { get; set; }
        public int EmEspera { get; set; }
        public int EmTrabalho { get; set; }
        public int Concluidas { get; set; }
        public decimal PercentagemConclusao { get; set; }
        public List<MoldeProjetoResumoDto> Projetos { get; set; } = [];
        public List<MoldeFaseResumoDto> Fases { get; set; } = [];
    }
}
