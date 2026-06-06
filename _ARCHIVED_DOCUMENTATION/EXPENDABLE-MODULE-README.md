# Module2-Expendable (Employee Online Shopping & Supply Request System)

Complete vertical slice implementation following AMIS (Full Stack Hero) DDD and EF Core multi-tenancy patterns.

## Overview

Module2-Expendable is a comprehensive system for managing employee supply requests, shopping carts, purchase orders, and inventory tracking. It implements a complete CQRS pattern with domain-driven design principles.

## Architecture

### Project Structure

```
Modules.Expendable.Contracts/          # Shared contracts and DTOs
├── v1/
│   ├── Products/                       # Product contracts
│   ├── Purchases/                      # Purchase order contracts
│   ├── Requests/                       # Supply request contracts
│   └── Cart/                           # Shopping cart contracts

Modules.Expendable/                    # Main module implementation
├── Domain/                             # DDD domain layer
│   ├── Products/                       # Product aggregate root
│   ├── Purchases/                      # Purchase aggregate root
│   ├── Requests/                       # Supply request aggregate root
│   ├── Cart/                           # Shopping cart aggregate root
│   └── Inventory/                      # Employee inventory aggregate root
├── Data/                               # EF Core data layer
│   ├── ExpenableDbContext.cs          # Multi-tenant DB context
│   └── Configurations/                 # Entity configurations
├── Features/                           # CQRS handlers
│   └── v1/
│       ├── Products/                   # Product handlers
│       ├── Purchases/                  # Purchase handlers
│       ├── Requests/                   # Supply request handlers
│       └── Cart/                       # Shopping cart handlers
├── ExpenableModule.cs                 # Module registration & endpoints
└── ExpenableModuleConstants.cs         # Constants & permissions
```

## Key Features

### 1. Product Management
- **Domain Entity**: `Product` (AggregateRoot)
- **Status**: Active, Inactive, Discontinued, OutOfStock
- **Operations**:
  - Create products with SKU, name, pricing, and stock levels
  - Update product details
  - Activate/Deactivate products
  - Discontinue products
  - Mark out of stock

### 2. Purchase Orders
- **Domain Entity**: `Purchase` (AggregateRoot)
- **Components**: `PurchaseLineItem` (Value Object)
- **Status**: Draft → Submitted → Approved → PartiallyReceived → FullyReceived (or Cancelled)
- **Operations**:
  - Create purchase orders
  - Add/remove line items
  - Submit for approval
  - Approve orders
  - Record receipts with rejection handling
  - Cancel orders
  - Track delivery status

### 3. Supply Requests
- **Domain Entity**: `SupplyRequest` (AggregateRoot)
- **Components**: `SupplyRequestItem` (Value Object)
- **Status**: Draft → Submitted → Approved (or Rejected) → Fulfilled (or Cancelled)
- **Operations**:
  - Create supply requests
  - Add/remove items with justification
  - Submit requests
  - Approve with quantity override capability
  - Reject with reasons
  - Mark as fulfilled
  - Cancel requests
  - Track approval status and timelines

### 4. Employee Shopping Cart
- **Domain Entity**: `EmployeeShoppingCart` (AggregateRoot)
- **Components**: `CartItem` (Value Object)
- **Status**: Active, Converted, Abandoned, Cleared
- **Operations**:
  - Get or create employee cart
  - Add items to cart
  - Update item quantities
  - Remove items
  - Convert cart to supply request
  - Clear cart
  - Track cart totals and item counts

### 5. Employee Inventory
- **Domain Entity**: `EmployeeInventory` (AggregateRoot)
- **Components**: `InventoryBatch` (Value Object with FIFO tracking)
- **Audit**: `InventoryConsumption` (Entity)
- **Operations**:
  - Track employee inventory per product
  - Receive inventory in batches
  - Consume inventory using FIFO principle
  - Handle batch expiration dates
  - Track consumption with audit trail
  - Manage inventory across tenants

## Database Design

### Multi-Tenancy
- All entities implement `IHasTenant` interface
- Automatic TenantId filtering via query filters
- Separate database contexts per tenant

### Concurrency Control
- Optimistic concurrency using `Version` (row version/timestamp)
- All aggregates include version token

### Soft Deletes
- Soft delete support via `ISoftDelete` interface
- Deleted entities automatically filtered from queries
- Tracks deletion timestamp and user

### Audit Tracking
- `IAuditableEntity` interface for audit fields
- Tracks: CreatedOnUtc, CreatedBy, LastModifiedOnUtc, LastModifiedBy
- Maintained automatically by framework

