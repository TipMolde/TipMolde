namespace TipMolde.Application.Dtos.EncomendaMoldeDto;

/// <summary>
/// Representa um item da fila global operacional de moldes.
/// </summary>
public class FilaGlobalMoldeItemDto
{
    public int EncomendaMolde_id { get; set; }
    public int Encomenda_id { get; set; }
    public int Molde_id { get; set; }
    public int Prioridade { get; set; }
    public DateTime DataEntregaPrevista { get; set; }
    public int Quantidade { get; set; }
    public string? NumeroEncomendaCliente { get; set; }
    public string? NomeCliente { get; set; }
    public string? NumeroMolde { get; set; }
    public string? NomeMolde { get; set; }
    public string? EstadoEncomenda { get; set; }
}
