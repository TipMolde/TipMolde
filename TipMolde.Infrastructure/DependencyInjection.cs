using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TipMolde.Application.Interface.Comercio.ICliente;
using TipMolde.Application.Interface.Comercio.IEncomenda;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Comercio.IFornecedor;
using TipMolde.Application.Interface.Comercio.IPedidoMaterial;
using TipMolde.Application.Interface.Comercio.IPedidoMaterial.IItemPedidoMaterial;
using TipMolde.Application.Interface.Desenho.IProjeto;
using TipMolde.Application.Interface.Desenho.IRegistoTempoProjeto;
using TipMolde.Application.Interface.Desenho.IRevisao;
using TipMolde.Application.Interface.Fichas.IFichaDocumento;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Application.Interface.Producao.IFasesProducao;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Producao.IRegistosProducao;
using TipMolde.Application.Interface.Relatorios;
using TipMolde.Application.Interface.Utilizador.IAuth;
using TipMolde.Application.Interface.Utilizador.ISecurity;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Application.Service;
using TipMolde.Infrastructure.DB;
using TipMolde.Infrastructure.Repositorio;
using TipMolde.Infrastructure.Service;
using TipMolde.Infrastructure.Settings;

namespace TipMolde.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.FichasRootPath), "Storage:FichasRootPath e obrigatorio.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.UploadsRootPath), "Storage:UploadsRootPath e obrigatorio.")
            .ValidateOnStart();

        services.AddOptions<TemplateOptions>()
            .Bind(configuration.GetSection(TemplateOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.RootPath), "Templates:RootPath e obrigatorio.")
            .ValidateOnStart();

        if (environment.IsEnvironment("Testing"))
        {
            services.AddDbContext<ApplicationDbContext>();
        }
        else
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' nao configurada.");
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        }

        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IEncomendaRepository, EncomendaRepository>();
        services.AddScoped<IMoldeRepository, MoldeRepository>();
        services.AddScoped<IFasesProducaoRepository, FasesProducaoRepository>();
        services.AddScoped<IPecaRepository, PecaRepository>();
        services.AddScoped<IRegistosProducaoRepository, RegistosProducaoRepository>();
        services.AddScoped<IFornecedorRepository, FornecedorRepository>();
        services.AddScoped<IPedidoMaterialRepository, PedidoMaterialRepository>();
        services.AddScoped<IItemPedidoMaterialRepository, ItemPedidoMaterialRepository>();
        services.AddScoped<IEncomendaMoldeRepository, EncomendaMoldeRepository>();
        services.AddScoped<IMaquinaRepository, MaquinaRepository>();
        services.AddScoped<IRevokedTokenRepository, RevokedTokenRepository>();
        services.AddScoped<IProjetoRepository, ProjetoRepository>();
        services.AddScoped<IRevisaoRepository, RevisaoRepository>();
        services.AddScoped<IRegistoTempoProjetoRepository, RegistoTempoProjetoRepository>();
        services.AddScoped<IFichaProducaoRepository, FichaProducaoRepository>();
        services.AddScoped<IRelatorioRepository, RelatorioRepository>();
        services.AddScoped<IFichaDocumentoRepository, FichaDocumentoRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IRelatorioService, RelatorioService>();
        services.AddScoped<IFichaDocumentoService, FichaDocumentoService>();
        services.AddScoped<IMoldeImageService, MoldeImageService>();
        services.AddScoped<IFichaDocumentoStorage, FichaDocumentoFileStorage>();
        services.AddScoped<IFichaDocumentoUnitOfWork, FichaDocumentoUnitOfWork>();

        return services;
    }
}
