namespace TipMolde.Domain.Entities.Fichas.Linhas
{
    /// <summary>
    /// Representa uma linha manual da ficha FRM.
    /// </summary>
    public class FichaFrmLinha
    {
        public int FichaFrmLinha_id { get; set; }
        public int FichaProducao_id { get; set; }
        public FichaProducao? FichaProducao { get; set; }
        public DateTime Data { get; set; }
        public string Defeito { get; set; } = string.Empty;
        public string? Pormenor { get; set; }
        public bool Verificado { get; set; }
        public int Responsavel_id { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
