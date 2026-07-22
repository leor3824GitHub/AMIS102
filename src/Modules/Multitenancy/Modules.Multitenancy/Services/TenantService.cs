using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Multitenancy.Contracts.Dtos;
using AMIS.Modules.Multitenancy.Contracts.v1.GetTenants;
using AMIS.Modules.Multitenancy.Data;
using AMIS.Modules.Multitenancy.Features.v1.GetTenants;
using AMIS.Modules.Multitenancy.Provisioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AMIS.Modules.Multitenancy.Services;

public sealed class TenantService : ITenantService
{
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly DatabaseOptions _config;
    private readonly IServiceProvider _serviceProvider;
    private readonly TenantDbContext _dbContext;
    private readonly ITenantProvisioningService _provisioningService;

    public TenantService(
        IMultiTenantStore<AppTenantInfo> tenantStore,
        IOptions<DatabaseOptions> config,
        IServiceProvider serviceProvider,
        TenantDbContext dbContext,
        ITenantProvisioningService provisioningService)
    {
        ArgumentNullException.ThrowIfNull(config);
        _tenantStore = tenantStore;
        _config = config.Value;
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
        _provisioningService = provisioningService;
    }

    public async Task<string> ActivateAsync(string id, CancellationToken cancellationToken)
    {
        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);

        if (tenant.IsActive)
        {
            throw new CustomException($"tenant {id} is already activated");
        }

        await _provisioningService.EnsureCanActivateAsync(id, cancellationToken).ConfigureAwait(false);

        tenant.Activate();

        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);

        return $"tenant {id} is now activated";
    }

    public async Task<string> CreateAsync(string id,
        string name,
        string? connectionString,
        string adminEmail, string? issuer, CancellationToken cancellationToken,
        Guid? officeId = null, string? officeCode = null)
    {
        if (connectionString?.Trim() == _config.ConnectionString.Trim())
        {
            connectionString = string.Empty;
        }

        AppTenantInfo tenant = new(id, name, connectionString, adminEmail, issuer);

        if (officeId.HasValue && officeId.Value != Guid.Empty)
        {
            tenant.LinkOffice(officeId.Value, officeCode);
        }

        await _tenantStore.AddAsync(tenant).ConfigureAwait(false);

        return tenant.Id;
    }

    public async Task<AppTenantInfo?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return await _dbContext.TenantInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AppTenantInfo?> FindByOfficeIdAsync(Guid officeId, CancellationToken cancellationToken = default)
    {
        if (officeId == Guid.Empty)
        {
            return null;
        }

        return await _dbContext.TenantInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.OfficeId == officeId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task MigrateTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
        {
            await initializer.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SeedTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
        {
            await initializer.SeedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string> DeactivateAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);
        if (!tenant.IsActive)
        {
            throw new CustomException($"tenant {id} is already deactivated");
        }

        int tenantCount = (await _tenantStore.GetAllAsync().ConfigureAwait(false)).Count(t => t.IsActive);
        if (tenantCount <= 1)
        {
            throw new CustomException("At least one active tenant is required.");
        }

        if (tenant.Id.Equals(MultitenancyConstants.Root.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new CustomException("The root tenant cannot be deactivated.");
        }

        tenant.Deactivate();
        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
        return $"tenant {id} is now deactivated";
    }

    public async Task<bool> ExistsWithIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _tenantStore.GetAsync(id).ConfigureAwait(false) is not null;

    public async Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default) =>
        (await _tenantStore.GetAllAsync().ConfigureAwait(false)).Any(t => t.Name == name);

    public async Task<PagedResponse<TenantDto>> GetAllAsync(GetTenantsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<AppTenantInfo> tenants = _dbContext.TenantInfo;
        var specification = new GetTenantsSpecification(query);
        IQueryable<TenantDto> projected = tenants.ApplySpecification(specification);

        return await projected
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AppTenantInfo>> GetAllTenantInfosAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.TenantInfo
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<TenantStatusDto> GetStatusAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);

        return new TenantStatusDto
        {
            Id = tenant.Id!,
            Name = tenant.Name!,
            IsActive = tenant.IsActive,
            ValidUpto = tenant.ValidUpto,
            HasConnectionString = !string.IsNullOrWhiteSpace(tenant.ConnectionString),
            AdminEmail = tenant.AdminEmail!,
            Issuer = tenant.Issuer
        };
    }

    public async Task<DateTime> UpgradeSubscriptionAsync(string id, DateTime extendedExpiryDate, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantInfoAsync(id, cancellationToken).ConfigureAwait(false);

        // Ensure the date is UTC for PostgreSQL compatibility
        var utcExpiryDate = extendedExpiryDate.Kind == DateTimeKind.Utc
            ? extendedExpiryDate
            : DateTime.SpecifyKind(extendedExpiryDate, DateTimeKind.Utc);

        tenant.SetValidity(utcExpiryDate);
        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);
        return tenant.ValidUpto;
    }

    /// <summary>
    /// Deliberately goes through <see cref="TenantDbContext"/> rather than the Finbuckle store. With
    /// <c>UseDistributedCacheStore</c> enabled the store is a cache: reading it misses any tenant the
    /// current session has not resolved, and writing to it would update the cache entry while leaving the
    /// registry table — the system of record this link is read back from — untouched.
    /// </summary>
    public async Task<AppTenantInfo> LinkOfficeAsync(string id, Guid officeId, string? officeCode, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.TenantInfo
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"{typeof(AppTenantInfo).Name} {id} Not Found.");

        var claimant = await FindByOfficeIdAsync(officeId, cancellationToken).ConfigureAwait(false);
        if (claimant is not null && !string.Equals(claimant.Id, tenant.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new CustomException($"Office is already linked to tenant '{claimant.Id}'.");
        }

        tenant.LinkOffice(officeId, officeCode);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Best-effort refresh so a cached copy of this tenant does not keep serving the old office.
        // Routing always re-reads the registry, so a cache miss here is harmless.
        await _tenantStore.UpdateAsync(tenant).ConfigureAwait(false);

        return tenant;
    }

    private async Task<AppTenantInfo> GetTenantInfoAsync(string id, CancellationToken cancellationToken = default) =>
        await _tenantStore.GetAsync(id).ConfigureAwait(false)
            ?? throw new NotFoundException($"{typeof(AppTenantInfo).Name} {id} Not Found.");
}

