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

        /// <summary>
        /// Obtem a sessao aberta de uma maquina com os dados necessarios para apresentacao na UI.
        /// </summary>
        public Task<SessaoMaquinaIndustrial?> GetAbertaComDetalhePorMaquinaAsync(int maquinaId)
        {
            return _context.SessoesMaquinaIndustrial
                .Include(s => s.Operador)
                .Include(s => s.Peca)
                    .ThenInclude(p => p!.Molde)
                .Include(s => s.Peca)
                    .ThenInclude(p => p!.ProximaFase)
                .Include(s => s.Fase)
                .Where(s =>
                    s.Maquina_id == maquinaId &&
                    s.EstadoSessao != EstadoSessaoMaquinaIndustrial.FECHADA)
                .OrderByDescending(s => s.LastSeenAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Obtem as pecas que ainda tem uma sessao industrial aberta.
        /// </summary>
        public async Task<HashSet<int>> GetPecaIdsComSessaoAbertaAsync(IEnumerable<int> pecaIds)
        {
            var idList = pecaIds
                .Distinct()
                .ToList();

            if (idList.Count == 0)
                return [];

            var result = await _context.SessoesMaquinaIndustrial
                .AsNoTracking()
                .Where(s =>
                    idList.Contains(s.Peca_id) &&
                    s.EstadoSessao != EstadoSessaoMaquinaIndustrial.FECHADA)
                .Select(s => s.Peca_id)
                .Distinct()
                .ToListAsync();

            return result.ToHashSet();
        }
    }
}
