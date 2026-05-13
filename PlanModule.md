﻿# Multi-Module Library Architecture Plan

## Overview

**Architecture Pattern:** Three-module hub-and-spoke with Module 3 (Library) as the master data source.

- **Module 3 (Library)**: Source of truth for Employee, Office, Product, Supplier
- **Module 1 (PPE & SEMI)**: Read-only consumer of Library data
- **Module 2 (Expendable)**: Read-only consumer of Library data
- **One-way dependency**: Mods 1→3 and Mods 2→3 (no circular dependencies)

---

## Step-by-Step Implementation

### 1. Create Module 3: Library (Master Data Hub)

**Projects:**
- `Modules.Library.Contracts` — Public API (safe for anyone to use)
- `Modules.Library` — Implementation (internal, no one touches)

**Location:** `src/Modules/Library/`

---

### 2. Domain Entities in Module 3 (Owned by Library)

**File:** `src/Modules/Library/Domain/Employee.cs`
```csharp
public class Employee : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string? Phone { get; private set; }
    public string Department { get; private set; }
    public Guid? OfficeId { get; private set; }
    public EmployeeStatus Status { get; private set; }
    
    // Auditing fields (auto-set by interceptor)
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    
    // Multi-tenancy
    public string TenantId { get; private set; }
    
    public static Employee Create(string name, string email, string department, Guid? officeId = null)
    {
        return new Employee
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Department = department,
            OfficeId = officeId,
            Status = EmployeeStatus.Active
        };
    }
}

public enum EmployeeStatus
{
    Active,
    Inactive,
    OnLeave
}
```

**File:** `src/Modules/Library/Domain/Office.cs`
```csharp
public class Office : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string Name { get; private set; }
    public string Location { get; private set; }
    public int Capacity { get; private set; }
    public Guid? ManagerId { get; private set; } // FK to Employee
    
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    public string TenantId { get; private set; }
    
    public static Office Create(string name, string location, int capacity)
    {
        return new Office
        {
            Id = Guid.NewGuid(),
            Name = name,
            Location = location,
            Capacity = capacity
        };
    }
}
```

**File:** `src/Modules/Library/Domain/Product.cs`
```csharp
public class Product : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Category { get; private set; }
    public decimal UnitPrice { get; private set; }
    public Guid SupplierId { get; private set; } // FK to Supplier
    
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    public string TenantId { get; private set; }
    
    public static Product Create(string code, string name, string category, decimal unitPrice, Guid supplierId)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Category = category,
            UnitPrice = unitPrice,
            SupplierId = supplierId
        };
    }
}
```

**File:** `src/Modules/Library/Domain/Supplier.cs`
```csharp
public class Supplier : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string Name { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? Phone { get; private set; }
    public string? Terms { get; private set; }
    
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    public string TenantId { get; private set; }
    
    public static Supplier Create(string name, string? contactPerson = null, string? contactEmail = null)
    {
        return new Supplier
        {
            Id = Guid.NewGuid(),
            Name = name,
            ContactPerson = contactPerson,
            ContactEmail = contactEmail
        };
    }
}
```

---

### 3. CRUD Features in Module 3 (Full Lifecycle)

**Path:** `src/Modules/Library/Features/v1/`

Each entity type (Employees, Offices, Products, Suppliers) has its own vertical slice:

#### Employees
- **Commands:**
  - `CreateEmployeeCommand` → `CreateEmployeeCommandHandler` → `CreateEmployeeValidator` → `CreateEmployeeEndpoint`
  - `UpdateEmployeeCommand` → `UpdateEmployeeCommandHandler` → `UpdateEmployeeValidator` → `UpdateEmployeeEndpoint`
  - `DeleteEmployeeCommand` → `DeleteEmployeeCommandHandler` → DeleteEmployeeEndpoint
  - `AssignToOfficeCommand` → `AssignToOfficeCommandHandler` → `AssignToOfficeValidator` → `AssignToOfficeEndpoint`
