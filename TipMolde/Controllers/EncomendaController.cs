using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.EncomendaDto;
using TipMolde.Application.Interface.Comercio.IEncomenda;
using TipMolde.Domain.Enums;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para gestao de encomendas no modulo comercial.
    /// </summary>
    /// <remarks>
    /// O controller valida parametros de entrada, delega regras de negocio ao servico e devolve contratos DTO.
    /// </remarks>
    [ApiController]
    [Route("api/encomendas")]
    public class EncomendaController : ControllerBase
    {
        private const string PedidoInvalido = "Pedido invalido";

        private readonly IEncomendaService _encomendaService;
        private readonly ILogger<EncomendaController> _logger;

        /// <summary>
        /// Construtor de EncomendaController.
        /// </summary>
        /// <param name="encomendaService">Servico responsavel pelos casos de uso de encomenda.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes do controller.</param>
        public EncomendaController(
            IEncomendaService encomendaService,
            ILogger<EncomendaController> logger)
        {
            _encomendaService = encomendaService;
            _logger = logger;
        }

        /// <summary>
        /// Lista encomendas com paginacao.
        /// </summary>
        /// <param name="page">Numero da pagina (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado HTTP com metadados de paginacao e itens.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpGet]
        public async Task<IActionResult> GetAllEncomendas([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser >= 1."));

            var result = await _encomendaService.GetAllAsync(page, pageSize);

            return Ok(new
            {
                result.TotalCount,
                result.CurrentPage,
                result.PageSize,
                Items = result.Items
            });
        }

        /// <summary>
        /// Obtem uma encomenda pelo identificador.
        /// </summary>
        /// <param name="id">Identificador da encomenda.</param>
        /// <returns>Resultado HTTP com a encomenda encontrada.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetEncomendaById(int id)
        {
            var encomenda = await _encomendaService.GetByIdAsync(id);
            if (encomenda == null)
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    $"Encomenda com ID {id} nao encontrada."));

            return Ok(encomenda);
        }

        /// <summary>
        /// Obtem uma encomenda com os moldes associados.
        /// </summary>
        /// <param name="id">Identificador da encomenda.</param>
        /// <returns>Resultado HTTP com a encomenda e relacoes de molde.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("{id:int}/moldes")]
        public async Task<IActionResult> GetEncomendaWithMoldes(int id)
        {
            var encomenda = await _encomendaService.GetEncomendaWithMoldesAsync(id);
            if (encomenda == null)
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    $"Encomenda com ID {id} nao encontrada."));

            return Ok(encomenda);
        }

        /// <summary>
        /// Lista encomendas com estado nao terminal.
        /// </summary>
        /// <returns>Resultado HTTP com encomendas por concluir.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("por-concluir")]
        public async Task<IActionResult> GetEncomendasPorConcluir([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser >= 1."));

            var encomendas = await _encomendaService.GetEncomendasPorConcluirAsync(page, pageSize);
            return Ok(encomendas);
        }

        /// <summary>
        /// Lista encomendas em producao para a pagina operacional.
        /// </summary>
        /// <remarks>
        /// Para esta consulta, em producao significa estado diferente de CONCLUIDA e CANCELADA.
        /// </remarks>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpGet("em-producao")]
        public async Task<IActionResult> GetEncomendasEmProducao([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser >= 1."));

            var encomendas = await _encomendaService.GetEncomendasEmProducaoAsync(page, pageSize);
            return Ok(encomendas);
        }

        /// <summary>
        /// Pesquisa encomendas em producao pelo nome da encomenda do cliente.
        /// </summary>
        /// <param name="searchTerm">Texto obrigatorio da pesquisa.</param>
        /// <param name="page">Numero da pagina (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>Resultado HTTP com encomendas em producao que correspondem ao criterio de pesquisa.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("em-producao/search")]
        public async Task<IActionResult> SearchEncomendasEmProducao([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser >= 1."));

            var encomendas = await _encomendaService.SearchEncomendasEmProducaoAsync(searchTerm ?? string.Empty, page, pageSize);
            return Ok(encomendas);
        }

        /// <summary>
        /// Lista encomendas por estado.
        /// </summary>
        /// <param name="estado">Nome do estado a filtrar (ex.: CONFIRMADA, EM_PRODUCAO).</param>
        /// <returns>Resultado HTTP com encomendas no estado solicitado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("por-estado")]
        public async Task<IActionResult> GetByEstado([FromQuery] EstadoEncomenda estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser >= 1."));

            var encomendas = await _encomendaService.GetByEstadoAsync(estado, page, pageSize);
            return Ok(encomendas);
        }

        /// <summary>
        /// Obtem uma encomenda pelo numero de referencia do cliente.
        /// </summary>
        /// <param name="numero">Numero de encomenda no sistema do cliente.</param>
        /// <returns>Resultado HTTP com a encomenda encontrada.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("por-numero-cliente")]
        public async Task<IActionResult> GetByNumeroCliente([FromQuery] string? numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "O numero de encomenda do cliente e obrigatorio."));

            var encomenda = await _encomendaService.GetByNumeroEncomendaClienteAsync(numero);
            if (encomenda == null)
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    $"Encomenda com numero '{numero}' nao encontrada."));

            return Ok(encomenda);
        }

        /// <summary>
        /// Cria uma nova encomenda.
        /// </summary>
        /// <param name="dto">Dados de criacao da encomenda.</param>
        /// <returns>Resultado HTTP de criacao com recurso persistido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPost]
        public async Task<IActionResult> CreateEncomenda([FromBody] CreateEncomendaDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(this.CreateProblem(
                StatusCodes.Status400BadRequest,
                PedidoInvalido,
                "Dados de criacao invalidos."));

            var created = await _encomendaService.CreateAsync(dto);
            _logger.LogInformation("Encomenda {EncomendaId} criada com sucesso", created.Encomenda_id);

            return CreatedAtAction(
                nameof(GetEncomendaById),
                new { id = created.Encomenda_id },
                created);
        }

        /// <summary>
        /// Atualiza parcialmente os dados de uma encomenda.
        /// </summary>
        /// <remarks>
        /// Campos nulos no DTO sao ignorados, preservando os valores atuais.
        /// </remarks>
        /// <param name="id">Identificador da encomenda a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>Resultado HTTP sem conteudo quando a operacao e concluida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEncomenda(int id, [FromBody] UpdateEncomendaDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(this.CreateProblem(
                StatusCodes.Status400BadRequest,
                PedidoInvalido,
                "Dados de atualizacao invalidos."));


            await _encomendaService.UpdateAsync(id, dto);
            _logger.LogInformation("Encomenda {EncomendaId} atualizada com sucesso", id);

            return NoContent();
        }

        /// <summary>
        /// Atualiza o estado de uma encomenda.
        /// </summary>
        /// <param name="id">Identificador da encomenda.</param>
        /// <param name="dto">Estado de destino.</param>
        /// <returns>Resultado HTTP sem conteudo quando a transicao e valida e aplicada.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> UpdateEstado(int id, [FromBody] UpdateEstadoEncomendaDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(this.CreateProblem(
                StatusCodes.Status400BadRequest,
                PedidoInvalido,
                "Dados de atualizacao invalidos."));

            await _encomendaService.UpdateEstadoAsync(id, dto);
            return NoContent();
        }

        /// <summary>
        /// Remove uma encomenda.
        /// </summary>
        /// <param name="id">Identificador da encomenda a remover.</param>
        /// <returns>Resultado HTTP sem conteudo quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEncomenda(int id)
        {
            await _encomendaService.DeleteAsync(id);
            _logger.LogInformation("Encomenda {EncomendaId} removida com sucesso", id);

            return NoContent();
        }
    }
}
