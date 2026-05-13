# Module 2: Expendable - Employee Online Shopping & Supply Request System

## Overview

**Module 2 (Expendable)** is an online shopping platform for employee-initiated supply requests, similar to e-commerce but for expendable items. It operates as a read-only consumer of **Module 3 (Library)** master data.

**Account-Based Access Model:**
- **Registered Employees**: Can browse catalog, add to cart, manage wishlist, and checkout supply requests online
- **Guest/Walk-In Employees**: Must visit supply office for in-person requests (no online access)
- **Supply Office Staff**: Can process walk-in requests and manage inventory

This system tracks:
- Online shopping carts for registered employees
- Supply request workflow (browse → add to cart → checkout → approval → allocation)
- Expendable item distribution to employees
- Usage tracking and inventory reconciliation
- Guest/walk-in request handling

---

## Architecture

### Projects:
- `Modules.Expendable.Contracts` — Public API (DTOs, Commands, Queries)
- `Modules.Expendable` — Implementation (internal logic)

### Location: 
`src/Modules/Expendable/`

### Dependencies:
- **Depends on:** `Modules.Library.Contracts` (read-only queries for employees/suppliers)
- **No dependencies from:** Library should never depend on Expendable
- **Database:** Separate `ExpenableDbContext`

### Note on Products:
Products for expendable items are **managed in this module** (not Library). This allows for category management, stock levels, and expendable-specific attributes separate from the general product library.

---

## Core Entities

### Product (Expendable Items Catalog)

**File:** `src/Modules/Expendable/Domain/Product.cs`

Represents expendable items available for employee requests. Manages the catalog of supplies that can be ordered through the system.

```csharp
public class Product : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    // Product Identity
    public string ProductCode { get; private set; }  // e.g., "EXP-PEN-BLK-001"
    public string ProductName { get; private set; }
    public string? Description { get; private set; }
    
    // Product Details
    public string Category { get; private set; }  // e.g., "Office Supplies", "Cleaning", "IT Equipment"
    public string UnitOfMeasure { get; private set; }  // e.g., "Box", "Pack", "Ream", "Unit", "Case"
    public decimal MinimumOrderQuantity { get; private set; }  // Minimum qty per order
    public bool RequiresApproval { get; private set; }  // If true, request needs approval even if authorized qty
    
    // Pricing & Costs
    public decimal EstimatedUnitCost { get; private set; }  // Estimated cost for budgeting (may differ from actual purchase price)
    public decimal MaxApprovedQuantityPerRequest { get; private set; }  // Max qty employee can request without higher approval
    
    // Status
    public ProductStatus Status { get; private set; }  // Active, Inactive, Discontinued
    public DateTime? ActivatedDate { get; private set; }
    public DateTime? InactivatedDate { get; private set; }
    
    // Auditing
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    
    // Multi-tenancy
    public string TenantId { get; private set; }
    
    // Factory Method
    public static Product Create(
        string productCode,
        string productName,
        string category,
        string unitOfMeasure,
        decimal estimatedUnitCost,
        decimal minOrderQty = 1,
        decimal maxApprovedQty = 10,
        string? description = null,
        bool requiresApproval = false)
    {
        if (string.IsNullOrWhiteSpace(productCode))
            throw new ArgumentException("Product code is required");
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required");
        if (estimatedUnitCost < 0)
            throw new ArgumentException("Estimated unit cost cannot be negative");
        
        return new Product
        {
            Id = Guid.NewGuid(),
            ProductCode = productCode,
            ProductName = productName,
            Description = description,
            Category = category,
            UnitOfMeasure = unitOfMeasure,
            MinimumOrderQuantity = minOrderQty,
            MaxApprovedQuantityPerRequest = maxApprovedQty,
            RequiresApproval = requiresApproval,
            EstimatedUnitCost = estimatedUnitCost,
            Status = ProductStatus.Active,
            ActivatedDate = DateTime.UtcNow,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }
    
    // Methods
    public void UpdateDetails(
        string productName,
        string category,
        string unitOfMeasure,
        decimal estimatedUnitCost,
        decimal minOrderQty,
        decimal maxApprovedQty,
        string? description = null,
        bool requiresApproval = false)
    {
        ProductName = productName;
        Category = category;
        UnitOfMeasure = unitOfMeasure;
        EstimatedUnitCost = estimatedUnitCost;
        MinimumOrderQuantity = minOrderQty;
        MaxApprovedQuantityPerRequest = maxApprovedQty;
        Description = description;
        RequiresApproval = requiresApproval;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
    
    public void Activate()
    {
        if (Status == ProductStatus.Active)
            throw new InvalidOperationException("Product is already active");
        
        Status = ProductStatus.Active;
        ActivatedDate = DateTime.UtcNow;
        InactivatedDate = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
    
    public void Deactivate(string reason = "")
    {
        if (Status == ProductStatus.Inactive)
            throw new InvalidOperationException("Product is already inactive");
        
        Status = ProductStatus.Inactive;
        InactivatedDate = DateTime.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
    
    public void Discontinue()
    {
        Status = ProductStatus.Discontinued;
        InactivatedDate = DateTime.UtcNow;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
    
    public bool IsAvailable => Status == ProductStatus.Active;
}

public enum ProductStatus
{
    Active,         // Available for ordering
    Inactive,       // Not available, but may be reactivated
    Discontinued    // Permanently discontinued
}
```

---

### EmployeeShoppingCart (Aggregate)

**File:** `src/Modules/Expendable/Domain/EmployeeShoppingCart.cs`

Represents an employee's online shopping cart for browsing and selecting expendable items.

```csharp
public class EmployeeShoppingCart : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    // Cart Identity
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; }
    public string EmployeeEmail { get; private set; }
    
    // Cart Items
    public List<CartItem> Items { get; private set; } = [];
    public CartStatus Status { get; private set; } // Active, Abandoned, CheckedOut
    
    // Cart Metadata
    public DateTime CreatedDate { get; private set; }
    public DateTime? LastModifiedDate { get; private set; }
    public DateTime? CheckoutDate { get; private set; }
    public Guid? CheckedOutRequestId { get; private set; } // Link to SupplyRequest after checkout
    
    // Auditing
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    
    // Multi-tenancy
    public string TenantId { get; private set; }
    
    // Factory Method
    public static EmployeeShoppingCart Create(
        Guid employeeId,
        string employeeName,
        string employeeEmail)
    {
        return new EmployeeShoppingCart
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            EmployeeEmail = employeeEmail,
            Status = CartStatus.Active,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
    }
    
    // Methods
    public void AddItem(Guid productId, string productCode, string productName, int quantity, decimal unitPrice)
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot modify non-active carts");
        
        var existingItem = Items.FirstOrDefault(i => i.ProductId == productId);
        
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            Items.Add(CartItem.Create(productId, productCode, productName, quantity, unitPrice));
        }
        
        LastModifiedDate = DateTime.UtcNow;
    }
    
    public void RemoveItem(Guid productId)
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot modify non-active carts");
        
        var item = Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new InvalidOperationException($"Item {productId} not in cart");
        
        Items.Remove(item);
        LastModifiedDate = DateTime.UtcNow;
    }
    
    public void UpdateItemQuantity(Guid productId, int newQuantity)
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot modify non-active carts");
        
        if (newQuantity <= 0)
        {
            RemoveItem(productId);
            return;
        }
        
        var item = Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new InvalidOperationException($"Item {productId} not in cart");
        
        item.UpdateQuantity(newQuantity);
        LastModifiedDate = DateTime.UtcNow;
    }
    
    public void Clear()
    {
        Items.Clear();
        Status = CartStatus.Active;
        LastModifiedDate = DateTime.UtcNow;
    }
    
    public void Checkout(Guid requestId)
    {
        if (Items.Count == 0)
            throw new InvalidOperationException("Cannot checkout empty cart");
        
        Status = CartStatus.CheckedOut;
        CheckoutDate = DateTime.UtcNow;
        CheckedOutRequestId = requestId;
        LastModifiedDate = DateTime.UtcNow;
    }
    
    public decimal GetTotalPrice() => Items.Sum(i => i.TotalPrice);
    public int GetTotalItems() => Items.Sum(i => i.Quantity);
}

public enum CartStatus
{
    Active,        // Being actively modified
    Abandoned,     // Not checked out after X days
    CheckedOut     // Converted to SupplyRequest
}
```

### CartItem (Value Object)

```csharp
public class CartItem : ValueObject
{
    public Guid ProductId { get; private set; }
    public string ProductCode { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    public DateTime AddedDate { get; private set; }
    
    private CartItem() { }
    
    public static CartItem Create(
        Guid productId,
        string productCode,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        return new CartItem
        {
            ProductId = productId,
            ProductCode = productCode,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            AddedDate = DateTime.UtcNow
        };
    }
    
    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        Quantity = newQuantity;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProductId;
    }
}
```

### Purchase (Inventory Cost Tracking)

**File:** `src/Modules/Expendable/Domain/Purchase.cs`

Tracks incoming purchases/stock with pricing information for accurate inventory valuation and controlled issuance.

```csharp
public class Purchase : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    // Purchase Identity
    public string PurchaseOrderNumber { get; private set; } // e.g., PO-2026-001
    public Guid SupplierId { get; private set; } // From Library module
    public string SupplierName { get; private set; }
    
    // Purchase Details
    public DateTime PurchaseDate { get; private set; }
    public DateTime? ReceiptDate { get; private set; }
    public decimal TotalCost { get; private set; }
    public List<PurchaseLineItem> LineItems { get; private set; } = [];
    
    // Status
    public PurchaseStatus Status { get; private set; } // Ordered, Received, PartiallyIssued, FullyIssued
    
    // Tracking
    public decimal TotalIssuedValue { get; private set; }
    public int TotalItemsReceived { get; private set; }
    public int TotalItemsIssued { get; private set; }
    
    // Reference to supply requests using items from this purchase
    public List<Guid> AssociatedSupplyRequestIds { get; private set; } = [];
    
    // Auditing
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    
    // Multi-tenancy
    public string TenantId { get; private set; }
    
    // Factory Method
    public static Purchase Create(
        Guid supplierId,
        string supplierName,
        DateTime purchaseDate)
    {
        var poNumber = $"PO-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().GetHashCode() % 10000:D4}";
        
        return new Purchase
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = poNumber,
            SupplierId = supplierId,
            SupplierName = supplierName,
            PurchaseDate = purchaseDate,
            Status = PurchaseStatus.Ordered,
            TotalCost = 0
        };
    }
    
    // Methods
    public void AddLineItem(Guid productId, string productCode, string productName, int quantity, decimal unitPrice)
    {
        if (Status != PurchaseStatus.Ordered)
            throw new InvalidOperationException("Cannot add items to non-ordered purchases");
        
        var existingItem = LineItems.FirstOrDefault(i => i.ProductId == productId && i.UnitPrice == unitPrice);
        
        if (existingItem != null)
        {
            existingItem.AddQuantity(quantity);
        }
        else
        {
            // Create separate line item if same product but different price
            LineItems.Add(PurchaseLineItem.Create(productId, productCode, productName, quantity, unitPrice));
        }
        
        RecalculateTotalCost();
    }
    
    public void ReceiveItems(Guid lineItemId, int quantityReceived)
    {
        if (Status != PurchaseStatus.Ordered && Status != PurchaseStatus.PartiallyIssued)
            throw new InvalidOperationException("Cannot receive items for this purchase");
        
        var lineItem = LineItems.FirstOrDefault(i => i.Id == lineItemId)
            ?? throw new InvalidOperationException($"Line item {lineItemId} not found");
        
        lineItem.MarkReceived(quantityReceived);
        TotalItemsReceived += quantityReceived;
        
        UpdateStatus();
    }
    
    public void IssueItems(Guid lineItemId, int quantityIssued)
    {
        if (Status == PurchaseStatus.Ordered)
            throw new InvalidOperationException("Cannot issue items from non-received purchase");
        
        var lineItem = LineItems.FirstOrDefault(i => i.Id == lineItemId)
            ?? throw new InvalidOperationException($"Line item {lineItemId} not found");
        
        lineItem.MarkIssued(quantityIssued);
        TotalItemsIssued += quantityIssued;
        TotalIssuedValue += lineItem.UnitPrice * quantityIssued;
        
        UpdateStatus();
    }
    
    public void AssociateSupplyRequest(Guid requestId)
    {
        if (!AssociatedSupplyRequestIds.Contains(requestId))
        {
            AssociatedSupplyRequestIds.Add(requestId);
        }
    }
    
    private void RecalculateTotalCost()
    {
        TotalCost = LineItems.Sum(i => i.TotalCost);
    }
    
    private void UpdateStatus()
    {
        if (TotalItemsIssued == 0)
        {
            Status = PurchaseStatus.Received;
        }
        else if (TotalItemsIssued < TotalItemsReceived)
        {
            Status = PurchaseStatus.PartiallyIssued;
        }
        else if (TotalItemsIssued == TotalItemsReceived)
        {
            Status = PurchaseStatus.FullyIssued;
        }
    }
}

public enum PurchaseStatus
{
    Ordered,          // Purchase order created, not yet received
    Received,         // Items received, not yet issued
    PartiallyIssued,  // Some items issued
    FullyIssued       // All items issued
}
```

