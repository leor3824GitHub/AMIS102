using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Records the capitalization thresholds in force at a given point in time,
/// per COA Circular 2022-004 Section 4.8.
///
/// Only one policy is active at any time. When a new policy is set via
/// SetThresholdPolicyCommand, the previous active policy is deactivated.
///
/// Thresholds (Philippine Peso):
///   Unit Cost ≤ LowValueThreshold         → AssetCategory.LowValuedSemi  (SPLV series ICS)
///   Unit Cost  > LowValueThreshold
///            and &lt; CapitalizationThreshold → AssetCategory.HighValuedSemi (SPHV series ICS)
///   Unit Cost ≥ CapitalizationThreshold   → Property, Plant and Equipment (PPE) — not handled here
/// </summary>
public sealed class CapitalizationThresholdPolicy : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>
    /// Items costing at or below this amount are classified as LowValuedSemi.
    /// Default per COA Circular 2022-004: ₱5,000.
    /// </summary>
    public decimal LowValueThreshold { get; private set; }

    /// <summary>
    /// Items costing at or above this amount must be recorded as PPE (Fixed Asset).
    /// Default per COA Circular 2022-004: ₱50,000.
    /// </summary>
    public decimal CapitalizationThreshold { get; private set; }

    /// <summary>Date from which this policy takes effect.</summary>
    public DateOnly EffectiveDate { get; private set; }

    /// <summary>Whether this is the currently active policy.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Explanation for why this policy was set (e.g., "Updated per COA Circular 2024-001").
    /// </summary>
    public string? Reason { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static CapitalizationThresholdPolicy Create(
        decimal lowValueThreshold,
        decimal capitalizationThreshold,
        DateOnly effectiveDate,
        string? reason)
    {
        return new CapitalizationThresholdPolicy
        {
            Id                     = Guid.NewGuid(),
            LowValueThreshold      = lowValueThreshold,
            CapitalizationThreshold = capitalizationThreshold,
            EffectiveDate          = effectiveDate,
            IsActive               = true,
            Reason                 = reason,
            CreatedOnUtc           = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Resolves the AssetCategory for a given unit cost against this policy's thresholds.
    /// </summary>
    public AssetCategory ClassifyUnitCost(decimal unitCost)
    {
        return unitCost <= LowValueThreshold
            ? AssetCategory.LowValuedSemi
            : AssetCategory.HighValuedSemi;
    }

    /// <summary>Called when a newer policy supersedes this one.</summary>
    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
