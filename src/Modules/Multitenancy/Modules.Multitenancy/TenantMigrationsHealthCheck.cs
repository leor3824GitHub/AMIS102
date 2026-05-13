using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Multitenancy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AMIS.Modules.Multitenancy;

public sealed class TenantMigrationsHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TenantMigrationsHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();

        // Read tenants directly from the tenant database instead of IMultiTenantStore.GetAllAsync(),
        // since distributed cache stores may not implement enumeration.
        var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var tenants = await tenantDbContext.Set<AppTenantInfo>()
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var details = new Dictionary<string, object>();

        foreach (var tenant in tenants)
        {
            try
            {
                using IServiceScope tenantScope = scope.ServiceProvider.CreateScope();

                tenantScope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
                    .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

                var dbContext = tenantScope.ServiceProvider.GetRequiredService<TenantDbContext>();

                var pendingMigrations = await dbContext.Database
                    .GetPendingMigrationsAsync(cancellationToken)
                    .ConfigureAwait(false);

                bool hasPending = pendingMigrations.Any();

                details[tenant.Id] = new
                {
                    tenant.Name,
                    tenant.IsActive,
                    tenant.ValidUpto,
                    HasPendingMigrations = hasPending,
                    PendingMigrations = pendingMigrations.ToArray()
                };
            }
            catch (Exception ex)
            {
                details[tenant.Id] = new
                {
                    tenant.Name,
                    tenant.IsActive,
                    tenant.ValidUpto,
                    Error = ex.Message
                };
            }
        }

        return HealthCheckResult.Healthy("Tenant migrations status collected.", details);
    }
}

