using System.Diagnostics.CodeAnalysis;
using Finbuckle.MultiTenant.Abstractions;

namespace AMIS.Framework.Shared.Multitenancy;

public class AppTenantInfo : TenantInfo, IAppTenantInfo
{
    // Parameterless constructor for tooling/EF.
    [SetsRequiredMembers]
    public AppTenantInfo()
    {
        Id = string.Empty;
        Identifier = string.Empty;
    }

    [SetsRequiredMembers]
    public AppTenantInfo(string id, string identifier, string? name = null)
    {
        Id = id;
        Identifier = identifier;
        Name = name;
    }

    [SetsRequiredMembers]
    public AppTenantInfo(string id, string name, string? connectionString, string adminEmail, string? issuer = null)
        : this(id, id, name)
    {
        ConnectionString = connectionString ?? string.Empty;
        AdminEmail = adminEmail;
        IsActive = true;
        Issuer = issuer;

        // Add Default 1 Month Validity for all new tenants. Something like a DEMO period for tenants.
        ValidUpto = DateTime.UtcNow.AddMonths(1);
    }

    public string ConnectionString { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime ValidUpto { get; set; }
    public string? Issuer { get; set; }

    /// <summary>
    /// The MasterData Office this agency <i>is</i>. Soft reference: MasterData lives in a different
    /// DbContext (and potentially a different database), so there is no foreign key.
    /// <para>
    /// Authoritative for routing inter-agency property transfers — an employee is resolved to their
    /// destination tenant by matching <c>EmployeeProfile.OfficeId</c> against this column. Null means the
    /// tenant has not been linked to an office yet and cannot be an auto-derived transfer destination.
    /// </para>
    /// </summary>
    public Guid? OfficeId { get; set; }

    /// <summary>
    /// Display snapshot of the linked <c>Office.Code</c>, taken when the link was made. Never used for
    /// matching — routing always goes through <see cref="OfficeId"/> — so drift here is cosmetic.
    /// </summary>
    public string? OfficeCode { get; set; }

    /// <summary>Links this tenant to the MasterData office it represents.</summary>
    public void LinkOffice(Guid officeId, string? officeCode)
    {
        if (officeId == Guid.Empty)
        {
            throw new InvalidOperationException("A tenant cannot be linked to an empty office id.");
        }

        OfficeId = officeId;
        OfficeCode = string.IsNullOrWhiteSpace(officeCode) ? null : officeCode.Trim();
    }

    public void AddValidity(int months) =>
        ValidUpto = ValidUpto.AddMonths(months);

    public void SetValidity(in DateTime validTill)
    {
        var normalized = validTill;
        ValidUpto = ValidUpto < normalized
            ? normalized
            : throw new InvalidOperationException("Subscription cannot be backdated.");
    }

    public void Activate()
    {
        if (Id == MultitenancyConstants.Root.Id)
        {
            throw new InvalidOperationException("Invalid Tenant");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (Id == MultitenancyConstants.Root.Id)
        {
            throw new InvalidOperationException("Invalid Tenant");
        }

        IsActive = false;
    }

    string? IAppTenantInfo.ConnectionString
    {
        get => ConnectionString;
        set => ConnectionString = value ?? throw new InvalidOperationException("ConnectionString can't be null.");
    }
}

