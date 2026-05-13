using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.Expendable.Domain.Purchases;

/// <summary>Purchase status enumeration</summary>
public enum PurchaseStatus
{
    None = 0,
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    PartiallyReceived = 4,
    FullyReceived = 5,
    Cancelled = 6
}

/// <summary>Purchase line item value object</summary>
public class PurchaseLineItem
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int ReceivedQuantity { get; set; }
    public int QuantityInspection { get; set; }
    public int RejectedQuantity { get; set; }
    public DateTimeOffset? ReceiptDate { get; set; }

    public PurchaseLineItem(Guid productId, string productCode, string productName, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductCode = productCode;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        ReceivedQuantity = 0;
        QuantityInspection = 0;
        RejectedQuantity = 0;
    }

    public decimal LineTotal => Quantity * UnitPrice;
}

public class Purchase : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string PurchaseOrderNumber { get; private set; } = default!;
    public string SupplierId { get; private set; } = default!;
    public string SupplierName { get; private set; } = default!;
    public Guid WarehouseLocationId { get; private set; }
    public string WarehouseLocationName { get; private set; } = default!;
    public DateTimeOffset OrderDate { get; private set; }
    public DateTimeOffset? ExpectedDeliveryDate { get; set; }
    public DateTimeOffset? DeliveryDate { get; set; }
    public DateTimeOffset? ReceiptDate { get; private set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? ReceivingNotes { get; set; }
    public byte[] Version { get; set; } = [];

    private readonly List<PurchaseLineItem> _lineItems = [];
    public IReadOnlyCollection<PurchaseLineItem> LineItems => _lineItems.AsReadOnly();

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDeletable
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Factory method to create a new purchase order</summary>
    public static Purchase Create(
        string tenantId,
        string poNumber,
        string supplierId,
        string supplierName,
        Guid warehouseLocationId,
        string warehouseLocationName,
        DateTimeOffset? expectedDelivery = null)
    {
        return new Purchase
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PurchaseOrderNumber = poNumber,
            SupplierId = supplierId,
            SupplierName = supplierName,
            WarehouseLocationId = warehouseLocationId,
            WarehouseLocationName = warehouseLocationName,
            OrderDate = DateTimeOffset.UtcNow,
            ExpectedDeliveryDate = expectedDelivery,
            Status = PurchaseStatus.Draft,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Add a line item to the purchase order</summary>
    public void AddLineItem(Guid productId, string productCode, string productName, int quantity, decimal unitPrice)
    {
        if (Status != PurchaseStatus.Draft)
            throw new InvalidOperationException("Cannot add line items to a submitted purchase order.");

        var existingItem = _lineItems.FirstOrDefault(x => x.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            existingItem.ProductCode = productCode;
            existingItem.ProductName = productName;
            existingItem.UnitPrice = unitPrice;
        }
        else
        {
            _lineItems.Add(new PurchaseLineItem(productId, productCode, productName, quantity, unitPrice));
        }

        RecalculateTotalAmount();
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Remove a line item from the purchase order</summary>
    public void RemoveLineItem(Guid productId)
    {
        if (Status != PurchaseStatus.Draft)
            throw new InvalidOperationException("Cannot remove line items from a submitted purchase order.");

        _lineItems.RemoveAll(x => x.ProductId == productId);
        RecalculateTotalAmount();
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Submit the purchase order</summary>
    public void Submit()
    {
        if (Status != PurchaseStatus.Draft)
            throw new InvalidOperationException("Only draft purchase orders can be submitted.");

        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Cannot submit purchase order without line items.");

        Status = PurchaseStatus.Submitted;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Approve the purchase order</summary>
    public void Approve()
    {
        if (Status != PurchaseStatus.Submitted)
            throw new InvalidOperationException("Only submitted purchase orders can be approved.");

        Status = PurchaseStatus.Approved;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Record receipt of items</summary>
    public void RecordReceipt(Guid productId, int receivedQuantity, int rejectedQuantity = 0)
    {
        if (Status is not PurchaseStatus.Approved and not PurchaseStatus.PartiallyReceived)
            throw new InvalidOperationException("Can only record receipt for approved purchase orders.");

        var lineItem = _lineItems.FirstOrDefault(x => x.ProductId == productId);
        if (lineItem == null)
            throw new InvalidOperationException($"Product {productId} not found in this purchase order.");

        if (receivedQuantity < 0 || rejectedQuantity < 0)
            throw new InvalidOperationException("Receipt quantities cannot be negative.");

        lineItem.ReceivedQuantity += receivedQuantity;
        lineItem.QuantityInspection += receivedQuantity;
        lineItem.RejectedQuantity += rejectedQuantity;
        lineItem.ReceiptDate = DateTimeOffset.UtcNow;

        if (lineItem.ReceivedQuantity > lineItem.Quantity)
            throw new InvalidOperationException("Received quantity cannot exceed ordered quantity.");

        if (lineItem.RejectedQuantity > lineItem.ReceivedQuantity)
            throw new InvalidOperationException("Rejected quantity cannot exceed received quantity.");

        UpdateStatus();
        ReceiptDate ??= DateTimeOffset.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void CompleteInspection(Guid productId, int quantityAccepted, int quantityRejected)
    {
        var lineItem = _lineItems.FirstOrDefault(x => x.ProductId == productId);
        if (lineItem == null)
            throw new InvalidOperationException($"Product {productId} not found in this purchase order.");

        var totalInspected = quantityAccepted + quantityRejected;
        if (totalInspected <= 0)
            throw new InvalidOperationException("Inspection total must be greater than zero.");

        if (lineItem.QuantityInspection < totalInspected)
            throw new InvalidOperationException("Inspection quantities exceed pending inspection quantity.");

        lineItem.QuantityInspection -= totalInspected;
        lineItem.RejectedQuantity += quantityRejected;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Cancel the purchase order</summary>
    public void Cancel()
    {
        if (Status == PurchaseStatus.FullyReceived || Status == PurchaseStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel received or already cancelled purchase orders.");

        Status = PurchaseStatus.Cancelled;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Soft delete the purchase</summary>
    public void SoftDelete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }

    private void RecalculateTotalAmount()
    {
        TotalAmount = _lineItems.Sum(x => x.LineTotal);
    }

    private void UpdateStatus()
    {
        var allReceived = _lineItems.All(x => x.ReceivedQuantity == x.Quantity);
        var anyReceived = _lineItems.Any(x => x.ReceivedQuantity > 0);

        if (allReceived)
        {
            Status = PurchaseStatus.FullyReceived;
            DeliveryDate = DateTimeOffset.UtcNow;
        }
        else if (anyReceived && Status == PurchaseStatus.Approved)
        {
            Status = PurchaseStatus.PartiallyReceived;
        }
    }
}


