using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a Supplies and Materials Receiving Report.
/// References a pre-registered TangibleItem. All snapshot fields are copied
/// from that item at the time the SMRR is created for document integrity.
/// </summary>
public sealed class SMRRItem : BaseEntity<Guid>
{
    /// <summary>FK to the parent SMRR.</summary>
    public Guid SMRRId { get; private set; }

    /// <summary>FK to the pre-registered tangible item being received.</summary>
    public Guid TangibleItemId { get; private set; }

    /// <summary>Source document reference (e.g., PO number, DR number).</summary>
    public string? Reference { get; private set; }

    // --- Snapshot fields (copied from the property unit for document integrity) ---

    /// <summary>Snapshot: property number at time of receipt.</summary>
    public string PropertyNo { get; private set; } = default!;

    /// <summary>Snapshot: FK to the unified item catalog.</summary>
    public Guid ItemId { get; private set; }

    /// <summary>Snapshot: item description / name.</summary>
    public string? Description { get; private set; }

    /// <summary>Snapshot: acquisition date.</summary>
    public DateOnly AcquisitionDate { get; private set; }

    /// <summary>Snapshot: quantity.</summary>
    public int Quantity { get; private set; }

    /// <summary>Snapshot: unit cost.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>Computed: Quantity × UnitCost. Stored for reporting.</summary>
    public decimal Amount { get; private set; }

    public static SMRRItem Create(
        Guid smrrId,
        Guid tangibleItemId,
        string? reference,
        string propertyNo,
        Guid itemId,
        string? description,
        DateOnly acquisitionDate,
        int quantity,
        decimal unitCost)
    {
        return new SMRRItem
        {
            Id             = Guid.NewGuid(),
            SMRRId         = smrrId,
            TangibleItemId = tangibleItemId,
            Reference      = reference,
            PropertyNo     = propertyNo,
            ItemId         = itemId,
            Description    = description,
            AcquisitionDate = acquisitionDate,
            Quantity       = quantity,
            UnitCost       = unitCost,
            Amount         = quantity * unitCost,
        };
    }
}
