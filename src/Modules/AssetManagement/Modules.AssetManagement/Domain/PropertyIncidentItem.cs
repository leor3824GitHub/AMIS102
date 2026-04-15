using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a Property Incident Report (RLSDDSP).
/// Snapshots the property details at the time of reporting for audit trail.
/// </summary>
public sealed class PropertyIncidentItem : BaseEntity<Guid>
{
    /// <summary>FK to the parent PropertyIncidentReport.</summary>
    public Guid ReportId { get; private set; }

    /// <summary>The specific property unit being reported.</summary>
    public Guid SemiExpendablePropertyId { get; private set; }

    /// <summary>Item number on the report form (sequential within the report).</summary>
    public int ItemNo { get; private set; }

    /// <summary>Description as it appears on the RLSDDSP form.</summary>
    public string? Description { get; private set; }

    /// <summary>Unit cost copied from the property at the time of reporting.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Asset category at the time of reporting.
    /// Frozen here so historical records remain accurate after reclassification.
    /// </summary>
    public AssetCategory CategoryAtTimeOfReport { get; private set; }

    public static PropertyIncidentItem Create(
        Guid reportId,
        Guid semiExpendablePropertyId,
        int itemNo,
        string? description,
        decimal unitCost,
        AssetCategory categoryAtTimeOfReport)
    {
        return new PropertyIncidentItem
        {
            Id                       = Guid.NewGuid(),
            ReportId                 = reportId,
            SemiExpendablePropertyId = semiExpendablePropertyId,
            ItemNo                   = itemNo,
            Description              = description,
            UnitCost                 = unitCost,
            CategoryAtTimeOfReport   = categoryAtTimeOfReport,
        };
    }
}
