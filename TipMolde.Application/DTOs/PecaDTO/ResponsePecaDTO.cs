namespace TipMolde.Application.Dtos.PecaDto
{
    /// <summary>
    /// Representa a resposta publica da feature Peca.
    /// </summary>
    public class ResponsePecaDto
    {
        public int PecaId { get; set; }
        public string? NumeroPeca { get; set; }
        public string? Designacao { get; set; }
        public int Prioridade { get; set; }
        public int Quantidade { get; set; }
        public string? Referencia { get; set; }
        public string? MaterialDesignacao { get; set; }
        public string? TratamentoTermico { get; set; }
        public string? Massa { get; set; }
        public string? Observacao { get; set; }
        public bool MaterialRecebido { get; set; }
        public int? ProximaFase_id { get; set; }
        public string? ProximaFaseNome { get; set; }
        public int Molde_id { get; set; }
    }
}
