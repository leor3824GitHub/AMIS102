using System.Collections.ObjectModel;
using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Expendable.Domain.Purchases;

/// <summary>
/// Quality control record for inspected purchase receipts.
/// Separate from Purchase to allow independent inspection workflow.
/// </summary>
public class PurchaseInspection : AggregateRoot<Guid>
    , IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    // Purchase Reference
    public Guid PurchaseId { get; private set; }
    public Guid ProductId { get; private set; }

    // Inspection Details
    public int QuantityReceivedForInspection { get; private set; }
    public int QuantityAccepted { get; private set; }
    public int QuantityRejected { get; private set; }

    // Inspector Info
    public Guid InspectedBy { get; private set; }
    public DateTimeOffset InspectionDate { get; private set; }
    public string? Notes { get; private set; }
    public string? RejectionReason { get; private set; }  // e.g., "Damaged shipment", "Wrong items", "Quantity mismatch"

    // Defects (optional, for detailed tracking)
    public Collection<InspectionDefect> Defects { get; private set; } = new Collection<InspectionDefect>();

    // Status
    public InspectionStatus Status { get; private set; }

    // Warehouse location for accepted items
    public Guid WarehouseLocationId { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDeletable
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }



    private PurchaseInspection() { }

    /// <summary>Factory: Create new inspection record</summary>
    public static PurchaseInspection Create(
        string tenantId,
        Guid purchaseId,
        Guid productId,
        int quantityReceived,
        Guid inspectedBy,
        Guid warehouseLocationId)
    {
        if (purchaseId == Guid.Empty)
            throw new ArgumentException("Purchase ID is required");
        if (quantityReceived <= 0)
            throw new ArgumentException("Quantity must be greater than 0");

        return new PurchaseInspection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PurchaseId = purchaseId,
            ProductId = productId,
            QuantityReceivedForInspection = quantityReceived,
            QuantityAccepted = 0,
            QuantityRejected = 0,
            InspectedBy = inspectedBy,
            InspectionDate = DateTimeOffset.UtcNow,
            Status = InspectionStatus.Pending,
            WarehouseLocationId = warehouseLocationId,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Mark all items as accepted</summary>
    public void MarkFullyAccepted(string? notes = null)
    {
        if (Status != InspectionStatus.Pending)
            throw new InvalidOperationException("Can only accept pending inspections");

        QuantityAccepted = QuantityReceivedForInspection;
        QuantityRejected = 0;
        Status = InspectionStatus.Accepted;
        Notes = notes;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Partial acceptance: some items pass, some fail</summary>
    public void MarkPartialAcceptance(
        int quantityAccepted,
        int quantityRejected,
        string rejectionReason,
        string? notes = null,
        Collection<InspectionDefect>? defects = null)
    {
        if (Status != InspectionStatus.Pending)
            throw new InvalidOperationException("Can only accept pending inspections");
        if (quantityAccepted + quantityRejected != QuantityReceivedForInspection)
            throw new InvalidOperationException(
                $"Accepted ({quantityAccepted}) + Rejected ({quantityRejected}) must equal Received ({QuantityReceivedForInspection})");

        QuantityAccepted = quantityAccepted;
        QuantityRejected = quantityRejected;
        Status = quantityAccepted > 0 ? InspectionStatus.PartiallyAccepted : InspectionStatus.Rejected;
        RejectionReason = rejectionReason;
        Notes = notes;

        if (defects != null)
            Defects = defects;

        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Mark all items as rejected (return to supplier)</summary>
    public void MarkFullyRejected(string rejectionReason, string? notes = null)
    {
        if (Status != InspectionStatus.Pending)
            throw new InvalidOperationException("Can only reject pending inspections");

        QuantityAccepted = 0;
        QuantityRejected = QuantityReceivedForInspection;
        Status = InspectionStatus.Rejected;
        RejectionReason = rejectionReason;
        Notes = notes;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Add defect details for specific units</summary>
    public void AddDefect(int unitNumber, string description, string? severity = null)
    {
        Defects.Add(new InspectionDefect
        {
            UnitNumber = unitNumber,
            DefectDescription = description,
            Severity = severity
        });
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

/// <summary>Individual unit defect details</summary>
public class InspectionDefect
{
    public int UnitNumber { get; set; }          // e.g., "Unit 5 of 500"
    public string? DefectDescription { get; set; } // e.g., "Cracked corner", "Torn packaging"
    public string? Severity { get; set; }        // "Minor", "Major", "Critical"
}

public enum InspectionStatus
{
    None = 0,
    Pending,              // Awaiting inspection
    Accepted,             // All units passed
    PartiallyAccepted,    // Some passed, some rejected
    Rejected,             // All units failed
    AcceptedWithDefects   // Accepted but defects noted (still usable)
}


