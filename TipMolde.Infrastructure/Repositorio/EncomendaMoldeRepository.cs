using Microsoft.EntityFrameworkCore;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementa persistencia especifica da relacao Encomenda-Molde.
    /// </summary>
    /// <remarks>
    /// Fornece consultas paginadas por FK e validacao de unicidade do par Encomenda-Molde.
    /// </remarks>
    public class EncomendaMoldeRepository : GenericRepository<EncomendaMolde, int>, IEncomendaMoldeRepository
    {
        /// <summary>
        /// Construtor de EncomendaMoldeRepository.
        /// </summary>
        /// <param name="context">Contexto EF Core da aplicacao.</param>
        public EncomendaMoldeRepository(ApplicationDbContext context) : base(context) { }

        /// <summary>
        /// Lista associacoes por encomenda com paginacao.
        /// </summary>
        /// <remarks>
        /// Inclui dados de Molde para simplificar consumo no endpoint.
        /// </remarks>
        /// <param name="encomendaId">Identificador da encomenda para filtro.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com associacoes da encomenda.</returns>
        public async Task<PagedResult<EncomendaMolde>> GetByEncomendaIdAsync(
            int encomendaId,
            int page,
            int pageSize)
        {
            var query = _context.EncomendasMoldes
                .AsNoTracking()
                .Include(em => em.Molde)
                .Where(em => em.Encomenda_id == encomendaId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(em => em.EncomendaMolde_id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<EncomendaMolde>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Lista associacoes por molde com paginacao.
        /// </summary>
        /// <remarks>
        /// Inclui dados de Encomenda para simplificar consumo no endpoint.
        /// </remarks>
        /// <param name="moldeId">Identificador do molde para filtro.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com associacoes do molde.</returns>
        public async Task<PagedResult<EncomendaMolde>> GetByMoldeIdAsync(
            int moldeId,
            int page,
            int pageSize)
        {
            var query = _context.EncomendasMoldes
                .AsNoTracking()
                .Include(em => em.Encomenda)
                .Where(em => em.Molde_id == moldeId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(em => em.EncomendaMolde_id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<EncomendaMolde>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Verifica se existe associacao duplicada para o par Encomenda-Molde.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda.</param>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="excludeEncomendaMoldeId">ID opcional a excluir em cenarios de update.</param>
        /// <returns>True quando existe duplicado; caso contrario, false.</returns>
        public Task<bool> ExistsAssociationAsync(
            int encomendaId,
            int moldeId,
            int? excludeEncomendaMoldeId = null)
        {
            return _context.EncomendasMoldes.AnyAsync(
                em => em.Encomenda_id == encomendaId &&
                      em.Molde_id == moldeId &&
                      (!excludeEncomendaMoldeId.HasValue || em.EncomendaMolde_id != excludeEncomendaMoldeId.Value));
        }

        /// <summary>
        /// Obtem a base completa da fila global de moldes, incluindo encomenda, cliente e molde.
        /// </summary>
        /// <returns>Colecao materializada de associacoes pertencentes a encomendas em aberto.</returns>
        public async Task<List<EncomendaMolde>> GetFilaGlobalAbertosAsync()
        {
            return await _context.EncomendasMoldes
                .Include(em => em.Encomenda)
                    .ThenInclude(e => e.Cliente)
                .Include(em => em.Molde)
                .Where(em => em.Encomenda != null &&
                             em.Encomenda!.Estado != EstadoEncomenda.CONCLUIDA &&
                             em.Encomenda!.Estado != EstadoEncomenda.CANCELADA)
                .ToListAsync();
        }

        /// <summary>
        /// Obtem a fila global de moldes com paginacao ja aplicada na base de dados.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com as associacoes ordenadas pela prioridade global.</returns>
        public async Task<PagedResult<EncomendaMolde>> GetFilaGlobalAsync(int page, int pageSize)
        {
            var query = _context.EncomendasMoldes
                .AsNoTracking()
                .Include(em => em.Encomenda)
                    .ThenInclude(e => e.Cliente)
                .Include(em => em.Molde)
                .Where(em => em.Encomenda != null &&
                             em.Encomenda!.Estado != EstadoEncomenda.CONCLUIDA &&
                             em.Encomenda!.Estado != EstadoEncomenda.CANCELADA)
                .OrderBy(em => em.Prioridade)
                .ThenBy(em => em.DataEntregaPrevista.Date)
                .ThenBy(em => em.EncomendaMolde_id);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<EncomendaMolde>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Persiste em lote as associacoes afetadas pelo rebalanceamento da fila global.
        /// </summary>
        /// <param name="entities">Associacoes com prioridades recalculadas.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        public async Task UpdateRangeAsync(IEnumerable<EncomendaMolde> entities)
        {
            _context.EncomendasMoldes.UpdateRange(entities);
            await _context.SaveChangesAsync();
        }
    }
}
