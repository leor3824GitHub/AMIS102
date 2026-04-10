using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within an Inventory Custodian Slip.
/// Each ICSItem references one specific SemiExpendableProperty unit being issued.
/// </summary>
public sealed class ICSItem : AggregateRoot<Guid>
{
    /// <summary>FK to the parent ICS.</summary>
    public Guid ICSId { get; private set; }

    /// <summary>The specific property unit being issued.</summary>
    public Guid SemiExpendablePropertyId { get; private set; }

    /// <summary>Item number on the ICS form (sequential within the ICS).</summary>
    public int ItemNo { get; private set; }

    /// <summary>Description as it appears on the ICS form.</summary>
    public string? Description { get; private set; }

    /// <summary>Unit cost copied from the property at issuance time.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>Estimated useful life in years, from the item catalog.</summary>
    public int? EstimatedUsefulLifeYears { get; private set; }

    public static ICSItem Create(
        Guid icsId,
        Guid semiExpendablePropertyId,
        int itemNo,
        string? description,
        decimal unitCost,
        int? estimatedUsefulLifeYears)
    {
        return new ICSItem
        {
            Id                       = Guid.NewGuid(),
            ICSId                    = icsId,
            SemiExpendablePropertyId = semiExpendablePropertyId,
            ItemNo                   = itemNo,
            Description              = description,
            UnitCost                 = unitCost,
            EstimatedUsefulLifeYears = estimatedUsefulLifeYears,
        };
    }
}
