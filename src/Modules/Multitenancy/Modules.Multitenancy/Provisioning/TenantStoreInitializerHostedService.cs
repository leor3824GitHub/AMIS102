using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.Multitenancy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AMIS.Modules.Multitenancy.Provisioning;

/// <summary>
/// Initializes the tenant catalog database and seeds the root tenant plus the demo tenants on startup.
/// </summary>
public sealed class TenantStoreInitializerHostedService : IHostedService
{
    /// <summary>
    /// Demo tenants seeded alongside root. Each carries its own admin email; the shared
    /// <see cref="MultitenancyConstants.DefaultPassword"/> applies to all of them. Identity tables are
    /// <c>.IsMultiTenant()</c>, so each tenant gets its own isolated admin user row and the
    /// <c>tenant</c> header/claim still selects which one you log into.
    /// </summary>
    private static readonly (string Id, string Name, string AdminEmail)[] DemoTenants =
    [
        ("sdsbo", "SDS BO", "leor3824@gmail.com"),
        ("adsbo", "ADS BO", "leor3824@yahoo.com")
    ];

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantStoreInitializerHostedService> _logger;

    public TenantStoreInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<TenantStoreInitializerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        await tenantDbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Applied tenant catalog migrations.");

        await SeedTenantAsync(
            tenantDbContext,
            MultitenancyConstants.Root.Id,
            MultitenancyConstants.Root.Name,
            MultitenancyConstants.Root.EmailAddress,
            cancellationToken).ConfigureAwait(false);

        foreach (var (id, name, adminEmail) in DemoTenants)
        {
            await SeedTenantAsync(tenantDbContext, id, name, adminEmail, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SeedTenantAsync(
        TenantDbContext tenantDbContext,
        string id,
        string name,
        string adminEmail,
        CancellationToken cancellationToken)
    {
        if (await tenantDbContext.TenantInfo.FindAsync([id], cancellationToken).ConfigureAwait(false) is not null)
        {
            return;
        }

        var tenant = new AppTenantInfo(
            id,
            name,
            string.Empty,
            adminEmail,
            issuer: MultitenancyConstants.Root.Issuer);

        tenant.SetValidity(DateTime.UtcNow.AddYears(1));
        await tenantDbContext.TenantInfo.AddAsync(tenant, cancellationToken).ConfigureAwait(false);
        await tenantDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Seeded tenant '{TenantId}' ({TenantName}).", id, name);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