### Data Types
- Decimal with precision(18,2) for monetary values
- JSON-serialized value objects (LineItems, Items)
- Separate batch table for inventory tracking

## CQRS Pattern

### Commands
Each feature has corresponding command handlers:
- **Product**: CreateProduct, UpdateProduct, ActivateProduct, DeactivateProduct, DiscontinueProduct, MarkOutOfStock
- **Purchase**: CreatePurchaseOrder, AddLineItem, RemoveLineItem, SubmitPurchaseOrder, ApprovePurchaseOrder, RecordReceipt, CancelPurchaseOrder
- **SupplyRequest**: CreateSupplyRequest, AddItem, RemoveItem, SubmitSupplyRequest, ApproveSupplyRequest, RejectSupplyRequest, MarkFulfilled, CancelSupplyRequest
- **Cart**: GetOrCreateCart, AddToCart, UpdateItemQuantity, RemoveFromCart, ConvertToRequest, ClearCart

### Queries
- **Product**: GetProduct, SearchProducts, ListActiveProducts
- **Purchase**: GetPurchase, SearchPurchases, GetPurchasesBySupplier
- **SupplyRequest**: GetSupplyRequest, SearchSupplyRequests, GetEmployeeSupplyRequests
- **Cart**: GetEmployeeCart, GetCart

## Validation

### FluentValidation Integration
- Automatic validation on command execution
- Custom validators for complex business rules
- Comprehensive error messages

### Validators Included
- `CreateProductCommandValidator`
- `UpdateProductCommandValidator`
- `CreatePurchaseOrderCommandValidator`
- `AddPurchaseLineItemCommandValidator`
- `RecordPurchaseReceiptCommandValidator`
- `CreateSupplyRequestCommandValidator`
- `AddSupplyRequestItemCommandValidator`
- `ApproveSupplyRequestCommandValidator`
- `AddToCartCommandValidator`
- `UpdateCartItemQuantityCommandValidator`
- `ConvertCartToSupplyRequestCommandValidator`

## Business Logic

### Product Entity
```csharp
// Factory method
Product.Create(tenantId, sku, name, description, unitPrice, unitOfMeasure, minStock, reorderQty)

// State transitions
product.Activate()
product.Deactivate()
product.Discontinue()
product.MarkOutOfStock()
product.Update(name, description, unitPrice, minStock, reorderQty)
```

### Purchase Entity
```csharp
// Factory and operations
Purchase.Create(tenantId, poNumber, supplierId, expectedDelivery)
purchase.AddLineItem(productId, quantity, unitPrice)
purchase.RemoveLineItem(productId)
purchase.Submit()
purchase.Approve()
purchase.RecordReceipt(productId, receivedQty, rejectedQty)
purchase.Cancel()
```

### SupplyRequest Entity
```csharp
// Factory and operations
SupplyRequest.Create(tenantId, requestNumber, employeeId, departmentId, justification, neededBy)
request.AddItem(productId, quantity, notes)
request.RemoveItem(productId)
request.Submit()
request.Approve(approvedBy, approvedQuantities)  // Map: ProductId -> ApprovedQty
request.Reject(reason)
request.MarkFulfilled()
request.Cancel()
```

### EmployeeShoppingCart Entity
```csharp
// Factory and operations
EmployeeShoppingCart.Create(tenantId, employeeId)
cart.AddItem(productId, quantity, unitPrice)
cart.UpdateItemQuantity(productId, newQuantity)
cart.RemoveItem(productId)
cart.GetCartTotal()
cart.GetTotalItemCount()
cart.ConvertToRequest(supplyRequestId)
cart.Clear()
```

### EmployeeInventory Entity
```csharp
// Factory and operations
EmployeeInventory.Create(tenantId, employeeId, productId)
inventory.ReceiveInventory(quantity, batchNumber, expiryDate)  // Adds batch
inventory.ConsumeInventory(quantity)  // FIFO consumption
inventory.GetAvailableBatches()
inventory.GetExpiredBatches()
```

## API Endpoints

### Products
- `POST /api/v1/expendable/products` - Create product
- `PUT /api/v1/expendable/products/{id}` - Update product
- `POST /api/v1/expendable/products/{id}/activate` - Activate
- `POST /api/v1/expendable/products/{id}/deactivate` - Deactivate
- `GET /api/v1/expendable/products/{id}` - Get product
- `GET /api/v1/expendable/products` - Search products

### Purchase Orders
- `POST /api/v1/expendable/purchases` - Create PO
- `POST /api/v1/expendable/purchases/{id}/submit` - Submit PO
- `POST /api/v1/expendable/purchases/{id}/approve` - Approve PO
- `GET /api/v1/expendable/purchases/{id}` - Get PO
- `GET /api/v1/expendable/purchases` - Search POs

