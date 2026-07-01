using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.AssetRegister.Data.Services;
using AMIS.Modules.Multitenancy.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.AssetRegister.Provisioning;

/// <summary>
/// Hangfire recurring job that posts monthly COA straight-line depreciation for all PPE assets.
/// Each tenant is processed inside its own DI scope with the multi-tenant context set, so Finbuckle's
/// tenant enforcement, query filters, and per-tenant connection strings all apply normally — saving
/// across tenants in a single context is rejected by Finbuckle, so we must loop tenant-by-tenant.
/// Runs monthly; safe to re-run — already-posted months are skipped and any missed months are caught
/// up on the next run. A failure in one tenant is logged and does not abort the remaining tenants.
/// </summary>
public sealed class DepreciationRecurringJob(
    IServiceScopeFactory scopeFactory,
    ITenantService tenantService,
    ILogger<DepreciationRecurringJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var period = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var tenants = await tenantService.GetAllTenantInfosAsync(cancellationToken).ConfigureAwait(false);

        foreach (var tenant in tenants)
        {
            if (!tenant.IsActive)
                continue;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                // Set the tenant context before resolving the DbContext so its query filters and
                // connection string bind to this tenant.
                scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                    .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

                var service = scope.ServiceProvider.GetRequiredService<DepreciationPostingService>();
                var result = await service.PostThroughAsync(period, cancellationToken).ConfigureAwait(false);

                if (result.EntriesPosted > 0 && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "DepreciationRecurringJob: tenant {TenantId} — posted {Entries} depreciation entries across {Assets} PPE asset(s) for {Period:yyyy-MM}.",
                        tenant.Id, result.EntriesPosted, result.AssetsProcessed, result.Period);
                }
            }
            catch (Exception ex)
            {
                // Isolate per-tenant failures so one bad tenant doesn't abort depreciation for the rest.
                logger.LogError(ex,
                    "DepreciationRecurringJob: failed to post depreciation for tenant {TenantId}.", tenant.Id);
            }
        }
    }
}
