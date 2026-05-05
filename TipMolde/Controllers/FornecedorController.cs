using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.FornecedorDto;
using TipMolde.Application.Interface.Comercio.IFornecedor;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints para gestao de fornecedores no modulo comercial.
    /// </summary>
    /// <remarks>
    /// Recebe pedidos HTTP, valida parametros de entrada e delega regras de negocio ao servico de fornecedor.
    /// </remarks>
    [ApiController]
    [Route("api/fornecedores")]
    public class FornecedorController : ControllerBase
    {
        private const string PedidoInvalido = "Pedido invalido";

        private readonly IFornecedorService _service;
        private readonly ILogger<FornecedorController> _logger;

        /// <summary>
        /// Construtor de FornecedorController.
        /// </summary>
        /// <param name="service">Servico responsavel pelos casos de uso de fornecedor.</param>
        /// <param name="logger">Logger para registo de operacoes do controlador.</param>
        public FornecedorController(
            IFornecedorService service,
            ILogger<FornecedorController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Lista fornecedores com paginacao.
        /// </summary>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado HTTP com lista paginada de fornecedores.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser >= 1."));
            }

            var result = await _service.GetAllAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Obtem um fornecedor pelo identificador.
        /// </summary>
        /// <param name="id">Identificador unico do fornecedor.</param>
        /// <returns>Resultado HTTP com o fornecedor encontrado ou erro de nao encontrado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var fornecedor = await _service.GetByIdAsync(id);
            if (fornecedor == null)
            {
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    $"Fornecedor com ID {id} nao encontrado."));
            }

            return Ok(fornecedor);
        }

        /// <summary>
        /// Pesquisa fornecedores por nome.
        /// </summary>
        /// <remarks>
        /// A pesquisa e parcial e devolve um resultado paginado com metadados de navegacao.
        /// </remarks>
        /// <param name="searchTerm">Termo parcial de pesquisa aplicado ao nome do fornecedor.</param>
        /// <param name="page">Numero da pagina a consultar.</param>
        /// <param name="pageSize">Quantidade de itens por pagina.</param>
        /// <returns>Resultado HTTP com fornecedores que correspondem ao termo informado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("search/by-name")]
        public async Task<IActionResult> SearchByName(
            [FromQuery] string? searchTerm,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "O parametro searchTerm e obrigatorio."));
            }

            if (page < 1 || pageSize < 1)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Page e pageSize devem ser >= 1."));
            }

            var result = await _service.SearchByNameAsync(searchTerm, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Cria um novo fornecedor.
        /// </summary>
        /// <remarks>
        /// O servico valida obrigatoriedade, unicidade do NIF e normaliza os campos textuais antes da persistencia.
        /// </remarks>
        /// <param name="dto">Dados de criacao do fornecedor.</param>
        /// <returns>Resultado HTTP de criacao com o fornecedor persistido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFornecedorDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Dados de criacao invalidos."));
            }

            var created = await _service.CreateAsync(dto);

            _logger.LogInformation("Fornecedor {FornecedorId} criado com sucesso", created.FornecedorId);

            return CreatedAtAction(nameof(GetById), new { id = created.FornecedorId }, created);
        }

        /// <summary>
        /// Atualiza os dados de um fornecedor existente.
        /// </summary>
        /// <param name="id">Identificador do fornecedor a atualizar.</param>
        /// <param name="dto">Dados enviados para atualizacao do fornecedor.</param>
        /// <returns>Resultado HTTP sem conteudo quando a atualizacao e concluida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateFornecedorDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    PedidoInvalido,
                    "Dados de atualizacao invalidos."));
            }

            await _service.UpdateAsync(id, dto);

            _logger.LogInformation("Fornecedor {FornecedorId} atualizado com sucesso", id);

            return NoContent();
        }

        /// <summary>
        /// Remove um fornecedor pelo identificador.
        /// </summary>
        /// <param name="id">Identificador do fornecedor a remover.</param>
        /// <returns>Resultado HTTP sem conteudo quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);

            _logger.LogInformation("Fornecedor {FornecedorId} removido com sucesso", id);

            return NoContent();
        }
    }
}
