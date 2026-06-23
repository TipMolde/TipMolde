using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http.Headers;
using TipMolde.Application.Interface.Comercio.ICliente;
using TipMolde.Application.Interface.Comercio.IEncomenda;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Comercio.IFornecedor;
using TipMolde.Application.Interface.Comercio.IPedidoMaterial;
using TipMolde.Application.Interface.Desenho.IProjeto;
using TipMolde.Application.Interface.Desenho.IRegistoTempoProjeto;
using TipMolde.Application.Interface.Desenho.IRevisao;
using TipMolde.Application.Interface.Fichas.IFichaDocumento;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Application.Interface.Ocorrencias;
using TipMolde.Application.Interface.Producao.IFasesProducao;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Producao.IRegistosProducao;
using TipMolde.Application.Interface.Relatorios;
using TipMolde.Application.Interface.Utilizador.IAuth;
using TipMolde.Application.Interface.Utilizador.IUser;

namespace TipMolde.Tests.Integracao.Controller;

/// <summary>
/// Cria uma API em memoria para validar controllers pelo contrato HTTP.
/// </summary>
/// <remarks>
/// A fixture substitui os servicos de aplicacao por mocks e preserva routing,
/// filtros, autorizacao, model binding, middleware e serializacao reais.
/// </remarks>
public sealed class ControllerIntegrationTestFactory : WebApplicationFactory<Program>
{
    public Mock<IClienteService> ClienteService { get; } = new();
    public Mock<IEncomendaService> EncomendaService { get; } = new();
    public Mock<IEncomendaMoldeService> EncomendaMoldeService { get; } = new();
    public Mock<IFasesProducaoService> FasesProducaoService { get; } = new();
    public Mock<IFornecedorService> FornecedorService { get; } = new();
    public Mock<IMaquinaService> MaquinaService { get; } = new();
    public Mock<IMoldeService> MoldeService { get; } = new();
    public Mock<IPecaService> PecaService { get; } = new();
    public Mock<IPedidoMaterialService> PedidoMaterialService { get; } = new();
    public Mock<IProjetoService> ProjetoService { get; } = new();
    public Mock<IRegistoTempoProjetoService> RegistoTempoProjetoService { get; } = new();
    public Mock<IRegistosProducaoService> RegistosProducaoService { get; } = new();
    public Mock<IRevisaoService> RevisaoService { get; } = new();
    public Mock<IFichaDocumentoService> FichaDocumentoService { get; } = new();
    public Mock<IFichaProducaoService> FichaProducaoService { get; } = new();
    public Mock<IOcorrenciasService> OcorrenciasService { get; } = new();
    public Mock<IRelatorioService> RelatorioService { get; } = new();
    public Mock<IAuthService> AuthService { get; } = new();
    public Mock<IUserManagementService> UserManagementService { get; } = new();
    public Mock<IPasswordService> PasswordService { get; } = new();

    /// <summary>
    /// Limpa o estado dos mocks para impedir acoplamento entre cenarios.
    /// </summary>
    public void ResetMocks()
    {
        ClienteService.Reset();
        EncomendaService.Reset();
        EncomendaMoldeService.Reset();
        FasesProducaoService.Reset();
        FornecedorService.Reset();
        MaquinaService.Reset();
        MoldeService.Reset();
        PecaService.Reset();
        PedidoMaterialService.Reset();
        ProjetoService.Reset();
        RegistoTempoProjetoService.Reset();
        RegistosProducaoService.Reset();
        RevisaoService.Reset();
        FichaDocumentoService.Reset();
        FichaProducaoService.Reset();
        OcorrenciasService.Reset();
        RelatorioService.Reset();
        AuthService.Reset();
        UserManagementService.Reset();
        PasswordService.Reset();
    }

    /// <summary>
    /// Configura o host de teste com autenticacao e doubles de aplicacao.
    /// </summary>
    /// <param name="builder">Builder do host ASP.NET Core usado pela WebApplicationFactory.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            foreach (var jsonSource in configurationBuilder.Sources.OfType<JsonConfigurationSource>())
                jsonSource.ReloadOnChange = false;

            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Port=3306;Database=tipmolde_controller_tests;Uid=root;Pwd=placeholder;",
                ["Jwt:Issuer"] = "TipMolde.Api.Tests",
                ["Jwt:Audience"] = "TipMolde.Client.Tests",
                ["Jwt:SecretKey"] = "ControllerTestsSecretKey_With_At_Least_32_Chars",
                ["Jwt:ExpirationMinutes"] = "60",
                ["Storage:FichasRootPath"] = "Storage/Fichas",
                ["Storage:UploadsRootPath"] = "Storage/Uploads",
                ["Templates:RootPath"] = "Templates"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddDataProtection()
                .UseEphemeralDataProtectionProvider();

            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });

            services.ReplaceScoped(ClienteService.Object);
            services.ReplaceScoped(EncomendaService.Object);
            services.ReplaceScoped(EncomendaMoldeService.Object);
            services.ReplaceScoped(FasesProducaoService.Object);
            services.ReplaceScoped(FornecedorService.Object);
            services.ReplaceScoped(MaquinaService.Object);
            services.ReplaceScoped(MoldeService.Object);
            services.ReplaceScoped(PecaService.Object);
            services.ReplaceScoped(PedidoMaterialService.Object);
            services.ReplaceScoped(ProjetoService.Object);
            services.ReplaceScoped(RegistoTempoProjetoService.Object);
            services.ReplaceScoped(RegistosProducaoService.Object);
            services.ReplaceScoped(RevisaoService.Object);
            services.ReplaceScoped(FichaDocumentoService.Object);
            services.ReplaceScoped(FichaProducaoService.Object);
            services.ReplaceScoped(OcorrenciasService.Object);
            services.ReplaceScoped(RelatorioService.Object);
            services.ReplaceScoped(AuthService.Object);
            services.ReplaceScoped(UserManagementService.Object);
            services.ReplaceScoped(PasswordService.Object);
        });
    }
}

internal static class ControllerIntegrationServiceCollectionExtensions
{
    public static void ReplaceScoped<TService>(this IServiceCollection services, TService implementation)
        where TService : class
    {
        services.RemoveAll<TService>();
        services.AddScoped(_ => implementation);
    }
}

internal static class ControllerIntegrationHttpClientExtensions
{
    public static void AuthenticateAs(this HttpClient client, string userId = "1", params string[] roles)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthorizationValue);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles.Length == 0 ? "ADMIN" : string.Join(',', roles));
    }
}
