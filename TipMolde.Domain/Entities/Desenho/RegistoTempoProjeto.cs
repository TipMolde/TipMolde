using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;

namespace TipMolde.Domain.Entities.Desenho
{
    /// <summary>
    /// Representa um evento pontual no historico de tempo de um projeto.
    /// </summary>
    /// <remarks>
    /// A entidade guarda apenas estado persistente para auditoria do fluxo temporal,
    /// incluindo o autor, o projeto e a peca do molde associada ao evento.
    /// </remarks>
    public class RegistoTempoProjeto
    {
        public int Registo_Tempo_Projeto_id { get; set; }
        public EstadoTempoProjeto Estado_tempo { get; set; } = EstadoTempoProjeto.INICIADO;
        public DateTime Data_hora { get; set; }

        public int Projeto_id { get; set; }
        public Projeto? Projeto { get; set; }

        public int Autor_id { get; set; }
        public User? Autor { get; set; }
    }
}
