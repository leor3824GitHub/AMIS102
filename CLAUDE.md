# FSH .NET Starter Kit — AI Assistant Guide

> Modular Monolith · CQRS · DDD · Multi-Tenant · .NET 10

## Quick Start

```powershell
dotnet build src/FSH.Framework.slnx              # Build (0 warnings required)
dotnet test src/FSH.Framework.slnx               # Run tests
dotnet run --project src/Playground/FSH.Playground.AppHost  # Run with Aspire
```

## Project Layout

```
src/
├── BuildingBlocks/     # Framework (11 packages) — ⚠️ Protected
├── Modules/            # Business features — Add code here
│   ├── Identity/           # Auth, users, roles, permissions
│   ├── Multitenancy/       # Tenant management (Finbuckle)
│   ├── Auditing/           # Audit logging
│   ├── MasterData/         # Reference data (units, categories, etc.)
│   ├── Expendable/         # Expendable supplies management
│   ├── AssetManagement/    # Fixed asset tracking
│   ├── AssetProcurement/   # Asset procurement workflow
│   ├── Vehicle/            # Fleet/vehicle management
│   ├── Finance/            # Disbursement vouchers, budget utilization
│   ├── ProcurementPlanning/    # Annual procurement planning
│   └── ProcurementAcquisition/ # Purchase requests, orders, canvass
├── Playground/         # Reference application
│   ├── FSH.Playground.AppHost/  # .NET Aspire orchestration
│   ├── Playground.Api/          # API host
│   ├── Playground.Blazor/       # Blazor UI client
│   └── Migrations.PostgreSQL/   # EF Core migrations
└── Tests/              # Architecture + unit tests
```

## The Pattern

Every feature = vertical slice:

```
Modules/{Module}/Features/v1/{Feature}/
├── {Action}{Entity}Command.cs      # ICommand<T>
├── {Action}{Entity}Handler.cs      # ICommandHandler<T,R>
├── {Action}{Entity}Validator.cs    # AbstractValidator<T>
└── {Action}{Entity}Endpoint.cs     # MapPost/Get/Put/Delete
```

## Critical Rules

| ⚠️ Rule                             | Why                                     |
| ----------------------------------- | --------------------------------------- |
| Use **Mediator** not MediatR        | Different library, different interfaces |
| `ICommand<T>` / `IQuery<T>`         | NOT `IRequest<T>`                       |
| `ValueTask<T>` return type          | NOT `Task<T>`                           |
| Every command needs validator       | FluentValidation, no exceptions         |
| `.RequirePermission()` on endpoints | Explicit authorization                  |
| Zero build warnings                 | CI blocks merges                        |

## Available Skills

Call skills with `/skill-name` in your prompt.

| Skill             | Purpose                                                           |
| ----------------- | ----------------------------------------------------------------- |
| `/add-feature`    | Create complete CQRS feature (command/handler/validator/endpoint) |
| `/add-entity`     | Add domain entity with base class inheritance                     |
| `/add-module`     | Scaffold new bounded context module                               |
| `/query-patterns` | Implement paginated/filtered queries                              |
| `/testing-guide`  | Write architecture + unit tests                                   |
| `/error-handling` | Exception types, HTTP status mappings, throw vs. handle patterns  |

## Available Agents

Delegate complex tasks to specialized agents.

| Agent                | Expertise                                                |
| -------------------- | -------------------------------------------------------- |
| `code-reviewer`      | Review changes against FSH patterns + architecture rules |
| `feature-scaffolder` | Generate complete feature slices from requirements       |
| `module-creator`     | Create new modules with contracts, persistence, DI setup |
| `architecture-guard` | Verify layering, dependencies, module boundaries         |
| `migration-helper`   | Generate and apply EF Core migrations                    |

## Example: Create Feature

```csharp
// Command
public sealed record CreateProductCommand(string Name, decimal Price)
    : ICommand<Guid>;

// Handler
public sealed class CreateProductHandler(IRepository<Product> repo)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var product = Product.Create(cmd.Name, cmd.Price);
        await repo.AddAsync(product, ct);
        return product.Id;
    }
}

// Validator
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

// Endpoint
public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
    endpoints.MapPost("/", async (CreateProductCommand cmd, IMediator mediator, CancellationToken ct) =>
        TypedResults.Created($"/api/v1/products/{await mediator.Send(cmd, ct)}"))
    .WithName(nameof(CreateProductCommand))
    .WithSummary("Create a new product")
    .RequirePermission({Module}Permissions.Products.Create); // e.g. MasterDataPermissions.Products.Create
```

## Architecture

- **Pattern:** Modular Monolith (not microservices)
- **CQRS:** Mediator library (commands/queries)
- **DDD:** Rich domain models, aggregates, value objects
- **Multi-Tenancy:** Finbuckle.MultiTenant (shared DB, tenant isolation)
- **Modules:** 3 core (Identity, Multitenancy, Auditing) + 8 business modules (MasterData, Expendable, AssetManagement, AssetProcurement, Vehicle, Finance, ProcurementPlanning, ProcurementAcquisition)
- **BuildingBlocks:** 11 packages (Core, Persistence, Caching, Jobs, Web, etc.)

Details: See `.claude/rules/architecture.md`

## Before Committing

```powershell
dotnet build src/FSH.Framework.slnx  # Must pass with 0 warnings
dotnet test src/FSH.Framework.slnx   # All tests must pass
```

## Documentation

- **Rules:** See `.claude/rules/*.md` (API conventions, testing, modules)
- **Skills:** See `.claude/skills/*/SKILL.md` (step-by-step guides)
- **Agents:** See `.claude/agents/*.md` (specialized assistants)

---

**Philosophy:** This is a production-ready starter kit. Every pattern is battle-tested. Follow the conventions, and you'll ship faster.

---
