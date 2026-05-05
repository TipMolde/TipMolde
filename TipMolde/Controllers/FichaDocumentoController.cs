using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.API.Requests;
using TipMolde.Application.Dtos.FichaDocumentoDto;
using TipMolde.Application.Interface.Fichas.IFichaDocumento;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Exponibiliza endpoints HTTP para upload, consulta e download dos documentos oficiais das fichas de producao.
    /// </summary>
    /// <remarks>
    /// Estes endpoints tratam artefactos documentais de qualidade e, por isso,
    /// exigem autorizacao explicita, rastreabilidade e um contrato HTTP sem exposicao
    /// de metadados internos de armazenamento.
    /// </remarks>
    [ApiController]
    [Route("api/fichas")]
    [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
    public class FichaDocumentoController : ControllerBase
    {
        private readonly IFichaDocumentoService _service;
        private readonly ILogger<FichaDocumentoController> _logger;

        /// <summary>
        /// Construtor de FichaDocumentoController.
        /// </summary>
        /// <param name="service">Servico responsavel pelos casos de uso documentais da ficha.</param>
        /// <param name="logger">Logger usado para rastreabilidade operacional dos endpoints documentais.</param>
        public FichaDocumentoController(
            IFichaDocumentoService service,
            ILogger<FichaDocumentoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Cria uma nova versao documental a partir de um upload manual.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha dona do documento.</param>
        /// <param name="request">Payload multipart contendo o ficheiro submetido pelo utilizador autenticado.</param>
        /// <returns>DTO seguro com os metadados da versao criada.</returns>
        [HttpPost("{fichaId:int}/documentos/upload")]
        public async Task<IActionResult> Upload(int fichaId, [FromForm] UploadFichaDocumentoRequest request)
        {
            if (!this.TryGetAuthenticatedUserId(out var userId, out var errorResult))
                return errorResult!;

            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Dados invalidos para o upload documental."));
            }

            _logger.LogInformation("Upload documental recebido para a ficha {FichaId}.", fichaId);

            await using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);

            var dto = new UploadFichaDocumentoDto
            {
                Content = ms.ToArray(),
                FileName = request.File.FileName,
                ContentType = request.File.ContentType
            };

            var response = await _service.UploadAsync(fichaId, dto, userId);
            return Ok(response);
        }

        /// <summary>
        /// Lista as versoes documentais de uma ficha de producao.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha.</param>
        /// <param name="page">Numero da pagina a ser retornada.</param>
        /// <param name="pageSize">Numero de itens por pagina.</param>
        /// <returns>Colecao de versoes documentais sem metadados internos do armazenamento.</returns>
        [HttpGet("{fichaId:int}/documentos")]
        public async Task<IActionResult> Listar(
            int fichaId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Page e pageSize devem ser maiores ou iguais a 1."));
            }

            var response = await _service.ListarAsync(fichaId, page, pageSize);
            return Ok(response);
        }

        /// <summary>
        /// Descarrega um documento pertencente a uma ficha especifica.
        /// </summary>
        /// <param name="fichaId">Identificador da ficha que contextualiza o acesso.</param>
        /// <param name="documentoId">Identificador do documento a descarregar.</param>
        /// <returns>Stream HTTP do ficheiro quando o documento existe e pertence a ficha.</returns>
        [HttpGet("{fichaId:int}/documentos/{documentoId:int}/download")]
        public async Task<IActionResult> Download(int fichaId, int documentoId)
        {
            var result = await _service.DownloadAsync(fichaId, documentoId);
            return File(result.Content, result.TipoFicheiro, result.FileName);
        }
    }
}
