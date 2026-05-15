using TipMolde.API.Extensions;
using TipMolde.API.Middleware;
using TipMolde.Application;
using TipMolde.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiServices(builder.Configuration, builder.Environment)
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(TipMolde.API.Extensions.ServiceCollectionExtensions.FrontendCorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

/// <summary>
/// Exposicao parcial da classe Program para suporte a testes de integracao e bootstrap controlado da API.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Impede instanciacao externa da classe de entrada mantendo o tipo acessivel para testes.
    /// </summary>
    protected Program()
    {
    }
}
