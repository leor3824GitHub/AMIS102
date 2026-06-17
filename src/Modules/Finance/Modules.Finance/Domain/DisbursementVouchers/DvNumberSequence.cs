using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Finance.Domain.DisbursementVouchers;

/// <summary>
/// Tracks the last-issued serial for Disbursement Voucher numbers per fiscal year (DV-YYYY-NNNNN).
/// One counter row per <see cref="Year"/>, guarded by PostgreSQL xmin optimistic concurrency so concurrent
/// creates can't hand out the same number. Keyed by year only (not tenant) to match the global unique
/// index on <c>DisbursementVoucher.DvNumber</c> — Finance vouchers are not tenant-scoped.
/// </summary>
public sealed class DvNumberSequence : BaseEntity<Guid>
{
    /// <summary>Fiscal year (4 digits).</summary>
    public int Year { get; private set; }

    /// <summary>The last serial number issued for this <see cref="Year"/>.</summary>
    public int LastSerial { get; private set; }

    private DvNumberSequence() { }

    public static DvNumberSequence Create(int year) =>
        new() { Id = Guid.NewGuid(), Year = year, LastSerial = 0 };

    /// <summary>Increments and returns the next serial number.</summary>
    public int NextSerial()
    {
        LastSerial++;
        return LastSerial;
    }
}
