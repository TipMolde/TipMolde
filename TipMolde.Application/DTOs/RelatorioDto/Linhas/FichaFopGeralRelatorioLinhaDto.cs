namespace TipMolde.Application.DTOs.RelatorioDto.Linhas
{
    /// <summary>
    /// Representa uma linha da FOP geral filtrada por intervalo de datas.
    /// </summary>
    public sealed class FichaFopGeralRelatorioLinhaDto
    {
        public int FichaFopLinha_id { get; set; }
        public int FichaFop_id { get; set; }
        public int EncomendaMolde_id { get; set; }
        public DateTime Data { get; set; }
        public string Ocorrencia { get; set; } = string.Empty;
        public string? Correcao { get; set; }
        public string ResponsavelNome { get; set; } = string.Empty;
        public int? Peca_id { get; set; }
        public string? PecaNumero { get; set; }
        public string? PecaDesignacao { get; set; }
        public int? Molde_id { get; set; }
        public string? MoldeNumero { get; set; }
        public string? MoldeNome { get; set; }
    }
}
