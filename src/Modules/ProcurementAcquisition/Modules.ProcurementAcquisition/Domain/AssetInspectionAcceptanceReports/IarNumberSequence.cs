using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.ProcurementAcquisition.Domain.AssetInspectionAcceptanceReports;

/// <summary>
/// Tracks the last-issued serial number for IAR number generation per tenant and fiscal year.
/// Key: (TenantId, Year). Serial resets to 1 at the start of each new year.
/// Uses PostgreSQL xmin optimistic concurrency to guard against race conditions.
/// </summary>
public sealed class IarNumberSequence : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;

    /// <summary>Fiscal year (4 digits).</summary>
    public int Year { get; private set; }

    /// <summary>The last serial number issued for this TenantId + Year.</summary>
    public int LastSerial { get; private set; }

    private IarNumberSequence() { }

    public static IarNumberSequence Create(string tenantId, int year) =>
        new() { Id = Guid.NewGuid(), TenantId = tenantId, Year = year, LastSerial = 0 };

    /// <summary>Increments and returns the next serial number.</summary>
    public int NextSerial()
    {
        LastSerial++;
        return LastSerial;
    }
}