- **Queries:**
  - `GetEmployeeQuery` → `GetEmployeeQueryHandler` → `GetEmployeeEndpoint`
  - `SearchEmployeesQuery` → `SearchEmployeesQueryHandler` → `SearchEmployeesEndpoint`
- **Endpoints:** POST/PUT/DELETE/GET `/employees`

#### Offices
- **Commands:** `CreateOfficeCommand`, `UpdateOfficeCommand`, `DeleteOfficeCommand`
- **Queries:** `GetOfficeQuery`, `SearchOfficesQuery`
- **Endpoints:** POST/PUT/DELETE/GET `/offices`

#### Products
- **Commands:** `CreateProductCommand`, `UpdateProductCommand`, `DeleteProductCommand`
- **Queries:** `GetProductQuery`, `SearchProductsQuery`
- **Endpoints:** POST/PUT/DELETE/GET `/products`

#### Suppliers
- **Commands:** `CreateSupplierCommand`, `UpdateSupplierCommand`, `DeleteSupplierCommand`
- **Queries:** `GetSupplierQuery`, `SearchSuppliersQuery`
- **Endpoints:** POST/PUT/DELETE/GET `/suppliers`

---

### 4. Contracts Project: Data Transfer Objects (Public API)

**Path:** `src/Modules/Library/Modules.Library.Contracts/v1/`

#### Employees
**File:** `Modules.Library.Contracts/v1/Employees/EmployeeDto.cs`
```csharp
public record EmployeeDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string Department,
    Guid? OfficeId,
    string Status,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy
);
```

**File:** `Modules.Library.Contracts/v1/Employees/CreateEmployeeCommand.cs`
```csharp
public class CreateEmployeeCommand : ICommand<CreateEmployeeResponse>
{
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public string Department { get; set; } = default!;
    public Guid? OfficeId { get; set; }
}

public record CreateEmployeeResponse(Guid EmployeeId);
```

**File:** `Modules.Library.Contracts/v1/Employees/GetEmployeeQuery.cs`
```csharp
public sealed record GetEmployeeQuery(Guid Id) : IQuery<EmployeeDto?>;
```

**File:** `Modules.Library.Contracts/v1/Employees/SearchEmployeesQuery.cs`
```csharp
public sealed record SearchEmployeesQuery(
    string? Search,
    string? Department,
    int PageNumber = 1,
    int PageSize = 10
) : IPagedQuery, IQuery<PagedResponse<EmployeeDto>>;
```

#### Offices, Products, Suppliers
- Follow same pattern: `OfficeDto`, `ProductDto`, `SupplierDto`
- Commands: `CreateOfficeCommand`, `UpdateOfficeCommand`, etc.
- Queries: `GetOfficeQuery`, `SearchOfficesQuery`, etc.

---

### 5. Database (Single LibraryDbContext)

**File:** `src/Modules/Library/Data/LibraryDbContext.cs`
```csharp
public class LibraryDbContext : 
    MultiTenantIdentityDbContext<AMISUser, AMISRole>
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    
    public LibraryDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<LibraryDbContext> options)
        : base(multiTenantContextAccessor, options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
```

**File:** `src/Modules/Library/Data/Configurations/EmployeeConfiguration.cs`
```csharp
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder
            .ToTable("Employees", LibraryModuleConstants.SchemaName)
            .IsMultiTenant()
            .IsSoftDelete();
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Department).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Phone).HasMaxLength(20);
        
        // Foreign key
        builder.HasOne<Office>()
            .WithMany()
            .HasForeignKey(e => e.OfficeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Indexes
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.Department);
        builder.HasIndex(e => e.TenantId);
    }
}
```

**File:** `src/Modules/Library/Data/Configurations/OfficeConfiguration.cs`
```csharp
public class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder
            .ToTable("Offices", LibraryModuleConstants.SchemaName)
            .IsMultiTenant()
            .IsSoftDelete();
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.Name).IsRequired().HasMaxLength(256);
        builder.Property(o => o.Location).IsRequired().HasMaxLength(256);
        builder.Property(o => o.Capacity).IsRequired();
        
        // Foreign key to Employee (Manager)
        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(o => o.ManagerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasIndex(o => o.TenantId);
    }
}
```