### PurchaseLineItem (Value Object)

```csharp
public class PurchaseLineItem : ValueObject
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductCode { get; private set; }
    public string ProductName { get; private set; }
    
    // Quantity Tracking
    public int QuantityOrdered { get; private set; }
    public int QuantityReceived { get; private set; }
    public int QuantityIssued { get; private set; }
    public int QuantityRemaining => QuantityReceived - QuantityIssued;
    
    // Pricing
    public decimal UnitPrice { get; private set; }
    public decimal TotalCost => QuantityOrdered * UnitPrice;
    public decimal IssuedValue => QuantityIssued * UnitPrice;
    
    // Dates
    public DateTime OrderedDate { get; private set; }
    public DateTime? ReceivedDate { get; private set; }
    
    private PurchaseLineItem() { }
    
    public static PurchaseLineItem Create(
        Guid productId,
        string productCode,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");
        
        return new PurchaseLineItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductCode = productCode,
            ProductName = productName,
            QuantityOrdered = quantity,
            UnitPrice = unitPrice,
            OrderedDate = DateTime.UtcNow
        };
    }
    
    public void AddQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        QuantityOrdered += quantity;
    }
    
    public void MarkReceived(int quantityReceived)
    {
        if (quantityReceived <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        if (quantityReceived > QuantityOrdered)
            throw new InvalidOperationException("Received quantity exceeds ordered quantity");
        
        QuantityReceived = quantityReceived;
        ReceivedDate = DateTime.UtcNow;
    }
    
    public void MarkIssued(int quantityIssued)
    {
        if (quantityIssued <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        if (QuantityIssued + quantityIssued > QuantityReceived)
            throw new InvalidOperationException("Issued quantity exceeds received quantity");
        
        QuantityIssued += quantityIssued;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProductId;
        yield return UnitPrice; // Price is part of identity
    }
}
```

### SupplyRequest (Primary Aggregate)

**File:** `src/Modules/Expendable/Domain/SupplyRequest.cs`

```csharp
public class SupplyRequest : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    // Request Identity
    public string RequestNumber { get; private set; } // e.g., EXP-2026-001
    public Guid? EmployeeId { get; private set; } // Who requested (null for walk-in guests)
    public string? EmployeeName { get; private set; } // Name for walk-in employees
    public Guid? OfficeId { get; private set; } // Which office/location
    public SupplyRequestSource Source { get; private set; } // Online or WalkIn
    public Guid? ShoppingCartId { get; private set; } // Reference to cart if from online
    
    // Items being requested
    public List<SupplyRequestItem> Items { get; private set; } = [];
    
    // Status Tracking
    public SupplyRequestStatus Status { get; private set; }
    public DateTime RequestedDate { get; private set; }
    public string? Justification { get; private set; }
    
    // Approval Flow
    public Guid? ApprovedBy { get; private set; } // Employee ID of approver
    public DateTime? ApprovedDate { get; private set; }
    public string? ApprovalNotes { get; private set; }
    
    // Allocation
    public Guid? AllocatedBy { get; private set; } // Employee ID of allocator
    public DateTime? AllocatedDate { get; private set; }
    public DateTime? ExpectedDeliveryDate { get; private set; }
    
    // Auditing
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    
    // Multi-tenancy
    public string TenantId { get; private set; }
    
    // Factory Methods
    public static SupplyRequest CreateFromCart(
        Guid employeeId,
        string employeeName,
        Guid? officeId,
        Guid shoppingCartId,
        string? justification = null)
    {
        var requestNumber = $"EXP-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().GetHashCode() % 10000:D4}";
        
        return new SupplyRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            OfficeId = officeId,
            ShoppingCartId = shoppingCartId,
            RequestNumber = requestNumber,
            Source = SupplyRequestSource.Online,
            Status = SupplyRequestStatus.Pending,
            RequestedDate = DateTime.UtcNow,
            Justification = justification
        };
    }
    
    public static SupplyRequest CreateWalkInRequest(
        string employeeName,
        Guid? officeId,
        string? justification = null)
    {
        var requestNumber = $"WALKIN-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().GetHashCode() % 10000:D4}";
        
        return new SupplyRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = null,
            EmployeeName = employeeName,
            OfficeId = officeId,
            ShoppingCartId = null,
            RequestNumber = requestNumber,
            Source = SupplyRequestSource.WalkIn,
            Status = SupplyRequestStatus.Pending,
            RequestedDate = DateTime.UtcNow,
            Justification = justification
        };
    }
    
    // Methods
    public void Approve(Guid approvedBy, string? notes = null)
    {
        if (Status != SupplyRequestStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be approved");
        
        Status = SupplyRequestStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedDate = DateTime.UtcNow;
        ApprovalNotes = notes;
    }
    
    public void Allocate(Guid allocatedBy, DateTime? expectedDelivery = null)
    {
        if (Status != SupplyRequestStatus.Approved)
            throw new InvalidOperationException("Only approved requests can be allocated");
        
        Status = SupplyRequestStatus.Allocated;
        AllocatedBy = allocatedBy;
        AllocatedDate = DateTime.UtcNow;
        ExpectedDeliveryDate = expectedDelivery ?? DateTime.UtcNow.AddDays(3);
    }
    
    public void Reject(string reason)
    {
        Status = SupplyRequestStatus.Rejected;
        ApprovalNotes = reason;
        ApprovedDate = DateTime.UtcNow;
    }
    
    public void AddItem(SupplyRequestItem item)
    {
        if (Status != SupplyRequestStatus.Pending)
            throw new InvalidOperationException("Cannot add items to non-pending requests");
        
        Items.Add(item);
    }
}

### Purchase (Inventory Cost Tracking)

**File:** `src/Modules/Expendable/Domain/Purchase.cs`

Tracks incoming purchases/stock with pricing information for accurate inventory valuation and controlled issuance.

```csharp
public class Purchase : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    // Purchase Identity
    public string PurchaseOrderNumber { get; private set; } // e.g., PO-2026-001
    public Guid SupplierId { get; private set; } // From Library module
    public string SupplierName { get; private set; }
    
    // Purchase Details
    public DateTime PurchaseDate { get; private set; }
    public DateTime? ReceiptDate { get; private set; }
    public decimal TotalCost { get; private set; }
    public List<PurchaseLineItem> LineItems { get; private set; } = [];
    
    // Status
    public PurchaseStatus Status { get; private set; } // Ordered, Received, PartiallyIssued, FullyIssued
    
    // Tracking
    public decimal TotalIssuedValue { get; private set; }
    public int TotalItemsReceived { get; private set; }
    public int TotalItemsIssued { get; private set; }
    
    // Reference to supply requests using items from this purchase
    public List<Guid> AssociatedSupplyRequestIds { get; private set; } = [];
    
    // Auditing
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    
    // Multi-tenancy
    public string TenantId { get; private set; }
    
    // Factory Method
    public static Purchase Create(
        Guid supplierId,
        string supplierName,
        DateTime purchaseDate)
    {
        var poNumber = $"PO-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().GetHashCode() % 10000:D4}";
        
        return new Purchase
        {
            Id = Guid.NewGuid(),
            PurchaseOrderNumber = poNumber,
            SupplierId = supplierId,
            SupplierName = supplierName,
            PurchaseDate = purchaseDate,
            Status = PurchaseStatus.Ordered,
            TotalCost = 0
        };
    }
    
    // Methods
    public void AddLineItem(Guid productId, string productCode, string productName, int quantity, decimal unitPrice)
    {
        if (Status != PurchaseStatus.Ordered)
            throw new InvalidOperationException("Cannot add items to non-ordered purchases");
        
        var existingItem = LineItems.FirstOrDefault(i => i.ProductId == productId && i.UnitPrice == unitPrice);
        
        if (existingItem != null)
        {
            existingItem.AddQuantity(quantity);
        }
        else
        {
            // Create separate line item if same product but different price
            LineItems.Add(PurchaseLineItem.Create(productId, productCode, productName, quantity, unitPrice));
        }
        
        RecalculateTotalCost();
    }
    
    public void ReceiveItems(Guid lineItemId, int quantityReceived)
    {
        if (Status != PurchaseStatus.Ordered && Status != PurchaseStatus.PartiallyIssued)
            throw new InvalidOperationException("Cannot receive items for this purchase");
        
        var lineItem = LineItems.FirstOrDefault(i => i.Id == lineItemId)
            ?? throw new InvalidOperationException($"Line item {lineItemId} not found");
        
        lineItem.MarkReceived(quantityReceived);
        TotalItemsReceived += quantityReceived;
        
        UpdateStatus();
    }
    
    public void IssueItems(Guid lineItemId, int quantityIssued)
    {
        if (Status == PurchaseStatus.Ordered)
            throw new InvalidOperationException("Cannot issue items from non-received purchase");
        
        var lineItem = LineItems.FirstOrDefault(i => i.Id == lineItemId)
            ?? throw new InvalidOperationException($"Line item {lineItemId} not found");
        
        lineItem.MarkIssued(quantityIssued);
        TotalItemsIssued += quantityIssued;
        TotalIssuedValue += lineItem.UnitPrice * quantityIssued;
        
        UpdateStatus();
    }
    
    public void AssociateSupplyRequest(Guid requestId)
    {
        if (!AssociatedSupplyRequestIds.Contains(requestId))
        {
            AssociatedSupplyRequestIds.Add(requestId);
        }
    }
    
    private void RecalculateTotalCost()
    {
        TotalCost = LineItems.Sum(i => i.TotalCost);
    }
    
    private void UpdateStatus()
    {
        if (TotalItemsIssued == 0)
        {
            Status = PurchaseStatus.Received;
        }
        else if (TotalItemsIssued < TotalItemsReceived)
        {
            Status = PurchaseStatus.PartiallyIssued;
        }
        else if (TotalItemsIssued == TotalItemsReceived)
        {
            Status = PurchaseStatus.FullyIssued;
        }
    }
}

