using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.ClienteDto;
using TipMolde.Application.Interface.Comercio.ICliente;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints para gestao de clientes no modulo comercial.
    /// </summary>
    /// <remarks>
    /// Recebe pedidos HTTP, valida parametros de entrada e delega regras de negocio ao servico de cliente.
    /// </remarks>
    [ApiController]
    [Route("api/clientes")]
    public class ClienteController : ControllerBase
    {
        private const string PedidoInvalido = "Pedido invalido";
        private const string RecursoNaoEncontrado = "Recurso nao encontrado";

        private readonly IClienteService _clienteService;
        private readonly ILogger<ClienteController> _logger;

        /// <summary>
        /// Construtor de ClienteController.
        /// </summary>
        /// <param name="clienteService">Servico responsavel pelos casos de uso de cliente.</param>
        /// <param name="logger">Logger para registo de operacoes do controlador.</param>
        public ClienteController(
            IClienteService clienteService,
            ILogger<ClienteController> logger)
        {
            _clienteService = clienteService;
            _logger = logger;
        }

        /// <summary>
        /// Lista clientes com paginacao.
        /// </summary>
        /// <remarks>
        /// Valida os parametros de pagina e devolve metadados de paginacao com os itens convertidos para DTO.
        /// </remarks>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado HTTP com lista paginada de clientes.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO")]
        [HttpGet]
        public async Task<IActionResult> GetAllClientes([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser >= 1."));

            var clientes = await _clienteService.GetAllAsync(page, pageSize);

            return Ok(clientes);
        }

        /// <summary>
        /// Obtem um cliente pelo identificador.
        /// </summary>
        /// <param name="id">Identificador unico do cliente.</param>
        /// <returns>Resultado HTTP com o cliente encontrado ou erro de nao encontrado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetClienteById(int id)
        {
            var cliente = await _clienteService.GetByIdAsync(id);
            if (cliente == null)
            {
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    RecursoNaoEncontrado,
                    $"Cliente com ID {id} nao encontrado."));
            }

            return Ok(cliente);
        }

        /// <summary>
        /// Obtem um cliente incluindo a respetiva colecao de encomendas.
        /// </summary>
        /// <param name="id">Identificador unico do cliente.</param>
        /// <returns>Resultado HTTP com dados do cliente e encomendas associadas.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("{id:int}/encomendas")]
        public async Task<IActionResult> GetClienteWithEncomendas(int id)
        {
            var cliente = await _clienteService.GetClienteWithEncomendasAsync(id);
            if (cliente == null)
            {
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    RecursoNaoEncontrado,
                    $"Cliente com ID {id} nao encontrado."));
            }

            return Ok(cliente);
        }

        /// <summary>
        /// Pesquisa clientes por nome.
        /// </summary>
        /// <param name="searchTerm">Termo parcial de pesquisa aplicado ao nome do cliente.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado HTTP com clientes que correspondem ao termo informado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO")]
        [HttpGet("search/by-name")]
        public async Task<IActionResult> SearchByName([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "O parametro searchTerm e obrigatorio."));
            }

            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser >= 1."));

            var clientes = await _clienteService.SearchByNameAsync(searchTerm, page, pageSize);
            return Ok(clientes);
        }

        /// <summary>
        /// Pesquisa clientes por sigla.
        /// </summary>
        /// <param name="searchTerm">Termo parcial de pesquisa aplicado a sigla do cliente.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado HTTP com clientes que correspondem ao termo informado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO")]
        [HttpGet("search/by-sigla")]
        public async Task<IActionResult> SearchBySigla([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "O parametro searchTerm e obrigatorio."));
            }

            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser >= 1."));

            var clientes = await _clienteService.SearchBySiglaAsync(searchTerm, page, pageSize);
            return Ok(clientes);
        }

        /// <summary>
        /// Cria um novo cliente.
        /// </summary>
        /// <remarks>
        /// Converte o DTO de entrada para entidade de dominio e devolve o recurso criado com localizacao.
        /// </remarks>
        /// <param name="dto">Dados de criacao do cliente.</param>
        /// <returns>Resultado HTTP de criacao com o cliente persistido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPost]
        public async Task<IActionResult> CreateCliente([FromBody] CreateClienteDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de criacao invalidos."));

            var created = await _clienteService.CreateAsync(dto);

            _logger.LogInformation("Cliente {ClienteId} criado com sucesso", created.Cliente_id);

            return CreatedAtAction(nameof(GetClienteById), new { id = created.Cliente_id }, created);
        }

        /// <summary>
        /// Atualiza os dados de um cliente existente.
        /// </summary>
        /// <param name="id">Identificador do cliente a atualizar.</param>
        /// <param name="dto">Dados enviados para atualizacao do cliente.</param>
        /// <returns>Resultado HTTP sem conteudo quando a atualizacao e concluida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCliente(int id, [FromBody] UpdateClienteDto dto)
        {
            var existingCliente = await _clienteService.GetByIdAsync(id);
            if (existingCliente == null)
            {
                _logger.LogWarning("Tentativa de atualizacao de cliente {ClienteId} falhou: recurso nao encontrado", id);
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    RecursoNaoEncontrado,
                    $"Cliente com ID {id} nao encontrado."));
            }

            await _clienteService.UpdateAsync(id, dto);

            _logger.LogInformation("Cliente {ClienteId} atualizado com sucesso", id);

            return NoContent();
        }

        /// <summary>
        /// Remove um cliente pelo identificador.
        /// </summary>
        /// <param name="id">Identificador do cliente a remover.</param>
        /// <returns>Resultado HTTP sem conteudo quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var existingCliente = await _clienteService.GetByIdAsync(id);
            if (existingCliente == null)
            {
                _logger.LogWarning("Tentativa de remocao de cliente {ClienteId} falhou: recurso nao encontrado", id);
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    RecursoNaoEncontrado,
                    $"Cliente com ID {id} nao encontrado."));
            }

            await _clienteService.DeleteAsync(id);

            _logger.LogInformation("Cliente {ClienteId} removido com sucesso", id);

            return NoContent();
        }
    }
}
