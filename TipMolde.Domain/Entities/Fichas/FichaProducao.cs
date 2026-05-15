using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Enums;

namespace TipMolde.Domain.Entities.Fichas
{
    /// <summary>
    /// Representa o cabecalho funcional comum das fichas editaveis de producao.
    /// </summary>
    /// <remarks>
    /// Esta entidade concentra o contexto Encomenda-Molde e o tipo documental.
    /// As linhas manuais continuam persistidas em tabelas dedicadas e sao validadas
    /// pela aplicacao com base no valor de <see cref="Tipo"/>.
    /// </remarks>
    public class FichaProducao
    {
        /// <summary>
        /// Identificador interno da ficha de producao.
        /// </summary>
        public int FichaProducao_id { get; set; }

        /// <summary>
        /// Tipo documental da ficha a gerar e preencher.
        /// </summary>
        public TipoFicha Tipo { get; set; }

        /// <summary>
        /// Data de criacao do cabecalho da ficha.
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Identificador da associacao Encomenda-Molde a que a ficha pertence.
        /// </summary>
        public int EncomendaMolde_id { get; set; }

        /// <summary>
        /// Navegacao para o contexto comercial da ficha.
        /// </summary>
        public EncomendaMolde? EncomendaMolde { get; set; }

        /// <summary>
        /// Versoes documentais geradas ou anexadas para esta ficha.
        /// </summary>
        public ICollection<FichaDocumento> Documentos { get; set; } = new List<FichaDocumento>();
    }
}
