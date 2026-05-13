using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.MasterData.Domain;

/// <summary>
/// Shared reference data — COA sets this nationally, all tenants read the same threshold.
/// </summary>
public sealed class CapitalizationThreshold : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>COA Circular reference, e.g. "COA Circular No. 2022-004"</summary>
    public string CircularName { get; private set; } = default!;

    /// <summary>Brief description of the circular's subject.</summary>
    public string Description { get; private set; } = default!;

    /// <summary>
    /// Unit cost at or above which a tangible item is capitalized as PPE.
    /// Items below this threshold are treated as semi-expendable.
    /// (COA Circular 2022-004: P50,000.00)
    /// </summary>
    public decimal CapitalizationAmount { get; private set; }

    /// <summary>
    /// Unit cost threshold that further splits semi-expendable property into
    /// low-valued (≤ this amount) and high-valued (> this amount but below CapitalizationAmount).
    /// (COA Circular 2022-004 §4.8: P5,000.00)
    /// </summary>
    public decimal SemiExpendableLowValueThreshold { get; private set; }

    /// <summary>Date the circular takes effect.</summary>
    public DateOnly EffectivityDate { get; private set; }

    /// <summary>Whether this is the currently applied threshold.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public static CapitalizationThreshold Create(
        string circularName,
        string description,
        decimal capitalizationAmount,
        decimal semiExpendableLowValueThreshold,
        DateOnly effectivityDate)
    {
        return new CapitalizationThreshold
        {
            Id = Guid.NewGuid(),
            CircularName = circularName,
            Description = description,
            CapitalizationAmount = capitalizationAmount,
            SemiExpendableLowValueThreshold = semiExpendableLowValueThreshold,
            EffectivityDate = effectivityDate,
            IsActive = false,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string circularName,
        string description,
        decimal capitalizationAmount,
        decimal semiExpendableLowValueThreshold,
        DateOnly effectivityDate)
    {
        CircularName = circularName;
        Description = description;
        CapitalizationAmount = capitalizationAmount;
        SemiExpendableLowValueThreshold = semiExpendableLowValueThreshold;
        EffectivityDate = effectivityDate;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

