using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a PPE Receiving Report (PPERR).
/// Each item represents a distinct PPE description/batch being received.
/// </summary>
public sealed class PPERRItem : BaseEntity<Guid>
{
    /// <summary>FK to the parent PPERR.</summary>
    public Guid PPERRId { get; private set; }

    /// <summary>Item number on the PPERR form (sequential within the report).</summary>
    public int ItemNo { get; private set; }

    /// <summary>PPE property code (coordinates with C.O.-GSD).</summary>
    public string PropertyCode { get; private set; } = default!;

    /// <summary>Complete specification of the PPE including serial number, if any.</summary>
    public string Description { get; private set; } = default!;

    /// <summary>Date the PPE was acquired/date of purchase.</summary>
    public DateOnly DateAcquired { get; private set; }

    /// <summary>Number of units received.</summary>
    public int Quantity { get; private set; }

    /// <summary>Cost per unit of measurement.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>Acquisition cost (Quantity × UnitCost), stored for reporting.</summary>
    public decimal Amount { get; private set; }

    public static PPERRItem Create(
        Guid pperrId,
        int itemNo,
        string propertyCode,
        string description,
        DateOnly dateAcquired,
        int quantity,
        decimal unitCost)
    {
        return new PPERRItem
        {
            Id           = Guid.NewGuid(),
            PPERRId      = pperrId,
            ItemNo       = itemNo,
            PropertyCode = propertyCode,
            Description  = description,
            DateAcquired = dateAcquired,
            Quantity     = quantity,
            UnitCost     = unitCost,
            Amount       = quantity * unitCost,
        };
    }
}
