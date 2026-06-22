using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.FichaProducaoDto;
using TipMolde.Application.Dtos.RegistoProducaoDto;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Application.Interface.Producao.IRegistosProducao;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Domain.Enums;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para a feature RegistosProducao.
    /// </summary>
    /// <remarks>
    /// O controller valida input HTTP, devolve respostas ProblemDetails quando aplicavel
    /// e delega regras de negocio ao servico de aplicacao.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class RegistosProducaoController : ControllerBase
    {
        private readonly IRegistosProducaoService _registosProducaoService;
        private readonly IFichaProducaoService _fichaProducaoService;
        private readonly IPecaService _pecaService;
        private readonly ILogger<RegistosProducaoController> _logger;

        /// <summary>
        /// Construtor de RegistosProducaoController.
        /// </summary>
        /// <param name="registosProducaoService">Servico responsavel pelos casos de uso da feature.</param>
        /// <param name="logger">Logger para rastreabilidade das operacoes HTTP.</param>
        public RegistosProducaoController(
            IRegistosProducaoService registosProducaoService,
            IFichaProducaoService fichaProducaoService,
            IPecaService pecaService,
            ILogger<RegistosProducaoController> logger)
        {
            _registosProducaoService = registosProducaoService;
            _fichaProducaoService = fichaProducaoService;
            _pecaService = pecaService;
            _logger = logger;
        }

        /// <summary>
        /// Lista registos de producao com paginacao.
        /// </summary>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com resultado paginado; HTTP 400 para paginacao invalida.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Page e pageSize devem ser maiores ou iguais a 1."));

            var result = await _registosProducaoService.GetAllAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Obtem um registo de producao por ID.
        /// </summary>
        /// <param name="id">Identificador unico do registo de producao.</param>
        /// <returns>HTTP 200 com o registo; HTTP 404 quando nao encontrado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var registoProducao = await _registosProducaoService.GetByIdAsync(id);
            if (registoProducao == null)
            {
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    $"Registo de producao com ID {id} nao encontrado."));
            }

            return Ok(registoProducao);
        }

        /// <summary>
        /// Obtem o historico de producao de uma peca numa fase.
        /// </summary>
        /// <param name="faseId">Identificador da fase de producao.</param>
        /// <param name="pecaId">Identificador da peca.</param>
        /// <param name="page">Pagina atual.</param>
        /// <param name="pageSize">Tamanho da pagina.</param>
        /// <returns>HTTP 200 com o historico encontrado.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("historico")]
        public async Task<IActionResult> GetHistorico([FromQuery] int faseId, [FromQuery] int pecaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Page e pageSize devem ser maiores ou iguais a 1."));

            var historico = await _registosProducaoService.GetHistoricoAsync(faseId, pecaId, page, pageSize);
            return Ok(historico);
        }

        /// <summary>
        /// Obtem o ultimo registo de producao de uma peca numa fase.
        /// </summary>
        /// <param name="faseId">Identificador da fase de producao.</param>
        /// <param name="pecaId">Identificador da peca.</param>
        /// <returns>HTTP 200 com o ultimo registo; HTTP 404 quando nao existe historico.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpGet("ultimo")]
        public async Task<IActionResult> GetUltimo([FromQuery] int faseId, [FromQuery] int pecaId)
        {
            var registo = await _registosProducaoService.GetUltimoRegistoAsync(faseId, pecaId);
            if (registo == null)
            {
                return NotFound(this.CreateProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso nao encontrado",
                    "Nao existe historico de producao para a fase e peca indicadas."));
            }

            return Ok(registo);
        }

        /// <summary>
        /// Cria um novo registo de producao.
        /// </summary>
        /// <remarks>
        /// A criacao pode alterar o estado operacional da maquina associada,
        /// mas essa alteracao e persistida pela camada de aplicacao numa transacao unica.
        /// </remarks>
        /// <param name="dto">Dados de criacao do registo de producao.</param>
        /// <returns>HTTP 201 com o registo criado; HTTP 400 quando o body e invalido.</returns>
        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRegistosProducaoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "Dados de criacao invalidos para o registo de producao."));
            }

            if (!string.IsNullOrWhiteSpace(dto.Ocorrencia) && !dto.EncomendaMolde_id.HasValue)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Pedido invalido",
                    "A ocorrencia precisa de um contexto Encomenda-Molde."));
            }

            var createdRegistoProducao = await _registosProducaoService.CreateAsync(dto);

            if (!string.IsNullOrWhiteSpace(dto.Ocorrencia))
            {
                var peca = await _pecaService.GetByIdAsync(createdRegistoProducao.Peca_id)
                    ?? throw new KeyNotFoundException($"Peca com ID {createdRegistoProducao.Peca_id} nao encontrada.");

                var fichaFop = await _fichaProducaoService.EnsureAsync(new CreateFichaProducaoDto
                {
                    Tipo = TipoFicha.FOP,
                    EncomendaMolde_id = dto.EncomendaMolde_id!.Value
                });

                await _fichaProducaoService.CreateLinhaFopAsync(
                    fichaFop.FichaProducao_id,
                    new CreateFichaFopLinhaDto
                    {
                        Data = createdRegistoProducao.Data_hora,
                        Ocorrencia = dto.Ocorrencia.Trim(),
                        Responsavel_id = createdRegistoProducao.Operador_id,
                        Peca_id = createdRegistoProducao.Peca_id,
                        Molde_id = peca.Molde_id
                    });
            }

            _logger.LogInformation(
                "Controller: registo de producao {RegistoId} criado.",
                createdRegistoProducao.Registo_Producao_id);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdRegistoProducao.Registo_Producao_id },
                createdRegistoProducao);
        }
    }
}
