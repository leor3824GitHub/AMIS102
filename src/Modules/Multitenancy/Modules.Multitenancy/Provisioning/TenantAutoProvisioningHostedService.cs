using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Shared.Multitenancy;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.Multitenancy.Provisioning;

public sealed class TenantAutoProvisioningHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantAutoProvisioningHostedService> _logger;
    private readonly MultitenancyOptions _options;

    public TenantAutoProvisioningHostedService(
        IServiceProvider serviceProvider,
        ILogger<TenantAutoProvisioningHostedService> logger,
        IOptions<MultitenancyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!ShouldRunProvisioning())
        {
            return;
        }

        if (!JobStorageAvailable())
        {
            if (_options.RunTenantMigrationsOnStartup)
            {
                _logger.LogWarning("Hangfire storage not initialized; running startup provisioning inline because RunTenantMigrationsOnStartup is enabled.");
                await ProvisionTenantsAsync(cancellationToken);
                return;
            }

            _logger.LogWarning("Hangfire storage not initialized; skipping auto-provisioning enqueue.");
            return;
        }

        await ProvisionTenantsAsync(cancellationToken);
    }

    private bool ShouldRunProvisioning() =>
        _options.AutoProvisionOnStartup || _options.RunTenantMigrationsOnStartup;

    private async Task ProvisionTenantsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();

        var tenants = await GetAllTenantsSafeAsync(scope.ServiceProvider).ConfigureAwait(false);

        foreach (var tenant in tenants)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await TryProvisionTenantAsync(provisioning, tenant, cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<AppTenantInfo>> GetAllTenantsSafeAsync(IServiceProvider serviceProvider)
    {
        var stores = serviceProvider.GetServices<IMultiTenantStore<AppTenantInfo>>().ToList();
        foreach (var store in stores)
        {
            try
            {
                var tenants = await store.GetAllAsync().ConfigureAwait(false);
                return tenants as IReadOnlyCollection<AppTenantInfo> ?? tenants.ToList();
            }
            catch (NotImplementedException)
            {
                _logger.LogDebug("Skipping tenant store {StoreType} because GetAllAsync is not implemented.", store.GetType().Name);
            }
        }

        _logger.LogWarning("No tenant store with GetAllAsync implementation was available during auto-provisioning startup.");
        return Array.Empty<AppTenantInfo>();
    }

    private async Task TryProvisionTenantAsync(ITenantProvisioningService provisioning, AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        try
        {
            if (await ShouldProvisionTenantAsync(provisioning, tenant.Id, cancellationToken))
            {
                await provisioning.StartAsync(tenant.Id, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Enqueued provisioning for tenant {TenantId} on startup.", tenant.Id);
            }
        }
        catch (CustomException ex)
        {
            _logger.LogInformation("Provisioning already in progress or recently queued for tenant {TenantId}: {Message}", tenant.Id, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue provisioning for tenant {TenantId}", tenant.Id);
        }
    }

    private async Task<bool> ShouldProvisionTenantAsync(ITenantProvisioningService provisioning, string tenantId, CancellationToken cancellationToken)
    {
        if (_options.RunTenantMigrationsOnStartup)
        {
            return true;
        }

        var latest = await provisioning.GetLatestAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return latest is null || latest.Status != TenantProvisioningStatus.Completed;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool JobStorageAvailable()
    {
        try
        {
            _ = JobStorage.Current;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

