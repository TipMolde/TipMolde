using TipMolde.Domain.Enums;

namespace TipMolde.Application.Dtos.RegistoTempoProjetoDto
{
    /// <summary>
    /// Representa a resposta publica da feature RegistoTempoProjeto.
    /// </summary>
    public class ResponseRegistoTempoProjetoDto
    {
        /// <summary>
        /// Identificador interno do registo de tempo.
        /// </summary>
        public int Registo_Tempo_Projeto_id { get; set; }

        /// <summary>
        /// Estado temporal persistido neste evento do historico.
        /// </summary>
        public EstadoTempoProjeto Estado_tempo { get; set; }

        /// <summary>
        /// Timestamp UTC gerado no servidor para o evento.
        /// </summary>
        public DateTime Data_hora { get; set; }

        /// <summary>
        /// Identificador do projeto associado.
        /// </summary>
        public int Projeto_id { get; set; }

        /// <summary>
        /// Identificador do autor do registo.
        /// </summary>
        public int Autor_id { get; set; }
    }
}
