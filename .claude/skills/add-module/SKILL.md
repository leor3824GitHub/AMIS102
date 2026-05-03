---
name: add-module
description: Create a new module (bounded context) using this repo's actual module structure, host wiring, permissions, contracts, and DbContext patterns.
argument-hint: "[ModuleName]"
---

# Add Module

Create a new bounded context with the same structure and host integration used by the existing repo modules.

## When To Create A New Module

- Has its own domain entities or aggregates
- Represents a distinct bounded context
- Needs its own contracts surface or persistence boundary

If it is just another use case inside an existing domain, use `add-feature` instead.

This repo is a modular monolith. Modules do not deploy independently; they load into the shared host through the module pipeline.

## Use These As The Baseline

- `src/Modules/MasterData/Modules.MasterData`
- `src/Modules/Expendable/Modules.Expendable`
- `src/Playground/Playground.Api/Program.cs`
- `src/Playground/Playground.Api/Playground.Api.csproj`

## Project Structure

```text
src/Modules/{Name}/
├── Modules.{Name}.Contracts/
│   ├── Modules.{Name}.Contracts.csproj
│   ├── {Name}ContractsMarker.cs
│   └── v1/{Area}/
│       └── DTOs, contracts, commands, queries, events
└── Modules.{Name}/
        ├── Modules.{Name}.csproj
        ├── {Name}Module.cs
        ├── {Name}ModuleConstants.cs
        ├── Data/
        │   ├── {Name}DbContext.cs
        │   ├── {Name}DbContextFactory.cs
        │   ├── {Name}DbInitializer.cs
        │   └── Configurations/
        ├── Domain/
        │   └── entities or subdomain folders
        ├── Features/v1/{Area}/{UseCase}/
        └── Provisioning/            # optional
```

## Step 1: Create Projects

### Main Module Project

`src/Modules/{Name}/Modules.{Name}/Modules.{Name}.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>FSH.Modules.{Name}</RootNamespace>
        <AssemblyName>FSH.Modules.{Name}</AssemblyName>
        <PackageId>FullStackHero.Modules.{Name}</PackageId>
        <NoWarn>$(NoWarn);CA1031;CA1812;CA2208;S3267;S3928;CA1062;CA1304;CA1308;CA1311;CA1862;CA2227</NoWarn>
    </PropertyGroup>
    <ItemGroup>
        <FrameworkReference Include="Microsoft.AspNetCore.App" />
    </ItemGroup>
    <ItemGroup>
        <PackageReference Include="Mediator.Abstractions" />
        <PackageReference Include="FluentValidation" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\..\..\BuildingBlocks\Caching\Caching.csproj" />
        <ProjectReference Include="..\..\..\BuildingBlocks\Persistence\Persistence.csproj" />
        <ProjectReference Include="..\..\..\BuildingBlocks\Web\Web.csproj" />
        <ProjectReference Include="..\Modules.{Name}.Contracts\Modules.{Name}.Contracts.csproj" />
        <!-- Add only when required by the domain -->
        <!-- <ProjectReference Include="..\..\..\BuildingBlocks\Eventing\Eventing.csproj" /> -->
        <!-- <ProjectReference Include="..\..\Identity\Modules.Identity.Contracts\Modules.Identity.Contracts.csproj" /> -->
    </ItemGroup>
</Project>
```

### Contracts Project

`src/Modules/{Name}/Modules.{Name}.Contracts/Modules.{Name}.Contracts.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <RootNamespace>FSH.Modules.{Name}.Contracts</RootNamespace>
        <AssemblyName>FSH.Modules.{Name}.Contracts</AssemblyName>
        <PackageId>FullStackHero.Modules.{Name}.Contracts</PackageId>
        <NoWarn>$(NoWarn);CA1002;CA1056;CS1572;S2094</NoWarn>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Mediator.Abstractions" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\..\..\BuildingBlocks\Eventing.Abstractions\Eventing.Abstractions.csproj" />
        <ProjectReference Include="..\..\..\BuildingBlocks\Shared\Shared.csproj" />
    </ItemGroup>
</Project>
```

## Step 2: Add Contracts Marker

```csharp
namespace FSH.Modules.{Name}.Contracts;

public sealed class {Name}ContractsMarker;
```

## Step 3: Implement IModule

```csharp
using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;

public class {Name}Module : IModule
{
        private static readonly IReadOnlyList<FshPermission> RegisteredPermissions =
        [
                new("View {Entities}", "View", "{Name}.{Entity}", IsBasic: true),
                new("Create {Entities}", "Create", "{Name}.{Entity}"),
                new("Update {Entities}", "Update", "{Name}.{Entity}"),
                new("Delete {Entities}", "Delete", "{Name}.{Entity}")
        ];

        public void ConfigureServices(IHostApplicationBuilder builder)
        {
                ArgumentNullException.ThrowIfNull(builder);

                PermissionConstants.Register(RegisteredPermissions);
                builder.Services.AddHeroDbContext<{Name}DbContext>();
                builder.Services.AddScoped<IDbInitializer, {Name}DbInitializer>();

                // Optional only when the module needs startup provisioning.
                // builder.Services.AddHostedService<{Name}DbInitializerHostedService>();
        }

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
                ArgumentNullException.ThrowIfNull(endpoints);

                var apiVersionSet = endpoints.NewApiVersionSet()
                        .HasApiVersion(new ApiVersion(1))
                        .ReportApiVersions()
                        .Build();

                var moduleGroup = endpoints
                        .MapGroup("api/v{version:apiVersion}/{route-segment}")
                        .WithTags("{Name}")
                        .WithApiVersionSet(apiVersionSet);

                var resourceGroup = moduleGroup.MapGroup("/{resource-route}");

                // Map feature endpoints here.
        }
}
```

