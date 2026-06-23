using Microsoft.EntityFrameworkCore;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementa a persistencia do agregado Molde.
    /// </summary>
    /// <remarks>
    /// As leituras de negocio carregam as especificacoes tecnicas para evitar Dtos de resposta incompletos.
    /// </remarks>
    public class MoldeRepository : GenericRepository<Molde, int>, IMoldeRepository
    {
        /// <summary>
        /// Construtor de MoldeRepository.
        /// </summary>
        /// <param name="context">Contexto EF Core da aplicacao.</param>
        public MoldeRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Lista moldes com especificacoes tecnicas de forma paginada.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado de moldes.</returns>
        public new async Task<PagedResult<Molde>> GetAllAsync(int page, int pageSize)
        {

            var query = _context.Moldes
                .AsNoTracking()
                .Include(m => m.Especificacoes);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(m => m.Molde_id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Molde>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Obtem um molde por identificador para leitura.
        /// </summary>
        /// <param name="id">Identificador do molde.</param>
        /// <returns>Molde encontrado com especificacoes; nulo caso nao exista.</returns>
        public new async Task<Molde?> GetByIdAsync(int id)
        {
            return await _context.Moldes
                .AsNoTracking()
                .Include(m => m.Especificacoes)
                .FirstOrDefaultAsync(m => m.Molde_id == id);
        }

        /// <summary>
        /// Obtem um molde pelo numero funcional.
        /// </summary>
        /// <param name="numero">Numero funcional do molde.</param>
        /// <returns>Molde encontrado com especificacoes; nulo caso nao exista.</returns>
        public Task<Molde?> GetByNumeroAsync(string numero)
        {
            return _context.Moldes
                .AsNoTracking()
                .Include(m => m.Especificacoes)
                .FirstOrDefaultAsync(m => m.Numero == numero);
        }

        /// <summary>
        /// Lista moldes associados a uma encomenda.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Colecao de moldes associados com especificacoes carregadas.</returns>
        public async Task<PagedResult<Molde>> GetByEncomendaIdAsync(int encomendaId, int page, int pageSize)
        {

            var query = _context.Moldes
                .AsNoTracking()
                .Include(m => m.Especificacoes)
                .Where(m => m.EncomendasMoldes.Any(em => em.Encomenda_id == encomendaId));


            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(m => m.Molde_id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Molde>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Lista moldes que possuem pelo menos uma associacao Encomenda-Molde.
        /// </summary>
        /// <param name="searchTerm">Termo opcional para filtrar numero, nome ou numero do cliente.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com moldes associados a encomendas.</returns>
        public async Task<PagedResult<Molde>> GetComEncomendaAsync(string? searchTerm, int page, int pageSize)
        {
            var query = _context.Moldes
                .AsNoTracking()
                .Include(m => m.Especificacoes)
                .Where(m => m.EncomendasMoldes.Any());

            var normalizedSearchTerm = string.IsNullOrWhiteSpace(searchTerm)
                ? null
                : searchTerm.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
                query = ApplySearchFilter(query, normalizedSearchTerm);

            query = query
                .OrderBy(m => m.Numero)
                .ThenBy(m => m.Nome)
                .ThenBy(m => m.Molde_id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Molde>(items, totalCount, page, pageSize);
        }


        /// <summary>
        /// Persiste molde e especificacoes tecnicas na mesma transacao.
        /// </summary>
        /// <param name="molde">Entidade principal do agregado.</param>
        /// <param name="specs">Especificacoes tecnicas do molde.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        public async Task AddMoldeWithSpecsAsync(Molde molde, EspecificacoesTecnicas specs)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            molde.Especificacoes = null;
            specs.Molde = null;

            await _context.Moldes.AddAsync(molde);
            await _context.SaveChangesAsync();

            specs.Molde_id = molde.Molde_id;
            await _context.EspecificacoesTecnicas.AddAsync(specs);

            await _context.SaveChangesAsync();
            molde.Especificacoes = specs;
            specs.Molde = molde;
            await tx.CommitAsync();
        }

        /// <summary>
        /// Aplica a pesquisa textual aos campos funcionais expostos ao frontend.
        /// </summary>
        /// <param name="query">Consulta base de moldes.</param>
        /// <param name="searchTerm">Termo normalizado de pesquisa.</param>
        /// <returns>Consulta filtrada.</returns>
        private static IQueryable<Molde> ApplySearchFilter(IQueryable<Molde> query, string searchTerm)
        {
            var normalizedTerm = searchTerm.ToLower();

            return query.Where(m =>
                (m.Numero != null && m.Numero.ToLower().Contains(normalizedTerm)) ||
                (m.Nome != null && m.Nome.ToLower().Contains(normalizedTerm)) ||
                (m.NumeroMoldeCliente != null && m.NumeroMoldeCliente.ToLower().Contains(normalizedTerm)));
        }
    }
}
