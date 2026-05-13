using AMIS.Framework.Persistence;
using AMIS.Modules.Expendable.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Expendable.Provisioning;

/// <summary>
/// Initializes the Expendable module database schema and seeds data on app startup.
/// Runs migrations for the shared Expendable database before any tenant operations.
/// </summary>
internal sealed class ExpendableDbInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpendableDbInitializerHostedService> _logger;

    public ExpendableDbInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<ExpendableDbInitializerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>()
            .OfType<ExpendableDbInitializer>()
            .ToList();

        if (initializers.Count == 0)
        {
            _logger.LogInformation("No database initializers found for Expendable module.");
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

