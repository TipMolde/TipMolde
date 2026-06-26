using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using TipMolde.Application.Exceptions;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Domain.Entities.Producao;
using TipMolde.Domain.Enums;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Infrastructure.Repositorio
{
    /// <summary>
    /// Implementa o acesso a dados da feature Maquina.
    /// </summary>
    /// <remarks>
    /// Traduz conflitos tecnicos de unicidade em conflitos de negocio
    /// e centraliza consultas especificas do agregado.
    /// </remarks>
    public class MaquinaRepository : GenericRepository<Maquina, int>, IMaquinaRepository
    {
        /// <summary>
        /// Construtor de MaquinaRepository.
        /// </summary>
        /// <param name="context">Contexto EF da aplicacao.</param>
        public MaquinaRepository(ApplicationDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Lista maquinas com fase dedicada carregada para suportar pesquisa e apresentacao.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com maquinas ordenadas por numero.</returns>
        public override async Task<PagedResult<Maquina>> GetAllAsync(int page, int pageSize)
        {
            var query = BuildQuery()
                .OrderBy(m => m.Numero);

            return await BuildPagedResultAsync(query, page, pageSize);
        }

        /// <summary>
        /// Obtem uma maquina pelo identificador interno.
        /// </summary>
        /// <param name="id">Identificador da maquina.</param>
        /// <returns>Entidade encontrada; nulo caso nao exista.</returns>
        public async Task<Maquina?> GetByIdUnicoAsync(int id)
        {
            var maquina = await BuildQuery()
                .FirstOrDefaultAsync(m => m.Maquina_id == id);

            if (maquina != null)
                await LoadFasesDedicadasAsync([maquina]);

            return maquina;
        }

        /// <summary>
        /// Obtem uma maquina pelo endereco IP configurado.
        /// </summary>
        /// <param name="ipAddress">Endereco IP da maquina.</param>
        /// <returns>Entidade encontrada; nulo caso nao exista.</returns>
        public async Task<Maquina?> GetByIpAddressAsync(string ipAddress)
        {
            var normalizedIp = ipAddress.Trim();

            if (string.IsNullOrWhiteSpace(normalizedIp))
                return null;

            var maquina = await BuildQuery()
                .FirstOrDefaultAsync(m => m.IpAddress == normalizedIp);

            if (maquina != null)
                await LoadFasesDedicadasAsync([maquina]);

            return maquina;
        }

        /// <summary>
        /// Lista maquinas por estado com paginacao.
        /// </summary>
        /// <param name="estado">Estado operacional a filtrar.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com maquinas filtradas.</returns>
        public async Task<PagedResult<Maquina>> GetByEstadoAsync(EstadoMaquina estado, int page, int pageSize)
        {
            var query = BuildQuery()
                .Where(m => m.Estado == estado)
                .OrderBy(m => m.Numero);

            return await BuildPagedResultAsync(query, page, pageSize);
        }

        /// <summary>
        /// Pesquisa maquinas por termo livre em numero, modelo, IP, protocolo, estado ou fase dedicada.
        /// </summary>
        /// <param name="searchTerm">Termo de pesquisa a aplicar.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com as maquinas correspondentes ao termo.</returns>
        public async Task<PagedResult<Maquina>> SearchAsync(string searchTerm, int page, int pageSize)
        {
            var normalizedTerm = NormalizeSearchTerm(searchTerm);
            if (string.IsNullOrWhiteSpace(normalizedTerm))
                return new PagedResult<Maquina>(Array.Empty<Maquina>(), 0, page, pageSize);

            var hasNumero = int.TryParse(searchTerm.Trim(), out var numero);
            var hasEstado = TryParseEnumFromSearchTerm(normalizedTerm, out EstadoMaquina estado);
            var hasFase = TryParseEnumFromSearchTerm(normalizedTerm, out NomeFases fase);
            var faseIds = hasFase
                ? await _context.Fases_Producao
                    .AsNoTracking()
                    .Where(f => f.Nome == fase)
                    .Select(f => f.Fases_producao_id)
                    .ToArrayAsync()
                : Array.Empty<int>();

            var query = BuildQuery().Where(m =>
                m.NomeModelo.Contains(searchTerm) ||
                (m.IpAddress != null && m.IpAddress.Contains(searchTerm)) ||
                (m.ProtocoloComunicacao != null && m.ProtocoloComunicacao.Contains(searchTerm)) ||
                (hasNumero && m.Numero == numero) ||
                (hasEstado && m.Estado == estado) ||
                (hasFase && faseIds.Contains(m.FaseDedicada_id)));

            query = query.OrderBy(m => m.Numero);

            return await BuildPagedResultAsync(query, page, pageSize);
        }

        /// <summary>
        /// Verifica se ja existe uma maquina com o numero fisico informado.
        /// </summary>
        /// <param name="numero">Numero fisico a validar.</param>
        /// <param name="excludeMaquinaId">Identificador a excluir na validacao, usado em updates.</param>
        /// <returns>True quando o numero ja estiver em uso.</returns>
        public Task<bool> ExistsNumeroAsync(int numero, int? excludeMaquinaId = null)
        {
            return _context.Maquinas
                .AsNoTracking()
                .AnyAsync(m =>
                    m.Numero == numero &&
                    (!excludeMaquinaId.HasValue || m.Maquina_id != excludeMaquinaId.Value));
        }

        /// <summary>
        /// Verifica se a fase dedicada existe.
        /// </summary>
        /// <param name="faseDedicadaId">Identificador da fase a validar.</param>
        /// <returns>True quando a fase existir.</returns>
        public Task<bool> ExistsFaseDedicadaAsync(int faseDedicadaId)
        {
            return _context.Fases_Producao
                .AsNoTracking()
                .AnyAsync(f => f.Fases_producao_id == faseDedicadaId);
        }

        /// <summary>
        /// Persiste uma nova maquina e traduz conflito de indice unico para conflito de negocio.
        /// </summary>
        /// <param name="maquina">Entidade a criar.</param>
        /// <returns>Entidade criada.</returns>
        public async Task<Maquina> CreateAsync(Maquina maquina)
        {
            try
            {
                await _context.Maquinas.AddAsync(maquina);
                await _context.SaveChangesAsync();
                return maquina;
            }
            catch (DbUpdateException ex) when (IsUniqueNumeroViolation(ex))
            {
                throw new BusinessConflictException($"Ja existe uma maquina com o numero '{maquina.Numero}'.");
            }
        }

        /// <summary>
        /// Atualiza uma maquina existente e traduz conflito de indice unico para conflito de negocio.
        /// </summary>
        /// <param name="maquina">Entidade a atualizar.</param>
        /// <returns>Task de conclusao da atualizacao.</returns>
        public async Task UpdateExistingAsync(Maquina maquina)
        {
            try
            {
                _context.Maquinas.Update(maquina);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueNumeroViolation(ex))
            {
                throw new BusinessConflictException($"Ja existe uma maquina com o numero '{maquina.Numero}'.");
            }
        }

        /// <summary>
        /// Avalia se a excecao recebida corresponde a violacao do indice unico de numero.
        /// </summary>
        /// <param name="ex">Excecao original do Entity Framework.</param>
        /// <returns>True quando a excecao representar duplicado funcional no numero.</returns>
        private static bool IsUniqueNumeroViolation(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;

            return message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                || (message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                    && message.Contains("Numero", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Cria a query base com tracking desligado e a fase dedicada carregada.
        /// </summary>
        /// <returns>Query EF pronta para filtragem e ordenacao.</returns>
        private IQueryable<Maquina> BuildQuery()
        {
            return _context.Maquinas
                .AsNoTracking();
        }

        /// <summary>
        /// Executa a paginacao de uma query ja ordenada.
        /// </summary>
        /// <param name="query">Query pronta a paginar.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>Resultado paginado com itens e metadados.</returns>
        private async Task<PagedResult<Maquina>> BuildPagedResultAsync(IQueryable<Maquina> query, int page, int pageSize)
        {
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (items.Count > 0)
                await LoadFasesDedicadasAsync(items);

            return new PagedResult<Maquina>(items, totalCount, page, pageSize);
        }

        /// <summary>
        /// Carrega as fases dedicadas das maquinas devolvidas para manter a informacao de apresentacao disponivel.
        /// </summary>
        /// <param name="items">Maquinas materializadas a enriquecer.</param>
        /// <returns>Task assincrona concluida apos associacao das fases existentes.</returns>
        private async Task LoadFasesDedicadasAsync(IReadOnlyCollection<Maquina> items)
        {
            var faseIds = items
                .Select(m => m.FaseDedicada_id)
                .Distinct()
                .ToArray();

            if (faseIds.Length == 0)
                return;

            var fases = await _context.Fases_Producao
                .AsNoTracking()
                .Where(f => faseIds.Contains(f.Fases_producao_id))
                .ToDictionaryAsync(f => f.Fases_producao_id);

            foreach (var maquina in items)
            {
                if (fases.TryGetValue(maquina.FaseDedicada_id, out var fase))
                    maquina.FaseDedicada = fase;
            }
        }

        /// <summary>
        /// Normaliza um termo para comparacoes funcionais independentes de espacos, acentos e pontuacao.
        /// </summary>
        /// <param name="value">Termo original.</param>
        /// <returns>Termo normalizado em minusculas sem marcas diacriticas.</returns>
        private static string NormalizeSearchTerm(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(character))
                    builder.Append(char.ToLowerInvariant(character));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Tenta mapear o termo normalizado para um valor de enum.
        /// </summary>
        /// <typeparam name="TEnum">Tipo do enum a interpretar.</typeparam>
        /// <param name="normalizedTerm">Termo normalizado.</param>
        /// <param name="value">Enum encontrado quando a interpretacao e bem sucedida.</param>
        /// <returns>True quando existe correspondencia funcional.</returns>
        private static bool TryParseEnumFromSearchTerm<TEnum>(string normalizedTerm, out TEnum value)
            where TEnum : struct, Enum
        {
            foreach (var candidate in Enum.GetValues<TEnum>())
            {
                var normalizedCandidate = NormalizeSearchTerm(candidate.ToString());
                if (normalizedCandidate == normalizedTerm)
                {
                    value = candidate;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
