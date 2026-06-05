using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Domain.Entities.Comercio
{
    /// <summary>
    /// Representa a tabela associativa entre Encomenda e Molde.
    /// </summary>
    /// <remarks>
    /// Armazena atributos de negocio da relacao N:M, como quantidade, prioridade e prazo.
    /// </remarks>
    public class EncomendaMolde
    {
        /// <summary>
        /// Identificador interno da associacao Encomenda-Molde.
        /// </summary>
        public int EncomendaMolde_id { get; set; }

        /// <summary>
        /// Quantidade de unidades a produzir para este molde na encomenda.
        /// </summary>
        public int Quantidade { get; set; }

        /// <summary>
        /// Prioridade relativa deste molde dentro da encomenda.
        /// </summary>
        public int Prioridade { get; set; }

        /// <summary>
        /// Estado atual do Molde na associacao Encomenda-Molde.
        /// </summary>
        public EstadoEncomendaMolde Estado { get; set; } = EstadoEncomendaMolde.PENDENTE;

        /// <summary>
        /// Data de entrega prevista para este molde no contexto da encomenda.
        /// </summary>
        public DateTime DataEntregaPrevista { get; set; }

        /// <summary>
        /// Identificador da encomenda associada.
        /// </summary>
        public int Encomenda_id { get; set; }

        /// <summary>
        /// Navegacao para a encomenda associada.
        /// </summary>
        public Encomenda? Encomenda { get; set; }

        /// <summary>
        /// Identificador do molde associado.
        /// </summary>
        public int Molde_id { get; set; }

        /// <summary>
        /// Navegacao para o molde associado.
        /// </summary>
        public Molde? Molde { get; set; }

        /// <summary>
        /// Fichas de producao geradas para esta associacao Encomenda-Molde.
        /// </summary>
        public ICollection<FichaProducao> Fichas { get; set; } = new List<FichaProducao>();
    }
}
