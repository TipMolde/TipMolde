using TipMolde.Application.Dtos.EncomendaMoldeDto;

namespace TipMolde.Application.Interface.Comercio.IEncomendaMolde;

/// <summary>
/// Define os casos de uso responsaveis pela prioridade global operacional dos moldes.
/// </summary>
public interface IPrioridadeGlobalMoldeService
{
    /// <summary>
    /// Recalcula as prioridades globais dos moldes pertencentes a encomendas em aberto.
    /// </summary>
    /// <returns>Task de conclusao da operacao.</returns>
    Task RecalcularAsync();

    /// <summary>
    /// Obtem a fila global de moldes com paginacao.
    /// </summary>
    /// <param name="page">Pagina atual (>= 1).</param>
    /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
    /// <returns>Resultado paginado com os moldes ordenados pela prioridade global.</returns>
    Task<PagedResult<FilaGlobalMoldeItemDto>> GetFilaGlobalAsync(int page = 1, int pageSize = 10);
}
