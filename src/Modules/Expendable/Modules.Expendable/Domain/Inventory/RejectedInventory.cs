using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Expendable.Domain.Inventory;

/// <summary>
/// Tracks rejected inventory awaiting return to supplier or disposal.
/// Separate aggregate for rejected stock management.
/// </summary>
public class RejectedInventory : AggregateRoot<Guid>
    , IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    // Reference to Purchase
    public Guid PurchaseId { get; private set; }

    // Reference to Inspection
    public Guid PurchaseInspectionId { get; private set; }

    // Product Info
    public Guid ProductId { get; private set; }
    public string? ProductCode { get; private set; }
    public string? ProductName { get; private set; }

    // Warehouse Location
    public Guid WarehouseLocationId { get; private set; }
    public string? WarehouseLocationName { get; private set; }

    // Rejected Stock Details
    public int QuantityRejected { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalValue => QuantityRejected * UnitPrice;

    // Rejection Details
    public string? RejectionReason { get; private set; }  // e.g., "Damaged", "Wrong Qty", "Defective"
    public string? Notes { get; private set; }
    public DateTimeOffset RejectionDate { get; private set; }

    // Disposition
    public RejectedInventoryStatus Status { get; private set; }  // AwaitingDisposition, ReturnedToSupplier, Disposed
    public DateTimeOffset? DispositionDate { get; private set; }
    public string? DispositionNotes { get; private set; }

    // Optimistic Locking
    public byte[] Version { get; private set; } = [];

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDeletable
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    private RejectedInventory() { }

    /// <summary>Factory: Create rejected inventory record</summary>
    public static RejectedInventory Create(
        Guid purchaseId,
        Guid productId,
        Guid purchaseInspectionId,
        string productCode,
        string productName,
        Guid warehouseLocationId,
        string warehouseLocationName,
        int quantityRejected,
        decimal unitPrice,
        string rejectionReason,
        string tenantId,
        string? notes = null)
    {
        if (quantityRejected <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");
        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("Rejection reason is required");

        return new RejectedInventory
        {
            Id = Guid.NewGuid(),
            PurchaseId = purchaseId,
            ProductId = productId,
            PurchaseInspectionId = purchaseInspectionId,
            ProductCode = productCode,
            ProductName = productName,
            WarehouseLocationId = warehouseLocationId,
            WarehouseLocationName = warehouseLocationName,
            QuantityRejected = quantityRejected,
            UnitPrice = unitPrice,
            RejectionReason = rejectionReason,
            Notes = notes,
            Status = RejectedInventoryStatus.AwaitingDisposition,
            RejectionDate = DateTimeOffset.UtcNow,
            TenantId = tenantId,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Mark as returned to supplier</summary>
    public void MarkAsReturned(int? quantityReturned = null, string? notes = null)
    {
        if (Status == RejectedInventoryStatus.Disposed)
            throw new InvalidOperationException("Cannot return disposed inventory");

        Status = RejectedInventoryStatus.ReturnedToSupplier;
        DispositionDate = DateTimeOffset.UtcNow;
        DispositionNotes = notes ?? $"Returned {quantityReturned ?? QuantityRejected} units";
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Mark as disposed (written off, destroyed, donated, etc.)</summary>
    public void MarkAsDisposed(string disposalMethod, string? notes = null)
    {
        if (Status == RejectedInventoryStatus.ReturnedToSupplier)
            throw new InvalidOperationException("Cannot dispose already returned inventory");

        Status = RejectedInventoryStatus.Disposed;
        DispositionDate = DateTimeOffset.UtcNow;
        DispositionNotes = notes ?? $"Disposed via {disposalMethod}";
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Revert to awaiting disposition if previous decision was wrong</summary>
    public void RevertToAwaitingDisposition()
    {
        Status = RejectedInventoryStatus.AwaitingDisposition;
        DispositionDate = null;
        DispositionNotes = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}

public enum RejectedInventoryStatus
{
    None = 0,
    AwaitingDisposition,      // No action taken yet
    ReturnedToSupplier,       // Shipped back for credit
    Disposed                  // Written off, destroyed, or donated
}


