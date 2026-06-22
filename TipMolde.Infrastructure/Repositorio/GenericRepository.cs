using Microsoft.EntityFrameworkCore;
using TipMolde.Application.Interface;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementa operacoes genericas de persistencia para entidades EF Core.
    /// </summary>
    /// <remarks>
    /// Este repositorio executa acesso a dados basico. As regras funcionais,
    /// como limites de paginacao, devem chegar resolvidas pela camada de aplicacao.
    /// </remarks>
    public class GenericRepository<T, TKey> : IGenericRepository<T, TKey> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _db;

        /// <summary>
        /// Construtor de GenericRepository.
        /// </summary>
        /// <param name="context">Contexto EF Core usado para aceder ao conjunto da entidade.</param>
        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _db = _context.Set<T>();
        }

        /// <summary>
        /// Lista entidades com paginacao usando parametros ja normalizados pela camada de aplicacao.
        /// </summary>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado paginado com entidades sem tracking.</returns>
        public virtual async Task<PagedResult<T>> GetAllAsync(int page, int pageSize)
        {
            var query = _db.AsNoTracking();

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Obtem uma entidade pelo identificador.
        /// </summary>
        /// <param name="id">Identificador da entidade.</param>
        /// <returns>Entidade encontrada ou nulo quando nao existe.</returns>
        public async Task<T?> GetByIdAsync(TKey id) => await _db.FindAsync(id);

        /// <summary>
        /// Adiciona uma nova entidade ao contexto e persiste a alteracao.
        /// </summary>
        /// <param name="entity">Entidade a adicionar.</param>
        /// <returns>Entidade adicionada apos persistencia.</returns>
        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Atualiza uma entidade existente e persiste a alteracao.
        /// </summary>
        /// <param name="entity">Entidade com os dados a persistir.</param>
        /// <returns>Task assincrona concluida apos persistencia.</returns>
        public async Task UpdateAsync(T entity)
        {
            _db.Update(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Remove uma entidade pelo identificador quando ela existe.
        /// </summary>
        /// <param name="id">Identificador da entidade a remover.</param>
        /// <returns>Task assincrona concluida apos remocao ou quando a entidade nao existe.</returns>
        public async Task DeleteAsync(TKey id)
        {
            var entity = await _db.FindAsync(id);
            if (entity is null) return;
            _db.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
