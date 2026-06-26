using Microsoft.EntityFrameworkCore;
using TipMolde.Application.Interface.Producao.IIndustrial;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementa o acesso a dados das sessoes industriais.
    /// </summary>
    public class SessaoMaquinaIndustrialRepository : GenericRepository<SessaoMaquinaIndustrial, int>, ISessaoMaquinaIndustrialRepository
    {
        public SessaoMaquinaIndustrialRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Obtem a sessao ainda aberta de uma maquina.
        /// </summary>
        /// <param name="maquinaId">Identificador da maquina.</param>
        /// <returns>Sessao ativa/aguardando confirmacao; nulo quando nao existe.</returns>
        public Task<SessaoMaquinaIndustrial?> GetAbertaPorMaquinaAsync(int maquinaId)
        {
            return _context.SessoesMaquinaIndustrial
                .Where(s =>
                    s.Maquina_id == maquinaId &&
                    s.EstadoSessao != EstadoSessaoMaquinaIndustrial.FECHADA)
                .OrderByDescending(s => s.LastSeenAt)
                .FirstOrDefaultAsync();
        }
    }
}
