namespace TipMolde.Application.DTOs.RelatorioDto.Linhas
{
    public sealed class FichaFopRelatorioLinhaDto
    {
        public DateTime Data { get; set; }
        public string Ocorrencia { get; set; } = string.Empty;
        public string? Correcao { get; set; }
        public string ResponsavelNome { get; set; } = string.Empty;
    }
}
