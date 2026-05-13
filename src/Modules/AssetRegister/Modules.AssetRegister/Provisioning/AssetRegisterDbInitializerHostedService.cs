using FSH.Framework.Persistence;
using FSH.Modules.AssetRegister.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.AssetRegister.Provisioning;

internal sealed class AssetRegisterDbInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AssetRegisterDbInitializerHostedService> _logger;

    public AssetRegisterDbInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<AssetRegisterDbInitializerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Asset Register module initializer starting.");
        using var scope = _serviceProvider.CreateScope();

        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>()
            .OfType<AssetRegisterDbInitializer>()
            .ToList();

        if (initializers.Count == 0)
        {
            _logger.LogInformation("No database initializers found for Asset Register module.");
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
