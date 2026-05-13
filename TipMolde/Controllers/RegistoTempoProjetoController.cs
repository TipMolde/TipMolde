using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.RegistoTempoProjetoDto;
using TipMolde.Application.Interface.Desenho.IRegistoTempoProjeto;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para a feature RegistoTempoProjeto.
    /// </summary>
    /// <remarks>
    /// O controller valida input HTTP, delega regras de negocio ao service
    /// e devolve contratos DTO estaveis para os consumidores da API.
    /// </remarks>
    [ApiController]
    [Route("api/registos-tempo-projeto")]
    public class RegistoTempoProjetoController : ControllerBase
    {
        private readonly IRegistoTempoProjetoService _service;
        private readonly ILogger<RegistoTempoProjetoController> _logger;

        /// <summary>
        /// Construtor de RegistoTempoProjetoController.
        /// </summary>
        /// <param name="service">Service responsavel pelos casos de uso da feature.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes HTTP.</param>
        public RegistoTempoProjetoController(
            IRegistoTempoProjetoService service,
            ILogger<RegistoTempoProjetoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Lista o historico temporal de um projeto para um autor.
        /// </summary>
        /// <param name="projetoId">Identificador do projeto.</param>
        /// <param name="autorId">Identificador do autor.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com a colecao de registos; HTTP 400 quando os identificadores sao invalidos.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetHistorico([FromQuery] int projetoId, [FromQuery] int autorId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (projetoId < 1 || autorId < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, "Pedido invalido", "ProjetoId e AutorId devem ser >= 1."));

            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, "Pedido invalido", "Page e pageSize devem ser maiores ou iguais a 1."));

            var historico = await _service.GetHistoricoAsync(projetoId, autorId, page, pageSize);
            return Ok(historico);
        }

        /// <summary>
        /// Obtem um registo de tempo por identificador.
        /// </summary>
        /// <param name="id">Identificador interno do registo.</param>
        /// <returns>HTTP 200 com o registo; HTTP 404 quando nao encontrado.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var registo = await _service.GetByIdAsync(id);
            if (registo == null)
                return NotFound(this.CreateProblem(StatusCodes.Status404NotFound, "Recurso nao encontrado", $"Registo de tempo com ID {id} nao encontrado."));

            return Ok(registo);
        }

        /// <summary>
        /// Cria um novo registo de tempo.
        /// </summary>
        /// <param name="dto">Dados de criacao do registo.</param>
        /// <returns>HTTP 201 com o registo criado; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRegistoTempoProjetoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, "Pedido invalido", "Dados de criacao invalidos."));

            var created = await _service.CreateRegistoAsync(dto);

            _logger.LogInformation(
                "Controller: RegistoTempoProjeto {RegistoId} criado para Projeto {ProjetoId}, Autor {AutorId}",
                created.Registo_Tempo_Projeto_id,
                created.Projeto_id,
                created.Autor_id);

            return CreatedAtAction(nameof(GetById), new { id = created.Registo_Tempo_Projeto_id }, created);
        }

        /// <summary>
        /// Remove um registo de tempo existente.
        /// </summary>
        /// <param name="id">Identificador do registo a remover.</param>
        /// <returns>HTTP 204 quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);

            _logger.LogInformation("Controller: RegistoTempoProjeto {RegistoId} removido", id);

            return NoContent();
        }
    }
}
