using FSH.Framework.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.AssetProcurement.Provisioning;

internal sealed class AssetProcurementDbInitializerHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<AssetProcurementDbInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Asset Procurement module initializer starting.");
        using var scope = scopeFactory.CreateScope();
        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>();
        foreach (var init in initializers)
        {
            await init.MigrateAsync(cancellationToken).ConfigureAwait(false);
            await init.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
