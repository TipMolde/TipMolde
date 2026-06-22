using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.EncomendaMoldeDto;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para gestao da relacao Encomenda-Molde.
    /// </summary>
    /// <remarks>
    /// O controller valida input HTTP e delega regras de negocio ao servico.
    /// </remarks>
    [ApiController]
    [Route("api/encomenda-moldes")]
    public class EncomendaMoldeController : ControllerBase
    {
        private const string PedidoInvalido = "Pedido invalido";

        private readonly IEncomendaMoldeService _service;
        private readonly ILogger<EncomendaMoldeController> _logger;

        /// <summary>
        /// Construtor de EncomendaMoldeController.
        /// </summary>
        /// <param name="service">Servico responsavel pelos casos de uso da feature EncomendaMolde.</param>
        /// <param name="logger">Logger para rastreabilidade de operacoes HTTP.</param>
        public EncomendaMoldeController(
            IEncomendaMoldeService service,
            ILogger<EncomendaMoldeController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Obtem uma associacao Encomenda-Molde por ID.
        /// </summary>
        /// <param name="id">Identificador da associacao.</param>
        /// <returns>HTTP 200 com DTO de resposta; HTTP 404 quando nao encontrado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var link = await _service.GetByIdAsync(id);
            if (link == null)
                return NotFound(this.CreateProblem(StatusCodes.Status404NotFound, "Recurso nao encontrado", $"EncomendaMolde com ID {id} nao encontrada."));

            return Ok(link);
        }

        /// <summary>
        /// Lista associacoes por encomenda com paginacao.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda para filtro.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("por-encomenda/{encomendaId:int}")]
        public async Task<IActionResult> GetByEncomendaId(
            int encomendaId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = await _service.GetByEncomendaIdAsync(encomendaId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Obtem a fila global de associacoes Encomenda-Molde com paginacao.
        /// </summary>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize]
        [HttpGet("fila-global")]
        public async Task<IActionResult> GetFilaGlobal([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest, 
                    PedidoInvalido, 
                    "Page e pageSize devem ser >= 1."));

            var result = await _service.GetFilaGlobalAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lista associacoes por molde com paginacao.
        /// </summary>
        /// <param name="moldeId">Identificador do molde para filtro.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("por-molde/{moldeId:int}")]
        public async Task<IActionResult> GetByMoldeId(
            int moldeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = await _service.GetByMoldeIdAsync(moldeId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lista associacoes Encomenda-Molde cujas encomendas estao confirmadas.
        /// </summary>
        /// <remarks>
        /// Este endpoint serve o modulo de desenho para identificar moldes ja autorizados a iniciar trabalho.
        /// </remarks>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO")]
        [HttpGet("encomendas-confirmadas")]
        public async Task<IActionResult> GetByEncomendasConfirmadas(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = await _service.GetByEncomendasConfirmadasAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lista associacoes Encomenda-Molde aptas para a pagina de desenho.
        /// </summary>
        /// <remarks>
        /// Para alem de encomenda confirmada, o molde tem de ter o ultimo projeto
        /// concluido e a ultima revisao aprovada pelo cliente.
        /// </remarks>
        /// <param name="searchTerm">Termo opcional para filtrar a lista de moldes aptos.</param>
        /// <param name="page">Pagina atual (>= 1).</param>
        /// <param name="pageSize">Tamanho da pagina (>= 1).</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO")]
        [HttpGet("encomendas-confirmadas-para-desenho")]
        public async Task<IActionResult> GetByEncomendasConfirmadasParaDesenho(
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = string.IsNullOrWhiteSpace(searchTerm)
                ? await _service.GetByEncomendasConfirmadasParaDesenhoAsync(page, pageSize)
                : await _service.SearchByTermForDesenhoAsync(searchTerm, page, pageSize);

            return Ok(result);
        }

        /// <summary>
        /// Cria uma nova associacao Encomenda-Molde.
        /// </summary>
        /// <param name="dto">Dados de criacao da associacao.</param>
        /// <returns>HTTP 201 com recurso criado; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEncomendaMoldeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de criacao invalidos."));

            var created = await _service.CreateAsync(dto);
            _logger.LogInformation("Controller: EncomendaMolde {EncomendaMoldeId} criado", created.EncomendaMolde_id);

            return CreatedAtAction(nameof(GetById), new { id = created.EncomendaMolde_id }, created);
        }

        /// <summary>
        /// Atualiza parcialmente uma associacao Encomenda-Molde.
        /// </summary>
        /// <remarks>
        /// Campos nao enviados sao preservados no registo atual.
        /// </remarks>
        /// <param name="id">Identificador da associacao a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>HTTP 204 quando a atualizacao e concluida; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEncomendaMoldeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de atualizacao invalidos."));

            await _service.UpdateAsync(id, dto);
            _logger.LogInformation("Controller: EncomendaMolde {EncomendaMoldeId} atualizado", id);

            return NoContent();
        }

        /// <summary>
        /// Atualiza o estado operacional de um molde dentro da encomenda.
        /// </summary>
        /// <param name="id">Identificador da associacao a atualizar.</param>
        /// <param name="dto">Estado de destino do molde.</param>
        /// <returns>HTTP 204 quando a transicao e aplicada com sucesso.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_PRODUCAO")]
        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> UpdateEstado(int id, [FromBody] UpdateEstadoEncomendaMoldeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de atualizacao invalidos."));

            await _service.UpdateEstadoAsync(id, dto);
            _logger.LogInformation("Controller: estado do EncomendaMolde {EncomendaMoldeId} atualizado", id);

            return NoContent();
        }

        /// <summary>
        /// Remove uma associacao Encomenda-Molde.
        /// </summary>
        /// <param name="id">Identificador da associacao a remover.</param>
        /// <returns>HTTP 204 quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            _logger.LogInformation("Controller: EncomendaMolde {EncomendaMoldeId} removido", id);

            return NoContent();
        }
    }
}
