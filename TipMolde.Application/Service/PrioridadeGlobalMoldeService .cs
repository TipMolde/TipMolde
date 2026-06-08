using TipMolde.Application.Dtos.EncomendaMoldeDto;
using TipMolde.Application.Interface;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;

namespace TipMolde.Application.Service;

/// <summary>
/// Implementa o calculo e a leitura da fila global operacional de moldes.
/// </summary>
/// <remarks>
/// A ordenacao segue primeiro a data de entrega prevista e, em caso de empate,
/// a ordem de criacao inferida pelo identificador da associacao Encomenda-Molde.
/// </remarks>
public class PrioridadeGlobalMoldeService : IPrioridadeGlobalMoldeService
{
    private readonly IEncomendaMoldeRepository _repo;

    /// <summary>
    /// Construtor de PrioridadeGlobalMoldeService.
    /// </summary>
    /// <param name="repo">Repositorio da relacao Encomenda-Molde.</param>
    public PrioridadeGlobalMoldeService(IEncomendaMoldeRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Recalcula a prioridade global de todos os moldes pertencentes a encomendas em aberto.
    /// </summary>
    /// <returns>Task de conclusao da operacao.</returns>
    public async Task RecalcularAsync()
    {
        var items = await _repo.GetFilaGlobalAbertosAsync();

        var ordered = items
            .OrderBy(x => x.DataEntregaPrevista)
            .ThenBy(x => x.EncomendaMolde_id)
            .ToList();

        var changed = false;

        for (var i = 0; i < ordered.Count; i++)
        {
            var novaPrioridade = i + 1;
            if (ordered[i].Prioridade == novaPrioridade)
                continue;

            ordered[i].Prioridade = novaPrioridade;
            changed = true;
        }

        if (changed)
            await _repo.UpdateRangeAsync(ordered);
    }

    /// <summary>
    /// Obtem a fila global de moldes com paginacao.
    /// </summary>
    /// <param name="page">Pagina atual (>= 1).</param>
    /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
    /// <returns>Resultado paginado com os moldes ja ordenados pela prioridade global.</returns>
    public async Task<PagedResult<FilaGlobalMoldeItemDto>> GetFilaGlobalAsync(int page = 1, int pageSize = 10)
    {
        var (normalizedPage, normalizedPageSize) = PaginationDefaults.Normalize(page, pageSize);
        var result = await _repo.GetFilaGlobalAsync(normalizedPage, normalizedPageSize);

        var items = result.Items.Select(x => new FilaGlobalMoldeItemDto
        {
            EncomendaMolde_id = x.EncomendaMolde_id,
            Encomenda_id = x.Encomenda_id,
            Molde_id = x.Molde_id,
            Prioridade = x.Prioridade,
            DataEntregaPrevista = x.DataEntregaPrevista,
            Quantidade = x.Quantidade,
            NumeroEncomendaCliente = x.Encomenda?.NumeroEncomendaCliente,
            NomeCliente = x.Encomenda?.Cliente?.Nome,
            NumeroMolde = x.Molde?.Numero,
            NomeMolde = x.Molde?.Nome,
            EstadoEncomenda = x.Encomenda?.Estado.ToString()
        });

        return new PagedResult<FilaGlobalMoldeItemDto>(
            items,
            result.TotalCount,
            result.CurrentPage,
            result.PageSize);
    }
}
