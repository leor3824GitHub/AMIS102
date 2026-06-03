using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.ProcurementAcquisition.Domain.PurchaseOrders;

/// <summary>
/// Tracks the last-issued serial number for PO number generation per tenant, fiscal year, and month.
/// Key: (TenantId, Year, Month). Serial resets to 1 at the start of each new month (PO numbers are
/// formatted YYYY-MM-NNN). Uses PostgreSQL xmin optimistic concurrency to guard against race conditions,
/// mirroring <see cref="AMIS.Modules.ProcurementAcquisition.Domain.PurchaseRequests.PrNumberSequence"/>.
/// </summary>
public sealed class PoNumberSequence : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;

    /// <summary>Fiscal year (4 digits).</summary>
    public int Year { get; private set; }

    /// <summary>Month (1–12). PO serials reset per month.</summary>
    public int Month { get; private set; }

    /// <summary>The last serial number issued for this TenantId + Year + Month.</summary>
    public int LastSerial { get; private set; }

    private PoNumberSequence() { }

    public static PoNumberSequence Create(string tenantId, int year, int month) =>
        new() { Id = Guid.NewGuid(), TenantId = tenantId, Year = year, Month = month, LastSerial = 0 };

    /// <summary>Increments and returns the next serial number.</summary>
    public int NextSerial()
    {
        LastSerial++;
        return LastSerial;
    }
}
