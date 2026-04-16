using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// An immutable audit record of a bulk reclassification run triggered by a threshold change.
///
/// When a new capitalization threshold is activated in Master Data, existing
/// <see cref="SemiExpendableProperty"/> records whose <see cref="SemiExpendableProperty.Category"/>
/// no longer matches the new thresholds must be updated. This entity captures:
///   - which threshold (by ID from MasterData) was active during the run
///   - how many properties were reclassified
///   - who triggered the run and when
///
/// One record is created per explicit ReclassifyPropertiesCommand invocation.
/// </summary>
public sealed class ReclassificationRecord : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>
    /// The ID of the MasterData CapitalizationThreshold that was active during this run.
    /// Cross-module reference — no FK constraint is enforced by EF.
    /// </summary>
    public Guid ThresholdId { get; private set; }

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

    public static ReclassificationRecord Create(Guid thresholdId, int totalReclassified, string? notes)
    {
        return new ReclassificationRecord
        {
            Id                = Guid.NewGuid(),
            ThresholdId       = thresholdId,
            TotalReclassified = totalReclassified,
            Notes             = notes,
            CreatedOnUtc      = DateTimeOffset.UtcNow,
        };
    }
}