public enum PurchaseStatus
{
    Ordered,          // Purchase order created, not yet received
    Received,         // Items received, not yet issued
    PartiallyIssued,  // Some items issued
    FullyIssued       // All items issued
}
```

### PurchaseLineItem (Value Object)

```csharp
public class PurchaseLineItem : ValueObject
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductCode { get; private set; }
    public string ProductName { get; private set; }
    
    // Quantity Tracking
    public int QuantityOrdered { get; private set; }
    public int QuantityReceived { get; private set; }
    public int QuantityIssued { get; private set; }
    public int QuantityRemaining => QuantityReceived - QuantityIssued;
    
    // Pricing
    public decimal UnitPrice { get; private set; }
    public decimal TotalCost => QuantityOrdered * UnitPrice;
    public decimal IssuedValue => QuantityIssued * UnitPrice;
    
    // Dates
    public DateTime OrderedDate { get; private set; }
    public DateTime? ReceivedDate { get; private set; }
    
    private PurchaseLineItem() { }
    
    public static PurchaseLineItem Create(
        Guid productId,
        string productCode,
        string productName,
        int quantity,
        decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");
        
        return new PurchaseLineItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductCode = productCode,
            ProductName = productName,
            QuantityOrdered = quantity,
            UnitPrice = unitPrice,
            OrderedDate = DateTime.UtcNow
        };
    }
    
    public void AddQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        QuantityOrdered += quantity;
    }
    
    public void MarkReceived(int quantityReceived)
    {
        if (quantityReceived <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        if (quantityReceived > QuantityOrdered)
            throw new InvalidOperationException("Received quantity exceeds ordered quantity");
        
        QuantityReceived = quantityReceived;
        ReceivedDate = DateTime.UtcNow;
    }
    
    public void MarkIssued(int quantityIssued)
    {
        if (quantityIssued <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        if (QuantityIssued + quantityIssued > QuantityReceived)
            throw new InvalidOperationException("Issued quantity exceeds received quantity");
        
        QuantityIssued += quantityIssued;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProductId;
        yield return UnitPrice; // Price is part of identity
    }
}
```
```

### SupplyRequestItem (Value Object)

**File:** `src/Modules/Expendable/Domain/SupplyRequestItem.cs`

```csharp
public class SupplyRequestItem : ValueObject
{
    public Guid ProductId { get; private set; }
    public string ProductCode { get; private set; }
    public string ProductName { get; private set; }
    public int QuantityRequested { get; private set; }
    public int? QuantityAllocated { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => QuantityRequested * UnitPrice;
    
    // Purchase Tracking
    public Guid? PurchaseId { get; private set; } // Source purchase for this item
    public Guid? PurchaseLineItemId { get; private set; } // Specific line item in purchase
    public DateTime? PurchaseDate { get; private set; }
    
    // If multiple purchases have same product, track original cost
    public bool RequiresSeparateIssuance { get; private set; } // True if price differs from other items
    
    private SupplyRequestItem() { }
    
    public static SupplyRequestItem Create(
        Guid productId,
        string productCode,
        string productName,
        int quantityRequested,
        decimal unitPrice,
        Guid? purchaseId = null,
        Guid? purchaseLineItemId = null,
        DateTime? purchaseDate = null)
    {
        if (quantityRequested <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");
        
        return new SupplyRequestItem
        {
            ProductId = productId,
            ProductCode = productCode,
            ProductName = productName,
            QuantityRequested = quantityRequested,
            UnitPrice = unitPrice,
            PurchaseId = purchaseId,
            PurchaseLineItemId = purchaseLineItemId,
            PurchaseDate = purchaseDate,
            RequiresSeparateIssuance = false
        };
    }
    
    public void SetPurchaseSource(
        Guid purchaseId,
        Guid purchaseLineItemId,
        DateTime purchaseDate)
    {
        PurchaseId = purchaseId;
        PurchaseLineItemId = purchaseLineItemId;
        PurchaseDate = purchaseDate;
    }
    
    public void MarkRequiresSeparateIssuance()
    {
        RequiresSeparateIssuance = true;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProductId;
        yield return UnitPrice; // Price is part of identity
        yield return PurchaseId; // Purchase source is part of identity
    }
}
```

### EmployeeInventory (Ledger with FIFO Batches)

**File:** `src/Modules/Expendable/Domain/EmployeeInventory.cs`

Tracks what each employee currently has in their possession. Implements FIFO (First-In-First-Out) batch tracking to handle multiple purchases of the same product at different prices.

**Key Feature**: Items with different prices from different purchases are tracked in separate FIFO batches, enabling:
- Accurate cost tracking (each batch maintains its own price)
- FIFO consumption (oldest batches consumed first)
- Complete audit trail (batch source linked to purchase)
- Quantity reconciliation (Received = Consumed + Remaining per batch)

```csharp
public class EmployeeInventory : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; }
    public List<InventoryItem> Items { get; private set; } = [];  // Items grouped by ProductId
    
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    public string TenantId { get; private set; }
    
    public static EmployeeInventory Create(Guid employeeId, string employeeName)
    {
        return new EmployeeInventory
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            EmployeeName = employeeName
        };
    }
    
    // Receive items from a purchase - adds to or creates new batch
    public void ReceiveItems(
        Guid productId,
        int quantity,
        decimal unitPrice,
        Guid purchaseId,
        Guid purchaseLineItemId,
        DateTime receivedDate)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);
        
        if (item != null)
        {
            // Add as new batch (same product may have different prices from different POs)
            item.AddBatch(purchaseId, purchaseLineItemId, quantity, unitPrice, receivedDate);
        }
        else
        {
            // Create new item with first batch
            Items.Add(InventoryItem.Create(productId, purchaseId, purchaseLineItemId, quantity, unitPrice, receivedDate));
        }
    }
    
    // FIFO consumption: returns which batches were used and quantities from each
    public List<(Guid PurchaseId, int Quantity, decimal Cost)> ConsumeItems(Guid productId, int quantityNeeded)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new InvalidOperationException($"Product {productId} not in inventory");
        
        return item.ConsumeItemsFIFO(quantityNeeded);
    }
    
    public int GetTotalQuantity(Guid productId) => 
        Items.FirstOrDefault(i => i.ProductId == productId)?.GetTotalQuantity() ?? 0;
    
    public decimal GetTotalValue(Guid productId) => 
        Items.FirstOrDefault(i => i.ProductId == productId)?.GetTotalValue() ?? 0m;
}

public class InventoryItem : AggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public List<InventoryBatch> Batches { get; private set; } = [];  // FIFO batches per price
    
    public int GetTotalQuantity() => Batches.Sum(b => b.AvailableQuantity);
    public decimal GetTotalValue() => Batches.Sum(b => b.AvailableValue);
    
    public static InventoryItem Create(
        Guid productId,
        Guid purchaseId,
        Guid purchaseLineItemId,
        int quantity,
        decimal unitPrice,
        DateTime receivedDate)
    {
        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Batches = new List<InventoryBatch>
            {
                InventoryBatch.Create(purchaseId, purchaseLineItemId, quantity, unitPrice, receivedDate)
            }
        };
    }
    
    // Add new batch for same product but potentially different price/purchase
    public void AddBatch(
        Guid purchaseId,
        Guid purchaseLineItemId,
        int quantity,
        decimal unitPrice,
        DateTime receivedDate)
    {
        Batches.Add(InventoryBatch.Create(purchaseId, purchaseLineItemId, quantity, unitPrice, receivedDate));
    }
    
    // FIFO consumption: oldest ReceivedDate first
    public List<(Guid PurchaseId, int Quantity, decimal Cost)> ConsumeItemsFIFO(int quantityNeeded)
    {
        var result = new List<(Guid, int, decimal)>();
        var remaining = quantityNeeded;
        
        // Sort by ReceivedDate ascending (FIFO: oldest first)
        foreach (var batch in Batches.OrderBy(b => b.ReceivedDate))
        {
            if (remaining <= 0) break;
            
            int quantityFromBatch = Math.Min(remaining, batch.AvailableQuantity);
            batch.IssueQuantity(quantityFromBatch);
            
            result.Add((batch.PurchaseId, quantityFromBatch, quantityFromBatch * batch.UnitPrice));
            remaining -= quantityFromBatch;
        }
        
        if (remaining > 0)
            throw new InvalidOperationException($"Insufficient inventory: need {quantityNeeded}, have {GetTotalQuantity()}");
        
        return result;
    }
}

// FIFO Batch: tracks items from a specific purchase at a specific price
public class InventoryBatch : ValueObject
{
    public Guid PurchaseId { get; private set; }
    public Guid PurchaseLineItemId { get; private set; }
    public int QuantityReceived { get; private set; }
    public int QuantityIssued { get; private set; }
    public decimal UnitPrice { get; private set; }
    public DateTime ReceivedDate { get; private set; }
    public int Version { get; private set; }  // Optimistic locking for concurrency
    
    public int AvailableQuantity => QuantityReceived - QuantityIssued;
    public decimal AvailableValue => AvailableQuantity * UnitPrice;
    
    public static InventoryBatch Create(
        Guid purchaseId,
        Guid purchaseLineItemId,
        int quantity,
        decimal unitPrice,
        DateTime receivedDate)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");
        
        return new InventoryBatch
        {
            PurchaseId = purchaseId,
            PurchaseLineItemId = purchaseLineItemId,
            QuantityReceived = quantity,
            QuantityIssued = 0,
            UnitPrice = unitPrice,
            ReceivedDate = receivedDate,
            Version = 1
        };
    }
    
    public void IssueQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        
        if (quantity > AvailableQuantity)
            throw new InvalidOperationException(
                $"Cannot issue {quantity} units, only {AvailableQuantity} available");
        
        QuantityIssued += quantity;
        Version++;  // Increment for optimistic locking
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PurchaseId;
        yield return UnitPrice;  // Batches with different prices are different
    }
}
```

### InventoryConsumption (Audit Trail)

**File:** `src/Modules/Expendable/Domain/InventoryConsumption.cs`

Audit trail entity that records every consumption transaction. Enables complete traceability from employee consumption back to supplier purchase.

```csharp
public class InventoryConsumption : AggregateRoot<Guid>, IAuditableEntity
{
    // What was consumed
    public Guid EmployeeId { get; private set; }
    public Guid ProductId { get; private set; }
    public int QuantityConsumed { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal CostConsumed { get; private set; }  // Quantity × UnitPrice
    
    // Where it came from
    public Guid PurchaseId { get; private set; }  // Source purchase
    public Guid PurchaseLineItemId { get; private set; }  // Specific line item in PO
    public DateTime ReceivedDate { get; private set; }  // When batch was received
    
    // When
    public DateTime ConsumedDate { get; private set; }
    public int DaysInInventory => (ConsumedDate - ReceivedDate).Days;
    
    // Auditing
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    
    // Multi-tenancy
    public string TenantId { get; private set; }
    
    public static InventoryConsumption Create(
        Guid employeeId,
        Guid productId,
        int quantityConsumed,
        decimal unitPrice,
        Guid purchaseId,
        Guid purchaseLineItemId,
        DateTime receivedDate)
    {
        return new InventoryConsumption
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            ProductId = productId,
            QuantityConsumed = quantityConsumed,
            UnitPrice = unitPrice,
            CostConsumed = quantityConsumed * unitPrice,
            PurchaseId = purchaseId,
            PurchaseLineItemId = purchaseLineItemId,
            ReceivedDate = receivedDate,
            ConsumedDate = DateTime.UtcNow,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }
}
```

---

## Commands (Write Operations)

### Product Management Commands

#### CreateProduct

**File:** `src/Modules/Expendable/Modules.Expendable.Contracts/v1/Products/CreateProductCommand.cs`

```csharp
public class CreateProductCommand : ICommand<CreateProductResponse>
{
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public string Category { get; set; }
    public string UnitOfMeasure { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal MinimumOrderQuantity { get; set; } = 1;
    public decimal MaxApprovedQuantityPerRequest { get; set; } = 10;
    public string? Description { get; set; }
    public bool RequiresApproval { get; set; } = false;
}

public record CreateProductResponse(Guid ProductId, string ProductCode, string ProductName);
```

#### UpdateProduct

```csharp
public class UpdateProductCommand : ICommand<UpdateProductResponse>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string Category { get; set; }
    public string UnitOfMeasure { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public decimal MaxApprovedQuantityPerRequest { get; set; }
    public string? Description { get; set; }
    public bool RequiresApproval { get; set; }
}

public record UpdateProductResponse(Guid ProductId, string Status);
```

#### ActivateProduct

```csharp
public class ActivateProductCommand : ICommand<ActivateProductResponse>
{
    public Guid ProductId { get; set; }
}

public record ActivateProductResponse(Guid ProductId, string Status);
```

#### DeactivateProduct

```csharp
public class DeactivateProductCommand : ICommand<DeactivateProductResponse>
{
    public Guid ProductId { get; set; }
    public string? Reason { get; set; }
}

public record DeactivateProductResponse(Guid ProductId, string Status);
```

#### DiscontinueProduct

```csharp
public class DiscontinueProductCommand : ICommand<DiscontinueProductResponse>
{
    public Guid ProductId { get; set; }
}

public record DiscontinueProductResponse(Guid ProductId, string Status);
```

### Purchase Management Commands

#### CreatePurchaseOrder

**File:** `src/Modules/Expendable/Modules.Expendable.Contracts/v1/Purchases/CreatePurchaseOrderCommand.cs`

```csharp
public class CreatePurchaseOrderCommand : ICommand<CreatePurchaseOrderResponse>
{
    public Guid SupplierId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public List<PurchaseLineItemDto> LineItems { get; set; } = [];
}

public record PurchaseLineItemDto(Guid ProductId, string ProductCode, string ProductName, int Quantity, decimal UnitPrice);
public record CreatePurchaseOrderResponse(Guid PurchaseId, string PurchaseOrderNumber, decimal TotalCost);
```

#### ReceivePurchaseItems

**File:** `src/Modules/Expendable/Modules.Expendable.Contracts/v1/Purchases/ReceivePurchaseItemsCommand.cs`

```csharp
public class ReceivePurchaseItemsCommand : ICommand<ReceivePurchaseItemsResponse>
{
    public Guid PurchaseId { get; set; }
    public List<ReceiveItemDto> Items { get; set; } = [];
    public DateTime ReceiptDate { get; set; }
}

public record ReceiveItemDto(Guid PurchaseLineItemId, int QuantityReceived);
public record ReceivePurchaseItemsResponse(Guid PurchaseId, string Status, int TotalReceived);
```

**Handler:** `src/Modules/Expendable/Features/v1/Purchases/ReceivePurchaseItems/ReceivePurchaseItemsCommandHandler.cs`

```csharp
public sealed class ReceivePurchaseItemsCommandHandler : ICommandHandler<ReceivePurchaseItemsCommand, ReceivePurchaseItemsResponse>
{
    private readonly IMediator _mediator;
    private readonly IRepository<Purchase> _purchaseRepo;
    
    public async ValueTask<ReceivePurchaseItemsResponse> Handle(
        ReceivePurchaseItemsCommand cmd,
        CancellationToken ct)
    {
        var purchase = await _purchaseRepo.GetByIdAsync(cmd.PurchaseId, ct)
            ?? throw new NotFoundException($"Purchase {cmd.PurchaseId} not found");
        
        foreach (var item in cmd.Items)
        {
            var lineItem = purchase.LineItems.FirstOrDefault(li => li.Id == item.PurchaseLineItemId)
                ?? throw new InvalidOperationException($"Line item not found");
            
            purchase.ReceiveItems(item.PurchaseLineItemId, item.QuantityReceived);
        }
        
        await _purchaseRepo.AddOrUpdateAsync(purchase, ct);
        
        return new ReceivePurchaseItemsResponse(
            PurchaseId: purchase.Id,
            Status: purchase.Status.ToString(),
            TotalReceived: purchase.TotalItemsReceived);
    }
}
```

#### IssuePurchaseItems

```csharp
public class IssuePurchaseItemsCommand : ICommand<IssuePurchaseItemsResponse>
{
    public Guid PurchaseId { get; set; }
    public Guid PurchaseLineItemId { get; set; }
    public Guid SupplyRequestId { get; set; }
    public int QuantityToIssue { get; set; }
    public DateTime IssueDate { get; set; }
}

public record IssuePurchaseItemsResponse(Guid PurchaseId, string Status, int TotalIssued, decimal TotalIssuedValue);
```

### Shopping Cart Commands

#### AddToCart

**File:** `src/Modules/Expendable/Modules.Expendable.Contracts/v1/Cart/AddToCartCommand.cs`

```csharp
public class AddToCartCommand : ICommand<AddToCartResponse>
{
    public Guid EmployeeId { get; set; }  // Only registered employees
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public record AddToCartResponse(Guid CartId, int TotalItems, decimal CartTotal);
```

**Handler:** `src/Modules/Expendable/Features/v1/Cart/AddToCart/AddToCartCommandHandler.cs`

```csharp
public sealed class AddToCartCommandHandler : ICommandHandler<AddToCartCommand, AddToCartResponse>
{
    private readonly IMediator _mediator;
    private readonly IRepository<EmployeeShoppingCart> _cartRepo;
    private readonly IRepository<Employee> _employeeRepo; // From Library
    
    public async ValueTask<AddToCartResponse> Handle(
        AddToCartCommand cmd,
        CancellationToken ct)
    {
        // Verify employee is registered and has account
        var employee = await _mediator.Send(
            new GetEmployeeQuery(cmd.EmployeeId),
            ct);
        
        if (employee == null)
            throw new UnauthorizedException(
                "Employee not found. Only registered employees can shop online. " +
                "Please visit the supply office for in-person requests.");
        
        // Verify product exists and is available
        var product = await _mediator.Send(
            new GetProductQuery(cmd.ProductId),
            ct);
        
        if (product == null)
            throw new NotFoundException($"Product {cmd.ProductId} not found");
        
        // Get or create cart for employee
        var cart = await _cartRepo.FirstOrDefaultAsync(
            c => c.EmployeeId == cmd.EmployeeId && c.Status == CartStatus.Active,
            ct);
        
        if (cart == null)
        {
            cart = EmployeeShoppingCart.Create(
                employeeId: cmd.EmployeeId,
                employeeName: employee.Name,
                employeeEmail: employee.Email);
        }
        
        // Add item to cart
        cart.AddItem(
            productId: product.Id,
            productCode: product.Code,
            productName: product.Name,
            quantity: cmd.Quantity,
            unitPrice: product.UnitPrice);
        
        await _cartRepo.AddOrUpdateAsync(cart, ct);
        
        return new AddToCartResponse(
            CartId: cart.Id,
            TotalItems: cart.GetTotalItems(),
            CartTotal: cart.GetTotalPrice());
    }
}
```

#### RemoveFromCart

```csharp
public class RemoveFromCartCommand : ICommand<RemoveFromCartResponse>
{
    public Guid EmployeeId { get; set; }
    public Guid ProductId { get; set; }
}

public record RemoveFromCartResponse(Guid CartId, int TotalItems, decimal CartTotal);
```

#### UpdateCartItemQuantity

```csharp
public class UpdateCartItemQuantityCommand : ICommand<UpdateCartItemQuantityResponse>
{
    public Guid EmployeeId { get; set; }
    public Guid ProductId { get; set; }
    public int NewQuantity { get; set; }
}

public record UpdateCartItemQuantityResponse(Guid CartId, int TotalItems, decimal CartTotal);
```

#### ClearCart

```csharp
public class ClearCartCommand : ICommand<ClearCartResponse>
{
    public Guid EmployeeId { get; set; }
}

public record ClearCartResponse(bool Success);
```

### Supply Request Commands

#### CheckoutCart (Online Purchase)

**File:** `src/Modules/Expendable/Modules.Expendable.Contracts/v1/Requests/CheckoutCartCommand.cs`

```csharp
public class CheckoutCartCommand : ICommand<CheckoutCartResponse>
{
    public Guid EmployeeId { get; set; }
    public Guid? OfficeId { get; set; }
    public string? Justification { get; set; }
}

public record CheckoutCartResponse(Guid RequestId, string RequestNumber, string Status, decimal TotalAmount);
```

**Handler:** `src/Modules/Expendable/Features/v1/Requests/CheckoutCart/CheckoutCartCommandHandler.cs`

```csharp
public sealed class CheckoutCartCommandHandler : ICommandHandler<CheckoutCartCommand, CheckoutCartResponse>
{
    private readonly IMediator _mediator;
    private readonly IRepository<SupplyRequest> _requestRepo;
    private readonly IRepository<EmployeeShoppingCart> _cartRepo;
    
    public async ValueTask<CheckoutCartResponse> Handle(
        CheckoutCartCommand cmd,
        CancellationToken ct)
    {
        // Get employee's active cart
        var cart = await _cartRepo.FirstOrDefaultAsync(
            c => c.EmployeeId == cmd.EmployeeId && c.Status == CartStatus.Active,
            ct);
        
        if (cart == null || cart.Items.Count == 0)
            throw new InvalidOperationException("Cart is empty or not found");
        
        // Verify employee exists
        var employee = await _mediator.Send(
            new GetEmployeeQuery(cmd.EmployeeId),
            ct);
        
        if (employee == null)
            throw new NotFoundException("Employee not found");
        
        // Create supply request from cart
        var request = SupplyRequest.CreateFromCart(
            employeeId: cmd.EmployeeId,
            employeeName: employee.Name,
            officeId: cmd.OfficeId,
            shoppingCartId: cart.Id,
            justification: cmd.Justification);
        
        // Add all cart items to request
        foreach (var cartItem in cart.Items)
        {
            var requestItem = SupplyRequestItem.Create(
                productId: cartItem.ProductId,
                productCode: cartItem.ProductCode,
                productName: cartItem.ProductName,
                quantityRequested: cartItem.Quantity,
                unitPrice: cartItem.UnitPrice);
            
            request.AddItem(requestItem);
        }
        
        // Save request
        await _requestRepo.AddAsync(request, ct);
        
        // Mark cart as checked out
        cart.Checkout(request.Id);
        await _cartRepo.AddOrUpdateAsync(cart, ct);
        
        return new CheckoutCartResponse(
            RequestId: request.Id,
            RequestNumber: request.RequestNumber,
            Status: request.Status.ToString(),
            TotalAmount: request.Items.Sum(i => i.TotalPrice));
    }
}
```

#### CreateWalkInSupplyRequest (Walk-In at Supply Office)

**File:** `src/Modules/Expendable/Modules.Expendable.Contracts/v1/Requests/CreateWalkInSupplyRequestCommand.cs`

```csharp
public class CreateWalkInSupplyRequestCommand : ICommand<CreateWalkInSupplyRequestResponse>
{
    public string EmployeeName { get; set; }  // Name of walk-in employee
    public Guid? OfficeId { get; set; }
    public List<RequestItemDto> Items { get; set; } = [];
    public string? Justification { get; set; }
}

public record RequestItemDto(Guid ProductId, int Quantity);
public record CreateWalkInSupplyRequestResponse(Guid RequestId, string RequestNumber, string Status);
```

**Handler:** `src/Modules/Expendable/Features/v1/Requests/CreateWalkInRequest/CreateWalkInSupplyRequestCommandHandler.cs`

```csharp
public sealed class CreateWalkInSupplyRequestCommandHandler 
    : ICommandHandler<CreateWalkInSupplyRequestCommand, CreateWalkInSupplyRequestResponse>
{
    private readonly IMediator _mediator;
    private readonly IRepository<SupplyRequest> _requestRepo;
    
    public async ValueTask<CreateWalkInSupplyRequestResponse> Handle(
        CreateWalkInSupplyRequestCommand cmd,
        CancellationToken ct)
    {
        // Verify office if provided
        if (cmd.OfficeId.HasValue)
        {
            var office = await _mediator.Send(
                new GetOfficeQuery(cmd.OfficeId.Value),
                ct);
            
            if (office == null)
                throw new NotFoundException($"Office {cmd.OfficeId} not found");
        }
        
        // Create walk-in request (no EmployeeId required)
        var request = SupplyRequest.CreateWalkInRequest(
            employeeName: cmd.EmployeeName,
            officeId: cmd.OfficeId,
            justification: cmd.Justification);
        
        // Add items with product validation
        foreach (var itemDto in cmd.Items)
        {
            var product = await _mediator.Send(
                new GetProductQuery(itemDto.ProductId),
                ct);
            
            if (product == null)
                throw new NotFoundException($"Product {itemDto.ProductId} not found");
            
            var item = SupplyRequestItem.Create(
                productId: product.Id,
                productCode: product.Code,
                productName: product.Name,
                quantityRequested: itemDto.Quantity,
                unitPrice: product.UnitPrice);
            
            request.AddItem(item);
        }
        
        await _requestRepo.AddAsync(request, ct);
        
        return new CreateWalkInSupplyRequestResponse(
            RequestId: request.Id,
            RequestNumber: request.RequestNumber,
            Status: request.Status.ToString());
    }
}
```

### ApproveSupplyRequest

```csharp
public class ApproveSupplyRequestCommand : ICommand<ApproveSupplyRequestResponse>
{
    public Guid RequestId { get; set; }
    public string? ApprovalNotes { get; set; }
}

public record ApproveSupplyRequestResponse(Guid RequestId, string Status);
```

### AllocateSupplyRequest

```csharp
public class AllocateSupplyRequestCommand : ICommand<AllocateSupplyRequestResponse>
{
    public Guid RequestId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public List<AllocationItemDto> AllocationItems { get; set; } = [];
}

public record AllocationItemDto(Guid ProductId, int QuantityToAllocate);
public record AllocateSupplyRequestResponse(Guid RequestId, string Status);
```

### DeliverAllocation

```csharp
public class DeliverAllocationCommand : ICommand<DeliverAllocationResponse>
{
    public Guid RequestId { get; set; }
    public DateTime DeliveryDate { get; set; }
}

public record DeliverAllocationResponse(Guid RequestId, string Status);
```

---

## Queries (Read Operations)

### Product Queries

#### GetProduct

```csharp
public sealed record GetProductQuery(Guid Id) : IQuery<ProductDto?>;

public record ProductDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string? Description,
    string Category,
    string UnitOfMeasure,
    decimal MinimumOrderQuantity,
    decimal MaxApprovedQuantityPerRequest,
    decimal EstimatedUnitCost,
    bool RequiresApproval,
    string Status,
    DateTime? ActivatedDate,
    DateTime? InactivatedDate
);
```

#### SearchProducts

```csharp
public sealed record SearchProductsQuery(
    string? Category,
    string? SearchTerm,
    bool IncludeInactive = false,
    int PageNumber = 1,
    int PageSize = 20
) : IPagedQuery, IQuery<PagedResponse<ProductDto>>;
```

#### ListActiveProducts

```csharp
public sealed record ListActiveProductsQuery(
    string? Category = null
) : IQuery<List<ProductDto>>;
```

#### GetProductsByCategory

```csharp
public sealed record GetProductsByCategoryQuery(string Category) : IQuery<List<ProductDto>>;
```

### Purchase Queries

#### GetPurchase

```csharp
public sealed record GetPurchaseQuery(Guid Id) : IQuery<PurchaseDto?>;

public record PurchaseDto(
    Guid PurchaseId,
    string PurchaseOrderNumber,
    Guid SupplierId,
    string SupplierName,
    DateTime PurchaseDate,
    DateTime? ReceiptDate,
    decimal TotalCost,
    string Status,
    int TotalItemsReceived,
    int TotalItemsIssued,
    decimal TotalIssuedValue,
    List<PurchaseLineItemDto> LineItems
);

public record PurchaseLineItemDto(
    Guid LineItemId,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int QuantityOrdered,
    int QuantityReceived,
    int QuantityIssued,
    int QuantityRemaining,
    decimal UnitPrice,
    decimal TotalCost,
    decimal IssuedValue
);
```

#### SearchPurchases

```csharp
public sealed record SearchPurchasesQuery(
    Guid? SupplierId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 10
) : IPagedQuery, IQuery<PagedResponse<PurchaseDto>>;
```

#### GetPurchasesByProduct

```csharp
public sealed record GetPurchasesByProductQuery(
    Guid ProductId,
    bool IncludeFullyIssued = false
) : IQuery<List<PurchaseWithAvailabilityDto>>;

public record PurchaseWithAvailabilityDto(
    Guid PurchaseId,
    string PurchaseOrderNumber,
    Guid LineItemId,
    int QuantityAvailable,
    decimal UnitPrice,
    DateTime? ReceiptDate
);
```

#### GetPurchasesNeedingIssuance

```csharp
public sealed record GetPurchasesNeedingIssuanceQuery(
    int PageNumber = 1,
    int PageSize = 10
) : IPagedQuery, IQuery<PagedResponse<PurchaseDto>>;
```

### Shopping Cart Queries

#### GetEmployeeCart

```csharp
public sealed record GetSupplyRequestQuery(Guid Id) : IQuery<SupplyRequestDto?>;

public record SupplyRequestDto(
    Guid Id,
    string RequestNumber,
    Guid EmployeeId,
    string EmployeeName,
    List<SupplyRequestItemDto> Items,
    string Status,
    DateTime RequestedDate,
    string? ApprovalNotes,
    DateTime? ApprovedDate,
    DateTime? AllocatedDate,
    DateTime? ExpectedDeliveryDate
);

public record SupplyRequestItemDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int QuantityRequested,
    int? QuantityAllocated,
    decimal UnitPrice,
    decimal TotalPrice
);
```

### SearchSupplyRequests

```csharp
public sealed record SearchSupplyRequestsQuery(
    Guid? EmployeeId,
    string? Status,
    DateTime? FromDate,
    DateTime? ToDate,
    int PageNumber = 1,
    int PageSize = 10
) : IPagedQuery, IQuery<PagedResponse<SupplyRequestDto>>;
```

### GetEmployeeInventory

```csharp
public sealed record GetEmployeeInventoryQuery(Guid EmployeeId) : IQuery<EmployeeInventoryDto?>;

public record EmployeeInventoryDto(
    Guid EmployeeId,
    string EmployeeName,
    List<InventoryItemDto> Items,
    decimal TotalValue
);

public record InventoryItemDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    int CurrentQuantity,
    decimal UnitPrice,
    decimal TotalValue,
    DateTime ReceivedDate
);
```

---

## API Endpoints

### Product Management Endpoints (Administrators/Inventory Managers Only)

```
POST   /api/v1/expendable/products                    → Create new product
GET    /api/v1/expendable/products/{id}               → Get product details
GET    /api/v1/expendable/products                    → Search/list products (with filters)
PUT    /api/v1/expendable/products/{id}               → Update product details
POST   /api/v1/expendable/products/{id}/activate      → Activate product
POST   /api/v1/expendable/products/{id}/deactivate    → Deactivate product
POST   /api/v1/expendable/products/{id}/discontinue   → Discontinue product
GET    /api/v1/expendable/products/category/{name}    → Get products by category
GET    /api/v1/expendable/products/active             → List all active products (for catalog)
```

### Product Catalog Endpoints (All Employees - Read-Only)

```
GET    /api/v1/expendable/catalog                     → Browse product catalog (active products only)
GET    /api/v1/expendable/catalog/categories          → List all product categories
GET    /api/v1/expendable/catalog/category/{name}     → Get products by category
GET    /api/v1/expendable/catalog/search              → Search products by name/code
```

### Shopping Cart Endpoints (Online Access - Registered Employees Only)

```
GET    /api/v1/expendable/catalog                     → Browse product catalog
GET    /api/v1/expendable/cart                        → Get current user's cart
POST   /api/v1/expendable/cart/items                  → Add item to cart
PUT    /api/v1/expendable/cart/items/{productId}      → Update item quantity
DELETE /api/v1/expendable/cart/items/{productId}      → Remove item from cart
DELETE /api/v1/expendable/cart                        → Clear entire cart
POST   /api/v1/expendable/cart/checkout               → Convert cart to supply request
```

### Walk-In Requests (Supply Office)

```
POST   /api/v1/expendable/requests/walkin             → Create walk-in supply request (no employee account)
GET    /api/v1/expendable/requests/walkin             → List walk-in requests
```

### Supply Request Management (Both Online & Walk-In)

```
GET    /api/v1/expendable/requests/{id}               → Get specific request
GET    /api/v1/expendable/requests                    → Search/list all requests
PUT    /api/v1/expendable/requests/{id}/approve       → Approve request (supply office)
PUT    /api/v1/expendable/requests/{id}/allocate      → Allocate items (supply office)
PUT    /api/v1/expendable/requests/{id}/deliver       → Mark as delivered (supply office)
DELETE /api/v1/expendable/requests/{id}               → Cancel request (if pending)
```

### Employee Inventory Endpoints

```
GET    /api/v1/expendable/inventory/{employeeId}     → Get employee's current inventory (own or view all if admin)
GET    /api/v1/expendable/inventory                   → List all employee inventories (admin)
GET    /api/v1/expendable/inventory/summary           → Inventory summary/analytics (admin)
```

### Purchase Management Endpoints

```
POST   /api/v1/expendable/purchases                   → Create purchase order
GET    /api/v1/expendable/purchases/{id}              → Get purchase details
GET    /api/v1/expendable/purchases                   → Search purchases
POST   /api/v1/expendable/purchases/{id}/receive      → Receive items from purchase
POST   /api/v1/expendable/purchases/{id}/issue        → Issue items from purchase
GET    /api/v1/expendable/purchases/product/{id}      → Get all purchases for product
GET    /api/v1/expendable/purchases/pending-issuance  → Get purchases ready for issuance
```

---

## Issuance System & Price-Based Separation

### Overview

The issuance system ensures items are issued from the correct purchase batch, especially when the same product is purchased at different prices. This is critical for:
- **Accurate Cost Accounting**: Track actual cost of issued items
- **FIFO/LIFO Valuation**: Support different inventory valuation methods
- **Compliance**: Maintain audit trail of which purchase items came from
- **Separate Issuance**: Items with different unit prices are never mixed in a single issuance

### Issuance Logic

**File:** `src/Modules/Expendable/Features/v1/Issuance/IssueSupplyRequest/IssueSupplyRequestCommandHandler.cs`

```csharp
public sealed class IssueSupplyRequestCommandHandler : ICommandHandler<IssueSupplyRequestCommand, IssueSupplyRequestResponse>
{
    private readonly IMediator _mediator;
    private readonly IRepository<SupplyRequest> _requestRepo;
    private readonly IRepository<Purchase> _purchaseRepo;
    private readonly IRepository<EmployeeInventory> _inventoryRepo;
    
    public async ValueTask<IssueSupplyRequestResponse> Handle(
        IssueSupplyRequestCommand cmd,
        CancellationToken ct)
    {
        var supplyRequest = await _requestRepo.GetByIdAsync(cmd.SupplyRequestId, ct)
            ?? throw new NotFoundException("Supply request not found");
        
        // Group items by price to identify separate issuances needed
        var itemsByPrice = supplyRequest.Items
            .GroupBy(i => new { i.ProductId, i.UnitPrice })
            .ToList();
        
        var issuanceGroups = new List<IssuanceGroup>();
        
        foreach (var priceGroup in itemsByPrice)
        {
            var productId = priceGroup.Key.ProductId;
            var unitPrice = priceGroup.Key.UnitPrice;
            var totalQtyNeeded = priceGroup.Sum(i => i.QuantityRequested);
            
            // Find available purchases for this product at this price
            var availablePurchases = await _purchaseRepo.FindAsync(
                p => p.Status != PurchaseStatus.FullyIssued &&
                     p.LineItems.Any(li => li.ProductId == productId && li.UnitPrice == unitPrice),
                ct);
            
            if (!availablePurchases.Any())
            {
                throw new InvalidOperationException(
                    $"No available inventory for product {productId} at price {unitPrice}");
            }
            
            var issuanceGroup = new IssuanceGroup
            {
                ProductId = productId,
                UnitPrice = unitPrice,
                QuantityNeeded = totalQtyNeeded,
                Items = priceGroup.ToList(),
                Purchases = availablePurchases
            };
            
            issuanceGroups.Add(issuanceGroup);
        }
        
        // Process each price group separately
        var allIssuances = new List<Guid>();
        var totalValue = 0m;
        
        foreach (var group in issuanceGroups)
        {
            var qtyRemaining = group.QuantityNeeded;
            
            foreach (var purchase in group.Purchases.OrderBy(p => p.PurchaseDate))
            {
                if (qtyRemaining <= 0) break;
                
                var lineItem = purchase.LineItems.FirstOrDefault(
                    li => li.ProductId == group.ProductId && 
                          li.UnitPrice == group.UnitPrice);
                
                if (lineItem == null || lineItem.QuantityRemaining == 0) continue;
                
                var qtyToIssue = Math.Min(qtyRemaining, lineItem.QuantityRemaining);
                
                // Issue from this purchase
                purchase.IssueItems(lineItem.Id, qtyToIssue);
                purchase.AssociateSupplyRequest(supplyRequest.Id);
                
                await _purchaseRepo.AddOrUpdateAsync(purchase, ct);
                
                qtyRemaining -= qtyToIssue;
                totalValue += lineItem.UnitPrice * qtyToIssue;
            }
            
            if (qtyRemaining > 0)
            {
                throw new InvalidOperationException(
                    $"Insufficient inventory: {qtyRemaining} units of product {group.ProductId} at price {group.UnitPrice}");
            }
        }
        
        // Update employee inventory
        var employee = await _mediator.Send(new GetEmployeeQuery(supplyRequest.EmployeeId!.Value), ct);
        
        var inventory = await _inventoryRepo.FirstOrDefaultAsync(
            i => i.EmployeeId == supplyRequest.EmployeeId,
            ct) ?? EmployeeInventory.Create(supplyRequest.EmployeeId!.Value, employee.Name);
        
        foreach (var item in supplyRequest.Items)
        {
            inventory.ReceiveItems(item.ProductId, item.QuantityRequested, item.UnitPrice);
        }
        
        await _inventoryRepo.AddOrUpdateAsync(inventory, ct);
        
        return new IssueSupplyRequestResponse(
            RequestId: supplyRequest.Id,
            Status: supplyRequest.Status.ToString(),
            TotalValue: totalValue,
            TotalItems: supplyRequest.Items.Sum(i => i.QuantityRequested));
    }
}

private class IssuanceGroup
{
    public Guid ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public int QuantityNeeded { get; set; }
    public List<SupplyRequestItem> Items { get; set; } = [];
    public List<Purchase> Purchases { get; set; } = [];
}

public class IssueSupplyRequestCommand : ICommand<IssueSupplyRequestResponse>
{
    public Guid SupplyRequestId { get; set; }
}

public record IssueSupplyRequestResponse(Guid RequestId, string Status, decimal TotalValue, int TotalItems);
```

### Price Separation Example

```
Scenario: Same product with different purchase prices

Purchase 1 (PO-2026-001):
- Product A: 100 units @ $10/unit
- Issued: 30 units

Purchase 2 (PO-2026-002):
- Product A: 50 units @ $12/unit
- Issued: 0 units

Supply Request for Product A:
- Request: 50 units

Issuance Process:
1. Identify unique price for Product A in request: $10 + $12 (mixed)
2. Items at $10 are issued separately from Purchase 1 (up to 70 remaining)
3. Items at $12 are issued separately from Purchase 2 (up to 50 available)
4. If request has both prices, create separate issuances
5. Each price group tracks actual cost used

Result:
- 20 units issued from Purchase 1 @ $10 = $200
- 30 units issued from Purchase 2 @ $12 = $360
- Total issuance value: $560 (actual cost, not assumed cost)
```

---

## Permissions

**File:** `src/Modules/Expendable/Constants/ExpenablePermissionConstants.cs`

```csharp
public static class ExpenablePermissionConstants
{
    public static class Products
    {
        public const string View = "Permissions.Expendable.Products.View";
        public const string ViewCatalog = "Permissions.Expendable.Products.ViewCatalog";
        public const string Create = "Permissions.Expendable.Products.Create";
        public const string Update = "Permissions.Expendable.Products.Update";
        public const string Delete = "Permissions.Expendable.Products.Delete";
        public const string Activate = "Permissions.Expendable.Products.Activate";
        public const string Deactivate = "Permissions.Expendable.Products.Deactivate";
    }
    
    public static class Shopping
    {
        public const string ViewCatalog = "Permissions.Expendable.Shopping.ViewCatalog";
        public const string Browse = "Permissions.Expendable.Shopping.Browse";
        public const string AddToCart = "Permissions.Expendable.Shopping.AddToCart";
        public const string Checkout = "Permissions.Expendable.Shopping.Checkout";
    }
    
    public static class Purchases
    {
        public const string View = "Permissions.Expendable.Purchases.View";
        public const string Create = "Permissions.Expendable.Purchases.Create";
        public const string Receive = "Permissions.Expendable.Purchases.Receive";
        public const string Issue = "Permissions.Expendable.Purchases.Issue";
        public const string ViewAnalytics = "Permissions.Expendable.Purchases.ViewAnalytics";
    }
    
    public static class Requests
    {
        public const string View = "Permissions.Expendable.Requests.View";
        public const string CreateOnline = "Permissions.Expendable.Requests.CreateOnline";
        public const string CreateWalkIn = "Permissions.Expendable.Requests.CreateWalkIn"; // Supply office staff
        public const string Approve = "Permissions.Expendable.Requests.Approve";
        public const string Allocate = "Permissions.Expendable.Requests.Allocate";
        public const string Issue = "Permissions.Expendable.Requests.Issue";
        public const string Reject = "Permissions.Expendable.Requests.Reject";
    }
    
    public static class Inventory
    {
        public const string ViewOwn = "Permissions.Expendable.Inventory.ViewOwn";
        public const string ViewAll = "Permissions.Expendable.Inventory.ViewAll";
        public const string Export = "Permissions.Expendable.Inventory.Export";
    }
}
```

**Applied in Endpoints:**
```csharp
// Product Management (Admin/Inventory Manager)
endpoints.MapPost("/products", handler)
    .RequirePermission(ExpenablePermissionConstants.Products.Create);

endpoints.MapGet("/products", handler)
    .RequirePermission(ExpenablePermissionConstants.Products.View);

endpoints.MapPut("/products/{id}", handler)
    .RequirePermission(ExpenablePermissionConstants.Products.Update);

endpoints.MapPost("/products/{id}/activate", handler)
    .RequirePermission(ExpenablePermissionConstants.Products.Activate);

endpoints.MapPost("/products/{id}/deactivate", handler)
    .RequirePermission(ExpenablePermissionConstants.Products.Deactivate);

// Product Catalog (All Employees - Public)
endpoints.MapGet("/catalog", handler)
    .AllowAnonymous();

endpoints.MapGet("/catalog/categories", handler)
    .AllowAnonymous();

// Shopping (Registered Employees)
endpoints.MapGet("/catalog", handler)
    .RequirePermission(ExpenablePermissionConstants.Shopping.ViewCatalog);

endpoints.MapPost("/cart/items", handler)
    .RequirePermission(ExpenablePermissionConstants.Shopping.AddToCart);

endpoints.MapPost("/cart/checkout", handler)
    .RequirePermission(ExpenablePermissionConstants.Shopping.Checkout);

// Purchase Management (Supply Office/Inventory Staff)
endpoints.MapPost("/purchases", handler)
    .RequirePermission(ExpenablePermissionConstants.Purchases.Create);

endpoints.MapPost("/purchases/{id}/receive", handler)
    .RequirePermission(ExpenablePermissionConstants.Purchases.Receive);

endpoints.MapPost("/purchases/{id}/issue", handler)
    .RequirePermission(ExpenablePermissionConstants.Purchases.Issue);

endpoints.MapGet("/purchases/pending-issuance", handler)
    .RequirePermission(ExpenablePermissionConstants.Requests.Issue);

// Walk-In (Supply Office Staff)
endpoints.MapPost("/requests/walkin", handler)
    .RequirePermission(ExpenablePermissionConstants.Requests.CreateWalkIn);

// Management
endpoints.MapPut("/{id}/approve", handler)
    .RequirePermission(ExpenablePermissionConstants.Requests.Approve);

endpoints.MapPost("/{id}/issue", handler)
    .RequirePermission(ExpenablePermissionConstants.Requests.Issue);

endpoints.MapGet("/inventory/{employeeId}", handler)
    .RequirePermission(ExpenablePermissionConstants.Inventory.ViewOwn); // Can view own, need ViewAll for others
```

---

## Database Context

**File:** `src/Modules/Expendable/Data/ExpenableDbContext.cs`

```csharp
public class ExpenableDbContext : 
    MultiTenantIdentityDbContext<AMISUser, AMISRole>
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<EmployeeShoppingCart> ShoppingCarts => Set<EmployeeShoppingCart>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<SupplyRequest> SupplyRequests => Set<SupplyRequest>();
    public DbSet<EmployeeInventory> EmployeeInventories => Set<EmployeeInventory>();
    public DbSet<InventoryConsumption> Consumptions => Set<InventoryConsumption>();  // ← Audit trail
}
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<SupplyRequest> SupplyRequests => Set<SupplyRequest>();
    public DbSet<EmployeeInventory> EmployeeInventories => Set<EmployeeInventory>();
    
    public ExpenableDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<ExpenableDbContext> options)
        : base(multiTenantContextAccessor, options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenableDbContext).Assembly);
    }
}
```

---

## Workflow Diagrams

### Online Shopping Workflow (Registered Employees)

```
┌────────────────────────────────────────────────────────────────┐
│ Registered Employee                                            │
│ Logs in to system                                             │
│ Has account in Library module                                 │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ↓
┌────────────────────────────────────────────────────────────────┐
│ Browse Product Catalog                                         │
│ (GET /api/v1/expendable/catalog)                              │
│ View all available expendable items                           │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ↓
┌────────────────────────────────────────────────────────────────┐
│ Add Items to Shopping Cart                                     │
│ (POST /api/v1/expendable/cart/items)                          │
│ Can add multiple items, update quantities                     │
│ Cart stays active for browsing                                │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ↓
┌────────────────────────────────────────────────────────────────┐
│ Review Cart & Checkout                                         │
│ (POST /api/v1/expendable/cart/checkout)                       │
│ Creates SupplyRequest from cart items                         │
│ Source: Online                                                │
│ Status: Pending                                               │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ↓
            [Approval Flow]
```

### Walk-In Request Workflow (Supply Office)

```
┌────────────────────────────────────────────────────────────────┐
│ Unregistered/Guest Employee                                    │
│ Visits Supply Office                                          │
│ Does NOT have online account                                  │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ↓
┌────────────────────────────────────────────────────────────────┐
│ Supply Office Staff                                            │
│ Enters walk-in request                                        │
│ (POST /api/v1/expendable/requests/walkin)                     │
│ Records employee name (no ID required)                        │
│ Adds items manually                                           │
│ Source: WalkIn                                                │
│ Status: Pending                                               │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ↓
            [Approval Flow]
```

### Common Approval & Fulfillment Flow (Both Paths)

```
┌────────────────────────────────────────────────────────────────┐
│ Manager/Admin Reviews Request                                  │
│ (PUT /api/v1/expendable/requests/{id}/approve)               │
│ Status: Approved or Rejected                                  │
└────────────────────┬───────────────────────────────────────────┘
                 │
         ┌───────┴───────┐
         │               │
         ↓               ↓
    Approved         Rejected
         │               │
         ↓               ↓
    Status:           Status:
    Approved          Rejected
         │               │
         └────────┬──────┘
                  │
┌────────────────────────────────────────────────────────────────┐
│ If Approved: Allocation Phase                                  │
│ Inventory Team allocates items                                │
│ (PUT /api/v1/expendable/requests/{id}/allocate)              │
│ Status: Allocated                                             │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ↓
┌────────────────────────────────────────────────────────────────┐
│ Delivery Phase                                                 │
│ Items handed to employee (online or walk-in)                  │
│ (PUT /api/v1/expendable/requests/{id}/deliver)               │
│ Status: Completed                                             │
│ Employee Inventory updated                                    │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ↓
         ┌───────────────────────────┐
         │ Employee Inventory        │
         │ now tracks items          │
         │ Employee has received     │
         └───────────────────────────┘
```

---

## Transaction Flow Through Entities

### Overview - Complete Supply Chain Transaction Flow

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                     MODULE 2 EXPENDABLE - TRANSACTION FLOW                              │
└─────────────────────────────────────────────────────────────────────────────────────────┘

PHASE 1: PRODUCT MANAGEMENT
═════════════════════════════════════════════════════════════════════════════════════════

Admin Action:
┌──────────────────┐
│ Create Product   │
│ Command          │
└────────┬─────────┘
         │
         ↓
┌──────────────────────────────────────────┐
│ Product Entity Created                   │
│ ├─ ProductCode: "EXP-PEN-001"           │
│ ├─ ProductName: "Ballpoint Pen"         │
│ ├─ Category: "Office Supplies"          │
│ ├─ EstimatedUnitCost: $0.50             │
│ ├─ MaxApprovedQuantityPerRequest: 100   │
│ └─ Status: Active                       │
└──────────────────────────────────────────┘
         │
         ↓
    [Stored in DB]
    ExpenableDbContext.Products


PHASE 2A: ONLINE SHOPPING (Registered Employees)
═════════════════════════════════════════════════════════════════════════════════════════

Employee Action:
┌─────────────────────────┐
│ Browse Catalog          │ ← Query: ListActiveProducts
│ SELECT * FROM Products  │   (reads active products)
│ WHERE Status = Active   │
└─────────────────────────┘
         │
         ↓
┌──────────────────────────┐
│ Add to Shopping Cart     │
│ AddToCartCommand         │
│ ├─ EmployeeId: emp-001  │
│ ├─ ProductId: prod-1    │
│ └─ Quantity: 50         │
└──────────────────────────┘
         │
         ↓
┌──────────────────────────────────────────────────┐
│ EmployeeShoppingCart Updated                     │
│ ├─ EmployeeId: emp-001                          │
│ ├─ Items (CartItems):                           │
│ │  ├─ ProductId: prod-1                         │
│ │  ├─ ProductName: "Ballpoint Pen"              │
│ │  ├─ Quantity: 50                              │
│ │  └─ UnitPrice: $0.50                          │
│ ├─ Status: Active                               │
│ └─ TotalPrice: $25.00                           │
└──────────────────────────────────────────────────┘
         │
         ↓
    [Stored in DB]
    ExpenableDbContext.ShoppingCarts


PHASE 2B: WALK-IN REQUEST (Unregistered Employees)
═════════════════════════════════════════════════════════════════════════════════════════

Supply Office Staff Action:
┌────────────────────────────────────┐
│ CreateWalkInSupplyRequest Command  │
│ ├─ EmployeeName: "John Doe"        │
│ ├─ OfficeId: office-1              │
│ └─ Items:                           │
│    └─ ProductId: prod-1, Qty: 50   │
└────────────────────────────────────┘
         │
         ↓
    [Skip to Phase 4: Supply Request Created]


PHASE 3: CHECKOUT (Online Path Only)
═════════════════════════════════════════════════════════════════════════════════════════

Employee Action:
┌─────────────────────────────┐
│ CheckoutCart Command        │
│ ├─ EmployeeId: emp-001      │
│ └─ Justification: "Supplies"│
└─────────────────────────────┘
         │
         ↓
┌──────────────────────────────────────────────────┐
│ SupplyRequest Created (from Cart)                │
│ ├─ RequestNumber: "EXP-2026-0001"                │
│ ├─ EmployeeId: emp-001                          │
│ ├─ Source: Online                               │
│ ├─ Status: Pending                              │
│ ├─ Items (SupplyRequestItems):                  │
│ │  ├─ ProductId: prod-1                         │
│ │  ├─ QuantityRequested: 50                     │
│ │  ├─ UnitPrice: $0.50                          │
│ │  └─ TotalPrice: $25.00                        │
│ └─ RequestedDate: 2026-03-07                    │
└──────────────────────────────────────────────────┘
         │
         ↓
    EmployeeShoppingCart Status → CheckedOut
         │
         ↓
    [Stored in DB]
    ExpenableDbContext.SupplyRequests
    ExpenableDbContext.ShoppingCarts


PHASE 4: APPROVAL WORKFLOW (Both Paths Converge)
═════════════════════════════════════════════════════════════════════════════════════════

Manager/Admin Action:
┌──────────────────────────────┐
│ ApproveSupplyRequest Command │
│ ├─ RequestId: req-1          │
│ └─ ApprovalNotes: "Approved" │
└──────────────────────────────┘
         │
         ↓
┌──────────────────────────────────────────┐
│ SupplyRequest Updated                    │
│ ├─ Status: Approved                      │
│ ├─ ApprovedBy: manager-1                 │
│ ├─ ApprovedDate: 2026-03-07 10:30 AM    │
│ └─ ApprovalNotes: "Approved"             │
└──────────────────────────────────────────┘
         │
         ↓
    [Stored in DB]
    ExpenableDbContext.SupplyRequests


PHASE 5: ALLOCATION (Supply Office / Inventory Team)
═════════════════════════════════════════════════════════════════════════════════════════

Inventory Team Action:
┌────────────────────────────────────┐
│ AllocateSupplyRequest Command      │
│ ├─ RequestId: req-1                │
│ ├─ AllocationItems:                │
│ │  └─ ProductId: prod-1, Qty: 50   │
│ └─ ExpectedDeliveryDate: 2026-03-10│
└────────────────────────────────────┘
         │
         ↓
┌──────────────────────────────────────────┐
│ SupplyRequest Updated                    │
│ ├─ Status: Allocated                     │
│ ├─ AllocatedBy: staff-1                  │
│ ├─ AllocatedDate: 2026-03-07 11:00 AM   │
│ └─ QuantityAllocated: 50                 │
│                                          │
│ SupplyRequestItem Updated:               │
│ └─ QuantityAllocated: 50                 │
└──────────────────────────────────────────┘
         │
         ↓
    [Stored in DB]
    ExpenableDbContext.SupplyRequests


PHASE 6: PURCHASE & RECEIPT (Sourcing)
═════════════════════════════════════════════════════════════════════════════════════════

Procurement Action:
┌──────────────────────────────────────┐
│ CreatePurchaseOrder Command          │
│ ├─ SupplierId: supplier-1            │
│ ├─ SupplierName: "Office Depot"      │
│ └─ LineItems:                        │
│    ├─ ProductId: prod-1              │
│    ├─ Quantity: 500                  │
│    └─ UnitPrice: $0.45 (negotiated)  │
└──────────────────────────────────────┘
         │
         ↓
┌──────────────────────────────────────────────────┐
│ Purchase Created                                 │
│ ├─ PurchaseOrderNumber: "PO-2026-0001"          │
│ ├─ SupplierId: supplier-1                       │
│ ├─ Status: Ordered                              │
│ ├─ TotalCost: $225.00 (500 × $0.45)             │
│ └─ LineItems (PurchaseLineItem):                │
│    ├─ ProductId: prod-1                         │
│    ├─ QuantityOrdered: 500                      │
│    ├─ UnitPrice: $0.45                          │
│    ├─ QuantityReceived: 0                       │
│    └─ QuantityIssued: 0                         │
└──────────────────────────────────────────────────┘
         │
         ↓
    [Stored in DB]
    ExpenableDbContext.Purchases

         ↓ (Goods Arrive)

┌────────────────────────────────────┐
│ ReceivePurchaseItems Command       │
│ ├─ PurchaseId: po-1                │
│ ├─ LineItemId: line-1              │
│ └─ QuantityReceived: 500           │
└────────────────────────────────────┘
         │
         ↓
┌──────────────────────────────────────────────────┐
│ Purchase Updated                                 │
│ ├─ Status: Received                             │
│ ├─ ReceiptDate: 2026-03-07 02:00 PM             │
│ ├─ TotalItemsReceived: 500                      │
│ └─ PurchaseLineItem Updated:                    │
│    ├─ QuantityReceived: 500                     │
│    └─ ReceivedDate: 2026-03-07 02:00 PM         │
└──────────────────────────────────────────────────┘
         │
         ↓
    [Stored in DB]
    ExpenableDbContext.Purchases


PHASE 7: ISSUANCE (Critical - Links Purchase → Employee Inventory)
═════════════════════════════════════════════════════════════════════════════════════════

Inventory Staff Action:
┌────────────────────────────────┐
│ DeliverAllocation Command      │
│ ├─ RequestId: req-1            │
│ └─ DeliveryDate: 2026-03-07    │
└────────────────────────────────┘
         │
         ↓
System Triggers: IssueSupplyRequest
         │
         ↓
┌────────────────────────────────────────────────┐
│ 1. FIND MATCHING PURCHASE                      │
│    Query: Find Purchase with:                  │
│    ├─ ProductId: prod-1                        │
│    ├─ Status: Received (not fully issued)      │
│    ├─ QuantityRemaining: 500 - 0 = 500 ✓      │
│    └─ Result: Purchase (PO-2026-0001)          │
└────────────────────────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────────────┐
│ 2. DEDUCT FROM PURCHASE                        │
│    Purchase.IssueItems(                        │
│    ├─ LineItemId: line-1                       │
│    └─ QuantityToIssue: 50                      │
│    )                                           │
│                                                │
│    Result:                                     │
│    ├─ QuantityIssued: 0 + 50 = 50              │
│    ├─ QuantityRemaining: 500 - 50 = 450       │
│    ├─ IssuedValue: 50 × $0.45 = $22.50        │
│    └─ Status: PartiallyIssued                  │
└────────────────────────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────────────┐
│ 3. CREATE INVENTORY BATCH                      │
│    InventoryBatch.Create(                      │
│    ├─ PurchaseId: po-1                         │
│    ├─ PurchaseLineItemId: line-1               │
│    ├─ QuantityReceived: 50                     │
│    ├─ UnitPrice: $0.45                         │
│    ├─ ReceivedDate: 2026-03-07 02:00 PM        │
│    └─ Version: 1                               │
│    )                                           │
│                                                │
│    Result:                                     │
│    └─ InventoryBatch created with:             │
│       ├─ AvailableQuantity: 50                 │
│       ├─ AvailableValue: 50 × $0.45 = $22.50  │
│       └─ Status: Ready for consumption         │
└────────────────────────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────────────┐
│ 4. ADD BATCH TO EMPLOYEE INVENTORY             │
│    EmployeeInventory.ReceiveItems(             │
│    ├─ EmployeeId: emp-001                      │
│    ├─ ProductId: prod-1                        │
│    ├─ Quantity: 50                             │
│    ├─ PurchaseId: po-1                         │
│    ├─ UnitPrice: $0.45                         │
│    └─ ReceivedDate: 2026-03-07                 │
│    )                                           │
│                                                │
│    Result:                                     │
│    └─ EmployeeInventory.Items:                 │
│       └─ InventoryItem (Product prod-1):       │
│          └─ Batches[]:                         │
│             └─ Batch 1:                        │
│                ├─ PurchaseId: po-1             │
│                ├─ Quantity: 50                 │
│                ├─ UnitPrice: $0.45             │
│                ├─ ReceivedDate: 2026-03-07     │
│                └─ AvailableQuantity: 50        │
└────────────────────────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────────────┐
│ 5. CREATE CONSUMPTION AUDIT LOG                │
│    InventoryConsumption.Create(                │
│    ├─ EmployeeId: emp-001                      │
│    ├─ ProductId: prod-1                        │
│    ├─ QuantityConsumed: 50                     │
│    ├─ UnitPrice: $0.45                         │
│    ├─ CostConsumed: 50 × $0.45 = $22.50        │
│    ├─ PurchaseId: po-1                         │
│    ├─ ReceivedDate: 2026-03-07 02:00 PM        │
│    ├─ ConsumedDate: 2026-03-07 03:00 PM        │
│    └─ DaysInInventory: 1                       │
│    )                                           │
│                                                │
│    Result:                                     │
│    └─ Complete audit trail created             │
│       linking: Supplier → PO → Employee        │
└────────────────────────────────────────────────┘
         │
         ↓
┌────────────────────────────────────────────────┐
│ 6. UPDATE SUPPLY REQUEST                       │
│    SupplyRequest Status: Completed             │
│    ├─ All items delivered                      │
│    ├─ DeliveredDate: 2026-03-07                │
│    └─ QuantityAllocated: 50 (matches request)  │
└────────────────────────────────────────────────┘
         │
         ↓
    [Stored in DB - ALL ENTITIES UPDATED]
    ExpenableDbContext.Purchases (Issue count + value)
    ExpenableDbContext.EmployeeInventories (Batch added)
    ExpenableDbContext.Consumptions (Audit log)
    ExpenableDbContext.SupplyRequests (Status)


PHASE 8: FUTURE CONSUMPTION BY EMPLOYEE
═════════════════════════════════════════════════════════════════════════════════════════

Employee Uses Items:
┌────────────────────────────────┐
│ Employee consumes 20 pens      │
│ from their inventory           │
│ (Manual or automatic tracking) │
└────────────────────────────────┘
         │
         ↓
Optional: Log Consumption
┌────────────────────────────────────────────────┐
│ InventoryConsumption logged again              │
│ ├─ EmployeeId: emp-001                         │
│ ├─ QuantityConsumed: 20                        │
│ ├─ Date: 2026-03-09                            │
│ ├─ DaysInInventory: 2 (time held)              │
│ └─ Links back to: PO-2026-0001 (PurchaseId)    │
└────────────────────────────────────────────────┘
         │
         ↓
    [Stored in DB]
    ExpenableDbContext.Consumptions


═════════════════════════════════════════════════════════════════════════════════════════

RECONCILIATION POINT:
For Product prod-1 @ $0.45:
  Purchase Received:     500 units @ $0.45 = $225.00
  ├─ Issued to emp-001:  50 units @ $0.45 = $22.50
  ├─ Remaining in PO:    450 units @ $0.45 = $202.50
  │
  Employee emp-001 Inventory:
  ├─ Batch 1:
  │  ├─ Received: 50 units
  │  ├─ Consumed: 20 units (logged)
  │  ├─ Available: 30 units
  │  └─ Value: 30 × $0.45 = $13.50
  │
  Total Accounting:
  ├─ Total Cost Paid: $225.00 (entire PO)
  ├─ Total Issued: $22.50 (emp-001)
  ├─ Total Remaining in PO: $202.50
  ├─ Total in Employee Inventory: $13.50 + $9.00 (consumed) = $22.50 ✓
  └─ Equation: $22.50 (issued) = $9.00 (consumed) + $13.50 (remaining) ✓
```

---

## Transaction Entity Dependency Map

```
UPSTREAM (Data Entry)          MIDDLE LAYER (Processing)         DOWNSTREAM (Inventory)
─────────────────────────────────────────────────────────────────────────────────────

Product                        SupplyRequest                      EmployeeInventory
├─ ProductCode          ┐     ├─ RequestNumber            ┐      ├─ EmployeeId
├─ ProductName          │     ├─ EmployeeId              │      ├─ Items[]
└─ UnitPrice            │     ├─ Source (Online/WalkIn)  │      │  └─ InventoryItem
                        │     ├─ Items (cart items)      │      │     ├─ ProductId
                        │     └─ Status (Pending→etc)    │      │     └─ Batches[]
                        │                                 │      │        └─ InventoryBatch
                        ↓                                 ↓      │           ├─ PurchaseId ◄─┐
                                                                │           ├─ UnitPrice    │
Purchase                       SupplyRequestItem          │           ├─ ReceivedDate  │
├─ PurchaseOrderNumber  ┐      ├─ ProductId             │           └─ QuantityIssued│
├─ SupplierId           │      ├─ Quantity              │                            │
├─ LineItems[]          │      ├─ UnitPrice             │      InventoryConsumption  │
│  └─ PurchaseLineItem  │      ├─ PurchaseId ───────────┼──►   ├─ EmployeeId        │
│     ├─ ProductId      │      ├─ PurchaseLineItemId    │      ├─ ProductId         │
│     ├─ Quantity       │      └─ TotalPrice            │      ├─ QuantityConsumed  │
│     └─ UnitPrice      │                               │      ├─ PurchaseId ───────┘
│                       │                               │      ├─ ReceivedDate
└─ Status              │                               │      └─ ConsumedDate
    (Ordered→          │                               │
    Received→          │                               │
    PartiallyIssued→   ├──────────────────────────────┘
    FullyIssued)       │
                        │
                        └─→ Links TO EmployeeInventory
                            during Phase 7 (Issuance)
```

---

## Data Flow Summary by Phase

| Phase | Source Entity | Action | Target Entity | Key Result |
|-------|---------------|--------|---------------|-----------|
| 1 | Admin | CreateProduct | Product | Product stored, Status=Active |
| 2A | Employee | AddToCart | EmployeeShoppingCart → CartItem | Items staged for checkout |
| 2B | Office Staff | CreateWalkInRequest | SupplyRequest → SupplyRequestItem | Direct to Phase 4 |
| 3 | Employee | CheckoutCart | SupplyRequest → SupplyRequestItem | Cart → Request, Status=Pending |
| 4 | Manager | ApproveSupplyRequest | SupplyRequest | Status=Approved |
| 5 | Inventory | AllocateSupplyRequest | SupplyRequest | Status=Allocated, Quantities set |
| 6 | Procurement | CreatePurchaseOrder | Purchase → PurchaseLineItem | PO created, Status=Ordered |
| 6 | Receiving | ReceivePurchaseItems | Purchase → PurchaseLineItem | Status=Received |
| 7 | System | IssueSupplyRequest (Issue) | Purchase deducted | QuantityIssued incremented |
| 7 | System | IssueSupplyRequest (Add) | EmployeeInventory → InventoryBatch | Batch created with FIFO tracking |
| 7 | System | IssueSupplyRequest (Log) | InventoryConsumption | Audit trail created |
| 8 | Employee | Use items | InventoryConsumption (optional) | Consumption logged for aging |

---

1. **Dual-Access Model**: 
   - **Online**: Registered employees with accounts can self-serve via shopping cart
   - **Walk-In**: Unregistered/guest employees can request via supply office staff

2. **Account-Based Access**: 
   - Shopping features require employee to exist in Library module
   - Non-registered employees are directed to supply office

3. **Shopping Cart Experience**: 
   - Familiar e-commerce workflow (browse → add to cart → checkout)
   - Carts persist for later completion
   - Abandoned carts tracked for analytics

4. **FIFO Batch Inventory Management** ← Industry-standard POS:
   - Items with **same product + same price** are **combined** into one batch (Purchase.AddLineItem combines if price matches)
   - Items with **same product + different price** create **separate batches** (each from different PO with different price)
   - **FIFO Consumption**: Oldest ReceivedDate batches consumed first (EmployeeInventory.ConsumeItems() sorts by ReceivedDate)
   - Each batch tracks: QuantityReceived, QuantityIssued, UnitPrice, ReceivedDate, PurchaseId, Version
   - Prevents price merging: Items with different prices never mix in same inventory entry

5. **Purchase & Cost Tracking**: 
   - Every item tracks its source purchase and line item for complete traceability
   - Each purchase maintains independent unit price per line item
   - Accurate cost calculation: Cost = Quantity × UnitPrice per batch
   - FIFO cost allocation for issuance (oldest batches issued first)

6. **Optimistic Locking for Concurrency** ← Enhanced for safety:
   - Version field on InventoryBatch incremented on each update
   - Prevents race conditions during concurrent issuances
   - Thread A and Thread B cannot both issue from same batch without detecting conflict

7. **Complete Audit Trail** ← Enhanced for compliance:
   - InventoryConsumption table logs every transaction
   - Tracks: EmployeeId, ProductId, QuantityConsumed, UnitPrice, CostConsumed, PurchaseId, ReceivedDate, ConsumedDate
   - Enables traceability: Supplier → Purchase → Employee Inventory → Consumption

8. **Automated Reconciliation** ← Industry best practice:
   - Verifies: QuantityReceived = QuantityIssued + QuantityInInventory (per batch)
   - Verifies: TotalCost = TotalIssuedValue + RemainingValue (per purchase)
   - Daily automated checks detect data integrity issues immediately

9. **Unified Approval Workflow**: 
   - Both online and walk-in requests follow same approval process
   - Source tracking differentiates request origin
   - No approval workflow difference based on source

10. **Read-Only Library**: Never modifies Library data, only queries it

11. **Tenant Isolation**: All data segregated by tenant

12. **Soft Delete**: Requests and purchases can be cancelled but never truly deleted (audit compliance)

---

## Future Enhancements

### Purchase Management Enhancements
- **Valuation Methods**: Support FIFO/LIFO/Weighted Average cost valuation
- **Price Variance Analysis**: Track differences between expected and actual costs
- **Supplier Performance**: Track delivery times, quality issues, price trends
- **Purchase Reconciliation**: Three-way match (PO → Receipt → Invoice)
- **Automated Reordering**: Auto-create POs based on consumption rates
- **Inventory Aging**: Identify slow-moving or obsolete items

### Online Shopping Features
- **Wishlist**: Save items for later purchase
- **Favorites**: Quick reorder of frequently requested items
- **Smart Recommendations**: Suggest items based on history
- **Notification**: Alert when low-stock items are back in stock
- **Price History**: Show price trends for items

### Inventory & Fulfillment
- **Return/Restock**: Process for employees to return unused items
- **Consumption Tracking**: Monitor how quickly items are used
- **Reorder Automation**: Auto-request items based on consumption patterns
- **Barcode/QR Scanning**: Physical inventory reconciliation
- **Expiration Tracking**: Alert for expiring items
- **Batch Number Tracking**: Track serial numbers and expiration dates per batch

### Analytics & Reporting
- **Cost Analytics**: Track actual vs budgeted costs per issuance
- **Dashboard**: Shopping trends, conversion rates, cart abandonment
- **Analytics**: Request trends, approval rates by manager
- **Compliance Reports**: Audit trail with cost details for each issuance
- **Budget Dashboard**: Track spending by department/employee with cost breakdown
- **Conversion Metrics**: Online vs walk-in request comparison
- **Cost Per Department**: Analyze expenditure by cost center/department

### Integration
- **Integration with Accounting**: Allocate costs to departments/cost centers using actual purchase prices
- **Email Notifications**: Order confirmation, approval updates, issuance confirmations
- **Mobile App**: Mobile shopping and issuance tracking for employees
- **Integration with HR**: Auto-update employee status changes
- **Fixed Asset Integration**: Track items that need to be registered as assets
