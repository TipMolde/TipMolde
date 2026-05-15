using Microsoft.EntityFrameworkCore;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Domain.Entities.Fichas;
using TipMolde.Domain.Entities.Fichas.Linhas;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementa queries e operacoes de persistencia das fichas editaveis de producao.
    /// </summary>
    public class FichaProducaoRepository : GenericRepository<FichaProducao, int>, IFichaProducaoRepository
    {
        /// <summary>
        /// Construtor de FichaProducaoRepository.
        /// </summary>
        /// <param name="context">Contexto EF Core usado para queries e persistencia das fichas editaveis.</param>
        public FichaProducaoRepository(ApplicationDbContext context) : base(context) { }

        public async Task<PagedResult<FichaProducao>> GetByEncomendaMoldeIdAsync(int encomendaMoldeId, int page, int pageSize)
        {
            var query = _context.FichasProducao
                .AsNoTracking()
                .Where(f => f.EncomendaMolde_id == encomendaMoldeId)
                .OrderByDescending(f => f.DataCriacao);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<FichaProducao>(items, totalCount, page, pageSize);
        }

        public async Task<PagedResult<FichaProducao>> GetByMoldeIdAsync(int moldeId, int page, int pageSize)
        {
            var query = _context.FichasProducao
                .AsNoTracking()
                .Where(f => f.EncomendaMolde != null && f.EncomendaMolde.Molde_id == moldeId)
                .OrderByDescending(f => f.DataCriacao);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<FichaProducao>(items, totalCount, page, pageSize);
        }

        public Task<FichaProducao?> GetByIdDetalheAsync(int id) =>
            _context.FichasProducao
                .AsNoTracking()
                .Include(f => f.EncomendaMolde!)
                    .ThenInclude(em => em.Encomenda!)
                        .ThenInclude(e => e.Cliente)
                .Include(f => f.EncomendaMolde!)
                    .ThenInclude(em => em.Molde)
                .Include(f => f.Documentos)
                .FirstOrDefaultAsync(f => f.FichaProducao_id == id);

        public async Task<PagedResult<FichaFrmLinha>> GetLinhasFrmByFichaIdAsync(int fichaId, int page, int pageSize)
        {
            var query = _context.FichasFrmLinhas.AsNoTracking().Where(x => x.FichaProducao_id == fichaId).OrderBy(x => x.Data).ThenBy(x => x.FichaFrmLinha_id);
            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<FichaFrmLinha>(items, totalCount, page, pageSize);
        }

        public Task<FichaFrmLinha?> GetLinhaFrmByIdAsync(int fichaId, int linhaId) =>
            _context.FichasFrmLinhas.FirstOrDefaultAsync(x => x.FichaProducao_id == fichaId && x.FichaFrmLinha_id == linhaId);

        public async Task<FichaFrmLinha> AddLinhaFrmAsync(FichaFrmLinha linha)
        {
            await _context.FichasFrmLinhas.AddAsync(linha);
            await _context.SaveChangesAsync();
            return linha;
        }

        public async Task UpdateLinhaFrmAsync(FichaFrmLinha linha)
        {
            _context.FichasFrmLinhas.Update(linha);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<FichaFraLinha>> GetLinhasFraByFichaIdAsync(int fichaId, int page, int pageSize)
        {
            var query = _context.FichasFraLinhas.
                AsNoTracking()
                .Where(x => x.FichaProducao_id == fichaId)
                .OrderBy(x => x.Data)
                .ThenBy(x => x.FichaFraLinha_id);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<FichaFraLinha>(items, totalCount, page, pageSize);
        }

        public Task<FichaFraLinha?> GetLinhaFraByIdAsync(int fichaId, int linhaId) =>
            _context.FichasFraLinhas.FirstOrDefaultAsync(x => x.FichaProducao_id == fichaId && x.FichaFraLinha_id == linhaId);

        public async Task<FichaFraLinha> AddLinhaFraAsync(FichaFraLinha linha)
        {
            await _context.FichasFraLinhas.AddAsync(linha);
            await _context.SaveChangesAsync();
            return linha;
        }

        public async Task UpdateLinhaFraAsync(FichaFraLinha linha)
        {
            _context.FichasFraLinhas.Update(linha);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<FichaFopLinha>> GetLinhasFopByFichaIdAsync(int fichaId, int page, int pageSize)
        {
            var query = _context.FichasFopLinhas
                .AsNoTracking()
                .Where(x => x.FichaProducao_id == fichaId)
                .OrderBy(x => x.Data)
                .ThenBy(x => x.FichaFopLinha_id);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<FichaFopLinha>(items, totalCount, page, pageSize);
        }

        public Task<FichaFopLinha?> GetLinhaFopByIdAsync(int fichaId, int linhaId) =>
            _context.FichasFopLinhas.FirstOrDefaultAsync(x => x.FichaProducao_id == fichaId && x.FichaFopLinha_id == linhaId);

        public async Task<FichaFopLinha> AddLinhaFopAsync(FichaFopLinha linha)
        {
            await _context.FichasFopLinhas.AddAsync(linha);
            await _context.SaveChangesAsync();
            return linha;
        }

        public async Task UpdateLinhaFopAsync(FichaFopLinha linha)
        {
            _context.FichasFopLinhas.Update(linha);
            await _context.SaveChangesAsync();
        }
    }
}
