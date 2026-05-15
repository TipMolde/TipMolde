using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza um endpoint leve para diagnostico rapido da API.
    /// </summary>
    [ApiController]
    [Route("api/health")]
    public sealed class HealthController : ControllerBase
    {
        /// <summary>
        /// Devolve o estado atual da API e o instante UTC da resposta.
        /// </summary>
        /// <returns>Objeto simples para validacao de conectividade entre cliente e servidor.</returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok",
                timestampUtc = DateTime.UtcNow
            });
        }
    }
}
