using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a Receipt for Returned Semi-Expendable Property.
/// Snapshots the property details at the time of return for audit trail.
/// </summary>
public sealed class RRSPItem : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;

    /// <summary>FK to the parent RRSP.</summary>
    public Guid RRSPId { get; private set; }

    /// <summary>The TangibleInventoryItem being returned.</summary>
    public Guid TangibleInventoryItemId { get; private set; }

    /// <summary>Item number on the RRSP form (sequential within the RRSP).</summary>
    public int ItemNo { get; private set; }

    /// <summary>Description as it appears on the RRSP form.</summary>
    public string? Description { get; private set; }

    /// <summary>Unit cost copied from the property at the time of return.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Asset type at the time of return.
    /// Frozen here so historical RRSP records remain accurate.
    /// </summary>
    public AssetType AssetTypeAtTimeOfReturn { get; private set; }

    public static RRSPItem Create(
        string tenantId,
        Guid rrspId,
        Guid tangibleInventoryItemId,
        int itemNo,
        string? description,
        decimal unitCost,
        AssetType assetTypeAtTimeOfReturn)
    {
        return new RRSPItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RRSPId = rrspId,
            TangibleInventoryItemId = tangibleInventoryItemId,
            ItemNo = itemNo,
            Description = description,
            UnitCost = unitCost,
            AssetTypeAtTimeOfReturn = assetTypeAtTimeOfReturn,
        };
    }
}

