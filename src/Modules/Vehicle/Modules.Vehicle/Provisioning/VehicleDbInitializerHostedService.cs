using FSH.Framework.Persistence;
using FSH.Modules.Vehicle.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Vehicle.Provisioning;

internal sealed class VehicleDbInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VehicleDbInitializerHostedService> _logger;

    public VehicleDbInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<VehicleDbInitializerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>()
            .OfType<VehicleDbInitializer>()
            .ToList();

        if (initializers.Count == 0)
        {
            _logger.LogInformation("No database initializers found for Vehicle module.");
            return;
        }

        foreach (var initializer in initializers)
        {
            try
            {
                await initializer.MigrateAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Applied migrations for {InitializerType}.", initializer.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating database for {InitializerType}.", initializer.GetType().Name);
                throw;
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
