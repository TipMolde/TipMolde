using Microsoft.EntityFrameworkCore;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementa operacoes de persistencia especificas para a entidade Peca.
    /// </summary>
    public class PecaRepository : GenericRepository<Peca, int>, IPecaRepository
    {
        /// <summary>
        /// Construtor de PecaRepository.
        /// </summary>
        /// <param name="context">Contexto EF Core da aplicacao.</param>
        public PecaRepository(ApplicationDbContext context) : base(context)
        {
        }

        public new async Task<PagedResult<Peca>> GetAllAsync(int page, int pageSize)
        {
            var query = _context.Pecas
                .AsNoTracking()
                .Include(p => p.ProximaFase);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Peca_id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Peca>(items, totalCount, page, pageSize);
        }

        public new Task<Peca?> GetByIdAsync(int id)
        {
            return _context.Pecas
                .AsNoTracking()
                .Include(p => p.ProximaFase)
                .FirstOrDefaultAsync(p => p.Peca_id == id);
        }

        /// <summary>
        /// Lista pecas associadas a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com pecas pertencentes ao molde informado.</returns>
        public async Task<PagedResult<Peca>> GetByMoldeIdAsync(int moldeId, int page, int pageSize)
        {

            var query = _context.Pecas
                .AsNoTracking()
                .Include(p => p.ProximaFase)
                .Where(p => p.Molde_id == moldeId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Peca_id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Peca>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Obtem todas as pecas associadas a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>Colecao completa de pecas do molde informado.</returns>
        public async Task<IReadOnlyList<Peca>> GetAllByMoldeIdAsync(int moldeId)
        {
            return await _context.Pecas
                .AsNoTracking()
                .Include(p => p.ProximaFase)
                .Where(p => p.Molde_id == moldeId)
                .OrderBy(p => p.Peca_id)
                .ToListAsync();
        }

        /// <summary>
        /// Obtem uma peca pela designacao dentro de um molde.
        /// </summary>
        /// <param name="designacao">Designacao funcional da peca.</param>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>Peca encontrada ou nulo quando nao existe correspondencia.</returns>
        public Task<Peca?> GetByDesignacaoAsync(string designacao, int moldeId)
        {
            return _context.Pecas
                .AsNoTracking()
                .Include(p => p.ProximaFase)
                .FirstOrDefaultAsync(p => p.Designacao == designacao && p.Molde_id == moldeId);
        }

        /// <summary>
        /// Obtem uma peca pelo numero funcional dentro de um molde.
        /// </summary>
        /// <param name="numeroPeca">Numero funcional da peca.</param>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>Peca encontrada ou nulo quando nao existe correspondencia.</returns>
        public Task<Peca?> GetByNumeroPecaAsync(string numeroPeca, int moldeId)
        {
            return _context.Pecas
                .AsNoTracking()
                .Include(p => p.ProximaFase)
                .FirstOrDefaultAsync(p => p.NumeroPeca == numeroPeca && p.Molde_id == moldeId);
        }

        /// <summary>
        /// Obtem todas as pecas correspondentes aos identificadores informados.
        /// </summary>
        /// <param name="ids">Colecao de identificadores a pesquisar.</param>
        /// <returns>Colecao de pecas encontradas para os ids informados.</returns>
        public async Task<IReadOnlyList<Peca>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();

            return await _context.Pecas
                .AsNoTracking()
                .Include(p => p.ProximaFase)
                .Where(p => idList.Contains(p.Peca_id))
                .OrderBy(p => p.Peca_id)
                .ToListAsync();
        }
    }
}
