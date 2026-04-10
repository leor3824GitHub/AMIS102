using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a Supplies and Materials Receiving Report.
/// Each SMRRItem with Quantity &gt; 1 creates multiple SemiExpendableProperty units
/// (one per physical piece), all linked back to this item via SMRRItemId.
/// </summary>
public sealed class SMRRItem : AggregateRoot<Guid>
{
    /// <summary>FK to the parent SMRR.</summary>
    public Guid SMRRId { get; private set; }

    /// <summary>Source document reference (e.g., PO number, DR number).</summary>
    public string? Reference { get; private set; }

    /// <summary>FK to the semi-expendable item catalog.</summary>
    public Guid SemiExpendableItemId { get; private set; }

    /// <summary>Specific description / brand / specs for this batch.</summary>
    public string? Description { get; private set; }

    /// <summary>Date of acquisition per the source document.</summary>
    public DateOnly AcquisitionDate { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitCost { get; private set; }

    /// <summary>Computed: Quantity × UnitCost. Stored for reporting.</summary>
    public decimal Amount { get; private set; }

    public static SMRRItem Create(
        Guid smrrId,
        string? reference,
        Guid semiExpendableItemId,
        string? description,
        DateOnly acquisitionDate,
        int quantity,
        decimal unitCost)
    {
        return new SMRRItem
        {
            Id                  = Guid.NewGuid(),
            SMRRId              = smrrId,
            Reference           = reference,
            SemiExpendableItemId = semiExpendableItemId,
            Description         = description,
            AcquisitionDate     = acquisitionDate,
            Quantity            = quantity,
            UnitCost            = unitCost,
            Amount              = quantity * unitCost,
        };
    }
}
