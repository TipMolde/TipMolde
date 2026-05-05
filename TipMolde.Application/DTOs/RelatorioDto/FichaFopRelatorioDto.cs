using TipMolde.Application.DTOs.RelatorioDto.Linhas;

namespace TipMolde.Application.Dtos.RelatorioDto
{
    public sealed class FichaFopRelatorioDto
    {
        public FichaRelatorioBaseDto Base { get; set; } = new();
        public IList<FichaFopRelatorioLinhaDto> Linhas { get; set; } = new List<FichaFopRelatorioLinhaDto>();
    }
}
