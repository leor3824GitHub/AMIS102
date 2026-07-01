using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Multitenancy.Contracts.Dtos;
using AMIS.Modules.Multitenancy.Contracts.v1.GetTenants;
using AMIS.Framework.Shared.Multitenancy;

namespace AMIS.Modules.Multitenancy.Contracts;

public interface ITenantService
{
    Task<PagedResponse<TenantDto>> GetAllAsync(GetTenantsQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Returns every tenant's full <see cref="AppTenantInfo"/> (including connection string) read
    /// directly from the tenant registry. Use for background/cross-tenant work that must set the
    /// multi-tenant context per tenant; unlike the Finbuckle store's GetAllAsync this is reliable
    /// regardless of whether a non-enumerable distributed-cache store is configured.
    /// </summary>
    Task<IReadOnlyList<AppTenantInfo>> GetAllTenantInfosAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsWithIdAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default);

    Task<TenantStatusDto> GetStatusAsync(string id, CancellationToken cancellationToken = default);

    Task<string> CreateAsync(string id, string name, string? connectionString, string adminEmail, string? issuer, CancellationToken cancellationToken);

    Task<string> ActivateAsync(string id, CancellationToken cancellationToken);

    Task<string> DeactivateAsync(string id, CancellationToken cancellationToken = default);

    Task<DateTime> UpgradeSubscriptionAsync(string id, DateTime extendedExpiryDate, CancellationToken cancellationToken = default);

    Task MigrateTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken);

    Task SeedTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken);
}

