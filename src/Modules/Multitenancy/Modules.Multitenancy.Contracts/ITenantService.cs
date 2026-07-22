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

    /// <summary>
    /// Finds the tenant that represents the given MasterData office, or null when no tenant claims it.
    /// <para>
    /// This is how a recipient employee is resolved to a destination agency for inter-agency property
    /// transfers: <c>EmployeeProfile.OfficeId</c> → the tenant whose <c>AppTenantInfo.OfficeId</c> matches.
    /// A unique index guarantees at most one tenant per office.
    /// </para>
    /// </summary>
    /// <summary>
    /// Reads a tenant straight from the tenant registry table.
    /// <para>
    /// Use this rather than <see cref="ExistsWithIdAsync"/> for administrative work on an arbitrary tenant:
    /// when <c>UseDistributedCacheStore</c> is on, the Finbuckle store is a cache that only knows tenants
    /// already resolved by an incoming request, so it reports "not found" for a perfectly real tenant the
    /// current session has never touched.
    /// </para>
    /// </summary>
    Task<AppTenantInfo?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<AppTenantInfo?> FindByOfficeIdAsync(Guid officeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Points an existing tenant at the MasterData office it represents. Throws when another tenant
    /// already claims that office.
    /// </summary>
    Task<AppTenantInfo> LinkOfficeAsync(string id, Guid officeId, string? officeCode, CancellationToken cancellationToken = default);

    Task<string> CreateAsync(string id, string name, string? connectionString, string adminEmail, string? issuer, CancellationToken cancellationToken, Guid? officeId = null, string? officeCode = null);

    Task<string> ActivateAsync(string id, CancellationToken cancellationToken);

    Task<string> DeactivateAsync(string id, CancellationToken cancellationToken = default);

    Task<DateTime> UpgradeSubscriptionAsync(string id, DateTime extendedExpiryDate, CancellationToken cancellationToken = default);

    Task MigrateTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken);

    Task SeedTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken);
}

