using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.ProcurementAcquisition.Domain.Canvass;

/// <summary>
/// Tracks the last-issued serial number for canvass (RIV) number generation per tenant and fiscal year.
/// Key: (TenantId, Year). Serial resets to 1 at the start of each new year (RIV numbers are formatted
/// RIV-YYYY-NNNN). Uses PostgreSQL xmin optimistic concurrency to guard against race conditions, mirroring
/// <see cref="AMIS.Modules.ProcurementAcquisition.Domain.InspectionAcceptanceReports.IarNumberSequence"/>.
/// </summary>
public sealed class RivNumberSequence : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;

    /// <summary>Fiscal year (4 digits).</summary>
    public int Year { get; private set; }

    /// <summary>The last serial number issued for this TenantId + Year.</summary>
    public int LastSerial { get; private set; }

    private RivNumberSequence() { }

    public static RivNumberSequence Create(string tenantId, int year) =>
        new() { Id = Guid.NewGuid(), TenantId = tenantId, Year = year, LastSerial = 0 };

    /// <summary>Increments and returns the next serial number.</summary>
    public int NextSerial()
    {
        LastSerial++;
        return LastSerial;
    }
}
