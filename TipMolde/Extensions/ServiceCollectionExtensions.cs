using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TipMolde.Application.Interface.Utilizador.IAuth;
using TipMolde.Infrastructure.Settings;

namespace TipMolde.API.Extensions;

/// <summary>
/// Reune a configuracao transversal da camada API no contentor de injecao de dependencias.
/// </summary>
/// <remarks>
/// Regista controllers, Swagger, ProblemDetails e a autenticacao JWT usada pelos endpoints.
/// Tambem valida a configuracao minima de seguranca antes do arranque da aplicacao.
/// </remarks>
public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicyName = "Frontend";

    /// <summary>
    /// Regista os servicos base da API e configura autenticacao/autorizacao.
    /// </summary>
    /// <remarks>
    /// Fluxo critico:
    /// 1. Regista infraestrutura HTTP comum da API.
    /// 2. Carrega e valida a configuracao JWT obrigatoria.
    /// 3. Configura validacao do token e verificacao de revogacao.
    /// 4. Ativa autorizacao para os endpoints protegidos.
    /// </remarks>
    /// <param name="services">Colecao de servicos usada para registar dependencias da API.</param>
    /// <param name="configuration">Configuracao da aplicacao usada para ler opcoes JWT.</param>
    /// <returns>Colecao de servicos encadeada com a configuracao da camada API aplicada.</returns>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter());
            });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddProblemDetails();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicyName, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else if (environment.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(_ => true);
                }

                policy.AllowAnyHeader();
                policy.AllowAnyMethod();
            });
        });

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Secao Jwt nao configurada.");

        if (string.IsNullOrWhiteSpace(jwt.SecretKey) || jwt.SecretKey.Length < 32)
            throw new InvalidOperationException("Jwt:SecretKey deve ter pelo menos 32 caracteres.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var repo = context.HttpContext.RequestServices.GetRequiredService<IRevokedTokenRepository>();
                        var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                        if (string.IsNullOrWhiteSpace(jti) || await repo.IsRevokedAsync(jti))
                            context.Fail("Token revogado.");
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
