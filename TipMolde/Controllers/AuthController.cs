using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.AuthDto;
using TipMolde.Application.Interface.Utilizador.IAuth;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints de autenticacao e encerramento de sessao.
    /// </summary>
    /// <remarks>
    /// Atua como camada de entrada HTTP, delegando regras de autenticacao no servico de aplicacao.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Construtor de AuthController.
        /// </summary>
        /// <param name="authService">Servico responsavel pelas operacoes de login e logout.</param>
        /// <param name="logger">Logger para auditoria operacional de autenticacao.</param>
        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Autentica um utilizador com base em email e password.
        /// </summary>
        /// <remarks>
        /// Valida o pedido HTTP e delega a verificacao de credenciais para o servico de autenticacao.
        /// Em caso de falha devolve resposta de nao autorizado com detalhe funcional.
        /// </remarks>
        /// <param name="dto">Credenciais enviadas pelo cliente para autenticacao.</param>
        /// <returns>Resultado HTTP com token de acesso quando as credenciais sao validas.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _authService.LoginAsync(dto.Email, dto.Password);

                _logger.LogInformation("Login bem-sucedido para utilizador com email {Email}", dto.Email);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Tentativa de login falhada para email {Email}", dto.Email);
                return Unauthorized(this.CreateProblem(
                    StatusCodes.Status401Unauthorized,
                    "Credenciais invalidas",
                    "Email ou password incorretos."));
            }
        }

        /// <summary>
        /// Termina a sessao do utilizador autenticado e invalida o token atual.
        /// </summary>
        /// <remarks>
        /// Extrai o token do cabecalho Authorization e delega a revogacao para o servico.
        /// Quando a operacao falha devolve erro funcional com detalhes do motivo.
        /// </remarks>
        /// <returns>Resultado HTTP com estado de sucesso ou erro de logout.</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(LogoutResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout()
        {
            var authHeader = Request.Headers.Authorization.ToString();
            var result = await _authService.LogoutAsync(authHeader);

            if (!result.Success)
            {
                return BadRequest(this.CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "Logout invalido",
                    result.Message));
            }

            _logger.LogInformation("Utilizador terminou sessao");
            return Ok(result);
        }
    }
}
