using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.PecaDto;
using TipMolde.Application.Interface.Producao.IPeca;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para a feature Peca.
    /// </summary>
    /// <remarks>
    /// O controller valida input HTTP e delega regras de negocio ao servico.
    /// </remarks>
    [ApiController]
    [Route("api/pecas")]
    public class PecaController : ControllerBase
    {
        private const string PedidoInvalido = "Pedido invalido";

        private readonly IPecaService _pecaService;
        private readonly ILogger<PecaController> _logger;

        /// <summary>
        /// Construtor de PecaController.
        /// </summary>
        /// <param name="pecaService">Servico responsavel pelos casos de uso da feature Peca.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes HTTP.</param>
        public PecaController(IPecaService pecaService, ILogger<PecaController> logger)
        {
            _pecaService = pecaService;
            _logger = logger;
        }

        /// <summary>
        /// Lista pecas com paginacao.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = await _pecaService.GetAllAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Obtem uma peca por ID.
        /// </summary>
        /// <param name="id">Identificador interno da peca.</param>
        /// <returns>HTTP 200 com a peca; HTTP 404 quando nao encontrada.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var peca = await _pecaService.GetByIdAsync(id);
            if (peca == null)
                return NotFound(this.CreateProblem(StatusCodes.Status404NotFound, "Recurso nao encontrado", $"Peca com ID {id} nao encontrada."));

            return Ok(peca);
        }

        /// <summary>
        /// Lista pecas de um molde com paginacao.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpGet("por-molde/{moldeId:int}")]
        public async Task<IActionResult> GetByMoldeId(int moldeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = await _pecaService.GetByMoldeIdAsync(moldeId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lista pecas de um molde que ainda nao foram incluídas em qualquer pedido de material.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpGet("por-molde/{moldeId:int}/sem-pedido-material")]
        public async Task<IActionResult> GetByMoldeIdWithoutPedidoMaterial(int moldeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = await _pecaService.GetByMoldeIdWithoutPedidoMaterialAsync(moldeId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lista pecas de um molde com pedido de material ainda pendente de rececao.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpGet("por-molde/{moldeId:int}/pendentes-rececao-material")]
        public async Task<IActionResult> GetByMoldeIdPendingMaterialReceipt(int moldeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = await _pecaService.GetByMoldeIdPendingMaterialReceiptAsync(moldeId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lista a fila de trabalho operacional das pecas da producao.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <param name="searchTerm">Termo de pesquisa opcional.</param>
        /// <param name="searchMode">Modo de pesquisa, por molde ou peca.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpGet("fila-trabalho")]
        public async Task<IActionResult> GetFilaTrabalho(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string searchMode = "Molde")
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = await _pecaService.GetFilaTrabalhoAsync(page, pageSize, searchTerm, searchMode);
            return Ok(result);
        }

        /// <summary>
        /// Obtem uma peca pela designacao dentro de um molde.
        /// </summary>
        /// <param name="designacao">Designacao funcional da peca.</param>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <returns>HTTP 200 com a peca; HTTP 400 quando a query e invalida; HTTP 404 quando nao encontrada.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpGet("por-designacao")]
        public async Task<IActionResult> GetByDesignacao([FromQuery] string? designacao, [FromQuery] int moldeId)
        {
            if (string.IsNullOrWhiteSpace(designacao))
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Designacao e obrigatoria."));

            var peca = await _pecaService.GetByDesignacaoAsync(designacao, moldeId);
            if (peca == null)
                return NotFound(this.CreateProblem(StatusCodes.Status404NotFound, "Recurso nao encontrado", $"Peca '{designacao.Trim()}' nao encontrada no molde {moldeId}."));

            return Ok(peca);
        }

        /// <summary>
        /// Cria uma nova peca.
        /// </summary>
        /// <param name="dto">Dados de criacao da peca.</param>
        /// <returns>HTTP 201 com a peca criada; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePecaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de criacao invalidos."));

            var created = await _pecaService.CreateAsync(dto);

            _logger.LogInformation("Controller: Peca {PecaId} criada", created.PecaId);

            return CreatedAtAction(nameof(GetById), new { id = created.PecaId }, created);
        }

        /// <summary>
        /// Importa pecas de um molde a partir de um ficheiro CSV.
        /// </summary>
        /// <param name="moldeId">Identificador do molde que recebe as pecas.</param>
        /// <param name="file">Ficheiro CSV com a lista de materiais.</param>
        /// <returns>HTTP 200 com o resumo da importacao; HTTP 400 quando o ficheiro nao e valido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpPost("por-molde/{moldeId:int}/importacao-csv")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportarCsv(int moldeId, IFormFile? file = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "O ficheiro CSV e obrigatorio."));

            await using var stream = file.OpenReadStream();
            var result = await _pecaService.ImportarCsvAsync(moldeId, stream);

            _logger.LogInformation("Controller: importacao CSV de pecas concluida para o molde {MoldeId}", moldeId);

            return Ok(result);
        }

        /// <summary>
        /// Atualiza parcialmente uma peca.
        /// </summary>
        /// <remarks>
        /// Campos nao enviados devem manter o valor atual da peca.
        /// </remarks>
        /// <param name="id">Identificador da peca a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>HTTP 204 quando a atualizacao e concluida; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePecaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de atualizacao invalidos."));

            await _pecaService.UpdateAsync(id, dto);

            _logger.LogInformation("Controller: Peca {PecaId} atualizada", id);

            return NoContent();
        }

        /// <summary>
        /// Atualiza o estado de rececao de material de uma peca.
        /// </summary>
        /// <param name="id">Identificador da peca a atualizar.</param>
        /// <param name="dto">Novo estado de rececao de material.</param>
        /// <returns>HTTP 204 quando a atualizacao e concluida; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpPatch("{id:int}/material-recebido")]
        public async Task<IActionResult> UpdateMaterialRecebido(int id, [FromBody] UpdateMaterialRecebidoDto dto)
        {
            if (dto.MaterialRecebido is null)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "O campo MaterialRecebido e obrigatorio."));
            }

            await _pecaService.UpdateAsync(id, new UpdatePecaDto
            {
                MaterialRecebido = dto.MaterialRecebido.Value
            });

            _logger.LogInformation("Controller: rececao de material da peca {PecaId} atualizada", id);

            return NoContent();
        }

        /// <summary>
        /// Remove uma peca.
        /// </summary>
        /// <param name="id">Identificador da peca a remover.</param>
        /// <returns>HTTP 204 quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _pecaService.DeleteAsync(id);

            _logger.LogInformation("Controller: Peca {PecaId} removida", id);

            return NoContent();
        }
    }
}