**File:** `src/Modules/Library/Data/Configurations/ProductConfiguration.cs`
```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder
            .ToTable("Products", LibraryModuleConstants.SchemaName)
            .IsMultiTenant()
            .IsSoftDelete();
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(256);
        builder.Property(p => p.Category).IsRequired().HasMaxLength(100);
        builder.Property(p => p.UnitPrice).HasPrecision(10, 2);
        
        // Foreign key
        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.Category);
    }
}
```

**File:** `src/Modules/Library/Data/Configurations/SupplierConfiguration.cs`
```csharp
public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder
            .ToTable("Suppliers", LibraryModuleConstants.SchemaName)
            .IsMultiTenant()
            .IsSoftDelete();
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Name).IsRequired().HasMaxLength(256);
        builder.Property(s => s.ContactPerson).HasMaxLength(256);
        builder.Property(s => s.ContactEmail).HasMaxLength(256);
        builder.Property(s => s.Phone).HasMaxLength(20);
        
        builder.HasIndex(s => s.Name).IsUnique();
    }
}
```

---

### 6. Module 1: PPE & SEMI (Depends on Library)

**Projects:**
- `Modules.PPE.Contracts` — Its own public API
- `Modules.PPE` — Implementation

**Location:** `src/Modules/PPE/`

#### Features Example: Issue PPE

**File:** `src/Modules/PPE/Modules.PPE.Contracts/v1/Issues/IssuePPECommand.cs`
```csharp
public class IssuePPECommand : ICommand<IssuePPEResponse>
{
    public Guid EmployeeId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public record IssuePPEResponse(Guid IssueId, string Message);
```

**File:** `src/Modules/PPE/Features/v1/Issues/IssuePPECommandHandler.cs`
```csharp
public sealed class IssuePPECommandHandler : ICommandHandler<IssuePPECommand, IssuePPEResponse>
{
    private readonly IMediator _mediator;
    private readonly IRepository<PPEIssue> _issueRepo;
    private readonly ICurrentUser _currentUser;
    
    public async ValueTask<IssuePPEResponse> Handle(
        IssuePPECommand cmd,
        CancellationToken ct)
    {
        // Step 1: Verify Employee exists in Library (read-only query)
        var employee = await _mediator.Send(
            new GetEmployeeQuery(cmd.EmployeeId),
            ct);
        
        if (employee == null)
            throw new NotFoundException($"Employee {cmd.EmployeeId} not found");
        
        // Step 2: Verify Product exists in Library (read-only query)
        var product = await _mediator.Send(
            new GetProductQuery(cmd.ProductId),
            ct);
        
        if (product == null)
            throw new NotFoundException($"Product {cmd.ProductId} not found");
        
        // Step 3: Create PPE issue in PPE module's context
        var ppeIssue = PPEIssue.Create(
            employeeId: cmd.EmployeeId,    // Store ID only
            productId: cmd.ProductId,      // Store ID only
            quantity: cmd.Quantity,
            notes: cmd.Notes,
            issuedBy: _currentUser.UserId);
        
        await _issueRepo.AddAsync(ppeIssue, ct);
        
        return new IssuePPEResponse(ppeIssue.Id, "PPE issued successfully");
    }
}
```

**File:** `src/Modules/PPE/Domain/PPEIssue.cs`
```csharp
public class PPEIssue : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public Guid EmployeeId { get; private set; }         // Reference to Library
    public Guid ProductId { get; private set; }          // Reference to Library
    public int Quantity { get; private set; }
    public string? Notes { get; private set; }
    public string IssuedBy { get; private set; }
    public DateTime IssuedOnUtc { get; private set; }
    
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }
    public string TenantId { get; private set; }
    
    public static PPEIssue Create(
        Guid employeeId,
        Guid productId,
        int quantity,
        string? notes,
        string issuedBy)
    {
        return new PPEIssue
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            ProductId = productId,
            Quantity = quantity,
            Notes = notes,
            IssuedBy = issuedBy,
            IssuedOnUtc = DateTime.UtcNow
        };
    }
}
```

