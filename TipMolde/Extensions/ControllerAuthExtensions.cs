using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TipMolde.API.Extensions
{
    /// <summary>
    /// Centraliza helpers de autenticacao comuns aos controllers da API.
    /// </summary>
    public static class ControllerAuthExtensions
    {
        /// <summary>
        /// Resolve o identificador do utilizador autenticado a partir dos claims do token.
        /// </summary>
        /// <param name="controller">Controller atual.</param>
        /// <param name="userId">Identificador do utilizador quando o token e valido.</param>
        /// <param name="errorResult">Resposta HTTP a devolver quando o token nao contem um utilizador valido.</param>
        /// <returns>True quando foi possivel resolver o utilizador autenticado.</returns>
        public static bool TryGetAuthenticatedUserId(
            this ControllerBase controller,
            out int userId,
            out IActionResult? errorResult)
        {
            var claimValue =
                controller.User.FindFirst("id")?.Value ??
                controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(claimValue, out userId))
            {
                errorResult = null;
                return true;
            }

            errorResult = controller.Unauthorized(controller.CreateProblem(
                StatusCodes.Status401Unauthorized,
                "Token invalido",
                "Nao foi possivel determinar o utilizador autenticado."));

            return false;
        }

        /// <summary>
        /// Resolve o identificador do utilizador autenticado a partir das claims do token.
        /// </summary>
        /// <remarks>
        /// Suporta emissores JWT diferentes que podem preencher o identificador em "sub"
        /// ou em <see cref="ClaimTypes.NameIdentifier"/>.
        /// </remarks>
        /// <param name="controller">Controller atual que contem o contexto HTTP autenticado.</param>
        /// <returns>ID do utilizador autenticado.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Lancada quando o token nao contem um identificador de utilizador valido.
        /// </exception>
        public static int GetAuthenticatedUserId(this ControllerBase controller)
        {
            var sub =
                controller.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                controller.User.FindFirst("id")?.Value;

            if (!int.TryParse(sub, out var userId))
                throw new UnauthorizedAccessException("Token invalido ou utilizador nao identificado.");

            return userId;
        }
    }
}
