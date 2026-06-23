using System.ComponentModel.DataAnnotations;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.RegistoProducaoDto
{
    /// <summary>
    /// Contrato de entrada para criar um registo de producao.
    /// </summary>
    /// <remarks>
    /// Campos nullable distinguem omissao no payload de valores funcionais validos,
    /// evitando defaults tecnicos em regras criticas de producao.
    /// </remarks>
    public class CreateRegistosProducaoDto
    {
        /// <summary>
        /// Identificador da peca associada ao registo.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Peca_id { get; set; }

        /// <summary>
        /// Identificador da fase de producao onde ocorre a transicao.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Fase_id { get; set; }

        /// <summary>
        /// Identificador opcional da maquina associada ao registo.
        /// </summary>
        /// <remarks>
        /// Opcional para suportar trabalho manual. Em PAUSADO e CONCLUIDO pode ser
        /// inferido pelo ultimo registo da peca na fase quando exista.
        /// </remarks>
        [Range(1, int.MaxValue)]
        public int? Maquina_id { get; set; }

        /// <summary>
        /// Identificador do operador responsavel pela transicao.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Operador_id { get; set; }

        /// <summary>
        /// Estado de producao solicitado para a nova transicao.
        /// </summary>
        /// <remarks>
        /// Nullable para obrigar envio explicito do estado e evitar o default do enum.
        /// </remarks>
        [Required]
        public EstadoProducao? Estado_producao { get; set; }

        /// <summary>
        /// Texto livre da ocorrencia a converter numa linha FOP.
        /// </summary>
        [MaxLength(4000)]
        public string? Ocorrencia { get; set; }

        /// <summary>
        /// Texto livre da correcao associada a ocorrencia, quando aplicavel.
        /// </summary>
        [MaxLength(4000)]
        public string? Correcao { get; set; }

        /// <summary>
        /// Identificador da associacao Encomenda-Molde a usar na FOP.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? EncomendaMolde_id { get; set; }

        /// <summary>
        /// Identificador opcional da proxima fase planeada para a peca.
        /// </summary>
        /// <remarks>
        /// Quando informado, o planeamento da peca e atualizado em conjunto com o
        /// registo de producao para refletir o proximo passo escolhido pelo utilizador.
        /// </remarks>
        [Range(1, int.MaxValue)]
        public int? ProximaFase_id { get; set; }
    }
}
