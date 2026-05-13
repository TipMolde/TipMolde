namespace TipMolde.Domain.Entities.Fichas.Linhas
{
    /// <summary>
    /// Representa uma linha manual da ficha FRA.
    /// </summary>
    public class FichaFraLinha
    {
        public int FichaFraLinha_id { get; set; }
        public int FichaProducao_id { get; set; }
        public FichaProducao? FichaProducao { get; set; }
        public DateTime Data { get; set; }
        public string Alteracoes { get; set; } = string.Empty;
        public bool Verificado { get; set; }
        public int Responsavel_id { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
