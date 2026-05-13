namespace TipMolde.Domain.Entities.Fichas.Linhas
{
    /// <summary>
    /// Representa uma linha manual da ficha FOP.
    /// </summary>
    public class FichaFopLinha
    {
        public int FichaFopLinha_id { get; set; }
        public int FichaProducao_id { get; set; }
        public FichaProducao? FichaProducao { get; set; }
        public DateTime Data { get; set; }
        public string Ocorrencia { get; set; } = string.Empty;
        public string? Correcao { get; set; }
        public int Responsavel_id { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
