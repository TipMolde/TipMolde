using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.RevisaoDto;
using TipMolde.Application.Interface.Desenho.IRevisao;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para a feature Revisao.
    /// </summary>
    /// <remarks>
    /// O controller valida input HTTP, delega regras de negocio ao servico
    /// e devolve contratos DTO estaveis para os consumidores da API.
    /// </remarks>
    [ApiController]
    [Route("api/revisoes")]
    public class RevisaoController : ControllerBase
    {
        private const string PedidoInvalido = "Pedido invalido";

        private readonly IRevisaoService _revisaoService;
        private readonly ILogger<RevisaoController> _logger;

        /// <summary>
        /// Construtor de RevisaoController.
        /// </summary>
        /// <param name="revisaoService">Servico responsavel pelos casos de uso da feature Revisao.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes HTTP.</param>
        public RevisaoController(
            IRevisaoService revisaoService,
            ILogger<RevisaoController> logger)
        {
            _revisaoService = revisaoService;
            _logger = logger;
        }

        /// <summary>
        /// Lista revisoes de um projeto.
        /// </summary>
        /// <param name="projetoId">Identificador do projeto.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com a colecao de revisoes; HTTP 400 para projetoId invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpGet]
        public async Task<IActionResult> GetByProjeto([FromQuery] int projetoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (projetoId < 1)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "ProjetoId deve ser >= 1."));
            }

            if (page < 1 || pageSize < 1)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser maiores ou iguais a 1."));
            }

            var revisoes = await _revisaoService.GetByProjetoIdAsync(projetoId, page, pageSize);
            return Ok(revisoes);
        }

        /// <summary>
        /// Obtem uma revisao por ID.
        /// </summary>
        /// <param name="id">Identificador interno da revisao.</param>
        /// <returns>HTTP 200 com a revisao; HTTP 404 quando nao encontrada.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var revisao = await _revisaoService.GetByIdAsync(id);
            if (revisao == null)
            {
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    $"Revisao com ID {id} nao encontrada."));
            }

            return Ok(revisao);
        }

        /// <summary>
        /// Cria uma nova revisao.
        /// </summary>
        /// <param name="dto">Dados de criacao da revisao.</param>
        /// <returns>HTTP 201 com a revisao criada; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRevisaoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Dados de criacao invalidos."));
            }

            var created = await _revisaoService.CreateAsync(dto);

            _logger.LogInformation("Controller: Revisao {RevisaoId} criada", created.Revisao_id);

            return CreatedAtAction(nameof(GetById), new { id = created.Revisao_id }, created);
        }

        /// <summary>
        /// Regista a resposta do cliente a uma revisao enviada.
        /// </summary>
        /// <param name="id">Identificador da revisao.</param>
        /// <param name="dto">Payload de resposta do cliente.</param>
        /// <returns>HTTP 204 quando a operacao e concluida; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id:int}/resposta-cliente")]
        public async Task<IActionResult> UpdateRespostaCliente(int id, [FromBody] UpdateRespostaRevisaoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Dados de resposta do cliente invalidos."));
            }

            await _revisaoService.UpdateRespostaClienteAsync(id, dto);

            _logger.LogInformation("Controller: resposta do cliente registada para a revisao {RevisaoId}", id);

            return NoContent();
        }

        /// <summary>
        /// Remove uma revisao.
        /// </summary>
        /// <param name="id">Identificador da revisao a remover.</param>
        /// <returns>HTTP 204 quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _revisaoService.DeleteAsync(id);

            _logger.LogInformation("Controller: Revisao {RevisaoId} removida", id);

            return NoContent();
        }
    }
}
