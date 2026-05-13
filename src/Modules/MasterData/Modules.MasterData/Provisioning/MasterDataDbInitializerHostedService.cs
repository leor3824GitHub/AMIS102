using AMIS.Framework.Persistence;
using AMIS.Modules.MasterData.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.MasterData.Provisioning;

/// <summary>
/// Initializes the MasterData module database schema and seeds data on app startup.
/// Runs migrations for the MasterData database before any tenant operations.
/// </summary>
internal sealed class MasterDataDbInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MasterDataDbInitializerHostedService> _logger;

    public MasterDataDbInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<MasterDataDbInitializerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>()
            .OfType<MasterDataDbInitializer>()
            .ToList();

        if (initializers.Count == 0)
        {
            _logger.LogInformation("No database initializers found for MasterData module.");
            return;
        }

        foreach (var initializer in initializers)
        {
            try
            {
                // Apply migrations for the module DB. Per-tenant seeding is handled by the multitenancy provisioning
                // (TenantAutoProvisioningHostedService / TenantService) to ensure a tenant context is set.
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

