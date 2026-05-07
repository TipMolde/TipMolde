using Microsoft.AspNetCore.Mvc;
using TipMolde.Application.Exceptions;

namespace TipMolde.API.Middleware
{
    /// <summary>
    /// Interceta excecoes nao tratadas e converte-as em respostas HTTP normalizadas.
    /// </summary>
    /// <remarks>
    /// Este middleware protege o contrato externo da API ao transformar excecoes tecnicas e de negocio
    /// em <see cref="ProblemDetails"/> com codigo de estado, detalhe e identificador de rastreio.
    /// </remarks>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        /// <summary>
        /// Construtor de ExceptionMiddleware.
        /// </summary>
        /// <param name="next">Delegate do proximo middleware no pipeline HTTP.</param>
        /// <param name="logger">Logger usado para registar a excecao e o trace do pedido.</param>
        /// <param name="env">Ambiente atual usado para controlar a exposicao de detalhe tecnico.</param>
        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Executa o middleware e converte excecoes nao tratadas em respostas ProblemDetails.
        /// </summary>
        /// <remarks>
        /// Fluxo critico:
        /// 1. Executa o restante pipeline HTTP.
        /// 2. Regista a excecao com o trace identifier do pedido.
        /// 3. Mapeia a excecao para um codigo de estado funcional.
        /// 4. Esconde detalhes tecnicos em producao para erros de servidor.
        /// 5. Devolve uma resposta `application/problem+json`.
        /// </remarks>
        /// <param name="context">Contexto HTTP atual a proteger e a preencher com a resposta de erro.</param>
        /// <returns>Tarefa assincrona que representa o processamento do pedido no pipeline.</returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro nao tratado. TraceId: {TraceId}", context.TraceIdentifier);

                var (status, title) = ex switch
                {
                    BusinessConflictException => (StatusCodes.Status409Conflict, "Conflito de negocio"),
                    ArgumentException => (StatusCodes.Status400BadRequest, "Pedido invalido"),
                    KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso nao encontrado"),
                    UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Nao autorizado"),
                    _ => (StatusCodes.Status500InternalServerError, "Erro interno")
                };

                var detail = status >= 500 && !_env.IsDevelopment()
                    ? "Ocorreu um erro ao processar o pedido."
                    : ex.Message;

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = detail,
                    Instance = context.Request.Path,
                    Type = $"https://httpstatuses.com/{status}"
                };

                problem.Extensions["traceId"] = context.TraceIdentifier;

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            }
        }
    }
}
