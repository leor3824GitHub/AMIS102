using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a Semi-expendable Materials Issuance Record (SMIR).
/// Each SMIRItem references one specific SemiExpendableProperty unit being transferred out.
/// </summary>
public sealed class SMIRItem : BaseEntity<Guid>
{
    /// <summary>FK to the parent SMIR.</summary>
    public Guid SMIRId { get; private set; }

    /// <summary>The specific property unit being transferred out.</summary>
    public Guid SemiExpendablePropertyId { get; private set; }

    /// <summary>Item number on the SMIR form (sequential within the SMIR).</summary>
    public int ItemNo { get; private set; }

    /// <summary>Description as it appears on the SMIR form.</summary>
    public string? Description { get; private set; }

    /// <summary>Unit cost copied from the property at time of issuance.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Asset category at the time this SMIR was processed.
    /// Frozen here so historical records remain accurate after reclassification.
    /// </summary>
    public AssetCategory CategoryAtTimeOfIssuance { get; private set; }

    public static SMIRItem Create(
        Guid smirId,
        Guid semiExpendablePropertyId,
        int itemNo,
        string? description,
        decimal unitCost,
        AssetCategory categoryAtTimeOfIssuance)
    {
        return new SMIRItem
        {
            Id                       = Guid.NewGuid(),
            SMIRId                   = smirId,
            SemiExpendablePropertyId = semiExpendablePropertyId,
            ItemNo                   = itemNo,
            Description              = description,
            UnitCost                 = unitCost,
            CategoryAtTimeOfIssuance = categoryAtTimeOfIssuance,
        };
    }
}
