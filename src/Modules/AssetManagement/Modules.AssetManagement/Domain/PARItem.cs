using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A line item within a Property Acknowledgement Receipt (PAR).
/// Each PARItem references one PPEItem being assigned to the accountable officer.
/// Key values (cost, EUL, date) are frozen at issuance time.
/// </summary>
public sealed class PARItem : BaseEntity<Guid>
{
    /// <summary>FK to the parent PAR.</summary>
    public Guid PARId { get; private set; }

    /// <summary>The TangibleInventoryItem (PPE type) being assigned.</summary>
    public Guid TangibleInventoryItemId { get; private set; }

    /// <summary>Item number on the PAR form (sequential within the PAR).</summary>
    public int ItemNo { get; private set; }

    /// <summary>Number of units issued to the employee.</summary>
    public int Quantity { get; private set; }

    /// <summary>Unit of measurement (piece, set, unit, etc.).</summary>
    public string Unit { get; private set; } = default!;

    /// <summary>
    /// Complete item description. For newly purchased items, includes acquisition date,
    /// supplier name, Purchase Receipt number/date, SI number/date, and PO/LO/JO number/date.
    /// </summary>
    public string ItemDescription { get; private set; } = default!;

    /// <summary>Unit cost frozen at issuance time.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>Total cost of PPE (Quantity × UnitCost), stored for reporting.</summary>
    public decimal TotalCost { get; private set; }

    /// <summary>Estimated useful life in years, frozen at issuance time.</summary>
    public int EstimatedUsefulLifeYears { get; private set; }

    /// <summary>Acquisition date of the PPE, frozen at issuance time.</summary>
    public DateOnly DateAcquired { get; private set; }

    public static PARItem Create(
        Guid parId,
        Guid tangibleInventoryItemId,
        int itemNo,
        int quantity,
        string unit,
        string itemDescription,
        decimal unitCost,
        int estimatedUsefulLifeYears,
        DateOnly dateAcquired)
    {
        return new PARItem
        {
            Id                       = Guid.NewGuid(),
            PARId                    = parId,
            TangibleInventoryItemId  = tangibleInventoryItemId,
            ItemNo                   = itemNo,
            Quantity                 = quantity,
            Unit                     = unit,
            ItemDescription          = itemDescription,
            UnitCost                 = unitCost,
            TotalCost                = quantity * unitCost,
            EstimatedUsefulLifeYears = estimatedUsefulLifeYears,
            DateAcquired             = dateAcquired,
        };
    }
}
