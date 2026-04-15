using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// An immutable audit record of a bulk reclassification run triggered by a threshold policy change.
///
/// When a new <see cref="CapitalizationThresholdPolicy"/> is activated, existing
/// <see cref="SemiExpendableProperty"/> records whose <see cref="SemiExpendableProperty.Category"/>
/// no longer matches the new thresholds must be updated. This entity captures:
///   - which policy was applied
///   - how many properties were reclassified
///   - who triggered the run and when
///
/// One record is created per explicit ReclassifyPropertiesCommand invocation.
/// </summary>
public sealed class ReclassificationRecord : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>The active policy that was applied during this run.</summary>
    public Guid PolicyId { get; private set; }

    /// <summary>Number of SemiExpendableProperty records whose category was changed.</summary>
    public int TotalReclassified { get; private set; }

    /// <summary>Optional notes provided by the user when triggering the reclassification.</summary>
    public string? Notes { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static ReclassificationRecord Create(Guid policyId, int totalReclassified, string? notes)
    {
        return new ReclassificationRecord
        {
            Id                = Guid.NewGuid(),
            PolicyId          = policyId,
            TotalReclassified = totalReclassified,
            Notes             = notes,
            CreatedOnUtc      = DateTimeOffset.UtcNow,
        };
    }
}
