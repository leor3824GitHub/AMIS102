---
paths:
  - "src/Modules/**"
---

# Module Rules

Modules are bounded contexts in the modular monolith. They are independently developed within a single deployment unit and are loaded by the host through the module pipeline.

## Source Of Truth

When in doubt, follow the repo's working modules instead of older generic examples:

- `src/Modules/MasterData/Modules.MasterData`
- `src/Modules/Expendable/Modules.Expendable`
- `src/Modules/MasterData/Modules.MasterData.Contracts`
- `src/Modules/Expendable/Modules.Expendable.Contracts`

Host wiring lives here:

- `src/Playground/Playground.Api/Program.cs`
- `src/Playground/Playground.Api/Playground.Api.csproj`

## Module Structure

Actual repo baseline:

```
Modules/{ModuleName}/
├── Modules.{ModuleName}.Contracts/
│   ├── Modules.{ModuleName}.Contracts.csproj
│   ├── {ModuleName}ContractsMarker.cs
│   └── v1/
│       └── {Area}/
│           └── Contracts, DTOs, queries, commands, events
└── Modules.{ModuleName}/
    ├── Modules.{ModuleName}.csproj
    ├── {ModuleName}Module.cs
    ├── {ModuleName}ModuleConstants.cs
    ├── Data/
    │   ├── {ModuleName}DbContext.cs
    │   ├── {ModuleName}DbContextFactory.cs
    │   ├── {ModuleName}DbInitializer.cs
    │   └── Configurations/
    ├── Domain/
    │   └── entities or subdomain folders
    ├── Features/
    │   └── v1/{Area}/{UseCase}/
    │       ├── {UseCase}Command.cs or {UseCase}Query.cs
    │       ├── {UseCase}CommandHandler.cs or {UseCase}QueryHandler.cs
    │       ├── {UseCase}CommandValidator.cs when applicable
    │       └── {UseCase}Endpoint.cs
    └── Provisioning/
        └── optional hosted services
```

Notes:

- Use `Domain/`, not `Entities/`.
- Use `Data/`, not `Persistence/`.
- **Exception:** The `Auditing` module is a legacy core framework module that uses `Core/`, `Infrastructure/`, and `Persistence/` instead of `Data/`. Do not copy this structure — it is intentionally left as-is for backward compatibility.
- Use `{ModuleName}Module.cs`, not `Extensions.cs`, for DI and endpoint mapping.
- Use `{ModuleName}ModuleConstants.cs` for schema and permission constants.
- `Provisioning/` is optional. Expendable uses it; MasterData does not.

## Module Independence

### Allowed

```csharp
// Reference another module's contracts only
using FSH.Modules.Identity.Contracts;
```

```csharp
// Use BuildingBlocks
using FSH.Framework.Persistence;
using FSH.Framework.Web.Modules;
```

### Forbidden

```csharp
// Direct reference to another module's implementation assembly internals
using FSH.Modules.Identity;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Features;
using FSH.Modules.Identity.Domain;
```

Rule:

- Other modules may depend on `Modules.{Other}.Contracts`
- Other modules must not depend on `Modules.{Other}` internals

## Communication Between Modules

### Option 1: Contracts

Keep public DTOs, public requests, and integration contracts in the `.Contracts` project under `v1/...`.

### Option 2: Events

When the module publishes integration events, use the contracts project plus `BuildingBlocks/Eventing.Abstractions` and only add `BuildingBlocks/Eventing` to the implementation project when required.

## Core Implementation Rules

### 1. Module Bootstrap

Each module implements `IModule`:

```csharp
public class CatalogModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        PermissionConstants.Register(RegisteredPermissions);
        builder.Services.AddHeroDbContext<CatalogDbContext>();
        builder.Services.AddScoped<IDbInitializer, CatalogDbInitializer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/catalog")
            .WithTags("Catalog")
            .WithApiVersionSet(apiVersionSet);
    }
}
```

Do not scaffold `AddCatalogModule()` or `MapCatalogEndpoints()` extension methods as the primary module integration pattern.

### 2. DbContext Pattern

Module DbContexts inherit `BaseDbContext`, not plain `DbContext`:

```csharp
public class CatalogDbContext : BaseDbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
```

Use `AddHeroDbContext<T>()` in the module. Do not register module DbContexts with raw `AddDbContext<T>()` unless there is a specific reason.

### 3. DbContext Factory

Add a design-time `DbContextFactory` under `Data/` for EF tooling and migrations.

### 4. Database Initializer

Implement `IDbInitializer` with the repo's actual interface:

