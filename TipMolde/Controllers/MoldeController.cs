using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.MoldeDto;
using TipMolde.Application.Interface.Producao.IMolde;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para a feature Molde.
    /// </summary>
    /// <remarks>
    /// O controller valida input HTTP e delega regras de negocio ao servico.
    /// </remarks>
    [ApiController]
    [Route("api/moldes")]
    public class MoldeController : ControllerBase
    {
        private const string PedidoInvalido = "Pedido invalido";
        private const string RecursoNaoEncontrado = "Recurso nao encontrado";

        private readonly IMoldeService _moldeService;
        private readonly ILogger<MoldeController> _logger;

        /// <summary>
        /// Construtor de MoldeController.
        /// </summary>
        /// <param name="moldeService">Servico responsavel pelos casos de uso da feature Molde.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes HTTP.</param>
        public MoldeController(
            IMoldeService moldeService,
            ILogger<MoldeController> logger)
        {
            _moldeService = moldeService;
            _logger = logger;
        }

        /// <summary>
        /// Lista moldes com paginacao.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = await _moldeService.GetAllAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Lista moldes que possuem associacao Encomenda-Molde.
        /// </summary>
        /// <remarks>
        /// Este endpoint serve a pagina de relatorios para apresentar apenas os moldes
        /// que fazem parte de uma encomenda e suportam pesquisa por campos funcionais.
        /// </remarks>
        /// <param name="searchTerm">Termo opcional para filtrar numero, nome ou numero do cliente.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("com-encomenda")]
        public async Task<IActionResult> GetComEncomenda(
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var result = await _moldeService.GetComEncomendaAsync(searchTerm, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Obtem um molde por ID.
        /// </summary>
        /// <param name="id">Identificador interno do molde.</param>
        /// <returns>HTTP 200 com o molde; HTTP 404 quando nao encontrado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var molde = await _moldeService.GetByIdAsync(id);
            if (molde == null)
                return NotFound(this.CreateProblem(StatusCodes.Status404NotFound, RecursoNaoEncontrado, $"Molde com ID {id} nao encontrado."));

            return Ok(molde);
        }

        /// <summary>
        /// Lista moldes associados a uma encomenda.
        /// </summary>
        /// <param name="encomendaId">Identificador da encomenda.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpGet("por-encomenda/{encomendaId:int}")]
        public async Task<IActionResult> GetByEncomendaId(
            int encomendaId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Page e pageSize devem ser >= 1."));

            var moldes = await _moldeService.GetByEncomendaIdAsync(encomendaId, page, pageSize);
            return Ok(moldes);
        }

        /// <summary>
        /// Obtem um molde pelo numero funcional.
        /// </summary>
        /// <param name="numero">Numero funcional do molde.</param>
        /// <returns>HTTP 200 com o molde; HTTP 400 quando o numero e invalido; HTTP 404 quando nao encontrado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL,GESTOR_DESENHO,GESTOR_PRODUCAO")]
        [HttpGet("por-numero")]
        public async Task<IActionResult> GetByNumero([FromQuery] string? numero)
        {
            if (string.IsNullOrWhiteSpace(numero))
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Numero do molde e obrigatorio."));

            var molde = await _moldeService.GetByNumeroAsync(numero);
            if (molde == null)
                return NotFound(this.CreateProblem(StatusCodes.Status404NotFound, RecursoNaoEncontrado, $"Molde com numero '{numero.Trim()}' nao encontrado."));
            return Ok(molde);
        }

        /// <summary>
        /// Cria um novo molde.
        /// </summary>
        /// <remarks>
        /// O contrato cria o agregado Molde apenas com as respetivas especificacoes tecnicas.
        /// A ligacao a encomendas e feita posteriormente pela feature EncomendaMolde.
        /// </remarks>
        /// <param name="dto">Dados de criacao do molde.</param>
        /// <returns>HTTP 201 com o molde criado; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Create([FromBody] CreateMoldeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de criacao invalidos."));

            var created = await _moldeService.CreateAsync(dto);

            _logger.LogInformation("Controller: Molde {MoldeId} criado", created.MoldeId);

            return CreatedAtAction(nameof(GetById), new { id = created.MoldeId }, created);
        }

        /// <summary>
        /// Cria um novo molde com a imagem enviada no mesmo request.
        /// </summary>
        /// <param name="dto">Dados de criacao do molde.</param>
        /// <param name="imagemCapa">Imagem opcional enviada pelo utilizador.</param>
        /// <returns>HTTP 201 com o molde criado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_COMERCIAL")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateMoldeDto dto, [FromForm] IFormFile? imagemCapa = null)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de criacao invalidos."));

            byte[]? imagemContent = null;
            string? imagemFileName = null;

            if (imagemCapa is not null && imagemCapa.Length > 0)
            {
                await using var memory = new MemoryStream();
                await imagemCapa.CopyToAsync(memory);
                imagemContent = memory.ToArray();
                imagemFileName = imagemCapa.FileName;
            }

            var created = await _moldeService.CreateAsync(dto, imagemContent, imagemFileName);

            _logger.LogInformation("Controller: Molde {MoldeId} criado com imagem opcional", created.MoldeId);

            return CreatedAtAction(nameof(GetById), new { id = created.MoldeId }, created);
        }

        /// <summary>
        /// Atualiza parcialmente um molde.
        /// </summary>
        /// <remarks>
        /// Campos nao enviados sao preservados no registo atual.
        /// </remarks>
        /// <param name="id">Identificador do molde a atualizar.</param>
        /// <param name="dto">Dados de atualizacao parcial.</param>
        /// <returns>HTTP 204 quando a atualizacao e concluida; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_DESENHO")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMoldeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "Dados de criacao invalidos."));

            await _moldeService.UpdateAsync(id, dto);

            _logger.LogInformation("Controller: Molde {MoldeId} atualizado", id);

            return NoContent();
        }

        /// <summary>
        /// Atualiza a imagem de capa de um molde.
        /// </summary>
        /// <param name="id">Identificador do molde.</param>
        /// <param name="file">Imagem enviada pelo utilizador.</param>
        /// <returns>HTTP 200 com o molde atualizado.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{id:int}/imagem-capa")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateImagemCapa(int id, IFormFile? file = null)
        {
            if (file is null || file.Length == 0)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, PedidoInvalido, "A imagem de capa e obrigatoria."));

            await using var memory = new MemoryStream();
            await file.CopyToAsync(memory);

            var updated = await _moldeService.UpdateImagemCapaAsync(id, memory.ToArray(), file.FileName);

            _logger.LogInformation("Controller: imagem de capa do Molde {MoldeId} atualizada", id);

            return Ok(updated);
        }

        /// <summary>
        /// Remove um molde.
        /// </summary>
        /// <param name="id">Identificador do molde a remover.</param>
        /// <returns>HTTP 204 quando a remocao e concluida.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _moldeService.DeleteAsync(id);

            _logger.LogInformation("Controller: Molde {MoldeId} removido", id);

            return NoContent();
        }
    }
}
