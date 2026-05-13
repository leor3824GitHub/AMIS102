# Module2-Expendable Quick Start Guide

## For Developers

### Getting Started

#### 1. Build the Solution
```bash
cd c:\AMIS101
dotnet build src/AMIS.Framework.slnx
```

#### 2. Create Database Migration
```bash
cd src/Modules/Expendable/Modules.Expendable
dotnet ef migrations add AddExpenableModule -o Data/Migrations
```

#### 3. Update Database
```bash
dotnet ef database update --startup-project ../../Playground/Playground.Api
```

### Common Tasks

#### Create a Product
```csharp
var command = new CreateProductCommand(
    SKU: "SKU-001",
    Name: "Office Chair",
    Description: "Ergonomic office chair",
    UnitPrice: 249.99m,
    UnitOfMeasure: "PCS",
    MinimumStockLevel: 5,
    ReorderQuantity: 10);

var product = await mediator.Send(command);
```

#### Create and Process a Purchase Order
```csharp
// Create PO
var createPoCmd = new CreatePurchaseOrderCommand(
    SupplierId: "SUPPLIER-001",
    ExpectedDeliveryDate: DateTime.UtcNow.AddDays(7));
var po = await mediator.Send(createPoCmd);

// Add line items
await mediator.Send(new AddPurchaseLineItemCommand(
    PurchaseId: po.Id,
    ProductId: productId,
    Quantity: 50,
    UnitPrice: 99.99m));

// Submit for approval
await mediator.Send(new SubmitPurchaseOrderCommand(po.Id));

// Approve
await mediator.Send(new ApprovePurchaseOrderCommand(po.Id));

// Record receipt
await mediator.Send(new RecordPurchaseReceiptCommand(
    PurchaseId: po.Id,
    ProductId: productId,
    ReceivedQuantity: 50));
```

#### Employee Shopping Cart to Supply Request
```csharp
// Get or create cart
var getCartCmd = new GetOrCreateCartCommand(employeeId);
var cart = await mediator.Send(getCartCmd);

// Add items to cart
await mediator.Send(new AddToCartCommand(
    CartId: cart.Id,
    ProductId: productId,
    Quantity: 5,
    UnitPrice: 99.99m));

// Convert cart to supply request
var convertCmd = new ConvertCartToSupplyRequestCommand(
    CartId: cart.Id,
    DepartmentId: "DEPT-IT",
    BusinessJustification: "Q1 office supplies",
    NeededByDate: DateTime.UtcNow.AddDays(7));
var supplyRequest = await mediator.Send(convertCmd);
```

#### Track Employee Inventory
```csharp
// Create inventory for employee
var inventory = EmployeeInventory.Create(tenantId, employeeId, productId);

// Receive inventory batch
inventory.ReceiveInventory(
    quantity: 100,
    batchNumber: "BATCH-001",
    expiryDate: DateTime.UtcNow.AddMonths(6));

// Consume inventory (FIFO)
inventory.ConsumeInventory(25);

// Get available batches
var available = inventory.GetAvailableBatches();

// Get expired batches
var expired = inventory.GetExpiredBatches();
```

### File Organization

```
Modules.Expendable/
├── Domain/              # Business logic, aggregates, entities
├── Data/                # EF Core mappings, DbContext
├── Features/            # Handlers, validators, mappers (organized by entity)
├── ExpenableModule.cs          # Module registration & endpoints
└── ExpenableModuleConstants.cs # Constants, permissions, features
```

### Adding New Features

#### 1. Create Domain Entity
```csharp
// src/Modules/Expendable/Domain/YourEntity/YourEntity.cs
public class YourEntity : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    // ... properties
    
    public static YourEntity Create(...)
    {
        return new YourEntity { /* ... */ };
    }
}
```

#### 2. Create Contracts
```csharp
// Modules.Expendable.Contracts/v1/YourEntity/YourEntityContracts.cs
public record YourEntityDto(...);
public record CreateYourEntityCommand(...) : ICommand<YourEntityDto>;
public record GetYourEntityQuery(Guid Id) : IQuery<YourEntityDto?>;
```

#### 3. Create Configuration
```csharp
// Data/Configurations/YourEntityConfiguration.cs
public class YourEntityConfiguration : IEntityTypeConfiguration<YourEntity>
{
    public void Configure(EntityTypeBuilder<YourEntity> builder)
    {
        builder.ToTable("YourEntities", ExpenableModuleConstants.SchemaName);
        // ... configuration
    }
}
```

#### 4. Create Handlers
```csharp
// Features/v1/YourEntity/YourEntityCommandHandlers.cs
internal sealed class CreateYourEntityCommandHandler : ICommandHandler<CreateYourEntityCommand, YourEntityDto>
{
    public async ValueTask<YourEntityDto> Handle(CreateYourEntityCommand command, CancellationToken cancellationToken)
    {
        // ... implementation
    }
}

// Features/v1/YourEntity/YourEntityQueryHandlers.cs
internal sealed class GetYourEntityQueryHandler : IQueryHandler<GetYourEntityQuery, YourEntityDto?>
{
    public async ValueTask<YourEntityDto?> Handle(GetYourEntityQuery query, CancellationToken cancellationToken)
    {
        // ... implementation
    }
}
```

