using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Dtos.OcorrenciaDto;

namespace TipMolde.Application.Interface.Ocorrencias
{
    /// <summary>
    /// Define os casos de uso para registar ocorrencias e correcao de forma independente da producao.
    /// </summary>
    public interface IOcorrenciasService
    {
        Task<ResponseFichaFopLinhaDto> CreateAsync(CreateOcorrenciaDto dto);
    }
}
