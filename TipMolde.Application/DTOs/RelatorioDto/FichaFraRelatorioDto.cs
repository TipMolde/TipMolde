using TipMolde.Application.DTOs.RelatorioDto.Linhas;

namespace TipMolde.Application.Dtos.RelatorioDto
{
    public sealed class FichaFraRelatorioDto
    {
        public FichaRelatorioBaseDto Base { get; set; } = new();
        public IList<FichaFraRelatorioLinhaDto> Linhas { get; set; } = new List<FichaFraRelatorioLinhaDto>();
    }
}