Do not use the old `Add{Module}Module()` and `Map{Module}Endpoints()` extension pattern as the primary integration mechanism.

## Step 4: Add Module Constants

```csharp
public static class {Name}ModuleConstants
{
        public const string SchemaName = "{schema}";

        public static class Permissions
        {
                public static class {Entities}
                {
                        public const string View = "Permissions.{Name}.{Entities}.View";
                        public const string Create = "Permissions.{Name}.{Entities}.Create";
                        public const string Update = "Permissions.{Name}.{Entities}.Update";
                        public const string Delete = "Permissions.{Name}.{Entities}.Delete";
                }
        }
}
```

Add `MigrationsTable`, top-level permission constants, or `Features` only if the module actually needs them.

## Step 5: Create DbContext

```csharp
public class {Name}DbContext : BaseDbContext
{
        public DbSet<{Entity}> {Entities} => Set<{Entity}>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
                base.OnModelCreating(modelBuilder);
                modelBuilder.ApplyConfigurationsFromAssembly(typeof({Name}DbContext).Assembly);
        }
}
```

Use `BaseDbContext` and `AddHeroDbContext<T>()`, not raw `DbContext` plus `AddDbContext<T>()`.

## Step 6: Create DbContext Factory

Add `{Name}DbContextFactory` under `Data/` for design-time EF operations.

## Step 7: Create DbInitializer

```csharp
internal sealed class {Name}DbInitializer : IDbInitializer
{
        public Task MigrateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

The interface in this repo uses `MigrateAsync` and `SeedAsync`.

## Step 8: Create Domain Entities

Put entities under `Domain/`.

Typical shape (see `add-entity` skill for the full template):

```csharp
public sealed class {Entity} : AggregateRoot<Guid>, IHasTenant, IAuditableEntity, ISoftDeletable
{
    public string Name { get; private set; } = default!;

    // IHasTenant
    public string TenantId { get; private set; } = null!;

    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }

    // ISoftDeletable
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private {Entity}() { }

    public static {Entity} Create(string name, string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new {Entity} { Id = Guid.NewGuid(), Name = name, TenantId = tenantId };
    }
}
```

## Step 9: Add Entity Configurations

Add `IEntityTypeConfiguration<T>` classes under `Data/Configurations/`.

## Step 10: Add Features

Use the repo's vertical-slice structure:

```text
Features/v1/{Area}/{UseCase}/
├── {UseCase}Command.cs or {UseCase}Query.cs
├── {UseCase}CommandHandler.cs or {UseCase}QueryHandler.cs
├── {UseCase}CommandValidator.cs when applicable
└── {UseCase}Endpoint.cs
```

## Step 11: Register In Program.cs

```csharp
builder.Services.AddMediator(o =>
{
        o.ServiceLifetime = ServiceLifetime.Scoped;
        o.Assemblies = [
                // existing entries,
                typeof({Name}Module),
                typeof({Name}ContractsMarker),
                typeof({RepresentativeFeatureType})
        ];
});

var moduleAssemblies = new Assembly[]
{
        typeof(IdentityModule).Assembly,
        typeof(MultitenancyModule).Assembly,
        typeof(AuditingModule).Assembly,
        typeof({Name}Module).Assembly,
};
```

Keep using the host pipeline that already exists:

- `builder.AddHeroPlatform(...)`
- `builder.AddModules(moduleAssemblies)`
- `app.UseHeroMultiTenantDatabases()`
- `app.UseHeroPlatform(p => p.MapModules = true)`

## Step 12: Add To Solution

```bash
dotnet sln src/FSH.Framework.slnx add src/Modules/{Name}/Modules.{Name}/Modules.{Name}.csproj
dotnet sln src/FSH.Framework.slnx add src/Modules/{Name}/Modules.{Name}.Contracts/Modules.{Name}.Contracts.csproj
```

## Step 13: Reference From API

In `src/Playground/Playground.Api/Playground.Api.csproj`:

```xml
<ProjectReference Include="..\..\Modules\{Name}\Modules.{Name}.Contracts\Modules.{Name}.Contracts.csproj" />
<ProjectReference Include="..\..\Modules\{Name}\Modules.{Name}\Modules.{Name}.csproj" />
```

## Step 14: Add Migration

New modules need an initial migration for their DbContext:

```powershell
dotnet ef migrations add "Initial_{Name}Schema" `
  --project src/Playground/Migrations.PostgreSQL `
  --startup-project src/Playground/Playground.Api `
  --context {Name}DbContext

dotnet ef database update `
  --project src/Playground/Migrations.PostgreSQL `
  --startup-project src/Playground/Playground.Api `
  --context {Name}DbContext
```

See the `migration-helper` agent for the full context name list.

## Step 15: Verify

```bash
dotnet build src/FSH.Framework.slnx
dotnet test src/FSH.Framework.slnx
```

## Checklist

- [ ] Both projects created
- [ ] Contracts marker created
- [ ] `IModule` implemented
- [ ] `ModuleConstants` added
- [ ] `BaseDbContext` used
- [ ] `DbContextFactory` created
- [ ] `IDbInitializer` implemented with `MigrateAsync` and `SeedAsync`
- [ ] Domain entities added under `Domain/`
- [ ] Entity configuration classes added under `Data/Configurations/`
- [ ] Features added under `Features/v1/...`
- [ ] Mediator assembly list updated in `Program.cs`
- [ ] `moduleAssemblies` updated in `Program.cs`
- [ ] API project references added
- [ ] Solution updated
- [ ] Initial migration created and applied
- [ ] Build passes with zero warnings

## Practical Guidance

- Use MasterData as the lean baseline.
- Use Expendable as the richer example with subdomains and optional provisioning.
- Prefer actual repo modules over older generic examples if they disagree.
