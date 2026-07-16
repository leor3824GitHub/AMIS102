using System.Collections.ObjectModel;
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

    // Product Identity — the product's code/name are NOT snapshotted here; readers join Product so the
    // warehouse always shows the live name (no rename drift). See WarehouseMapper.
    public Guid ProductId { get; private set; }

    // Warehouse Location (id only; the display name resolves via ExpendableModuleConstants.ResolveWarehouseName)
    public Guid WarehouseLocationId { get; private set; }

    // Stock Quantities
    public int QuantityAvailable { get; private set; }     // Ready to issue (not reserved/not issued)
    public int QuantityReserved { get; private set; }      // Allocated to supply requests (awaiting delivery)
    public int QuantityIssued { get; private set; }        // Total ever issued to employees
    public int QuantityOnHand => QuantityAvailable + QuantityReserved;  // Total in warehouse

    // Value Tracking (for cost accounting)
    public decimal TotalValue { get; private set; }        // Moving-average inventory value
    public decimal AverageUnitPrice => QuantityOnHand > 0
        ? Math.Round(TotalValue / QuantityOnHand, 4)
        : 0m;
    // Pure derivation (like AverageUnitPrice) — recomputed on read, never stored, so it can't drift.
    public decimal ReservedValue => Math.Round(QuantityReserved * AverageUnitPrice, 2, MidpointRounding.AwayFromZero);

    // Purchase receipt batches retained for traceability
    public Collection<InventoryBatch> Batches { get; private set; } = new Collection<InventoryBatch>();

    // System Dates
    public DateTimeOffset? FirstReceiptDate { get; private set; }
    public DateTimeOffset? LastReceiptDate { get; private set; }
    public DateTimeOffset? LastIssueDate { get; private set; }

    // Status for lifecycle management
    public ProductInventoryStatus Status { get; private set; }  // Active, Discontinued, Archived

    // Optimistic concurrency is handled by the Postgres system column xmin (mapped in
    // ProductInventoryConfiguration), mirroring AssetRegistry — no domain-managed token field.

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
        Guid warehouseLocationId)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required");
        if (warehouseLocationId == Guid.Empty)
            throw new ArgumentException("Warehouse location ID is required");

        return new ProductInventory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            WarehouseLocationId = warehouseLocationId,
            QuantityAvailable = 0,
            QuantityReserved = 0,
            QuantityIssued = 0,
            TotalValue = 0,
            Status = ProductInventoryStatus.Active,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Receive inspected/accepted stock into the warehouse.
    /// <paramref name="sourceReceiptId"/> is the originating receipt id (an accepted IAR's id);
    /// <paramref name="sourceReference"/> is its human-readable document number (e.g. IAR number) for the Stock Card.</summary>
    public void ReceiveFromPurchase(
        Guid sourceReceiptId,
        Guid productId,
        int quantityAccepted,
        decimal unitPrice,
        string? sourceReference = null)
    {
        if (quantityAccepted <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");

        var batch = InventoryBatch.Create(sourceReceiptId, productId, quantityAccepted, unitPrice, sourceReference);
        Batches.Add(batch);

        QuantityAvailable += quantityAccepted;
        TotalValue = Math.Round(TotalValue + (quantityAccepted * unitPrice), 2, MidpointRounding.AwayFromZero);

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

        LastModifiedOnUtc = DateTimeOffset.UtcNow;
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
        LastIssueDate = DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;

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
}

/// <summary>Receipt batch: Tracks items from a specific purchase at a specific price</summary>
public class InventoryBatch
{
    /// <summary>Stable identity — the primary key now that batches live in their own relational table
    /// (<c>ProductInventoryBatches</c>) instead of a JSON-owned collection.</summary>
    public Guid Id { get; private set; }

    public Guid PurchaseId { get; private set; }
    public Guid ProductId { get; private set; }

    // Quantities
    public int QuantityAvailable { get; private set; }     // Total received from this batch
    public int QuantityIssued { get; private set; }        // Issued so far from this batch
    public int QuantityRemaining => QuantityAvailable - QuantityIssued;

    // Pricing
    public decimal UnitPrice { get; private set; }
    public decimal TotalValue => QuantityRemaining * UnitPrice;

    /// <summary>Human-readable source document number for this receipt (e.g. the IAR number). Shown as the
    /// Reference on the Stock Card. Null for legacy batches received before this field existed.</summary>
    public string? SourceReference { get; private set; }

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
        decimal unitPrice,
        string? sourceReference = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");

        return new InventoryBatch
        {
            Id = Guid.NewGuid(),
            PurchaseId = purchaseId,
            ProductId = productId,
            QuantityAvailable = quantity,
            QuantityIssued = 0,
            UnitPrice = unitPrice,
            SourceReference = string.IsNullOrWhiteSpace(sourceReference) ? null : sourceReference.Trim(),
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


