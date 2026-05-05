using Microsoft.AspNetCore.Mvc;

namespace TipMolde.API.Extensions
{
    public static class ControllerProblemDetailsExtensions
    {
        /// <summary>
        /// Cria um objeto de erro padrao no formato ProblemDetails.
        /// </summary>
        /// <param name="status">Codigo de estado HTTP da resposta.</param>
        /// <param name="title">Titulo curto do problema.</param>
        /// <param name="detail">Descricao detalhada do problema.</param>
        /// <returns>Instancia de ProblemDetails preenchida com o contexto do pedido.</returns>
        public static ProblemDetails CreateProblem(
            this ControllerBase controller,
            int status,
            string title,
            string detail)
        {
            return new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = controller.HttpContext?.Request?.Path
            };
        }
    }
}
