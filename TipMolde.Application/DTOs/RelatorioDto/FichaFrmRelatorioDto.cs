using TipMolde.Application.DTOs.RelatorioDto.Linhas;

namespace TipMolde.Application.Dtos.RelatorioDto
{
    public sealed class FichaFrmRelatorioDto
    {
        public FichaRelatorioBaseDto Base { get; set; } = new();
        public IList<FichaFrmRelatorioLinhaDto> Linhas { get; set; } = new List<FichaFrmRelatorioLinhaDto>();
    }
}