**File:** `src/Modules/PPE/Data/PPEDbContext.cs`
```csharp
public class PPEDbContext : 
    MultiTenantIdentityDbContext<AMISUser, AMISRole>
{
    public DbSet<PPEIssue> PPEIssues => Set<PPEIssue>();
    
    public PPEDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<PPEDbContext> options)
        : base(multiTenantContextAccessor, options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PPEDbContext).Assembly);
    }
}
```

**Dependencies in PPEModule:**
```csharp
// In PPEModule.ConfigureServices()
// Add reference to Library Contracts ONLY
using Modules.Library.Contracts.v1.Employees;
using Modules.Library.Contracts.v1.Products;

// Don't reference internal implementations:
// ❌ using Modules.Library;
// ❌ using Modules.Library.Domain;
```

---

### 7. Module 2: Expendable (Depends on Library)

**Module 2: Expendable** is a specialized employee-based point-of-sale (POS) system for inventory management and supply request workflows. 

**Location:** `src/Modules/Expendable/`

**See:** [Module2-Expendable-POS.md](Module2-Expendable-POS.md) for detailed implementation guide covering:
- Entity design (SupplyRequest, EmployeeInventory)
- Supply request workflow (Request → Approve → Allocate → Deliver)
- Employee inventory tracking
- API endpoints and permissions
- Database schema and configurations

**Dependencies in ExpenableModule:**
```csharp
using Modules.Library.Contracts.v1.Employees;
using Modules.Library.Contracts.v1.Offices;
using Modules.Library.Contracts.v1.Products;

// No internal Library references allowed
```

---

### 8. Dependency Graph (Enforced by Architecture)

```
┌──────────────────────────────────────────────┐
│ Module 1: PPE & SEMI                         │
│ ├─ Features: IssuePPE, etc.                 │
│ ├─ Depends on: Library.Contracts            │
│ └─ DbContext: PPEDbContext                  │
└──────────────────────────────────────────────┘
                    ↓ (queries via Mediator)
┌──────────────────────────────────────────────┐
│ Module 3: Library (Hub)                      │
│ ├─ Entities: Employee, Office, etc.         │
│ ├─ Features: CRUD for all entities          │
│ ├─ Contracts: EmployeeDto, ProductDto, etc. │
│ └─ DbContext: LibraryDbContext              │
└──────────────────────────────────────────────┘
                    ↑ (queries via Mediator)
┌──────────────────────────────────────────────┐
│ Module 2: Expendable                         │
│ ├─ Features: RequestSupply, Allocate, etc.  │
│ ├─ Depends on: Library.Contracts            │
│ └─ DbContext: ExpenableDbContext            │
└──────────────────────────────────────────────┘

** CRITICAL: Library NEVER depends on 1 or 2 **
** No circular dependencies allowed **
```

---

### 9. Module Registration (Program.cs)

**File:** `src/Playground/Playground.Api/Program.cs`

```csharp
// Register in dependency order (base → consumers)
var moduleAssemblies = new Assembly[]
{
    typeof(LibraryModule).Assembly,       // Module 3 - no dependencies
    typeof(PPEModule).Assembly,           // Module 1 - depends on 3
    typeof(ExpenableModule).Assembly,     // Module 2 - depends on 3
};

builder.AddModules(moduleAssemblies);

// Mediator automatically discovers all commands/queries/handlers
builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.Assemblies = [
        typeof(GetEmployeeQuery).Assembly,
        typeof(IssuePPECommand).Assembly,
        typeof(RequestSupplyCommand).Assembly,
    ];
});
```

---

### 10. API Routes (Separate Versioning)

- **Module 3 (Library):** `api/v1/library/employees`, `api/v1/library/products`, `api/v1/library/offices`, `api/v1/library/suppliers`
- **Module 1 (PPE):** `api/v1/ppe/issue`, `api/v1/ppe/records`
- **Module 2 (Expendable):** `api/v1/expendable/request`, `api/v1/expendable/allocate`

