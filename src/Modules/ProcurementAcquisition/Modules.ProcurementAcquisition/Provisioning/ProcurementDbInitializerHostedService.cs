using AMIS.Framework.Persistence;
using AMIS.Modules.ProcurementAcquisition.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.ProcurementAcquisition.Provisioning;

/// <summary>
/// Initializes the ProcurementAcquisition module database schema on app startup.
/// Runs migrations before any tenant operations.
/// </summary>
internal sealed class ProcurementDbInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcurementDbInitializerHostedService> _logger;

    public ProcurementDbInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<ProcurementDbInitializerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>()
            .OfType<ProcurementDbInitializer>()
            .ToList();

        if (initializers.Count == 0)
        {
            _logger.LogInformation("No database initializers found for ProcurementAcquisition module.");
            return;
        }

        foreach (var initializer in initializers)
        {
            try
            {
                await initializer.MigrateAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Applied migrations for {InitializerType}. Seeding will run per-tenant.", initializer.GetType().Name);
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

