using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.MaquinaDto;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Domain.Enums;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para a feature Maquina.
    /// </summary>
    /// <remarks>
    /// O controller valida input HTTP e delega regras de negocio ao servico de aplicacao.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class MaquinaController : ControllerBase
    {
        private const string PedidoInvalido = "Pedido invalido";

        private readonly IMaquinaService _service;
        private readonly ILogger<MaquinaController> _logger;

        /// <summary>
        /// Construtor de MaquinaController.
        /// </summary>
        /// <param name="service">Servico responsavel pelos casos de uso da feature.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes HTTP.</param>
        public MaquinaController(
            IMaquinaService service,
            ILogger<MaquinaController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Lista maquinas com paginacao.
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
                    PedidoInvalido,
                    "Page e pageSize devem ser maiores ou iguais a 1."));

            var result = await _service.GetAllAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Obtem uma maquina por ID.
        /// </summary>
        /// <param name="id">Identificador interno da maquina.</param>
        /// <returns>HTTP 200 com a maquina; HTTP 404 quando nao encontrada.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var maquina = await _service.GetByIdAsync(id);
            if (maquina == null)
            {
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    $"Maquina com ID {id} nao encontrada."));
            }

            return Ok(maquina);
        }

        /// <summary>
        /// Lista maquinas por estado com paginacao.
        /// </summary>
        /// <param name="estado">Estado operacional a filtrar.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado filtrado por estado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("por-estado")]
        public async Task<IActionResult> GetByEstado([FromQuery] EstadoMaquina estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser maiores ou iguais a 1."));

            var maquinas = await _service.GetByEstadoAsync(estado, page, pageSize);
            return Ok(maquinas);
        }

        /// <summary>
        /// Cria uma nova maquina.
        /// </summary>
        /// <remarks>
        /// O contrato de criacao tem de transportar numero fisico, nome/modelo,
        /// fase dedicada e estado inicial para evitar registos operacionais incompletos.
        /// </remarks>
        /// <param name="dto">Dados de criacao da maquina.</param>
        /// <returns>HTTP 201 com o recurso criado; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMaquinaDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Dados de criacao invalidos para a maquina."));
            }

            var created = await _service.CreateAsync(dto);

            _logger.LogInformation("Controller: maquina {MaquinaId} criada.", created.Maquina_id);

            return CreatedAtAction(nameof(GetById), new { id = created.Maquina_id }, created);
        }

        /// <summary>
        /// Atualiza parcialmente uma maquina.
        /// </summary>
        /// <remarks>
        /// Campos omitidos preservam o valor atual e nunca aplicam defaults tecnicos com impacto operacional.
        /// </remarks>
        /// <param name="id">Identificador da maquina a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>HTTP 204 quando a atualizacao e concluida; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMaquinaDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Dados de atualizacao invalidos para a maquina."));
            }

            var isAdmin = User.IsInRole("ADMIN");
            var isGestorProducao = User.IsInRole("GESTOR_PRODUCAO");

            if (!isAdmin && isGestorProducao && HasRestrictedFieldsForGestorProducao(dto))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    this.CreateProblem(
                        StatusCodes.Status403Forbidden,
                        "Operacao nao permitida",
                        "O gestor de producao so pode atualizar o estado operacional da maquina."));
            }

            await _service.UpdateAsync(id, dto);

            _logger.LogInformation("Controller: maquina {MaquinaId} atualizada.", id);

            return NoContent();
        }

        /// <summary>
        /// Remove uma maquina.
        /// </summary>
        /// <param name="id">Identificador da maquina a remover.</param>
        /// <returns>HTTP 204 quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);

            _logger.LogInformation("Controller: maquina {MaquinaId} removida.", id);

            return NoContent();
        }

        private static bool HasRestrictedFieldsForGestorProducao(UpdateMaquinaDto dto)
        {
            return dto.Numero.HasValue
                   || !string.IsNullOrWhiteSpace(dto.NomeModelo)
                   || !string.IsNullOrWhiteSpace(dto.IpAddress)
                   || dto.FaseDedicada_id.HasValue;
        }
    }
}
