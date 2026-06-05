using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.EncomendaMoldeDto
{
    public class ResponseEncomendaMoldeDto
    {
        public int EncomendaMolde_id { get; set; }
        public int Encomenda_id { get; set; }
        public int Molde_id { get; set; }
        public int Quantidade { get; set; }
        public int Prioridade { get; set; }
        public EstadoEncomendaMolde Estado { get; set; }
        public DateTime DataEntregaPrevista { get; set; }

        public string? NumeroEncomendaCliente { get; set; }
        public string? NumeroMolde { get; set; }
    }
}
