using FSH.Framework.Core.Domain;

namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// A Property, Plant and Equipment (PPE) item — an asset above the capitalization threshold.
/// Created from a PPERR line item. Accountability is tracked via PAR (in-office) and
/// PPEIR (inter-office transfer).
/// </summary>
public sealed class PPEItem : AggregateRoot<Guid>, IAuditableEntity
{
    /// <summary>PPE property code (coordinates with C.O.-GSD).</summary>
    public string PropertyCode { get; private set; } = default!;

    /// <summary>Property number assigned by the Supply Officer.</summary>
    public string PropertyNumber { get; private set; } = default!;

    /// <summary>Full specification including serial number, if any.</summary>
    public string Description { get; private set; } = default!;

    /// <summary>Serial number of the equipment, if applicable.</summary>
    public string? SerialNumber { get; private set; }

    /// <summary>Date the PPE was acquired/date of purchase.</summary>
    public DateOnly DateAcquired { get; private set; }

    /// <summary>Unit cost / acquisition cost of the PPE.</summary>
    public decimal UnitCost { get; private set; }

    /// <summary>Estimated useful life in years.</summary>
    public int EstimatedUsefulLifeYears { get; private set; }

    /// <summary>Current lifecycle status of this PPE item.</summary>
    public PPEItemStatus Status { get; private set; } = PPEItemStatus.OnHand;

    /// <summary>
    /// Employee currently accountable for this PPE item (via active PAR).
    /// Null when item is OnHand or Transferred.
    /// References MasterData.EmployeeProfile — plain FK, no cross-module navigation.
    /// </summary>
    public Guid? CurrentAccountableEmployeeId { get; private set; }

    /// <summary>The PPERR that first registered this PPE item.</summary>
    public Guid? SourcePPERRId { get; private set; }

    public byte[] Version { get; set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static PPEItem Create(
        string propertyCode,
        string propertyNumber,
        string description,
        string? serialNumber,
        DateOnly dateAcquired,
        decimal unitCost,
        int estimatedUsefulLifeYears,
        Guid? sourcePPERRId)
    {
        return new PPEItem
        {
            Id                       = Guid.NewGuid(),
            PropertyCode             = propertyCode,
            PropertyNumber           = propertyNumber,
            Description              = description,
            SerialNumber             = serialNumber,
            DateAcquired             = dateAcquired,
            UnitCost                 = unitCost,
            EstimatedUsefulLifeYears = estimatedUsefulLifeYears,
            Status                   = PPEItemStatus.OnHand,
            SourcePPERRId            = sourcePPERRId,
            CreatedOnUtc             = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Assigns accountability to an employee via PAR.</summary>
    public void AssignPAR(Guid employeeId)
    {
        Status = PPEItemStatus.IssuedPAR;
        CurrentAccountableEmployeeId = employeeId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Marks the item as transferred to another office via PPEIR.</summary>
    public void MarkTransferred()
    {
        Status = PPEItemStatus.Transferred;
        CurrentAccountableEmployeeId = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Returns the item to supply custody (serviceable RRP).</summary>
    public void ReturnToStock()
    {
        Status = PPEItemStatus.OnHand;
        CurrentAccountableEmployeeId = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Marks the item as disposed (junked RRP).</summary>
    public void MarkDisposed()
    {
        Status = PPEItemStatus.Disposed;
        CurrentAccountableEmployeeId = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
