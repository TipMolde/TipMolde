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
        /// Lista associacoes Encomenda-Molde cujas encomendas estao confirmadas.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado paginado com associacoes aptas para inicio do desenho.</returns>
        public async Task<PagedResult<EncomendaMolde>> GetByEncomendasConfirmadasAsync(int page, int pageSize)
        {
            var query = _context.EncomendasMoldes
                .AsNoTracking()
                .Include(em => em.Encomenda)
                .Include(em => em.Molde)
                .Where(em => em.Encomenda != null && em.Encomenda.Estado == EstadoEncomenda.CONFIRMADA)
                .OrderBy(em => em.DataEntregaPrevista)
                .ThenBy(em => em.Prioridade)
                .ThenBy(em => em.EncomendaMolde_id);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<EncomendaMolde>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Obtem uma associacao por ID com a encomenda associada carregada em tracking.
        /// </summary>
        /// <param name="id">Identificador da associacao.</param>
        /// <returns>Associacao encontrada ou nulo quando nao existe.</returns>
        public Task<EncomendaMolde?> GetByIdWithEncomendaAsync(int id)
        {
            return _context.EncomendasMoldes
                .Include(em => em.Encomenda)
                .FirstOrDefaultAsync(em => em.EncomendaMolde_id == id);
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
        /// Indica se a encomenda ainda tem moldes nao concluidos.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda.</param>
        /// <param name="excludeEncomendaMoldeId">Associacao opcional a ignorar.</param>
        /// <returns>True quando ainda existe pelo menos um molde nao concluido.</returns>
        public Task<bool> HasMoldesNaoConcluidosAsync(int encomendaId, int? excludeEncomendaMoldeId = null)
        {
            return _context.EncomendasMoldes.AnyAsync(
                em => em.Encomenda_id == encomendaId &&
                      em.Estado != EstadoEncomendaMolde.CONCLUIDO &&
                      (!excludeEncomendaMoldeId.HasValue || em.EncomendaMolde_id != excludeEncomendaMoldeId.Value));
        }

        /// <summary>
        /// Indica se todas as pecas do molde ja receberam material.
        /// </summary>
        /// <param name="moldeId">Identificador do molde a validar.</param>
        /// <returns>True quando o molde tem pecas e todas possuem MaterialRecebido = true.</returns>
        public async Task<bool> TodasPecasTemMaterialRecebidoAsync(int moldeId)
        {
            var totalPecas = await _context.Pecas
                .AsNoTracking()
                .CountAsync(p => p.Molde_id == moldeId);

            if (totalPecas == 0)
                return false;

            var totalComMaterialRecebido = await _context.Pecas
                .AsNoTracking()
                .CountAsync(p => p.Molde_id == moldeId && p.MaterialRecebido);

            return totalPecas == totalComMaterialRecebido;
        }

        /// <summary>
        /// Indica se todas as pecas do molde estao concluidas na fase de montagem.
        /// </summary>
        /// <param name="moldeId">Identificador do molde a validar.</param>
        /// <returns>True quando cada peca tem como ultimo registo a fase MONTAGEM em estado CONCLUIDO.</returns>
        public async Task<bool> TodasPecasConcluidasNaMontagemAsync(int moldeId)
        {
            var faseMontagemId = await _context.Fases_Producao
                .AsNoTracking()
                .Where(f => f.Nome == NomeFases.MONTAGEM)
                .Select(f => (int?)f.Fases_producao_id)
                .FirstOrDefaultAsync();

            if (!faseMontagemId.HasValue)
                return false;

            var pecaIds = await _context.Pecas
                .AsNoTracking()
                .Where(p => p.Molde_id == moldeId)
                .Select(p => p.Peca_id)
                .ToListAsync();

            if (pecaIds.Count == 0)
                return false;

            var ultimosRegistosPorPeca = await _context.RegistosProducao
                .AsNoTracking()
                .Where(r => pecaIds.Contains(r.Peca_id))
                .GroupBy(r => r.Peca_id)
                .Select(g => g.OrderByDescending(r => r.Data_hora).First())
                .ToListAsync();

            if (ultimosRegistosPorPeca.Count != pecaIds.Count)
                return false;

            return ultimosRegistosPorPeca.All(r =>
                r.Fase_id == faseMontagemId.Value &&
                r.Estado_producao == EstadoProducao.CONCLUIDO);
        }

        /// <summary>
        /// Obtem o conjunto de estados atuais dos moldes associados a uma encomenda.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda.</param>
        /// <returns>Lista materializada com os estados correntes dos moldes da encomenda.</returns>
        public async Task<List<EstadoEncomendaMolde>> GetEstadosByEncomendaIdAsync(int encomendaId)
        {
            return await _context.EncomendasMoldes
                .AsNoTracking()
                .Where(em => em.Encomenda_id == encomendaId)
                .OrderBy(em => em.EncomendaMolde_id)
                .Select(em => em.Estado)
                .ToListAsync();
        }

        /// <summary>
        /// Obtem a base completa da fila global de moldes, incluindo encomenda, cliente e molde.
        /// </summary>
        /// <returns>Colecao materializada de associacoes pertencentes a encomendas em aberto.</returns>
        public async Task<List<EncomendaMolde>> GetFilaGlobalAbertosAsync()
        {
            return await _context.EncomendasMoldes
                .Include(em => em.Encomenda!)
                    .ThenInclude(e => e.Cliente)
                .Include(em => em.Molde)
                .Where(em => em.Encomenda != null &&
                             em.Estado != EstadoEncomendaMolde.CONCLUIDO &&
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
                .Include(em => em.Encomenda!)
                    .ThenInclude(e => e.Cliente)
                .Include(em => em.Molde)
                .Where(em => em.Encomenda != null &&
                             em.Encomenda!.Estado != EstadoEncomenda.CONCLUIDA &&
                             em.Encomenda!.Estado != EstadoEncomenda.CANCELADA)
                .OrderBy(em => em.Prioridade)
                .ThenBy(em => em.DataEntregaPrevista)
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
