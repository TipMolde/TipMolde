using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TipMolde.Application.Interface.Producao.IIndustrial;

namespace TipMolde.API.HostedServices
{
    /// <summary>
    /// Processa em segundo plano os eventos industriais que ja foram persistidos como recebidos.
    /// </summary>
    public sealed class IndustrialEventosRecebidosWorker : BackgroundService
    {
        private static readonly TimeSpan IntervaloProcessamento = TimeSpan.FromSeconds(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<IndustrialEventosRecebidosWorker> _logger;

        public IndustrialEventosRecebidosWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<IndustrialEventosRecebidosWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(IntervaloProcessamento);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessarEventosRecebidosAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao processar eventos industriais recebidos em segundo plano.");
                }

                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
        }

        private async Task ProcessarEventosRecebidosAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IIndustrialProducaoService>();
            await service.ProcessarEventosRecebidosAsync(cancellationToken);
        }
    }
}
