using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Interface.Relatorios;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Exponibiliza as exportacoes oficiais e indicadores do modulo de relatorios.
    /// </summary>
    /// <remarks>
    /// Este controller centraliza as respostas HTTP das exportacoes documentais para garantir
    /// uniformidade de contrato, logging minimo e rastreabilidade operacional.
    /// </remarks>
    [ApiController]
    [Route("api/fichas-producao")]
    public class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _relatorioService;
        private readonly ILogger<RelatorioController> _logger;

        /// <summary>
        /// Construtor de RelatorioController.
        /// </summary>
        /// <param name="relatorioService">Servico aplicacional responsavel pela geracao dos relatorios.</param>
        /// <param name="logger">Logger usado para rastrear exportacoes documentais.</param>
        public RelatorioController(
            IRelatorioService relatorioService,
            ILogger<RelatorioController> logger)
        {
            _relatorioService = relatorioService;
            _logger = logger;
        }

        /// <summary>
        /// Exporta a ficha FLT oficial.
        /// </summary>
        /// <remarks>
        /// A FLT e gerada diretamente a partir do contexto Encomenda-Molde e nao de uma ficha editavel persistida.
        /// </remarks>
        /// <param name="encomendaMoldeId">Identificador da relacao Encomenda-Molde usada como contexto da FLT.</param>
        /// <returns>Ficheiro Excel oficial da ficha FLT.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("encomendas-moldes/{encomendaMoldeId:int}/export/flt")]
        [HttpGet("encomendas-moldes/{encomendaMoldeId:int}/export-FLT")]
        public Task<IActionResult> ExportFLT(int encomendaMoldeId) =>
            ExportFichaAsync(encomendaMoldeId, "FLT", _relatorioService.GerarFichaExcelFLTAsync);

        /// <summary>
        /// Exporta a ficha FRE oficial.
        /// </summary>
        /// <param name="id">Identificador interno da ficha de producao.</param>
        /// <returns>Ficheiro Excel versionado da ficha FRE.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id:int}/export/fre")]
        [HttpGet("{id:int}/export-FRE")]
        public Task<IActionResult> ExportFRE(int id) =>
            ExportFichaAsync(id, "FRE", _relatorioService.GerarFichaExcelFREAsync);

        /// <summary>
        /// Exporta a ficha FRM oficial.
        /// </summary>
        /// <param name="id">Identificador interno da ficha de producao.</param>
        /// <returns>Ficheiro Excel versionado da ficha FRM.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id:int}/export/frm")]
        [HttpGet("{id:int}/export-FRM")]
        public Task<IActionResult> ExportFRM(int id) =>
            ExportFichaAsync(id, "FRM", _relatorioService.GerarFichaExcelFRMAsync);

        /// <summary>
        /// Exporta a ficha FRA oficial.
        /// </summary>
        /// <param name="id">Identificador interno da ficha de producao.</param>
        /// <returns>Ficheiro Excel versionado da ficha FRA.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id:int}/export/fra")]
        [HttpGet("{id:int}/export-FRA")]
        public Task<IActionResult> ExportFRA(int id) =>
            ExportFichaAsync(id, "FRA", _relatorioService.GerarFichaExcelFRAAsync);

        /// <summary>
        /// Exporta a ficha FOP oficial.
        /// </summary>
        /// <param name="id">Identificador interno da ficha de producao.</param>
        /// <returns>Ficheiro Excel versionado da ficha FOP.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{id:int}/export/fop")]
        [HttpGet("{id:int}/export-FOP")]
        public Task<IActionResult> ExportFOP(int id) =>
            ExportFichaAsync(id, "FOP", _relatorioService.GerarFichaExcelFOPAsync);

        /// <summary>
        /// Executa o fluxo comum de exportacao de uma ficha oficial.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Resolve o utilizador autenticado a partir do token.
        /// 2. Executa a geracao do artefacto.
        /// 3. Regista logging minimo para auditoria operacional.
        /// 4. Devolve o ficheiro com o nome final versionado.
        /// </remarks>
        /// <param name="fichaId">Identificador interno da ficha.</param>
        /// <param name="tipoFicha">Codigo funcional da ficha exportada.</param>
        /// <param name="exportFunc">Funcao de geracao especifica para o tipo de ficha.</param>
        /// <returns>Resposta HTTP com o ficheiro gerado.</returns>
        private async Task<IActionResult> ExportFichaAsync(
            int fichaId,
            string tipoFicha,
            Func<int, int, Task<(byte[] Content, string FileName)>> exportFunc)
        {
            if (!this.TryGetAuthenticatedUserId(out var userId, out var errorResult))
                return errorResult!;

            var result = await exportFunc(fichaId, userId);

            _logger.LogInformation(
                "Exportacao {TipoFicha} gerada para ficha {FichaId} pelo utilizador {UserId}",
                tipoFicha,
                fichaId,
                userId);

            return File(
                result.Content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName);
        }
    }
}
