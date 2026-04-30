using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a Receipt for Returned Property (RRP).
/// Each RRPItem references one PPEItem being returned/surrendered.
/// Key values are snapshots from the PPEItem at return time.
/// </summary>
public sealed class RRPItem : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;

    /// <summary>FK to the parent RRP.</summary>
    public Guid RRPId { get; private set; }

    /// <summary>The TangibleInventoryItem (PPE type) being returned.</summary>
    public Guid TangibleInventoryItemId { get; private set; }

    /// <summary>Item number on the RRP form (sequential within the RRP).</summary>
    public int ItemNo { get; private set; }

    /// <summary>
    /// Reference to the source accountability document (PAR number or PTR number).
    /// </summary>
    public string? SourceDocumentRef { get; private set; }

    /// <summary>PPE property code — snapshot at return time.</summary>
    public string PropertyCode { get; private set; } = default!;

    /// <summary>Full item description — snapshot at return time.</summary>
    public string Description { get; private set; } = default!;

    /// <summary>Number of units returned.</summary>
    public int Quantity { get; private set; }

    /// <summary>Unit cost — snapshot at return time.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>Total cost (Quantity × UnitCost), stored for reporting.</summary>
    public decimal TotalCost { get; private set; }

    public static RRPItem Create(
        string tenantId,
        Guid rrpId,
        Guid tangibleInventoryItemId,
        int itemNo,
        string? sourceDocumentRef,
        string propertyCode,
        string description,
        int quantity,
        decimal unitCost)
    {
        return new RRPItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RRPId = rrpId,
            TangibleInventoryItemId = tangibleInventoryItemId,
            ItemNo = itemNo,
            SourceDocumentRef = sourceDocumentRef,
            PropertyCode = propertyCode,
            Description = description,
            Quantity = quantity,
            UnitCost = unitCost,
            TotalCost = quantity * unitCost,
        };
    }
}
