using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.ProjetoDto;
using TipMolde.Application.Interface.Desenho.IProjeto;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para a feature Projeto.
    /// </summary>
    /// <remarks>
    /// O controller valida input HTTP, delega regras de negocio ao servico
    /// e devolve contratos DTO estaveis para os consumidores da API.
    /// </remarks>
    [ApiController]
    [Route("api/projetos")]
    public class ProjetoController : ControllerBase
    {
        private const string PedidoInvalido = "Pedido invalido";

        private readonly IProjetoService _projetoService;
        private readonly ILogger<ProjetoController> _logger;

        /// <summary>
        /// Construtor de ProjetoController.
        /// </summary>
        /// <param name="projetoService">Servico responsavel pelos casos de uso da feature Projeto.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes HTTP.</param>
        public ProjetoController(
            IProjetoService projetoService,
            ILogger<ProjetoController> logger)
        {
            _projetoService = projetoService;
            _logger = logger;
        }

        /// <summary>
        /// Lista projetos com paginacao.
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

            var result = await _projetoService.GetAllAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Obtem um projeto por ID.
        /// </summary>
        /// <param name="id">Identificador interno do projeto.</param>
        /// <returns>HTTP 200 com o projeto; HTTP 404 quando nao encontrado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var projeto = await _projetoService.GetByIdAsync(id);
            if (projeto == null)
                return NotFound(this.CreateProblem(StatusCodes.Status404NotFound, "Recurso nao encontrado", $"Projeto com ID {id} nao encontrado."));

            return Ok(projeto);
        }

        /// <summary>
        /// Obtem um projeto com as revisoes associadas.
        /// </summary>
        /// <param name="id">Identificador interno do projeto.</param>
        /// <returns>HTTP 200 com o detalhe enriquecido; HTTP 404 quando nao encontrado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpGet("{id:int}/com-revisoes")]
        public async Task<IActionResult> GetWithRevisoes(int id)
        {
            var projeto = await _projetoService.GetWithRevisoesAsync(id);
            if (projeto == null)
                return NotFound(this.CreateProblem(StatusCodes.Status404NotFound, "Recurso nao encontrado", $"Projeto com ID {id} nao encontrado."));

            return Ok(projeto);
        }

        /// <summary>
        /// Lista projetos associados a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador do molde.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpGet("por-molde/{moldeId:int}")]
        public async Task<IActionResult> GetByMoldeId(
            int moldeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (moldeId < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "MoldeId deve ser >= 1."));

            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var projetos = await _projetoService.GetByMoldeIdAsync(moldeId, page, pageSize);
            return Ok(projetos);
        }

        /// <summary>
        /// Cria um novo projeto.
        /// </summary>
        /// <remarks>
        /// O contrato persiste tambem o campo CaminhoPastaServidor como parte do agregado Projeto.
        /// </remarks>
        /// <param name="dto">Dados de criacao do projeto.</param>
        /// <returns>HTTP 201 com o projeto criado; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProjetoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de criacao invalidos."));

            var created = await _projetoService.CreateAsync(dto);

            _logger.LogInformation("Controller: Projeto {ProjetoId} criado", created.Projeto_id);

            return CreatedAtAction(nameof(GetById), new { id = created.Projeto_id }, created);
        }

        /// <summary>
        /// Atualiza parcialmente um projeto existente.
        /// </summary>
        /// <remarks>
        /// Campos nao enviados devem preservar o valor atual do agregado.
        /// </remarks>
        /// <param name="id">Identificador do projeto a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>HTTP 204 quando a atualizacao e concluida; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProjetoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de atualizacao invalidos."));

            await _projetoService.UpdateAsync(id, dto);

            _logger.LogInformation("Controller: Projeto {ProjetoId} atualizado", id);

            return NoContent();
        }

        /// <summary>
        /// Remove um projeto.
        /// </summary>
        /// <param name="id">Identificador do projeto a remover.</param>
        /// <returns>HTTP 204 quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _projetoService.DeleteAsync(id);

            _logger.LogInformation("Controller: Projeto {ProjetoId} removido", id);

            return NoContent();
        }
    }
}
