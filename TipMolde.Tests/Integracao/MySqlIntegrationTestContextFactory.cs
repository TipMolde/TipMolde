using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TipMolde.Infrastructure.DB;

namespace TipMolde.Tests.Integracao;

/// <summary>
/// Cria contextos EF Core ligados a uma base MySQL dedicada para smoke tests de integracao.
/// </summary>
internal static class MySqlIntegrationTestContextFactory
{
    private const string TestConnectionEnvironmentVariable = "TIPMOLDE_TEST_DB_CONNECTION";
    private static readonly Lazy<string> BaseConnectionString = new(ResolveConnectionString);

    public static ApplicationDbContext CreateContext()
    {
        var connectionString = BaseConnectionString.Value;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .EnableDetailedErrors()
            .Options;

        var context = new ApplicationDbContext(options);

        return context;
    }

    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(TestConnectionEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        var fromUserSecrets = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromUserSecrets))
        {
            return fromUserSecrets;
        }

        throw new InvalidOperationException(
            $"Nao foi encontrada uma connection string para testes. Define a variavel '{TestConnectionEnvironmentVariable}' " +
            "ou configura 'ConnectionStrings:DefaultConnection' nos User Secrets do projeto TipMolde.API.");
    }
}
