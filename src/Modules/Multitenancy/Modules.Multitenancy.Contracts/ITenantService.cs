using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Multitenancy.Contracts.Dtos;
using AMIS.Modules.Multitenancy.Contracts.v1.GetTenants;
using AMIS.Framework.Shared.Multitenancy;

namespace AMIS.Modules.Multitenancy.Contracts;

public interface ITenantService
{
    Task<PagedResponse<TenantDto>> GetAllAsync(GetTenantsQuery query, CancellationToken cancellationToken);

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

