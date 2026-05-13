using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a Semi-expendable Materials Issuance Record (SMIR).
/// Each SMIRItem references one specific SemiExpendableProperty unit being transferred out.
/// </summary>
public sealed class SMIRItem : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;

    /// <summary>FK to the parent SMIR.</summary>
    public Guid SMIRId { get; private set; }

    /// <summary>The TangibleInventoryItem being transferred out.</summary>
    public Guid TangibleInventoryItemId { get; private set; }

    /// <summary>Item number on the SMIR form (sequential within the SMIR).</summary>
    public int ItemNo { get; private set; }

    /// <summary>Description as it appears on the SMIR form.</summary>
    public string? Description { get; private set; }

    /// <summary>Unit cost copied from the property at time of issuance.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Asset type at the time this SMIR was processed.
    /// Frozen here so historical records remain accurate.
    /// </summary>
    public AssetType AssetTypeAtTimeOfIssuance { get; private set; }

    public static SMIRItem Create(
        string tenantId,
        Guid smirId,
        Guid tangibleInventoryItemId,
        int itemNo,
        string? description,
        decimal unitCost,
        AssetType assetTypeAtTimeOfIssuance)
    {
        return new SMIRItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SMIRId = smirId,
            TangibleInventoryItemId = tangibleInventoryItemId,
            ItemNo = itemNo,
            Description = description,
            UnitCost = unitCost,
            AssetTypeAtTimeOfIssuance = assetTypeAtTimeOfIssuance,
        };
    }
}

