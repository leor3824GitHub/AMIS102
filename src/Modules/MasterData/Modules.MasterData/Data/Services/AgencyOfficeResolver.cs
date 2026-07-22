using Finbuckle.MultiTenant.Abstractions;
using AMIS.Framework.Shared.Multitenancy;
using AMIS.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Data.Services;

/// <summary>
/// Resolves the MasterData office the current tenant <i>is</i>, from the tenant registry's
/// <see cref="AppTenantInfo.OfficeId"/>.
/// <para>
/// That link is the single source of truth for an agency's office identity: it is what routes inter-agency
/// property transfers, so deriving the Organization Profile's agency code from it too means the two can
/// never disagree. Everything the profile needs is on <see cref="AppTenantInfo"/> (BuildingBlocks/Shared)
/// and the shared, non-tenanted <c>Offices</c> table, so this needs no reference to the Multitenancy module.
/// </para>
/// <para>
/// A null result is the normal "not linked yet" case — root and freshly created tenants — not an error.
/// Callers fall back to whatever agency code is already stored on the profile.
/// </para>
/// </summary>
public sealed class AgencyOfficeResolver(
    MasterDataDbContext db,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
{
    /// <summary>Longest agency code the <c>OfficeCode</c> ownership-stamp columns can hold.</summary>
    private const int MaxAgencyCodeLength = 8;

    /// <summary>The office this tenant represents, or null when the tenant has no office link.</summary>
    public async Task<Office?> GetLinkedOfficeAsync(CancellationToken cancellationToken)
    {
        var officeId = tenantAccessor.MultiTenantContext.TenantInfo?.OfficeId;

        if (officeId is not { } id || id == Guid.Empty)
        {
            return null;
        }

        return await db.Offices
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// The agency code stamped on this tenant's records and used in property code generation — the linked
    /// office's own <see cref="Office.Code"/>, matching what the Tenants page shows.
    /// <para>
    /// <c>Office.Code</c> allows 32 characters but every ownership-stamp column is 8, so an over-long code
    /// is reported as "not linked" rather than being silently truncated into a code that belongs to nobody.
    /// </para>
    /// </summary>
    public async Task<string?> GetAgencyCodeAsync(CancellationToken cancellationToken)
    {
        var office = await GetLinkedOfficeAsync(cancellationToken).ConfigureAwait(false);
        var code = office?.Code?.Trim();

        return string.IsNullOrEmpty(code) || code.Length > MaxAgencyCodeLength ? null : code;
    }
}
