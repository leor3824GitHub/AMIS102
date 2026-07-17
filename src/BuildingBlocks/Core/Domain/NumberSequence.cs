namespace AMIS.Framework.Core.Domain;

/// <summary>
/// Generic monotonic "last serial" counter row shared by every document-number generator. Generalizes the
/// per-document counters that previously lived in each module (PR/PO/JO/RIV/IAR/BUR/DV) into a single shape
/// keyed by <see cref="SequenceKey"/> plus period.
/// <para>
/// Unique key: (<see cref="TenantId"/>, <see cref="SequenceKey"/>, <see cref="Year"/>, <see cref="Month"/>).
/// <list type="bullet">
/// <item><see cref="Month"/> is <c>0</c> for year-only sequences; <c>1</c>–<c>12</c> when the serial resets
/// per month.</item>
/// <item><see cref="TenantId"/> is <c>""</c> for global (non-multitenant) sequences.</item>
/// </list>
/// Increment under PostgreSQL <c>xmin</c> optimistic concurrency (configured by the EF mapping); allocation
/// retries on conflict — see <c>SequenceAllocator</c> in AMIS.Framework.Persistence.
/// </para>
/// </summary>
public sealed class NumberSequence : BaseEntity<Guid>, IHasTenant
{
    /// <summary>Tenant identifier, or <c>""</c> for a global sequence.</summary>
    public string TenantId { get; private set; } = default!;

    /// <summary>Short discriminator for the document series, e.g. "PR", "PO", "JO", "RIV", "IAR", "BUR", "DV".</summary>
    public string SequenceKey { get; private set; } = default!;

    /// <summary>Fiscal year (4 digits).</summary>
    public int Year { get; private set; }

    /// <summary>Month (1–12) when the serial resets per month; <c>0</c> when the sequence is year-only.</summary>
    public int Month { get; private set; }

    /// <summary>The last serial number issued for this (TenantId, SequenceKey, Year, Month).</summary>
    public int LastSerial { get; private set; }

    private NumberSequence() { }

    /// <summary>
    /// Creates a counter row. <paramref name="lastSerial"/> seeds the starting point — pass the highest serial
    /// already issued for this key/period so a missing or desynced counter never re-issues an existing number.
    /// </summary>
    public static NumberSequence Create(string tenantId, string sequenceKey, int year, int month, int lastSerial = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SequenceKey = sequenceKey,
            Year = year,
            Month = month,
            LastSerial = lastSerial
        };

    /// <summary>Increments and returns the next serial number.</summary>
    public int NextSerial() => ++LastSerial;
}
