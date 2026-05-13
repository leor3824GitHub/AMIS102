using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a Property Incident Report (RLSDDSP).
/// Snapshots the property details at the time of reporting for audit trail.
/// </summary>
public sealed class PropertyIncidentItem : BaseEntity<Guid>, IHasTenant
{
    public string TenantId { get; private set; } = default!;

    /// <summary>FK to the parent PropertyIncidentReport.</summary>
    public Guid ReportId { get; private set; }

    /// <summary>The TangibleInventoryItem being reported.</summary>
    public Guid TangibleInventoryItemId { get; private set; }

    /// <summary>Item number on the report form (sequential within the report).</summary>
    public int ItemNo { get; private set; }

    /// <summary>Description as it appears on the RLSDDSP form.</summary>
    public string? Description { get; private set; }

    /// <summary>Unit cost copied from the property at the time of reporting.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>
    /// Asset type at the time of reporting.
    /// Frozen here so historical records remain accurate.
    /// </summary>
    public AssetType AssetTypeAtTimeOfReport { get; private set; }

    public static PropertyIncidentItem Create(
        string tenantId,
        Guid reportId,
        Guid tangibleInventoryItemId,
        int itemNo,
        string? description,
        decimal unitCost,
        AssetType assetTypeAtTimeOfReport)
    {
        return new PropertyIncidentItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportId = reportId,
            TangibleInventoryItemId = tangibleInventoryItemId,
            ItemNo = itemNo,
            Description = description,
            UnitCost = unitCost,
            AssetTypeAtTimeOfReport = assetTypeAtTimeOfReport,
        };
    }
}

