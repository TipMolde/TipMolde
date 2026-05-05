using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.PedidoMaterialDto;
using TipMolde.Application.Interface.Comercio.IPedidoMaterial;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints para gestao do ciclo de vida de pedidos de material.
    /// </summary>
    /// <remarks>
    /// O controlador limita-se a validar parametros HTTP, aplicar regras de autorizacao
    /// e delegar a logica funcional ao servico de aplicacao.
    /// </remarks>
    [ApiController]
    [Route("api/pedidos-material")]
    public class PedidoMaterialController : ControllerBase
    {
        private readonly IPedidoMaterialService _service;
        private readonly ILogger<PedidoMaterialController> _logger;

        /// <summary>
        /// Construtor de PedidoMaterialController.
        /// </summary>
        /// <param name="service">Servico responsavel pelos casos de uso de pedido de material.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes do controlador.</param>
        public PedidoMaterialController(
            IPedidoMaterialService service,
            ILogger<PedidoMaterialController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Lista pedidos de material com paginacao.
        /// </summary>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado HTTP com lista paginada de pedidos de material.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Page e pageSize devem ser >= 1."));
            }

            var result = await _service.GetAllAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Obtem um pedido de material pelo identificador.
        /// </summary>
        /// <param name="id">Identificador unico do pedido.</param>
        /// <returns>Resultado HTTP com o pedido encontrado ou erro de nao encontrado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var pedido = await _service.GetByIdAsync(id);
            if (pedido == null)
            {
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    $"Pedido de material com ID {id} nao encontrado."));
            }

            return Ok(pedido);
        }

        /// <summary>
        /// Lista pedidos de material de um fornecedor.
        /// </summary>
        /// <param name="fornecedorId">Identificador do fornecedor.</param>
        /// <returns>Resultado HTTP com os pedidos associados ao fornecedor informado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("fornecedores/{fornecedorId:int}")]
        public async Task<IActionResult> GetByFornecedorId(int fornecedorId)
        {
            var pedidos = await _service.GetByFornecedorIdAsync(fornecedorId);
            return Ok(pedidos);
        }

        /// <summary>
        /// Cria um novo pedido de material.
        /// </summary>
        /// <remarks>
        /// O servico valida fornecedor, pecas e duplicados antes de persistir o agregado completo.
        /// </remarks>
        /// <param name="dto">Dados de criacao do pedido e das respetivas linhas.</param>
        /// <returns>Resultado HTTP de criacao com o pedido persistido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePedidoMaterialDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Dados de criacao invalidos."));
            }

            var created = await _service.CreateAsync(dto);

            _logger.LogInformation("Pedido de material {PedidoId} criado com sucesso", created.PedidoMaterialId);

            return CreatedAtAction(nameof(GetById), new { id = created.PedidoMaterialId }, created);
        }

        /// <summary>
        /// Regista a rececao de um pedido de material.
        /// </summary>
        /// <remarks>
        /// Seguranca: o utilizador conferente e derivado do token autenticado para impedir impersonacao.
        /// A operacao atualiza o estado do pedido e desbloqueia as pecas associadas para producao.
        /// </remarks>
        /// <param name="id">Identificador do pedido a marcar como recebido.</param>
        /// <returns>Resultado HTTP sem conteudo quando a rececao e concluida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_PRODUCAO")]
        [HttpPut("{id:int}/rececao")]
        public async Task<IActionResult> RegistarRececao(int id)
        {
            var userId = this.GetAuthenticatedUserId();

            await _service.RegistarRececaoAsync(id, userId);

            _logger.LogInformation(
                "Rececao do pedido de material {PedidoId} registada pelo utilizador autenticado {UserId}",
                id,
                userId);

            return NoContent();
        }

        /// <summary>
        /// Remove um pedido de material pelo identificador.
        /// </summary>
        /// <param name="id">Identificador do pedido a remover.</param>
        /// <returns>Resultado HTTP sem conteudo quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);

            _logger.LogInformation("Pedido de material {PedidoId} removido com sucesso", id);

            return NoContent();
        }
    }
}