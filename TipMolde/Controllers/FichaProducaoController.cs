using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Interface.Fichas.IFichaProducao;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Exponibiliza operacoes de consulta e manutencao das fichas editaveis de producao.
    /// </summary>
    [ApiController]
    [Route("api/fichas-producao")]
    public class FichaProducaoController : ControllerBase
    {
        private readonly IFichaProducaoService _service;

        /// <summary>
        /// Construtor de FichaProducaoController.
        /// </summary>
        /// <param name="service">Servico responsavel pelos casos de uso das fichas editaveis de producao.</param>
        public FichaProducaoController(IFichaProducaoService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista as fichas editaveis associadas a uma relacao Encomenda-Molde.
        /// </summary>
        /// <param name="encomendaMoldeId">Identificador da relacao Encomenda-Molde usada como contexto da ficha.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Pagina com os cabecalhos das fichas editaveis encontradas.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("by-encomendamolde")]
        public async Task<IActionResult> GetByEncomendaMoldeId(int encomendaMoldeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, "Pedido invalido", "Page e pageSize devem ser maiores ou iguais a 1."));

            return Ok(await _service.GetByEncomendaMoldeIdAsync(encomendaMoldeId, page, pageSize));
        }

        /// <summary>
        /// Lista as fichas editaveis associadas a um molde.
        /// </summary>
        /// <param name="moldeId">Identificador interno do molde.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Pagina com os cabecalhos das fichas editaveis encontradas.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("by-molde")]
        public async Task<IActionResult> GetByMoldeId(int moldeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, "Pedido invalido", "Page e pageSize devem ser maiores ou iguais a 1."));

            return Ok(await _service.GetByMoldeIdAsync(moldeId, page, pageSize));
        }

        /// <summary>
        /// Obtem o detalhe completo de uma ficha editavel.
        /// </summary>
        /// <param name="id">Identificador interno da ficha editavel.</param>
        /// <returns>Detalhe da ficha quando existe.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ficha = await _service.GetByIdAsync(id);
            if (ficha == null)
            {
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    $"Ficha de producao com ID {id} nao encontrada."));
            }

            return Ok(ficha);
        }

        /// <summary>
        /// Cria uma nova ficha editavel de producao.
        /// </summary>
        /// <param name="dto">Dados minimos necessarios para abrir a ficha no contexto Encomenda-Molde.</param>
        /// <returns>Ficha criada em estado de rascunho.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateFichaProducaoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Dados invalidos para a criacao da ficha de producao."));
            }

            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.FichaProducao_id }, created);
        }
    }
}
