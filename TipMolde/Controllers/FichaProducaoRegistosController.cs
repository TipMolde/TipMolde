using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Interface.Fichas.IFichaProducao;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Exponibiliza endpoints HTTP para as linhas manuais das fichas FRM, FRA e FOP.
    /// </summary>
    [ApiController]
    [Route("api/fichas-producao/{fichaId:int}")]
    public class FichaProducaoRegistosController : ControllerBase
    {
        private const string PedidoInvalido = "Pedido invalido";

        private readonly IFichaProducaoService _service;

        /// <summary>
        /// Construtor de FichaProducaoRegistosController.
        /// </summary>
        /// <param name="service">Servico responsavel pela manutencao das linhas manuais das fichas FRM, FRA e FOP.</param>
        public FichaProducaoRegistosController(IFichaProducaoService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista as linhas manuais de uma ficha FRM.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FRM.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Pagina com as linhas manuais da ficha FRM.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("linhas-frm")]
        public async Task<IActionResult> GetLinhasFrm(int fichaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser maiores ou iguais a 1."));

            return Ok(await _service.GetLinhasFrmAsync(fichaId, page, pageSize));
        }

        /// <summary>
        /// Adiciona uma nova linha manual a uma ficha FRM.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FRM.</param>
        /// <param name="dto">Dados manuais da linha de melhoria.</param>
        /// <returns>Linha criada com o identificador persistido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpPost("linhas-frm")]
        public async Task<IActionResult> CreateLinhaFrm(int fichaId, [FromBody] CreateFichaFrmLinhaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados invalidos para a linha FRM."));

            var created = await _service.CreateLinhaFrmAsync(fichaId, dto);
            return CreatedAtAction(nameof(GetLinhasFrm), new { fichaId }, created);
        }

        /// <summary>
        /// Lista as linhas manuais de uma ficha FRA.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FRA.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Pagina com as linhas manuais da ficha FRA.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("linhas-fra")]
        public async Task<IActionResult> GetLinhasFra(int fichaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser maiores ou iguais a 1."));

            return Ok(await _service.GetLinhasFraAsync(fichaId, page, pageSize));
        }

        /// <summary>
        /// Adiciona uma nova linha manual a uma ficha FRA.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FRA.</param>
        /// <param name="dto">Dados manuais da linha de alteracao.</param>
        /// <returns>Linha criada com o identificador persistido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpPost("linhas-fra")]
        public async Task<IActionResult> CreateLinhaFra(int fichaId, [FromBody] CreateFichaFraLinhaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados invalidos para a linha FRA."));

            var created = await _service.CreateLinhaFraAsync(fichaId, dto);
            return CreatedAtAction(nameof(GetLinhasFra), new { fichaId }, created);
        }

        /// <summary>
        /// Lista as linhas manuais de uma ficha FOP.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FOP.</param>
        /// <param name="page">Pagina pedida pelo consumidor.</param>
        /// <param name="pageSize">Quantidade maxima de registos por pagina.</param>
        /// <returns>Pagina com as linhas manuais da ficha FOP.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("linhas-fop")]
        public async Task<IActionResult> GetLinhasFop(int fichaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser maiores ou iguais a 1."));

            return Ok(await _service.GetLinhasFopAsync(fichaId, page, pageSize));
        }

        /// <summary>
        /// Adiciona uma nova linha manual a uma ficha FOP.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha FOP.</param>
        /// <param name="dto">Dados manuais da linha de ocorrencia.</param>
        /// <returns>Linha criada com o identificador persistido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpPost("linhas-fop")]
        public async Task<IActionResult> CreateLinhaFop(int fichaId, [FromBody] CreateFichaFopLinhaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados invalidos para a linha FOP."));

            var created = await _service.CreateLinhaFopAsync(fichaId, dto);
            return CreatedAtAction(nameof(GetLinhasFop), new { fichaId }, created);
        }
    }
}
