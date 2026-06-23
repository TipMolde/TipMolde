using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TipMolde.API.Extensions;
using TipMolde.Application.Dtos.OcorrenciaDto;
using TipMolde.Application.Interface.Ocorrencias;

namespace TipMolde.API.Controllers
{
    /// <summary>
    /// Disponibiliza endpoints HTTP para o registo independente de ocorrencias e correcoes.
    /// </summary>
    [ApiController]
    [Route("api/ocorrencias")]
    public class OcorrenciasController : ControllerBase
    {
        private readonly IOcorrenciasService _ocorrenciasService;

        public OcorrenciasController(IOcorrenciasService ocorrenciasService)
        {
            _ocorrenciasService = ocorrenciasService;
        }

        [Authorize(Roles = "ADMIN,GESTOR_PRODUCAO")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOcorrenciaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(this.CreateProblem(StatusCodes.Status400BadRequest, "Pedido invalido", "Dados invalidos para o registo de ocorrencia."));

            var created = await _ocorrenciasService.CreateAsync(dto);
            return Created("/api/ocorrencias", created);
        }
    }
}
