using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Tracks the last-used sequence number for property code generation per tenant,
/// classification code, item code, and fiscal year.
/// Key: TenantId + ClassCode + ItemCode + Year.
/// Uses optimistic concurrency (Version) to guard against race conditions.
/// </summary>
public sealed class PropertyCodeCounter : BaseEntity<Guid>
{
    public string TenantId { get; private set; } = default!;

    /// <summary>2-character classification code from PropertyClass (e.g. "OE", "TS").</summary>
    public string ClassCode { get; private set; } = default!;

    /// <summary>1-character category code from PropertyClassItem (e.g. "C", "F").</summary>
    public string ItemCode { get; private set; } = default!;

    /// <summary>Fiscal year (4 digits).</summary>
    public int Year { get; private set; }

    /// <summary>The last sequence number issued for this TenantId + ClassCode + ItemCode + Year.</summary>
    public int LastSequence { get; private set; }

    private PropertyCodeCounter() { }

    public static PropertyCodeCounter Start(
        string tenantId,
        string classCode,
        string itemCode,
        int year)
    {
        return new PropertyCodeCounter
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClassCode = classCode.ToUpperInvariant(),
            ItemCode = itemCode.ToUpperInvariant(),
            Year = year,
            LastSequence = 0
        };
    }

    /// <summary>Increments and returns the next sequence number.</summary>
    public int NextSequence()
    {
        LastSequence += 1;
        return LastSequence;
    }
}
