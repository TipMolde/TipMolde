using System.ComponentModel.DataAnnotations;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.RegistoTempoProjetoDto
{
    /// <summary>
    /// Representa o pedido de criacao de um registo de tempo de projeto.
    /// </summary>
    /// <remarks>
    /// O Estado_tempo e anulavel para que a validacao do model binder consiga
    /// distinguir entre campo omisso e valor explicitamente enviado pelo cliente.
    /// </remarks>
    public class CreateRegistoTempoProjetoDto
    {
        /// <summary>
        /// Novo estado temporal a registar no historico.
        /// </summary>
        [Required]
        public EstadoTempoProjeto? Estado_tempo { get; set; }

        /// <summary>
        /// Identificador do projeto a que o registo pertence.
        /// </summary>
        [Required, Range(1, int.MaxValue)]
        public int Projeto_id { get; set; }

        /// <summary>
        /// Identificador do autor que executou a operacao.
        /// </summary>
        [Required, Range(1, int.MaxValue)]
        public int Autor_id { get; set; }
    }
}
