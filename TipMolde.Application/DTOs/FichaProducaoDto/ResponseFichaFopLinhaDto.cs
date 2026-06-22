namespace TipMolde.Application.Dtos.FichaProducaoDto
{
    /// <summary>
    /// Representa uma linha publica da ficha FOP.
    /// </summary>
    public class ResponseFichaFopLinhaDto
    {
        public int FichaFopLinha_id { get; set; }
        public int FichaFop_id { get; set; }
        public DateTime Data { get; set; }
        public string Ocorrencia { get; set; } = string.Empty;
        public string? Correcao { get; set; }
        public int Responsavel_id { get; set; }
        public int? Peca_id { get; set; }
        public int? Molde_id { get; set; }
        public DateTime CriadoEm { get; set; }
    }
}
