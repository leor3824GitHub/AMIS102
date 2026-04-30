using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within an Inspection and Inventory Report of Unserviceable
/// Semi-Expendable Property (IIRUSP).
/// Snapshots the property details at the time of reporting for audit trail.
/// </summary>
public sealed class UnserviceablePropertyItem : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;

    /// <summary>FK to the parent UnserviceablePropertyReport.</summary>
    public Guid ReportId { get; private set; }

    /// <summary>The TangibleInventoryItem being declared unserviceable.</summary>
    public Guid TangibleInventoryItemId { get; private set; }

    /// <summary>Item number on the report form (sequential within the report).</summary>
    public int ItemNo { get; private set; }

    /// <summary>Description / name of the item as it appears on the form.</summary>
    public string? Description { get; private set; }

    /// <summary>Unit cost copied from the property at the time of reporting.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Asset type at the time of reporting.
    /// Frozen here so historical records remain accurate.
    /// </summary>
    public AssetType AssetTypeAtTimeOfReport { get; private set; }

    /// <summary>
    /// Condition notes per item (e.g. "broken display", "motor seized").
    /// Supports the inspector's remarks column on the IIRUSP form.
    /// </summary>
    public string? ConditionRemarks { get; private set; }

    public static UnserviceablePropertyItem Create(
        string tenantId,
        Guid reportId,
        Guid tangibleInventoryItemId,
        int itemNo,
        string? description,
        decimal unitCost,
        AssetType assetTypeAtTimeOfReport,
        string? conditionRemarks)
    {
        return new UnserviceablePropertyItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportId = reportId,
            TangibleInventoryItemId = tangibleInventoryItemId,
            ItemNo = itemNo,
            Description = description,
            UnitCost = unitCost,
            AssetTypeAtTimeOfReport = assetTypeAtTimeOfReport,
            ConditionRemarks = conditionRemarks,
        };
    }
}
