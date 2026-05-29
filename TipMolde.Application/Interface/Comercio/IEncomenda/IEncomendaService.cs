using TipMolde.Application.Dtos.EncomendaDto;
using TipMolde.Domain.Enums;

namespace TipMolde.Application.Interface.Comercio.IEncomenda
{
    /// <summary>
    /// Define os casos de uso da feature Encomenda.
    /// </summary>
    public interface IEncomendaService
    {
        /// <summary>Lista encomendas com paginacao.</summary>
        Task<PagedResult<ResponseEncomendaDto>> GetAllAsync(int page = 1, int pageSize = 10);

        /// <summary>Obtem encomenda por ID.</summary>
        Task<ResponseEncomendaDto?> GetByIdAsync(int id);

        /// <summary>Obtem encomenda com moldes associados.</summary>
        Task<ResponseEncomendaDto?> GetEncomendaWithMoldesAsync(int id);

        /// <summary>Lista encomendas por estado.</summary>
        Task<PagedResult<ResponseEncomendaDto>> GetByEstadoAsync(EstadoEncomenda estado, int page = 1, int pageSize = 10);

        /// <summary>Lista encomendas por concluir.</summary>
        Task<PagedResult<ResponseEncomendaDto>> GetEncomendasPorConcluirAsync(int page = 1, int pageSize = 10);

        /// <summary>Lista encomendas em producao para a UI operacional.</summary>
        Task<PagedResult<ResponseEncomendaDto>> GetEncomendasEmProducaoAsync(int page = 1, int pageSize = 10);

        /// <summary>Pesquisa encomendas em producao pelo numero da encomenda do cliente.</summary>
        Task<PagedResult<ResponseEncomendaDto>> SearchEncomendasEmProducaoAsync(string searchTerm, int page = 1, int pageSize = 10);

        /// <summary>Obtem encomenda por numero do cliente.</summary>
        Task<ResponseEncomendaDto?> GetByNumeroEncomendaClienteAsync(string numero);

        /// <summary>Cria encomenda com base em DTO.</summary>
        Task<ResponseEncomendaDto> CreateAsync(CreateEncomendaDto dto);

        /// <summary>Atualiza parcialmente uma encomenda por ID.</summary>
        Task UpdateAsync(int id, UpdateEncomendaDto dto);

        /// <summary>Atualiza estado da encomenda.</summary>
        Task UpdateEstadoAsync(int id, UpdateEstadoEncomendaDto dto);

        /// <summary>Elimina encomenda por ID.</summary>
        Task DeleteAsync(int id);
    }
}
