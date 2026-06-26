using Microsoft.EntityFrameworkCore;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IIndustrial;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementa o acesso a dados dos eventos industriais.
    /// </summary>
    public class EventoMaquinaIndustrialRepository : GenericRepository<EventoMaquinaIndustrial, int>, IEventoMaquinaIndustrialRepository
    {
        public EventoMaquinaIndustrialRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Lista eventos ainda pendentes de intervencao do utilizador.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado de eventos pendentes.</returns>
        public async Task<PagedResult<EventoMaquinaIndustrial>> GetPendentesAsync(int page, int pageSize)
        {
            var query = _context.EventosMaquinaIndustrial
                .AsNoTracking()
                .Where(e => e.EstadoResolucao == EstadoResolucaoEventoMaquinaIndustrial.PENDENTE)
                .OrderBy(e => e.OccurredAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<EventoMaquinaIndustrial>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Obtem o ultimo STOPPED pendente de uma sessao.
        /// </summary>
        /// <param name="sessaoId">Identificador da sessao.</param>
        /// <returns>Evento STOPPED pendente; nulo quando nao existe.</returns>
        public Task<EventoMaquinaIndustrial?> GetUltimoStoppedPendenteAsync(int sessaoId)
        {
            return _context.EventosMaquinaIndustrial
                .Where(e =>
                    e.SessaoMaquinaIndustrial_id == sessaoId &&
                    e.EstadoMaquina == "STOPPED" &&
                    e.EstadoResolucao == EstadoResolucaoEventoMaquinaIndustrial.PENDENTE)
                .OrderByDescending(e => e.OccurredAt)
                .FirstOrDefaultAsync();
        }
    }
}
