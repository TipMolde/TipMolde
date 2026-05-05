using Microsoft.EntityFrameworkCore;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Domain.Entities.Comercio;
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
        /// Persiste molde, especificacoes tecnicas e associacao EncomendaMolde na mesma transacao.
        /// </summary>
        /// <param name="molde">Entidade principal do agregado.</param>
        /// <param name="specs">Especificacoes tecnicas do molde.</param>
        /// <param name="link">Associacao inicial entre encomenda e molde.</param>
        /// <returns>Task de conclusao da operacao.</returns>
        public async Task AddMoldeWithSpecsAndLinkAsync(Molde molde, EspecificacoesTecnicas specs, EncomendaMolde link)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            await _context.Moldes.AddAsync(molde);
            await _context.SaveChangesAsync();

            specs.Molde_id = molde.Molde_id;
            await _context.EspecificacoesTecnicas.AddAsync(specs);

            link.Molde_id = molde.Molde_id;
            await _context.EncomendasMoldes.AddAsync(link);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
    }
}
