using AMIS.Framework.Persistence;
using AMIS.Modules.AssetProcurement.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetProcurement.Provisioning;

internal sealed class AssetProcurementDbInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AssetProcurementDbInitializerHostedService> _logger;

    public AssetProcurementDbInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<AssetProcurementDbInitializerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Asset Procurement module initializer starting.");
        using var scope = _serviceProvider.CreateScope();

        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>()
            .OfType<AssetProcurementDbInitializer>()
            .ToList();

        if (initializers.Count == 0)
        {
            _logger.LogInformation("No database initializers found for Asset Procurement module.");
            return;
        }

        foreach (var initializer in initializers)
        {
            try
            {
                // Apply migrations only. Per-tenant seeding is handled by the multitenancy provisioning
                // (TenantService) to ensure a tenant context is set.
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

