using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.FasesProducaoDto;
using TipMolde.Application.Interface.Producao.IFasesProducao;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para a feature FasesProducao.
    /// </summary>
    /// <remarks>
    /// O controller valida input HTTP e delega regras de negocio ao servico de aplicacao.
    /// </remarks>
    [ApiController]
    [Route("api/fases-producao")]
    public class FasesProducaoController : ControllerBase
    {
        private readonly IFasesProducaoService _fasesProducaoService;
        private readonly ILogger<FasesProducaoController> _logger;

        /// <summary>
        /// Construtor de FasesProducaoController.
        /// </summary>
        /// <param name="fasesProducaoService">Servico responsavel pelos casos de uso da feature.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes HTTP.</param>
        public FasesProducaoController(
            IFasesProducaoService fasesProducaoService,
            ILogger<FasesProducaoController> logger)
        {
            _fasesProducaoService = fasesProducaoService;
            _logger = logger;
        }

        /// <summary>
        /// Lista fases de producao com paginacao.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Page e pageSize devem ser maiores ou iguais a 1."));

            var fases = await _fasesProducaoService.GetAllAsync(page, pageSize);
            return Ok(fases);
        }

        /// <summary>
        /// Obtem uma fase de producao por ID.
        /// </summary>
        /// <param name="id">Identificador interno da fase.</param>
        /// <returns>HTTP 200 com a fase; HTTP 404 quando nao encontrada.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var fase = await _fasesProducaoService.GetByIdAsync(id);
            if (fase == null)
            {
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    $"Fase de producao com ID {id} nao encontrada."));
            }

            return Ok(fase);
        }

        /// <summary>
        /// Cria uma nova fase de producao.
        /// </summary>
        /// <param name="dto">Dados de criacao da fase.</param>
        /// <returns>HTTP 201 com a fase criada; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFasesProducaoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Dados de criacao invalidos para a fase de producao."));
            }

            var created = await _fasesProducaoService.CreateAsync(dto);

            _logger.LogInformation("Controller: fase de producao {FaseId} criada.", created.FasesProducao_id);

            return CreatedAtAction(nameof(GetById), new { id = created.FasesProducao_id }, created);
        }

        /// <summary>
        /// Atualiza parcialmente uma fase de producao.
        /// </summary>
        /// <param name="id">Identificador da fase a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>HTTP 204 quando a atualizacao e concluida; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateFasesProducaoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Dados de atualizacao invalidos para a fase de producao."));
            }

            await _fasesProducaoService.UpdateAsync(id, dto);

            _logger.LogInformation("Controller: fase de producao {FaseId} atualizada.", id);

            return NoContent();
        }

        /// <summary>
        /// Remove uma fase de producao.
        /// </summary>
        /// <param name="id">Identificador da fase a remover.</param>
        /// <returns>HTTP 204 quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _fasesProducaoService.DeleteAsync(id);

            _logger.LogInformation("Controller: fase de producao {FaseId} removida.", id);

            return NoContent();
        }
    }
}
