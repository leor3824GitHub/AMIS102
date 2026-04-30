using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within an Inventory Custodian Slip.
/// Each ICSItem references one specific SemiExpendableProperty unit being issued.
/// </summary>
public sealed class ICSItem : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;

    /// <summary>FK to the parent ICS.</summary>
    public Guid ICSId { get; private set; }

    /// <summary>The TangibleInventoryItem (SE type) being issued.</summary>
    public Guid TangibleInventoryItemId { get; private set; }

    /// <summary>Item number on the ICS form (sequential within the ICS).</summary>
    public int ItemNo { get; private set; }

    /// <summary>Description as it appears on the ICS form.</summary>
    public string? Description { get; private set; }

    /// <summary>Unit cost copied from the property at issuance time.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>Estimated useful life in years, from the item catalog.</summary>
    public int? EstimatedUsefulLifeYears { get; private set; }

    /// <summary>
    /// Asset type at the time this ICS was issued.
    /// Frozen here so historical ICS records remain accurate.
    /// </summary>
    public AssetType AssetTypeAtTimeOfIssuance { get; private set; }

    public static ICSItem Create(
        string tenantId,
        Guid icsId,
        Guid tangibleInventoryItemId,
        int itemNo,
        string? description,
        decimal unitCost,
        int? estimatedUsefulLifeYears,
        AssetType assetTypeAtTimeOfIssuance)
    {
        return new ICSItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ICSId = icsId,
            TangibleInventoryItemId = tangibleInventoryItemId,
            ItemNo = itemNo,
            Description = description,
            UnitCost = unitCost,
            EstimatedUsefulLifeYears = estimatedUsefulLifeYears,
            AssetTypeAtTimeOfIssuance = assetTypeAtTimeOfIssuance,
        };
    }
}
