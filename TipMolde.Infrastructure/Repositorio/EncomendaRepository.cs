using Microsoft.EntityFrameworkCore;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomenda;
using TipMolde.Domain.Entities.Comercio;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementacao EF Core para persistencia de encomendas.
    /// </summary>
    public class EncomendaRepository : GenericRepository<Encomenda, int>, IEncomendaRepository
    {
        /// <summary>
        /// Construtor de EncomendaRepository.
        /// </summary>
        /// <param name="context">Contexto de base de dados.</param>
        public EncomendaRepository(ApplicationDbContext context) : base(context) { }

        /// <summary>
        /// Lista encomendas por estado.
        /// </summary>
        public async Task<PagedResult<Encomenda>> GetByEstadoAsync(EstadoEncomenda estado, int page, int pageSize)
        {

            var query = _context.Encomendas
                .AsNoTracking()
                .Where(e => e.Estado == estado)
                .OrderByDescending(e => e.DataRegisto);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Encomenda>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Lista encomendas por concluir (nao concluidas e nao canceladas).
        /// </summary>
        public async Task<PagedResult<Encomenda>> GetEncomendasPorConcluirAsync(int page, int pageSize)
        {

            var query = _context.Encomendas
                .AsNoTracking()
                .Where(e => e.Estado != EstadoEncomenda.CONCLUIDA
                         && e.Estado != EstadoEncomenda.CANCELADA)
                .OrderByDescending(e => e.DataRegisto);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Encomenda>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Lista encomendas operacionais para a pagina de encomendas, incluindo cliente e excluindo apenas concluidas.
        /// </summary>
        public async Task<PagedResult<Encomenda>> GetEncomendasEmProducaoAsync(int page, int pageSize)
        {
            var query = _context.Encomendas
                .AsNoTracking()
                .Include(e => e.Cliente)
                .Where(e => e.Estado != EstadoEncomenda.CONCLUIDA)
                .OrderByDescending(e => e.DataRegisto);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Encomenda>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Obtem encomenda pelo numero do cliente.
        /// </summary>
        public async Task<Encomenda?> GetByNumeroEncomendaClienteAsync(string numero)
        {
            var numeroNormalizado = numero.Trim();

            return await _context.Encomendas
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.NumeroEncomendaCliente == numeroNormalizado);
        }

        /// <summary>
        /// Verifica duplicidade do numero da encomenda.
        /// </summary>
        public async Task<bool> ExistsNumeroEncomendaClienteAsync(string numero, int? excludeEncomendaId = null)
        {
            var numeroNormalizado = numero.Trim();

            return await _context.Encomendas
                .AsNoTracking()
                .AnyAsync(e =>
                    e.NumeroEncomendaCliente == numeroNormalizado &&
                    (!excludeEncomendaId.HasValue || e.Encomenda_id != excludeEncomendaId.Value));
        }

        /// <summary>
        /// Obtem encomenda por ID com moldes associados.
        /// </summary>
        public Task<Encomenda?> GetWithMoldesAsync(int id) =>
            _context.Encomendas
                .Include(e => e.EncomendasMoldes)
                    .ThenInclude(em => em.Molde)
                .FirstOrDefaultAsync(e => e.Encomenda_id == id);
    }
}
