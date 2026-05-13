using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.FichaProducaoDto
{
    /// <summary>
    /// Representa o detalhe completo de uma ficha de producao editavel.
    /// </summary>
    /// <remarks>
    /// O DTO agrega cabecalho, contexto comercial e os dados manuais especificos
    /// de FRE, FRM, FRA ou FOP para suportar consulta e edicao na aplicacao.
    /// </remarks>
    public class ResponseFichaProducaoDetalheDto
    {
        public int FichaProducao_id { get; set; }
        public TipoFicha Tipo { get; set; }
        public DateTime DataCriacao { get; set; }
        public int EncomendaMolde_id { get; set; }
        public string? NumeroMolde { get; set; }
        public string? NomeMolde { get; set; }
        public string? NomeCliente { get; set; }
        public string? NumeroEncomendaCliente { get; set; }
        public IList<ResponseFichaFrmLinhaDto> LinhasFrm { get; set; } = new List<ResponseFichaFrmLinhaDto>();
        public IList<ResponseFichaFraLinhaDto> LinhasFra { get; set; } = new List<ResponseFichaFraLinhaDto>();
        public IList<ResponseFichaFopLinhaDto> LinhasFop { get; set; } = new List<ResponseFichaFopLinhaDto>();
    }
}
