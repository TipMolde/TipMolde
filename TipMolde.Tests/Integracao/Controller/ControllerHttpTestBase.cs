using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;

namespace TipMolde.Tests.Integracao.Controller;

/// <summary>
/// Base comum dos testes de integracao HTTP dos controllers.
/// </summary>
/// <remarks>
/// Centraliza criacao do host, autenticacao de teste e leitura de ProblemDetails
/// para manter cada cenario focado no contrato REST validado.
/// </remarks>
public abstract class ControllerHttpTestBase
{
    protected ControllerIntegrationTestFactory Factory = null!;
    protected HttpClient Client = null!;

    [SetUp]
    public void SetUpBase()
    {
        // ARRANGE
        Factory = new ControllerIntegrationTestFactory();
        Client = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        Client.AuthenticateAs("1", "ADMIN");
    }

    [TearDown]
    public void TearDownBase()
    {
        Client?.Dispose();
        Factory?.Dispose();
    }

    protected static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        return problem!;
    }

    protected static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode statusCode,
        string title)
    {
        response.StatusCode.Should().Be(statusCode);

        var problem = await ReadProblemAsync(response);
        problem.Status.Should().Be((int)statusCode);
        problem.Title.Should().Be(title);
    }
}
