namespace TipMolde.Domain.Entities.Producao
{
    /// <summary>
    /// Representa um componente individual de um molde.
    /// </summary>
    /// <remarks>
    /// A entidade guarda os dados funcionais da peca usados no planeamento e
    /// no acompanhamento da producao dentro do respetivo molde.
    /// </remarks>
    public class Peca
    {
        public int Peca_id { get; set; }

        /// <summary>
        /// Identificador funcional da peca no desenho/lista de materiais do molde.
        /// </summary>
        public string? NumeroPeca { get; set; }

        /// <summary>
        /// Designacao funcional unica da peca dentro do mesmo molde.
        /// </summary>
        public required string Designacao { get; set; }

        /// <summary>
        /// Prioridade relativa da peca no planeamento de producao.
        /// </summary>
        public int Prioridade { get; set; }

        /// <summary>
        /// Quantidade total consolidada da peca dentro do molde.
        /// </summary>
        public int Quantidade { get; set; } = 1;

        /// <summary>
        /// Referencia tecnica ou dimensional usada na lista de materiais.
        /// </summary>
        public string? Referencia { get; set; }

        /// <summary>
        /// Designacao do material previsto para fabricar a peca.
        /// </summary>
        public string? MaterialDesignacao { get; set; }

        /// <summary>
        /// Tratamento termico previsto para a peca, quando aplicavel.
        /// </summary>
        public string? TratamentoTermico { get; set; }

        /// <summary>
        /// Massa registada na lista de materiais.
        /// </summary>
        public string? Massa { get; set; }

        /// <summary>
        /// Observacao adicional importada da lista de materiais.
        /// </summary>
        public string? Observacao { get; set; }

        /// <summary>
        /// Indica se o material necessario para a peca ja foi rececionado.
        /// </summary>
        public bool MaterialRecebido { get; set; }

        /// <summary>
        /// Identificador da proxima fase planeada para a peca.
        /// </summary>
        /// <remarks>
        /// Este campo representa o proximo posto de trabalho esperado e pode ser
        /// ajustado manualmente para suportar reentrada em fases anteriores.
        /// </remarks>
        public int? ProximaFase_id { get; set; }

        /// <summary>
        /// Navegacao para a proxima fase planeada da peca.
        /// </summary>
        public FasesProducao? ProximaFase { get; set; }

        public int Molde_id { get; set; }
        public Molde? Molde { get; set; }
    }
}
