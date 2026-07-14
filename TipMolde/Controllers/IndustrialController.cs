using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.IndustrialProducaoDto;
using TipMolde.Application.Interface.Producao.IIndustrial;
using TipMolde.Application.Interface.Producao.IMaquina;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Endpoints de comunicacao entre middleware industrial, backend e frontend de producao.
    /// </summary>
    [ApiController]
    [Route("api/industrial")]
    public class IndustrialController : ControllerBase
    {
        private readonly IIndustrialProducaoService _industrialProducaoService;
        private readonly IMaquinaService _maquinaService;
        private readonly ILogger<IndustrialController> _logger;

        public IndustrialController(
            IIndustrialProducaoService industrialProducaoService,
            IMaquinaService maquinaService,
            ILogger<IndustrialController> logger)
        {
            _industrialProducaoService = industrialProducaoService;
            _maquinaService = maquinaService;
            _logger = logger;
        }

        /// <summary>
        /// Lista o catalogo tecnico de maquinas para sincronizacao do middleware industrial.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("machines")]
        public async Task<IActionResult> GetMachines([FromQuery] int page = 1, [FromQuery] int pageSize = 250)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Page e pageSize devem ser maiores ou iguais a 1."));

            var result = await _maquinaService.GetAllAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Recebe eventos normalizados enviados pelo middleware industrial.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("events")]
        public async Task<IActionResult> ReceiveEvents([FromBody] IEnumerable<IndustrialTelemetryDto> eventos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Dados de telemetria industrial invalidos."));
            }

            var result = await _industrialProducaoService.ProcessarTelemetriaAsync(eventos);

            _logger.LogInformation(
                "Controller: telemetria industrial recebida. Recebidos={Recebidos}, Processados={Processados}, Pendentes={Pendentes}, Resolvidos={Resolvidos}, Ignorados={Ignorados}.",
                result.Recebidos,
                result.Processados,
                result.Pendentes,
                result.Resolvidos,
                result.Ignorados);

            return Ok(result);
        }

        /// <summary>
        /// Lista eventos industriais que ainda precisam de intervencao do utilizador.
        /// </summary>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("eventos/pendentes")]
        public async Task<IActionResult> GetPendentes([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Page e pageSize devem ser maiores ou iguais a 1."));

            var result = await _industrialProducaoService.GetEventosPendentesAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Obtem a acao manual pendente mais relevante para uma maquina.
        /// </summary>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("maquinas/{maquinaId:int}/evento-pendente")]
        public async Task<IActionResult> GetEventoPendenteMaquina(int maquinaId)
        {
            var result = await _industrialProducaoService.GetEventoPendenteMaquinaAsync(maquinaId);
            return result is null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// Obtem a sessao industrial ativa de uma maquina para apresentar a peca atualmente em producao.
        /// </summary>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("maquinas/{maquinaId:int}/sessao-ativa")]
        public async Task<IActionResult> GetSessaoAtiva(int maquinaId)
        {
            var result = await _industrialProducaoService.GetSessaoAtivaAsync(maquinaId);
            return result is null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// Completa o contexto de um evento RUNNING pendente.
        /// </summary>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpPost("eventos/{eventoId:int}/completar-contexto")]
        public async Task<IActionResult> CompletarContexto(
            int eventoId,
            [FromBody] CompletarContextoEventoIndustrialDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Dados invalidos para completar contexto industrial."));
            }

            var result = await _industrialProducaoService.CompletarContextoAsync(eventoId, dto);
            return Ok(result);
        }

        /// <summary>
        /// Confirma se uma paragem STOPPED terminou o trabalho ou apenas pausou a producao.
        /// </summary>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpPost("eventos/{eventoId:int}/confirmar-paragem")]
        public async Task<IActionResult> ConfirmarParagem(
            int eventoId,
            [FromBody] ConfirmarParagemIndustrialDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Dados invalidos para confirmar paragem industrial."));
            }

            var result = await _industrialProducaoService.ConfirmarParagemAsync(eventoId, dto);
            return Ok(result);
        }
    }
}
