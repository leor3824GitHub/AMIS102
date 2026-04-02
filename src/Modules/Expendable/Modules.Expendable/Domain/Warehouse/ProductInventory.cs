using System.Collections.ObjectModel;
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Expendable.Domain.Warehouse;

/// <summary>
/// Central warehouse inventory ledger for a product at a specific location.
/// Multi-warehouse support: Each location has its own ProductInventory record per product.
/// </summary>
public class ProductInventory : AggregateRoot<Guid>
    , IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;

    // Product Identity
    public Guid ProductId { get; private set; }
    public string? ProductCode { get; private set; }
    public string? ProductName { get; private set; }

    // Warehouse Location (Multi-warehouse support)
    public Guid WarehouseLocationId { get; private set; }  // e.g., "Central Warehouse", "Branch Office", "Supply Room"
    public string? WarehouseLocationName { get; private set; }

    // Stock Quantities
    public int QuantityAvailable { get; private set; }     // Ready to issue (not reserved/not issued)
    public int QuantityReserved { get; private set; }      // Allocated to supply requests (awaiting delivery)
    public int QuantityIssued { get; private set; }        // Total ever issued to employees
    public int QuantityOnHand => QuantityAvailable + QuantityReserved;  // Total in warehouse

    // Value Tracking (for cost accounting)
    public decimal TotalValue { get; private set; }        // Sum of all batches' value
    public decimal ReservedValue { get; private set; }     // Value of reserved stock

    // FIFO Batches (from multiple Purchase Orders)
    public Collection<InventoryBatch> Batches { get; private set; } = new Collection<InventoryBatch>();

    // System Dates
    public DateTimeOffset? FirstReceiptDate { get; private set; }
    public DateTimeOffset? LastReceiptDate { get; private set; }
    public DateTimeOffset? LastIssueDate { get; private set; }

    // Status for lifecycle management
    public ProductInventoryStatus Status { get; private set; }  // Active, Discontinued, Archived

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

    private ProductInventory() { }

    /// <summary>Factory: Create new product inventory for a warehouse location</summary>
    public static ProductInventory Create(
        string tenantId,
        Guid productId,
        string productCode,
        string productName,
        Guid warehouseLocationId,
        string warehouseLocationName)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            throw new ArgumentException("Product code is required");
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required");
        if (warehouseLocationId == Guid.Empty)
            throw new ArgumentException("Warehouse location ID is required");

        return new ProductInventory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            ProductCode = productCode,
            ProductName = productName,
            WarehouseLocationId = warehouseLocationId,
            WarehouseLocationName = warehouseLocationName,
            QuantityAvailable = 0,
            QuantityReserved = 0,
            QuantityIssued = 0,
            TotalValue = 0,
            ReservedValue = 0,
            Status = ProductInventoryStatus.Active,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Receive inspected stock into warehouse</summary>
    public void ReceiveFromPurchase(
        Guid purchaseId,
        Guid productId,
        int quantityAccepted,
        decimal unitPrice)
    {
        if (quantityAccepted <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");

        var batch = InventoryBatch.Create(purchaseId, productId, quantityAccepted, unitPrice);
        Batches.Add(batch);

        QuantityAvailable += quantityAccepted;
        RecalculateValue();

        if (FirstReceiptDate == null)
            FirstReceiptDate = DateTimeOffset.UtcNow;
        LastReceiptDate = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Reserve stock for a supply request (allocation)</summary>
    public void ReserveForAllocation(int quantityToReserve)
    {
        if (quantityToReserve <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (QuantityAvailable < quantityToReserve)
            throw new InvalidOperationException(
                $"Insufficient available stock. Available: {QuantityAvailable}, Requested to reserve: {quantityToReserve}");

        QuantityAvailable -= quantityToReserve;
        QuantityReserved += quantityToReserve;

        RecalculateReservedValue();
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Cancel a reservation (if supply request is rejected)</summary>
    public void CancelReservation(int quantityToRelease)
    {
        if (quantityToRelease <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (QuantityReserved < quantityToRelease)
            throw new InvalidOperationException(
                $"Cannot release {quantityToRelease}. Only {QuantityReserved} reserved");

        QuantityReserved -= quantityToRelease;
        QuantityAvailable += quantityToRelease;

        RecalculateReservedValue();
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Issue reserved stock to employee (FIFO batches)</summary>
    public Collection<IssuedBatchDetail> IssueFromBatches(int quantityToIssue)
    {
        if (quantityToIssue <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (QuantityReserved < quantityToIssue)
            throw new InvalidOperationException(
                $"Insufficient reserved stock. Reserved: {QuantityReserved}, Requested to issue: {quantityToIssue}");

        var issuedDetails = new Collection<IssuedBatchDetail>();
        var remaining = quantityToIssue;

        // FIFO: Issue from oldest batch first
        foreach (var batch in Batches.OrderBy(b => b.ReceivedDate))
        {
            if (remaining <= 0) break;

            var canIssueFromBatch = Math.Min(batch.QuantityAvailable - batch.QuantityIssued, remaining);
            if (canIssueFromBatch > 0)
            {
                batch.MarkIssued(canIssueFromBatch);

                issuedDetails.Add(new IssuedBatchDetail
                {
                    PurchaseId = batch.PurchaseId,
                    ProductId = batch.ProductId,
                    QuantityIssued = canIssueFromBatch,
                    UnitPrice = batch.UnitPrice,
                    TotalValue = canIssueFromBatch * batch.UnitPrice
                });

                QuantityReserved -= canIssueFromBatch;
                QuantityIssued += canIssueFromBatch;
                remaining -= canIssueFromBatch;
            }
        }

        if (remaining > 0)
            throw new InvalidOperationException(
                $"Inventory data inconsistency: could only issue {quantityToIssue - remaining} of {quantityToIssue} from FIFO batches for product {ProductId}. " +
                "Reserved quantity exceeds actual batch stock.");

        RecalculateValue();
        LastIssueDate = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;

        return issuedDetails;
    }

    /// <summary>Get available stock for allocation (not reserved)</summary>
    public int AvailableForAllocation => QuantityAvailable;

    /// <summary>Discontinue product from this warehouse</summary>
    public void Discontinue()
    {
        if (QuantityOnHand > 0)
            throw new InvalidOperationException("Cannot discontinue product with stock remaining. Issue or return all items first.");

        Status = ProductInventoryStatus.Discontinued;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    private void RecalculateValue()
    {
        TotalValue = Batches.Sum(b => b.TotalValue);
    }

    private void RecalculateReservedValue()
    {
        ReservedValue = Batches
            .Where(b => b.QuantityAvailable > b.QuantityIssued)
            .Sum(b => (Math.Min(b.QuantityAvailable - b.QuantityIssued, QuantityReserved)) * b.UnitPrice);
    }
}

/// <summary>FIFO Batch: Tracks items from a specific purchase at a specific price</summary>
public class InventoryBatch
{
    public Guid PurchaseId { get; private set; }
    public Guid ProductId { get; private set; }

    // Quantities
    public int QuantityAvailable { get; private set; }     // Total received from this batch
    public int QuantityIssued { get; private set; }        // Issued so far from this batch
    public int QuantityRemaining => QuantityAvailable - QuantityIssued;

    // Pricing
    public decimal UnitPrice { get; private set; }
    public decimal TotalValue => QuantityRemaining * UnitPrice;

    // Dates
    public DateTimeOffset ReceivedDate { get; private set; }
    public DateTimeOffset? InspectionDate { get; private set; }
    public DateTimeOffset? FirstIssueDate { get; private set; }

    // Optimistic locking for concurrency control
    public int Version { get; private set; }

    private InventoryBatch() { }

    public static InventoryBatch Create(
        Guid purchaseId,
        Guid productId,
        int quantity,
        decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");

        return new InventoryBatch
        {
            PurchaseId = purchaseId,
            ProductId = productId,
            QuantityAvailable = quantity,
            QuantityIssued = 0,
            UnitPrice = unitPrice,
            ReceivedDate = DateTimeOffset.UtcNow,
            InspectionDate = DateTimeOffset.UtcNow,
            Version = 1
        };
    }

    public void MarkIssued(int quantityIssued)
    {
        if (quantityIssued <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (QuantityRemaining < quantityIssued)
            throw new InvalidOperationException(
                $"Cannot issue {quantityIssued}. Only {QuantityRemaining} available from batch");

        QuantityIssued += quantityIssued;
        Version++;  // Increment for optimistic locking

        if (FirstIssueDate == null)
            FirstIssueDate = DateTimeOffset.UtcNow;
    }
}

/// <summary>Details of quantities issued from each batch (for audit trail)</summary>
public class IssuedBatchDetail
{
    public Guid PurchaseId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityIssued { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalValue { get; set; }
}

public enum ProductInventoryStatus
{
    None = 0,
    Active,          // Accepting stock, can issue
    Discontinued,    // No longer accepting new stock
    Archived         // Closed for analysis purposes
}

