using Microsoft.Extensions.DependencyInjection;
using TipMolde.Application.Interface.Comercio.ICliente;
using TipMolde.Application.Interface.Comercio.IEncomenda;
using TipMolde.Application.Interface.Comercio.IEncomendaMolde;
using TipMolde.Application.Interface.Comercio.IFornecedor;
using TipMolde.Application.Interface.Comercio.IPedidoMaterial;
using TipMolde.Application.Interface.Desenho.IProjeto;
using TipMolde.Application.Interface.Desenho.IRegistoTempoProjeto;
using TipMolde.Application.Interface.Desenho.IRevisao;
using TipMolde.Application.Interface.Fichas.IFichaProducao;
using TipMolde.Application.Interface.Ocorrencias;
using TipMolde.Application.Interface.Producao.IFasesProducao;
using TipMolde.Application.Interface.Producao.IIndustrial;
using TipMolde.Application.Interface.Producao.IMaquina;
using TipMolde.Application.Interface.Producao.IMolde;
using TipMolde.Application.Interface.Producao.IPeca;
using TipMolde.Application.Interface.Producao.IRegistosProducao;
using TipMolde.Application.Interface.Utilizador.IUser;
using TipMolde.Application.Mappings;
using TipMolde.Application.Service;

namespace TipMolde.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IEncomendaService, EncomendaService>();
        services.AddScoped<IMoldeService, MoldeService>();
        services.AddScoped<IFasesProducaoService, FasesProducaoService>();
        services.AddScoped<IPecaService, PecaService>();
        services.AddScoped<IRegistosProducaoService, RegistosProducaoService>();
        services.AddScoped<IFornecedorService, FornecedorService>();
        services.AddScoped<IPedidoMaterialService, PedidoMaterialService>();
        services.AddScoped<IEncomendaMoldeService, EncomendaMoldeService>();
        services.AddScoped<IPrioridadeGlobalMoldeService, PrioridadeGlobalMoldeService>();
        services.AddScoped<IMaquinaService, MaquinaService>();
        services.AddScoped<IIndustrialProducaoService, IndustrialProducaoService>();
        services.AddScoped<IProjetoService, ProjetoService>();
        services.AddScoped<IRevisaoService, RevisaoService>();
        services.AddScoped<IRegistoTempoProjetoService, RegistoTempoProjetoService>();
        services.AddScoped<IFichaProducaoService, FichaProducaoService>();
        services.AddScoped<IOcorrenciasService, OcorrenciasService>();

        services.AddAutoMapper(typeof(UserProfile).Assembly);

        return services;
    }
}
