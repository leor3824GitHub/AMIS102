using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within an Inspection and Inventory Report of Unserviceable
/// Semi-Expendable Property (IIRUSP).
/// Snapshots the property details at the time of reporting for audit trail.
/// </summary>
public sealed class UnserviceablePropertyItem : BaseEntity<Guid>
{
    /// <summary>FK to the parent UnserviceablePropertyReport.</summary>
    public Guid ReportId { get; private set; }

    /// <summary>The specific property unit being declared unserviceable.</summary>
    public Guid SemiExpendablePropertyId { get; private set; }

    /// <summary>Item number on the report form (sequential within the report).</summary>
    public int ItemNo { get; private set; }

    /// <summary>Description / name of the item as it appears on the form.</summary>
    public string? Description { get; private set; }

    /// <summary>Unit cost copied from the property at the time of reporting.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Asset category at the time of reporting.
    /// Frozen here so historical records remain accurate after reclassification.
    /// </summary>
    public AssetCategory CategoryAtTimeOfReport { get; private set; }

    /// <summary>
    /// Condition notes per item (e.g. "broken display", "motor seized").
    /// Supports the inspector's remarks column on the IIRUSP form.
    /// </summary>
    public string? ConditionRemarks { get; private set; }

    public static UnserviceablePropertyItem Create(
        Guid reportId,
        Guid semiExpendablePropertyId,
        int itemNo,
        string? description,
        decimal unitCost,
        AssetCategory categoryAtTimeOfReport,
        string? conditionRemarks)
    {
        return new UnserviceablePropertyItem
        {
            Id                       = Guid.NewGuid(),
            ReportId                 = reportId,
            SemiExpendablePropertyId = semiExpendablePropertyId,
            ItemNo                   = itemNo,
            Description              = description,
            UnitCost                 = unitCost,
            CategoryAtTimeOfReport   = categoryAtTimeOfReport,
            ConditionRemarks         = conditionRemarks,
        };
    }
}