---

### 11. Permissions (Independent per Module)

#### Library Module
```csharp
public static class LibraryPermissionConstants
{
    public static class Employees
    {
        public const string View = "Permissions.Library.Employees.View";
        public const string Create = "Permissions.Library.Employees.Create";
        public const string Update = "Permissions.Library.Employees.Update";
        public const string Delete = "Permissions.Library.Employees.Delete";
    }
    
    public static class Products
    {
        public const string View = "Permissions.Library.Products.View";
        public const string Create = "Permissions.Library.Products.Create";
        public const string Update = "Permissions.Library.Products.Update";
        public const string Delete = "Permissions.Library.Products.Delete";
    }
    
    // Similarly for Offices, Suppliers
}
```

#### PPE Module
```csharp
public static class PPEPermissionConstants
{
    public static class Issues
    {
        public const string View = "Permissions.PPE.Issues.View";
        public const string Create = "Permissions.PPE.Issues.Create";
        public const string Approve = "Permissions.PPE.Issues.Approve";
    }
}
```

#### Expendable Module
See [Module2-Expendable-POS.md](Module2-Expendable-POS.md#permissions) for detailed permission structure.

**Quick Reference:**
```csharp
public static class ExpenablePermissionConstants
{
    public static class Requests
    {
        public const string View = "Permissions.Expendable.Requests.View";
        public const string Create = "Permissions.Expendable.Requests.Create";
        public const string Approve = "Permissions.Expendable.Requests.Approve";
    }
}
```

**Applied in Endpoints:**
```csharp
endpoints.MapPost("/issue", handler)
    .RequirePermission(PPEPermissionConstants.Issues.Create);

endpoints.MapPost("/request", handler)
    .RequirePermission(ExpenablePermissionConstants.Requests.Create);
```

---

### 12. Data Integrity & Validation

#### At Source (Module 3 - Library)
- All CRUD operations validate Employee/Product/Office/Supplier before save
- Constraints enforced in EF Core configuration (unique emails, foreign keys, etc.)
- Domain models use factory methods and validation

#### At Consumer (Modules 1 & 2)
- Load entity from Library via query before using
- Check for null/not found before creating related data
- Example:
  ```csharp
  var employee = await _mediator.Send(new GetEmployeeQuery(cmd.EmployeeId), ct);
  if (employee == null) throw new NotFoundException("Employee not found");
  ```

#### Soft Delete & Orphaning
- If Employee deleted in Library (soft-delete only: IsDeleted = true):
  - PPE issue records retain EmployeeId (can query history)
  - Business logic: decide if orphaned records are valid or need cascade logic

---

### 13. Testing Strategy

#### Module 3 (Library) Tests
**Path:** `src/Tests/Library.Tests/`

```csharp
// Unit tests for entity creation and domain logic
[Fact]
public void Employee_Create_ValidInput_ReturnsEmployee()
{
    var emp = Employee.Create("John Doe", "john@example.com", "Engineering");
    
    emp.Name.Should().Be("John Doe");
    emp.Email.Should().Be("john@example.com");
    emp.Status.Should().Be(EmployeeStatus.Active);
}

// Handler tests with mocked repository
[Fact]
public async Task CreateEmployeeCommandHandler_ValidCommand_ReturnsEmployeeId()
{
    var cmd = new CreateEmployeeCommand 
    { 
        Name = "Jane Doe", 
        Email = "jane@example.com",
        Department = "HR"
    };
    
    var handler = new CreateEmployeeCommandHandler(_employeeRepository);
    var result = await handler.Handle(cmd, CancellationToken.None);
    
    result.EmployeeId.Should().NotBe(Guid.Empty);
}
```

#### Module 1 (PPE) Tests
**Path:** `src/Tests/PPE.Tests/`

```csharp
// Unit test: handler calls Library query correctly
[Fact]
public async Task IssuePPECommandHandler_EmployeeNotFound_ThrowsNotFoundException()
{
    var cmd = new IssuePPECommand 
    { 
        EmployeeId = Guid.NewGuid(), 
        ProductId = Guid.NewGuid(),
        Quantity = 1
    };
    
    // Mock: GetEmployeeQuery returns null
    var mediatorMock = new Mock<IMediator>();
    mediatorMock
        .Setup(m => m.Send(It.IsAny<GetEmployeeQuery>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((EmployeeDto?)null);
    
    var handler = new IssuePPECommandHandler(mediatorMock.Object, _repo, _currentUser);
    
    await handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
        .Should()
        .ThrowAsync<NotFoundException>();
}

// Integration test: create Employee in Library → Issue PPE in Module 1
[Fact]
public async Task IssuePPE_WithValidEmployeeAndProduct_CreatesIssue()
{
    // Create employee in Library
    var empResult = await _mediator.Send(new CreateEmployeeCommand 
    { 
        Name = "John", 
        Email = "john@test.com",
        Department = "IT"
    });
    
    // Create product in Library
    var prodResult = await _mediator.Send(new CreateProductCommand 
    { 
        Code = "PPE001", 
        Name = "Safety Helmet",
        Category = "PPE",
        UnitPrice = 50m,
        SupplierId = _supplierId
    });
    
    // Issue PPE in Module 1
    var issueResult = await _mediator.Send(new IssuePPECommand
    {
        EmployeeId = empResult.EmployeeId,
        ProductId = prodResult.ProductId,
        Quantity = 2
    });
    
    issueResult.IssueId.Should().NotBe(Guid.Empty);
}
```

#### Module 2 (Expendable) Tests
Similar pattern to Module 1.

#### Architecture Tests
**Path:** `src/Tests/Architecture.Tests/`

```csharp
[Fact]
public void Modules_ShouldNotHaveCircularDependencies()
{
    var ppe = typeof(PPEModule).Assembly;
    var expendable = typeof(ExpenableModule).Assembly;
    var library = typeof(LibraryModule).Assembly;
    
    // PPE should not reference Expendable or Library internals
    ppe.GetReferencedAssemblies().Should()
        .NotContain(a => a.Name == "Modules.Expendable");
    
    ppe.GetTypes()
        .SelectMany(t => t.GetReferencedTypes())
        .Where(t => t.Namespace?.StartsWith("Modules.Library") == true)
        .Should()
        .AllSatisfy(t => t.Namespace?.StartsWith("Modules.Library.Contracts") == true);
    
    // Library should not depend on PPE or Expendable
    library.GetReferencedAssemblies().Should()
        .NotContain(a => a.Name.StartsWith("Modules.PPE") || a.Name.StartsWith("Modules.Expendable"));
}

[Fact]
public void Modules_Handlers_Should_Be_Sealed()
{
    var types = typeof(PPEModule).Assembly
        .GetTypes()
        .Where(t => typeof(ICommandHandler).IsAssignableFrom(t));
    
    types.Should()
        .AllSatisfy(t => t.Should().BeSealed());
}
```

---

### 14. Verification Checklist

- [ ] Build passes: `dotnet build src/AMIS.Framework.slnx` (0 warnings)
- [ ] All tests pass: `dotnet test src/AMIS.Framework.slnx`
- [ ] Module 1 cannot reference Library internals (compile error if tries)
- [ ] Module 2 cannot reference Library internals (compile error if tries)
- [ ] No circular dependencies detected by architecture tests
- [ ] End-to-end flow: Create Employee in Library → Issue PPE in Module 1 ✓
- [ ] End-to-end flow: Create Product in Library → Request Supply in Module 2 ✓
- [ ] Permissions applied per endpoint
- [ ] DTOs returned from queries (no domain entities leaked)

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Single Library Module** | Centralized master data, single source of truth |
| **Contracts-only visibility** | Modules 1 & 2 cannot access Library internals → stability |
| **Separate DbContexts** | PPE, Expendable, Library each own their schema → clear boundaries |
| **Mediator for cross-module queries** | Decoupled communication, easy to mock in tests |
| **Read-only references** | Modules 1 & 2 load Library data but cannot modify → integrity |
| **Soft delete in Library** | Employees/Products can be marked inactive, not destroyed |
| **Multi-tenancy via interceptor** | Auto-filters by TenantId across all modules |
| **Auditing via interceptor** | Auto-tracks CreatedBy, CreatedOnUtc across all modules |
| **Immutable Contracts** | Shared DTOs documented, stable for external consumers |

---

## Summary

This architecture ensures:

✅ **Clear ownership** — Library owns master data, modules 1 & 2 consume  
✅ **No circular dependencies** — Library never depends on PPE or Expendable  
✅ **Easy testing** — Each module testable in isolation or integrated  
✅ **Data integrity** — Constraints + domain logic prevent orphaned data  
✅ **Scale** — Easy to add Module 4, 5, etc. that also depend on Library  
✅ **Maintainability** — Change Library without breaking consumer modules  
✅ **Multi-tenancy** — Built-in tenant isolation across all modules  
✅ **Auditing** — Auto-tracked across all data changes  

---

## File Structure Overview

```
src/Modules/
├── Library/
│   ├── Modules.Library.Contracts/
│   │   └── v1/
│   │       ├── Employees/ (EmployeeDto, CreateEmployeeCommand, GetEmployeeQuery, etc.)
│   │       ├── Offices/ (OfficeDto, CreateOfficeCommand, GetOfficeQuery, etc.)
│   │       ├── Products/ (ProductDto, CreateProductCommand, GetProductQuery, etc.)
│   │       └── Suppliers/ (SupplierDto, CreateSupplierCommand, GetSupplierQuery, etc.)
│   ├── Modules.Library/
│   │   ├── Domain/ (Employee.cs, Office.cs, Product.cs, Supplier.cs)
│   │   ├── Features/v1/
│   │   │   ├── Employees/ (Handlers, Validators, Endpoints)
│   │   │   ├── Offices/ (Handlers, Validators, Endpoints)
│   │   │   ├── Products/ (Handlers, Validators, Endpoints)
│   │   │   └── Suppliers/ (Handlers, Validators, Endpoints)
│   │   ├── Data/ (LibraryDbContext, Configurations)
│   │   └── LibraryModule.cs
│   └── Migrations.Library/
│
├── PPE/
│   ├── Modules.PPE.Contracts/
│   │   └── v1/ (IssuePPECommand, PPEIssueDto, etc.)
│   ├── Modules.PPE/
│   │   ├── Domain/ (PPEIssue.cs, etc.)
│   │   ├── Features/v1/Issues/ (Handlers, Validators, Endpoints)
│   │   ├── Data/ (PPEDbContext, Configurations)
│   │   └── PPEModule.cs
│   └── Migrations.PPE/
│
└── Expendable/
    ├── Modules.Expendable.Contracts/
    │   └── v1/ (RequestSupplyCommand, SupplyRequestDto, etc.)
    ├── Modules.Expendable/
    │   ├── Domain/ (SupplyRequest.cs, etc.)
    │   ├── Features/v1/Requests/ (Handlers, Validators, Endpoints)
    │   ├── Data/ (ExpenableDbContext, Configurations)
    │   └── ExpenableModule.cs
    └── Migrations.Expendable/

src/Tests/
├── Architecture.Tests/ (module isolation, no circular deps)
├── Library.Tests/ (Employee, Office, Product, Supplier CRUD tests)
├── PPE.Tests/ (IssuePPE integration and unit tests)
└── Expendable.Tests/ (RequestSupply integration and unit tests)
```

---

**Next Steps:**
1. Create the three module projects (Library, PPE, Expendable)
2. Define domain entities in Module 3 (Library)
3. Implement CRUD features in Library
4. Expose Contracts in Library.Contracts
5. Implement features in Modules 1 & 2 that consume Library.Contracts
6. Write tests for each module
7. Run architecture tests to verify no circular dependencies
8. Deploy with zero build warnings