#### 5. Create Validator (if needed)
```csharp
// Features/v1/YourEntity/YourEntityValidators.cs
public class CreateYourEntityCommandValidator : AbstractValidator<CreateYourEntityCommand>
{
    public CreateYourEntityCommandValidator()
    {
        RuleFor(x => x.Property)
            .NotEmpty().WithMessage("Property is required");
    }
}
```

#### 6. Add Endpoint (in ExpenableModule.cs)
```csharp
group.MapPost("/your-entities", CreateYourEntity)
    .WithName("CreateYourEntity");
```

### Key Design Patterns

#### Domain Events
```csharp
// In your aggregate
private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

// Record event when state changes
public void PerformAction()
{
    // ... business logic
    AddDomainEvent(YourEventOccurred.Create(...));
}
```

#### Business Rule Validation
```csharp
// In domain entity or handler
if (Status != ProductStatus.Active)
    throw new InvalidOperationException("Only active products can be updated.");
```

#### Query Filtering
```csharp
// Automatic filtering by:
// - TenantId (from context)
// - IsDeleted = false
// - Any custom query filters

var items = _repository.Query()
    .Where(x => x.Status == Status.Active)
    .ToList();
```

### Testing Examples

#### Domain Entity Tests
```csharp
[Test]
public void CreateProduct_WithValidData_CreatesSuccessfully()
{
    // Arrange
    var tenantId = "tenant-1";
    var sku = "SKU-001";
    
    // Act
    var product = Product.Create(tenantId, sku, "Name", "Desc", 99.99m, "PCS", 5, 10);
    
    // Assert
    Assert.AreEqual(tenantId, product.TenantId);
    Assert.AreEqual(sku, product.SKU);
}
```

#### Handler Tests
```csharp
[Test]
public async Task CreateProductCommandHandler_WithValidCommand_ReturnsProductDto()
{
    // Arrange
    var command = new CreateProductCommand(...);
    var mockRepository = new Mock<IRepository<Product>>();
    var mockCurrentUser = new Mock<ICurrentUser>();
    var handler = new CreateProductCommandHandler(mockRepository.Object, mockCurrentUser.Object);
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.IsNotNull(result);
}
```

### Performance Tips

1. **Use Pagination**
   - Always paginate list queries
   - Default: PageSize = 10

2. **Index Usage**
   - Queries on TenantId + Status are indexed
   - Unique indexes prevent duplicates

3. **Lazy Loading**
   - Use Include() for related entities when needed
   - Avoid N+1 queries

4. **Caching (Future Enhancement)**
   - Cache active products
   - Cache supplier lists

### Debugging Tips

1. **Enable SQL Logging**
   ```csharp
   optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
   ```

2. **Check Multi-Tenancy**
   - Verify TenantId is set in context
   - Check tenant-specific connections

3. **Concurrency Issues**
   - If DbUpdateConcurrencyException, check Version field
   - User updated entity before your changes

4. **Soft Delete Issues**
   - Remember query filters exclude deleted items
   - Use `IgnoreQueryFilters()` only if needed

### Common Errors & Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| `Entity not found` | Wrong tenant or soft deleted | Check TenantId, verify IsDeleted |
| `Concurrency conflict` | Version mismatch | Reload entity before update |
| `Validation failed` | Business rule violation | Check validator messages |
| `Foreign key violation` | Product/Supplier not exists | Verify IDs before creating |

### Useful Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Create migration
dotnet ef migrations add MigrationName

# View pending migrations
dotnet ef migrations list

# Revert migration
dotnet ef migrations remove

# Update database
dotnet ef database update

# View generated SQL
dotnet ef migrations script
```

### Useful Queries

#### Get All Products
```csharp
var query = new SearchProductsQuery(PageNumber: 1, PageSize: 100);
var result = await mediator.Send(query);
```

#### Get Pending Supply Requests
```csharp
var query = new SearchSupplyRequestsQuery(
    Status: nameof(SupplyRequestStatus.Submitted),
    PageNumber: 1);
var result = await mediator.Send(query);
```

#### Get Employee's Cart
```csharp
var query = new GetEmployeeCartQuery(employeeId);
var cart = await mediator.Send(query);
```

### Best Practices

✅ **DO**
- Use factory methods for entity creation
- Validate in domain entities, not just handlers
- Use value objects for composed properties
- Keep handlers focused and single-responsibility
- Document complex business logic
- Test edge cases and state transitions

❌ **DON'T**
- Expose internal lists (use ReadOnlyCollection)
- Bypass validation with direct property setters
- Mix query and command logic
- Use magic strings (use constants)
- Forget to set TenantId in entities
- Leave null reference exceptions unhandled

### Resources

- [AMIS Documentation](https://AMIS (Asset Management Information System).net)
- [Entity Framework Core Docs](https://docs.microsoft.com/en-us/ef/core)
- [MediatR GitHub](https://github.com/jbogard/MediatR)
- [FluentValidation Docs](https://fluentvalidation.net)

### Support

For issues or questions:
1. Check the [EXPENDABLE-MODULE-README.md](../EXPENDABLE-MODULE-README.md)
2. Review test examples in Tests folder
3. Check existing module implementations
4. Consult team lead or architect

---

**Last Updated**: March 7, 2026  
**Version**: 1.0