### Supply Requests
- `POST /api/v1/expendable/supply-requests` - Create request
- `POST /api/v1/expendable/supply-requests/{id}/submit` - Submit
- `POST /api/v1/expendable/supply-requests/{id}/approve` - Approve
- `GET /api/v1/expendable/supply-requests/{id}` - Get request
- `GET /api/v1/expendable/supply-requests` - Search requests

### Shopping Cart
- `POST /api/v1/expendable/cart/get-or-create` - Get/create cart
- `POST /api/v1/expendable/cart/{id}/add-item` - Add to cart
- `GET /api/v1/expendable/cart/{id}` - Get cart
- `POST /api/v1/expendable/cart/{id}/convert-to-request` - Convert to request

## Permission Model

```csharp
Permissions.Expendable.Products.*
Permissions.Expendable.Purchases.*
Permissions.Expendable.SupplyRequests.*
Permissions.Expendable.ShoppingCarts.*
Permissions.Expendable.Inventory.*
```

## Feature Flags

- `Expendable:ProductManagement`
- `Expendable:PurchaseOrders`
- `Expendable:SupplyRequests`
- `Expendable:ShoppingCart`
- `Expendable:InventoryTracking`

## Usage Examples

### Create and Submit a Purchase Order

```csharp
// Create PO
var createCmd = new CreatePurchaseOrderCommand("SUPPLIER-001", DateTime.UtcNow.AddDays(14));
var po = await mediator.Send(createCmd);

// Add line items
await mediator.Send(new AddPurchaseLineItemCommand(po.Id, productId, 10, 99.99m));

// Submit for approval
await mediator.Send(new SubmitPurchaseOrderCommand(po.Id));

// Approve
await mediator.Send(new ApprovePurchaseOrderCommand(po.Id));

// Record receipt
await mediator.Send(new RecordPurchaseReceiptCommand(po.Id, productId, 10, 0));
```

### Employee Shopping Cart Workflow

```csharp
// Get or create cart
var getCart = new GetOrCreateCartCommand(employeeId);
var cart = await mediator.Send(getCart);

// Add items
await mediator.Send(new AddToCartCommand(cart.Id, productId, 5, unitPrice));

// Convert to supply request
var convertCmd = new ConvertCartToSupplyRequestCommand(
    cart.Id, 
    "DEPT-001",
    "Office supplies needed for Q1 projects",
    DateTime.UtcNow.AddDays(7));
var request = await mediator.Send(convertCmd);
```

### Employee Inventory Tracking

```csharp
// Create inventory for employee
var inventory = EmployeeInventory.Create(tenantId, employeeId, productId);

// Receive batch
inventory.ReceiveInventory(100, "BATCH-001", DateTime.UtcNow.AddMonths(6));

// Consume items (FIFO)
var consumed = inventory.ConsumeInventory(25);

// Check available
var available = inventory.GetAvailableBatches();
var expired = inventory.GetExpiredBatches();
```

## Integration Points

### With Other Modules
- **Identity Module**: User authentication and authorization
- **Auditing Module**: Comprehensive audit trail
- **Multitenancy Module**: Multi-tenant configuration

### Event Publishing
- Domain events can be published for:
  - Product status changes
  - PO lifecycle events
  - Supply request approvals/rejections
  - Inventory consumption

## Testing Considerations

1. **Domain Logic**: Test business rule enforcement
2. **CQRS Handlers**: Test command/query execution
3. **Validation**: Test FluentValidation rules
4. **Multi-Tenancy**: Test tenant isolation
5. **Soft Deletes**: Test deleted record filtering
6. **Concurrency**: Test optimistic locking

## Migration

The module uses Entity Framework Core migrations. Initial migration name: `AddExpenableModule`

```bash
dotnet ef migrations add AddExpenableModule --project src/Modules/Expendable/Modules.Expendable
```

## Schema

All tables created in `expendable` schema for clear module separation.

## Performance Considerations

1. **Indexes**: Created on TenantId + Status + other key search fields
2. **Pagination**: Implemented on all search queries
3. **JSON Storage**: Value objects stored as JSON for performance
4. **Query Filters**: Automatic tenant and soft-delete filtering

## Future Enhancements

- Workflow engine for approval processes
- Notification system for status changes
- Advanced reporting and analytics
- Budget tracking and allocations
- Supplier rating and history
- Batch expiration warnings
- Integration with external procurement systems

---

**Version**: 1.0  
**Last Updated**: 2026-03-07

