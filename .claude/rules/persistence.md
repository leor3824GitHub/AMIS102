---
paths:
  - "src/Modules/**/Data/**"
  - "src/Modules/**/Domain/**"
---

# Persistence Rules

EF Core data-access patterns in AMIS. Handlers inject their module's `DbContext` directly — there is no
repository layer.

## DbContext Pattern

### One DbContext Per Module

```csharp
namespace AMIS.Modules.Catalog.Persistence;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) 
    : BaseDbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");  // ✅ Module-specific schema
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### BaseDbContext Features

Inherited from `BuildingBlocks.Persistence`:
- Automatic tenant filtering
- Audit trail (Created/Modified timestamps)
- Soft delete support
- Domain event publishing

## Entity Configuration

### Use Fluent API (NOT Data Annotations)

```csharp
namespace AMIS.Modules.Catalog.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", "catalog");
        
        // Primary key
        builder.HasKey(p => p.Id);
        
        // Properties
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(p => p.Description)
            .HasMaxLength(2000);
        
        // Value object (owned type)
        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 2);
            
            price.Property(m => m.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3);
        });
        
        // Relationships
        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId);
        
        // Indexes
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.TenantId);  // ✅ For multi-tenancy
    }
}
```

## Data Access — Inject the Module DbContext

> ⚠️ **There is no `IRepository<T>` in this codebase.** No repository abstraction has ever existed here.
> All 390+ handlers inject their module's `DbContext` directly. Do not scaffold, mock, or reference
> `IRepository<T>` — it will not compile.

### Usage in Handlers

Inject the module `DbContext` (plus `ICurrentUser` when you need tenant or user identity). Construct
entities through their static factory method, add, save.

```csharp
public sealed class CreateProductCommandHandler(
    ExpendableDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateProductCommand, ProductDto>
{
    public async ValueTask<ProductDto> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required");

        var product = Product.Create(tenantId, command.StockNo, command.Name, command.UnitPrice /* … */);
        product.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return product.ToProductDto();
    }
}
```

Rules:

- **Always `.ConfigureAwait(false)`** on awaits in handlers — matches every existing handler.
- **Enforce invariants in the entity's factory/behavior methods**, not the handler. The handler
  translates domain exceptions into HTTP-shaped ones (see `.claude/skills/error-handling`).
- **Uniqueness checks** that the database enforces with an index should *also* be caught on
  `DbUpdateException` and rethrown as a `ValidationException` — the pre-check races.
  `CreateProductCommandHandler` is the reference for this.

Reference: [CreateProductCommandHandler.cs](../../src/Modules/Expendable/Modules.Expendable/Features/v1/Products/CreateProduct/CreateProductCommandHandler.cs)

## Specification Pattern

### Creating Specifications

```csharp
namespace AMIS.Modules.Catalog.Specifications;

public class ProductsByNameSpec : Specification<Product>
{
    public ProductsByNameSpec(string searchTerm)
    {
        Query
            .Where(p => p.Name.Contains(searchTerm))
            .OrderBy(p => p.Name);
    }
}