```csharp
internal sealed class CatalogDbInitializer : IDbInitializer
{
    public Task MigrateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

Do not scaffold `InitializeDatabasesAsync` or `SeedDatabaseAsync`.

### 5. Domain Entities

Place domain entities under `Domain/`.

Typical pattern:

```csharp
public class Product : BaseEntity, IAuditable, IMustHaveTenant
{
    public string Name { get; private set; } = default!;
    public Guid TenantId { get; set; }
}
```

Use subfolders only when the domain is large, as in Expendable.

### 6. Configuration Classes

Add `IEntityTypeConfiguration<T>` types under `Data/Configurations/`.

### 7. Constants

Use `{ModuleName}ModuleConstants.cs` for:

- `SchemaName`
- nested `Permissions`
- optional `MigrationsTable`
- optional `Features`

Keep constants minimal. Do not copy Expendable's richer constants structure unless the module actually needs it.

## Host Wiring

The host integrates modules through the existing framework pipeline.

In `Playground.Api/Program.cs`, new modules typically require:

- representative types added to the Mediator assembly list
- `typeof({ModuleName}Module).Assembly` added to `moduleAssemblies`
- no replacement of `AddHeroPlatform`, `AddModules`, `UseHeroMultiTenantDatabases`, or `UseHeroPlatform`

Pattern:

```csharp
builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.Assemblies = [
        // existing entries,
        typeof(CatalogModule),
        typeof(CatalogContractsMarker),
        typeof(CreateCatalogItemCommand)
    ];
});

var moduleAssemblies = new Assembly[]
{
    typeof(IdentityModule).Assembly,
    typeof(MultitenancyModule).Assembly,
    typeof(AuditingModule).Assembly,
    typeof(CatalogModule).Assembly
};

builder.AddHeroPlatform(...);
builder.AddModules(moduleAssemblies);

var app = builder.Build();
app.UseHeroMultiTenantDatabases();
app.UseHeroPlatform(p => p.MapModules = true);
```

## Project References

### Implementation Project

Baseline references usually include:

- `BuildingBlocks/Caching`
- `BuildingBlocks/Persistence`
- `BuildingBlocks/Web`
- `Modules.{Name}.Contracts`

Optional references include:

- `BuildingBlocks/Eventing`
- other module `.Contracts` projects

### Contracts Project

Contracts projects usually include:

- `Mediator.Abstractions`
- `BuildingBlocks/Eventing.Abstractions`
- `BuildingBlocks/Shared`

## Vertical Slice Rules

Each use case stays self-contained under `Features/v1/...`.

Typical shape:

```
Features/v1/{Area}/{UseCase}/
├── {UseCase}Command.cs or {UseCase}Query.cs
├── {UseCase}CommandHandler.cs or {UseCase}QueryHandler.cs
├── {UseCase}CommandValidator.cs when applicable
└── {UseCase}Endpoint.cs
```

Follow the Mediator library, not MediatR.

## Creating A New Module

1. Create both projects under `src/Modules/{Name}`.
2. Add the correct project references.
3. Add `ContractsMarker`, `ModuleConstants`, `DbContext`, `DbContextFactory`, and `DbInitializer`.
4. Implement `IModule` with permissions, `AddHeroDbContext`, and endpoint grouping.
5. Add at least one vertical slice under `Features/v1/...`.
6. Add both projects to `src/FSH.Framework.slnx`.
7. Add both project references to `Playground.Api.csproj`.
8. Update `Playground.Api/Program.cs` Mediator and `moduleAssemblies` wiring.
9. Build and test the full solution.

## Verification

```bash
dotnet build src/FSH.Framework.slnx
dotnet test src/FSH.Framework.slnx
```

## Practical Baseline

- Use MasterData as the lean baseline.
- Use Expendable as the richer example with subdomains, Eventing, and optional provisioning.
- Prefer the actual repo modules over outdated generic examples when the two disagree.

## Common Patterns

### Permissions

```csharp
namespace FSH.Modules.Catalog.Permissions;

public static class CatalogPermissions
{
    public static class Products
    {
        public const string View = "catalog.products.view";
        public const string Create = "catalog.products.create";
        public const string Update = "catalog.products.update";
        public const string Delete = "catalog.products.delete";
    }
}
```

### DTOs (in Contracts)

```csharp
namespace FSH.Modules.Catalog.Contracts;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    DateTime CreatedAt);
```

### Events (in Contracts)

```csharp
namespace FSH.Modules.Catalog.Contracts;

public record ProductCreatedEvent(Guid ProductId, string Name) : DomainEvent;
public record ProductUpdatedEvent(Guid ProductId) : DomainEvent;
public record ProductDeletedEvent(Guid ProductId) : DomainEvent;
```

## Key Rules

1. **Contracts are public**, internals are `internal`
2. **Modules communicate via Contracts or events**, never direct references
3. **Each module has its own DbContext**
4. **Features are vertical slices** within modules
5. **BuildingBlocks are shared**, modules are independent

---

For scaffolding help: Use `/add-module` skill or `module-creator` agent.
