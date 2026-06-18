using Microsoft.EntityFrameworkCore;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Desenho.IProjeto;
using TipMolde.Domain.Entities.Desenho;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementa as operacoes de persistencia da feature Projeto.
    /// </summary>
    public class ProjetoRepository : GenericRepository<Projeto, int>, IProjetoRepository
    {
        /// <summary>
        /// Construtor de ProjetoRepository.
        /// </summary>
        /// <param name="context">Contexto EF Core da aplicacao.</param>
        public ProjetoRepository(ApplicationDbContext context) : base(context) { }

        /// <summary>
        /// Lista projetos associados a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com projetos associados.</returns>
        public async Task<PagedResult<Projeto>> GetByMoldeIdAsync(int moldeId, int page, int pageSize)
        {

            var query = _context.Projetos
                .AsNoTracking()
                .Where(p => p.Molde_id == moldeId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.Projeto_id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Projeto>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Obtem um projeto com as revisoes carregadas.
        /// </summary>
        /// <remarks>
        /// Trade-off: a leitura enriquecida privilegia conveniencia para o detalhe funcional,
        /// enquanto o shape final do payload continua controlado pelo mapping para DTO na camada Application.
        /// </remarks>
        /// <param name="id">Identificador interno do projeto.</param>
        /// <returns>Projeto com revisoes quando encontrado; nulo caso contrario.</returns>
        public Task<Projeto?> GetWithRevisoesAsync(int id) =>
            _context.Projetos
                .AsNoTracking()
                .Include(p => p.Revisoes)
                .FirstOrDefaultAsync(p => p.Projeto_id == id);

        /// <summary>
        /// Obtem o projeto mais recente associado a um molde com revisoes e registos temporais carregados.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>Projeto mais recente quando existe; nulo caso contrario.</returns>
        public Task<Projeto?> GetLatestWithRevisoesAndTempoByMoldeAsync(int moldeId) =>
            _context.Projetos
                .AsNoTracking()
                .Include(p => p.Revisoes)
                .Include(p => p.RegistosTempo)
                .Where(p => p.Molde_id == moldeId)
                .OrderByDescending(p => p.Projeto_id)
                .FirstOrDefaultAsync();
    }
}
