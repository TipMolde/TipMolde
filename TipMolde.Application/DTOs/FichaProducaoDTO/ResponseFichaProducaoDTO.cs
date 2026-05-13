using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.FichaProducaoDto
{
    /// <summary>
    /// Representa a resposta resumida de uma ficha de producao.
    /// </summary>
    /// <remarks>
    /// Este DTO e usado nas listagens paginadas para evitar carregar o detalhe completo
    /// dos registos manuais quando o utilizador so precisa do cabecalho.
    /// </remarks>
    public class ResponseFichaProducaoDto
    {
        public int FichaProducao_id { get; set; }
        public TipoFicha Tipo { get; set; }
        public DateTime DataCriacao { get; set; }
        public int EncomendaMolde_id { get; set; }
    }
}
