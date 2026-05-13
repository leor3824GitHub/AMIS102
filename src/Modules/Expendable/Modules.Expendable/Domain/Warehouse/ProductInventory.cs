using System.Collections.ObjectModel;
using System.Security.Cryptography;
using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Expendable.Domain.Warehouse;

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
    public decimal TotalValue { get; private set; }        // Moving-average inventory value
    public decimal ReservedValue { get; private set; }     // Reserved quantity valued at moving-average cost
    public decimal AverageUnitPrice => QuantityOnHand > 0
        ? Math.Round(TotalValue / QuantityOnHand, 4)
        : 0m;

    // Purchase receipt batches retained for traceability
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
            Version = NewVersion(),
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    private static byte[] NewVersion() => RandomNumberGenerator.GetBytes(8);

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
        TotalValue = Math.Round(TotalValue + (quantityAccepted * unitPrice), 2, MidpointRounding.AwayFromZero);
        RecalculateReservedValue();

        if (FirstReceiptDate == null)
            FirstReceiptDate = DateTimeOffset.UtcNow;
        LastReceiptDate = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();
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
        Version = NewVersion();
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
        Version = NewVersion();
    }

    /// <summary>Issue reserved stock to employee using moving-average valuation.</summary>
    public IssuanceDetail IssueReservedStock(int quantityToIssue)
    {
        if (quantityToIssue <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (QuantityReserved < quantityToIssue)
            throw new InvalidOperationException(
                $"Insufficient reserved stock. Reserved: {QuantityReserved}, Requested to issue: {quantityToIssue}");

        var averageUnitPrice = AverageUnitPrice;
        var totalIssuedValue = Math.Round(quantityToIssue * averageUnitPrice, 2, MidpointRounding.AwayFromZero);

        QuantityReserved -= quantityToIssue;
        QuantityIssued += quantityToIssue;

        TotalValue = Math.Round(
            Math.Max(0m, TotalValue - totalIssuedValue),
            2,
            MidpointRounding.AwayFromZero);
        RecalculateReservedValue();
        LastIssueDate = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        Version = NewVersion();

        return new IssuanceDetail
        {
            ProductId = ProductId,
            QuantityIssued = quantityToIssue,
            UnitPrice = averageUnitPrice,
            TotalValue = totalIssuedValue
        };
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

    private void RecalculateReservedValue()
    {
        ReservedValue = Math.Round(QuantityReserved * AverageUnitPrice, 2, MidpointRounding.AwayFromZero);
    }
}

/// <summary>Receipt batch: Tracks items from a specific purchase at a specific price</summary>
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

/// <summary>Aggregate issuance details for moving-average valuation.</summary>
public class IssuanceDetail
{
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