public class ActiveProductsSpec : Specification<Product>
{
    public ActiveProductsSpec()
    {
        Query
            .Where(p => !p.IsDeleted && p.IsActive)
            .Include(p => p.Category)
            .OrderByDescending(p => p.CreatedAt);
    }
}
```

### Using Specifications

Apply a specification to the `DbContext`'s `DbSet` with `ApplySpecification`:

```csharp
public sealed class GetProductsQueryHandler(ExpendableDbContext dbContext)
    : IQueryHandler<GetProductsQuery, List<ProductDto>>
{
    public async ValueTask<List<ProductDto>> Handle(GetProductsQuery query, CancellationToken ct)
    {
        return await dbContext.Products
            .ApplySpecification(new ActiveProductsSpec())
            .Select(p => new ProductDto(p.Id, p.Name /* … */))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
```

> **Reality check:** the specification engine in `BuildingBlocks/Persistence/Specifications` is fully
> featured (`AsNoTracking` default-on, `AsSplitQuery`, `IgnoreQueryFilters`, includes, whitelisted
> `ApplySortingOverride`, projected `ISpecification<T, TResult>`) but is currently used in **exactly one**
> place: [GetTenantsSpecification.cs](../../src/Modules/Multitenancy/Modules.Multitenancy/Features/v1/GetTenants/GetTenantsSpecification.cs).
> Every other query handler composes `IQueryable` inline. Both are acceptable; prefer a specification
> when the same query shape is needed in more than one handler, and inline `IQueryable` for a one-off.

### Pagination Specification

```csharp
public class ProductsPaginatedSpec : Specification<Product>
{
    public ProductsPaginatedSpec(int pageNumber, int pageSize)
    {
        Query
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }
}
```

## Entity Base Classes

All of these live in `AMIS.Framework.Core.Domain` (`src/BuildingBlocks/Core/Domain/`). The signatures
below are the **actual** ones — note every marker interface is **get-only**, so the backing properties on
your entity stay `private set` and are mutated through domain methods.

### `BaseEntity<TId>` / `AggregateRoot<TId>`

```csharp
public abstract class BaseEntity<TId> : IEntity<TId>, IHasDomainEvents
{
    public TId Id { get; protected set; } = default!;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
    protected void AddDomainEvent(IDomainEvent @event);
    public void ClearDomainEvents();
}
```

`AggregateRoot<TId>` is a marker subclass — use it for aggregate roots, `BaseEntity<TId>` for entities
that are not roots. Note it is **generic**; there is no non-generic `BaseEntity`.

### `IAuditableEntity`

```csharp
public interface IAuditableEntity
{
    DateTimeOffset  CreatedOnUtc        { get; }
    string?         CreatedBy           { get; }
    DateTimeOffset? LastModifiedOnUtc   { get; }
    string?         LastModifiedBy      { get; }
}
```

⚠️ There is **no `AuditableEntity` base class** — each entity declares these four itself, and handlers
currently set `CreatedBy`/`LastModifiedBy` by hand at ~114 sites. Follow the surrounding module's
convention until that is automated.

### `IHasTenant`

```csharp
public interface IHasTenant
{
    string TenantId { get; }   // string, NOT Guid
}
```

⚠️ **Implementing this does not filter anything by itself.** Tenant filtering activates only when the
entity's EF configuration calls `builder.ToTable(...).IsMultiTenant()` (Finbuckle). Without that call the
entity leaks across tenants.

### `ISoftDeletable`

```csharp
public interface ISoftDeletable
{
    bool            IsDeleted     { get; }
    DateTimeOffset? DeletedOnUtc  { get; }
    string?         DeletedBy     { get; }
}
```

Expose a domain method `SoftDelete(string deletedBy)`; keep the three fields `private set` and never
mutate them from a handler. See the query-filter rules in the Multi-Tenancy section below — on an
`.IsMultiTenant()` entity the soft-delete filter **must** be the named form.

## Multi-Tenancy

> ⚠️ **Authoritative repo convention (read this first — the idealized samples below differ).**
> The interface names `IMustHaveTenant` / `ISoftDelete` shown later are illustrative only. The actual
> codebase uses:
>
> - **Tenant marker:** `IHasTenant` (`string TenantId { get; }`) — from `AMIS.Framework.Core.Domain`.
>   Declare `public string TenantId { get; private set; } = default!;` on the entity.
> - **Soft-delete marker:** `ISoftDeletable` (getter-only: `bool IsDeleted`, `DateTimeOffset? DeletedOnUtc`,
>   `string? DeletedBy`). Expose a domain method `SoftDelete(string deletedBy)`; keep the three fields
>   `private set`. Do **not** mutate them from handlers.
> - **Tenant filtering is NOT automatic from the interface.** It activates only when the entity's EF
>   configuration calls `builder.ToTable(...).IsMultiTenant()` (Finbuckle). Without it, the entity leaks
>   across tenants. See [#1 in the vulnerability review].
>
> ### Multi-tenant + soft-delete query filters (EF Core 10)
>
> `.IsMultiTenant()` registers a **named** query filter. EF Core 10 forbids mixing an *anonymous* filter
> with *named* ones on the same entity. Therefore on any `.IsMultiTenant()` entity the soft-delete filter
> **must** be the named form — an anonymous one throws `Both anonymous and named query filters cannot be
> applied simultaneously` at **runtime model build**, taking down the whole module.
>
> ```csharp
> // ✅ Correct — multi-tenant entity with named soft-delete filter
> builder.ToTable("DisbursementVouchers", SchemaName).IsMultiTenant();
> builder.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
> builder.HasIndex(x => x.TenantId);
> builder.Property(x => x.IsDeleted).HasDefaultValue(false);
> builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted);   // NAMED — required alongside .IsMultiTenant()
>
> // ❌ Wrong — anonymous filter collides with the Finbuckle tenant filter → runtime crash
> builder.HasQueryFilter(x => !x.IsDeleted);
> ```
>
> Reference-data entities that are **not** `.IsMultiTenant()` (e.g. MasterData `Category`, `Office`) may
> keep the anonymous `HasQueryFilter(x => !x.IsDeleted)` form.

### Tenant-Aware Entities

```csharp
public class Order : BaseEntity, IAuditable, IMustHaveTenant
{
    public Guid TenantId { get; set; }  // ✅ Required for tenant isolation
    public string OrderNumber { get; private set; } = default!;
    public decimal Total { get; private set; }
    
    // ...
}
```

### Global Query Filter (Automatic)

BaseDbContext automatically applies:
```csharp
modelBuilder.Entity<Order>()
    .HasQueryFilter(e => e.TenantId == currentTenantId);
```

**Result:** All queries automatically filter by current tenant. No need to add `.Where(x => x.TenantId == ...)` everywhere.

### Shared Entities (No Tenant)

```csharp
public class Country : BaseEntity  // ❌ No IMustHaveTenant
{
    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
}
```

## Migrations

### Creating Migrations

```bash
# From solution root
dotnet ef migrations add InitialCatalog \
    --project src/Host/Migrations.PostgreSQL \
    --context CatalogDbContext \
    --output-dir Migrations/Catalog
```

### Applying Migrations

```bash
# Automatic on startup (AMIS.Api)
# Or manually:
dotnet ef database update \
    --project src/Host/Migrations.PostgreSQL \
    --context CatalogDbContext
```

### Migration Project Pattern

AMIS uses a separate migrations project (`Migrations.PostgreSQL`) to:
- Keep migrations out of module code
- Support multiple database providers
- Simplify deployment

## Transactions

### Implicit Transactions

A single `SaveChangesAsync` is one transaction — stage every change, then save once:
```csharp
public async ValueTask<Guid> Handle(CreateOrderCommand cmd, CancellationToken ct)
{
    var order = Order.Create(...);
    dbContext.Orders.Add(order);

    var payment = Payment.Create(...);
    dbContext.Payments.Add(payment);

    // ✅ Both rows written in one transaction by the single save
    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    return order.Id;
}
```

### Explicit Transactions

Only needed when you must span **multiple** `SaveChangesAsync` calls (e.g. an allocated document number
must commit before dependent rows):

```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

try
{
    dbContext.Orders.Add(order);
    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

    dbContext.Payments.Add(payment);
    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

    await transaction.CommitAsync(ct).ConfigureAwait(false);
}
catch
{
    await transaction.RollbackAsync(ct).ConfigureAwait(false);
    throw;
}
```

## Performance Patterns

### Projection (DTO Mapping)

```csharp
// ❌ Bad: Load full entity, map in memory
var products = await dbContext.Products.Where(p => p.IsActive).ToListAsync(ct);
return products.Select(p => new ProductDto(...)).ToList();

// ✅ Good: Project in database
var query = dbContext.Products
    .Where(p => !p.IsDeleted)
    .Select(p => new ProductDto(p.Id, p.Name, p.Price.Amount));
return await query.ToListAsync(ct);
```

### AsNoTracking for Read-Only

```csharp
public class ProductsReadOnlySpec : Specification<Product>
{
    public ProductsReadOnlySpec()
    {
        Query
            .AsNoTracking()  // ✅ Faster for queries
            .Where(p => !p.IsDeleted);
    }
}
```

### Batch Operations

```csharp
// ✅ Good: Batch delete
await dbContext.Products
    .Where(p => p.CategoryId == categoryId)
    .ExecuteDeleteAsync(ct);

// ✅ Good: Batch update
await dbContext.Products
    .Where(p => p.CategoryId == categoryId)
    .ExecuteUpdateAsync(p => p.SetProperty(x => x.IsActive, false), ct);
```

## Common Pitfalls

### ❌ Tracking Issues

```csharp
// ❌ Don't detach or set EntityState by hand
dbContext.Entry(product).State = EntityState.Detached;

// ✅ A tracked entity you mutate is persisted by SaveChangesAsync — no Update() call needed
var product = await dbContext.Products.FirstAsync(p => p.Id == id, ct).ConfigureAwait(false);
product.Discontinue();                                    // domain method mutates state
await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
```

⚠️ Corollary: adding `AsNoTracking()` to a query whose results are later mutated will silently stop
persisting those changes. That is why `AsNoTracking` is added per-handler after checking the write path,
and never flipped on globally.

### ❌ N+1 Queries

```csharp
// ❌ Bad: N+1
var orders = await dbContext.Orders.ToListAsync(ct);
foreach (var order in orders)
{
    var customer = await dbContext.Customers.FindAsync(order.CustomerId, ct);  // N queries!
}

// ✅ Good: batch-load then look up in memory (the prevailing pattern in this codebase)
var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
var customers = await dbContext.Customers
    .Where(c => customerIds.Contains(c.Id))
    .ToDictionaryAsync(c => c.Id, ct)
    .ConfigureAwait(false);
```

### ❌ Lazy Loading

```csharp
// ❌ Lazy loading is DISABLED in AMIS
var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
var customer = order.Customer;  // ❌ NULL! Not loaded

// ✅ Include explicitly
var order = await dbContext.Orders
    .Include(o => o.Customer)
    .FirstOrDefaultAsync(o => o.Id == id, ct)
    .ConfigureAwait(false);
var customer = order.Customer;  // ✅ Loaded
```

## Key Rules

1. **One DbContext per module**, separate schemas
2. **Fluent API for configuration**, not data annotations
3. **Inject the module `DbContext` directly** — there is no `IRepository<T>` in this codebase
4. **Specifications for query shapes reused across handlers**; inline `IQueryable` for one-offs
5. **Tenant isolation requires `.IsMultiTenant()` in the EF configuration** — the `IHasTenant`
   interface alone filters nothing
6. **Migrations in separate project** (Migrations.PostgreSQL)
7. **`AsNoTracking()` on every read-only query** — currently missing from ~99 query handlers
8. **Project to DTOs in the database** (`.Select(...)` before `ToListAsync`), never load full entities
   and map in memory
9. **`.ConfigureAwait(false)` on every await** in handlers

---

For migration help: Use `migration-helper` agent or see EF Core docs.

