using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.Application.Interface.Relatorios;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP de relatorio e dashboard associados ao ciclo de vida do molde.
    /// </summary>
    [ApiController]
    [Route("api/moldes/{moldeId:int}")]
    public class MoldeRelatorioController : ControllerBase
    {
        private readonly IRelatorioService _relatorioService;
        private readonly ILogger<MoldeRelatorioController> _logger;

        /// <summary>
        /// Construtor de MoldeRelatorioController.
        /// </summary>
        /// <param name="relatorioService">Servico responsavel pela geracao de relatorios do molde.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes HTTP.</param>
        public MoldeRelatorioController(
            IRelatorioService relatorioService,
            ILogger<MoldeRelatorioController> logger)
        {
            _relatorioService = relatorioService;
            _logger = logger;
        }

        /// <summary>
        /// Exporta o ciclo de vida do molde para PDF.
        /// </summary>
        /// <param name="moldeId">Identificador interno do molde.</param>
        /// <returns>Ficheiro PDF gerado para o molde indicado.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("ciclo-vida-pdf")]
        public async Task<IActionResult> ExportCicloVidaPdf(int moldeId)
        {
            var result = await _relatorioService.GerarCicloVidaMoldePdfAsync(moldeId);

            _logger.LogInformation("PDF de ciclo de vida exportado para o molde {MoldeId}", moldeId);

            return File(result.Content, "application/pdf", result.FileName);
        }

        /// <summary>
        /// Mostra o dashboard do ciclo de vida do molde.
        /// </summary>
        /// <param name="moldeId">Identificador interno do molde.</param>
        /// <returns>Dados do dashboard do ciclo de vida do molde.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("dashboard-ciclo-vida")]
        public async Task<IActionResult> GetDashboardCicloVida(int moldeId)
        {
            var result = await _relatorioService.ObterDashboardMoldeAsync(moldeId);
            _logger.LogInformation("Dashboard de ciclo de vida consultado para o molde {MoldeId}", moldeId);
            return Ok(result);
        }
    }
}
