namespace TipMolde.Application.DTOs.RelatorioDto.Linhas
{
    public sealed class FichaFraRelatorioLinhaDto
    {
        public DateTime Data { get; set; }
        public string Alteracoes { get; set; } = string.Empty;
        public bool Verificado { get; set; }
        public string ResponsavelNome { get; set; } = string.Empty;
    }
}
