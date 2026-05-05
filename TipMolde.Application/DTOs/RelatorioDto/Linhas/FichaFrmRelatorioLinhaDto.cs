namespace TipMolde.Application.DTOs.RelatorioDto.Linhas
{
    public sealed class FichaFrmRelatorioLinhaDto
    {
        public DateTime Data { get; set; }
        public string Defeito { get; set; } = string.Empty;
        public string? Pormenor { get; set; }
        public bool Verificado { get; set; }
        public string ResponsavelNome { get; set; } = string.Empty;
    }
}