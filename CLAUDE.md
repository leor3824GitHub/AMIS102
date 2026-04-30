# FSH .NET Starter Kit — AI Assistant Guide

> Modular Monolith · CQRS · DDD · Multi-Tenant · .NET 10

## Quick Start

```bash
dotnet build src/FSH.Framework.slnx              # Build (0 warnings required)
dotnet test src/FSH.Framework.slnx               # Run tests
dotnet run --project src/Playground/FSH.Playground.AppHost  # Run with Aspire
```

## Project Layout

```
src/
├── BuildingBlocks/     # Framework (11 packages) — ⚠️ Protected
├── Modules/            # Business features — Add code here
│   ├── Identity/       # Auth, users, roles, permissions
│   ├── Multitenancy/   # Tenant management (Finbuckle)
│   └── Auditing/       # Audit logging
├── Playground/         # Reference application
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

| ⚠️ Rule | Why |
|---------|-----|
| Use **Mediator** not MediatR | Different library, different interfaces |
| `ICommand<T>` / `IQuery<T>` | NOT `IRequest<T>` |
| `ValueTask<T>` return type | NOT `Task<T>` |
| Every command needs validator | FluentValidation, no exceptions |
| `.RequirePermission()` on endpoints | Explicit authorization |
| Zero build warnings | CI blocks merges |

## Available Skills

Call skills with `/skill-name` in your prompt.

| Skill | Purpose |
|-------|---------|
| `/add-feature` | Create complete CQRS feature (command/handler/validator/endpoint) |
| `/add-entity` | Add domain entity with base class inheritance |
| `/add-module` | Scaffold new bounded context module |
| `/query-patterns` | Implement paginated/filtered queries |
| `/testing-guide` | Write architecture + unit tests |

## Available Agents

Delegate complex tasks to specialized agents.

| Agent | Expertise |
|-------|----------|
| `code-reviewer` | Review changes against FSH patterns + architecture rules |
| `feature-scaffolder` | Generate complete feature slices from requirements |
| `module-creator` | Create new modules with contracts, persistence, DI setup |
| `architecture-guard` | Verify layering, dependencies, module boundaries |
| `migration-helper` | Generate and apply EF Core migrations |

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
    .RequirePermission(CatalogPermissions.Products.Create);
```

## Architecture

- **Pattern:** Modular Monolith (not microservices)
- **CQRS:** Mediator library (commands/queries)
- **DDD:** Rich domain models, aggregates, value objects
- **Multi-Tenancy:** Finbuckle.MultiTenant (shared DB, tenant isolation)
- **Modules:** 3 core (Identity, Multitenancy, Auditing) + your features
- **BuildingBlocks:** 11 packages (Core, Persistence, Caching, Jobs, Web, etc.)

Details: See `.claude/rules/architecture.md`

## Before Committing

```bash
dotnet build src/FSH.Framework.slnx  # Must pass with 0 warnings
dotnet test src/FSH.Framework.slnx   # All tests must pass
```

## Documentation

- **Architecture:** See `ARCHITECTURE_ANALYSIS.md` (19KB deep-dive)
- **Rules:** See `.claude/rules/*.md` (API conventions, testing, modules)
- **Skills:** See `.claude/skills/*/SKILL.md` (step-by-step guides)
- **Agents:** See `.claude/agents/*.md` (specialized assistants)

---

**Philosophy:** This is a production-ready starter kit. Every pattern is battle-tested. Follow the conventions, and you'll ship faster.

---

## Work Done — Branch `April82026`

### ProcurementPlanning Module (complete)

Full PPMP + APP workflow implemented and tested.

**Domain concepts (do not confuse):**
- `PpmpPhase` — Indicative / Final / Updated (was incorrectly called `PpmpType` in older code)
- `AppPhase` — Indicative / Final / Updated
- `AppRevisionType` — Original / Supplemental / Revised (separate concept, not the same as phase)
- `AppLineReference` — live FK from APP → PPMP items (mutable)
- `AppSnapshot` — immutable point-in-time copy created at Publish and Approve
- `CreateUpdate()` — domain method for amending a Final/Updated APP; **forbidden on Indicative APPs** (those must use `PromoteToFinal()` instead)
- `PpmpItemData` — domain-internal item type; `PpmpItemRequest` is the contract/API type

**Merge conflicts resolved (April 2026):**
- `AppHost.cs` — kept 223 suffix for volume/database names
- `appsettings.json` and `appsettings.Development.json` — kept `AMIS223` database name
- `ProcurementPlanningClient.cs` — kept `EnsureApiSuccessAsync`; removed stale `TryGetApiMessage`
- Migration `20260428001944` — deleted (empty .cs + conflicted designer); `20260428161849` is the canonical initial migration
- AssetManagement migration `20260428002013` — .cs file was missing; created with full 24-table Up()/Down()

**Key renames applied throughout contracts, client, and Blazor pages:**
- `PpmpType` → `Phase` (on `PpmpDto` and `PpmpSummaryDto`)
- `RevisionType` → `Phase` (on `AnnualProcurementPlanDto` and summary)
- `AmendPpmpCommand` → `CreateUpdatePpmpCommand` at `/create-update` endpoint
- `AppMapper.ToDto()` → `AppReadProjection.BuildDtoAsync()` (mapper class doesn't exist)

**Handler/projection fixes:**
- `PromoteToFinalAppCommandHandler` — removed `.Include(x => x.Items)` (entity uses `LineReferences`); use `AppReadProjection.BuildDtoAsync` for return value
- `GetAppVersionsQueryHandler` — `x.RevisionType` → `x.Phase`

**Domain behaviour fixes:**
- `PromoteToFinalPpmpCommandHandler` and `PromoteToFinalAppCommandHandler` — removed `original.Supersede()` call; the Indicative document must remain Approved as the filed approval copy after the Final is created
- `AnnualProcurementPlan.PromoteToFinal()` — no longer clones items; Final APP starts empty and must be re-consolidated from Final-phase PPMPs
- `AnnualProcurementPlan.ConsolidatePpmps()` — added phase alignment guard: throws if any supplied PPMP's `Phase` does not match `(PpmpPhase)(int)this.Phase`

**Unit test fixes (`AnnualProcurementPlanWorkflowTests`):**
- `CreateApprovedPpmp` helper → `PpmpPhase.Final` (was Indicative)
- `CreateDraftAppFrom` helper → `AppPhase.Final` (was Indicative)
- Reason: `ConsolidatePpmps` requires PPMP phase to match APP phase (`(PpmpPhase)(int)Phase`), and `CreateUpdate` throws on Indicative APPs. Tests exercising the amendment workflow must use Final-phase entities.
- Handler class is `CreateUpdateAppCommandHandler` (namespace `AmendAnnualProcurementPlan`)

---

### AssetManagement Module — Multi-Tenancy (complete)

All 24 domain entities updated to implement `IHasTenant` (carries `TenantId` property) for Finbuckle data isolation.

**Two-layer fix required for tenant isolation:**
1. Domain entity must implement `IHasTenant` — done for all 24 entities
2. EF configuration must call `.IsMultiTenant()` on the `ToTable()` chain — activates Finbuckle's automatic `WHERE TenantId = @tenant` query filter

**EF configuration fixes (`using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;` required):**
- Added `.IsMultiTenant()` to 11 item-entity configurations that were missing it:
  `ICSItems`, `PARItems`, `PPEIRItems`, `PhysicalCountEntries`, `PropertyCodeCounters`, `PropertyIncidentItems`, `RRPItems`, `RRSPItems`, `SMIRItems`, `TangibleInventoryItems`, `UnserviceablePropertyItems`
- The 13 parent-document configurations already had `.IsMultiTenant()`

**`.IgnoreQueryFilters()` removal — document-number uniqueness (11 command handlers):**
- Document numbers (ICSNo, PARNo, RRSPNo, etc.) are unique **per tenant**, not globally — each agency runs its own numbering series
- Removed `.IgnoreQueryFilters()` from all uniqueness `AnyAsync` checks in: `CreateICSCommandHandler`, `RenewICSCommandHandler`, `CreatePARCommandHandler`, `CreatePPEIRCommandHandler`, `CreateRRSPCommandHandler`, `CreateRRPCommandHandler`, `CreateSMIRCommandHandler`, `CreatePropertyIncidentReportCommandHandler`, `CreateUnserviceablePropertyReportCommandHandler`, `CreatePhysicalCountSessionCommandHandler`, `CreateTangibleInventoryCommandHandler`

**`.IgnoreQueryFilters()` removal — report/query handlers (15+ handlers):**
- After item entities gained `IHasTenant` + `.IsMultiTenant()`, any `.IgnoreQueryFilters()` on joined tables bypasses the new tenant filter — all removed
- Affected handlers: `GetSPCQueryHandler`, `GetPropertyHistoryQueryHandler`, `GetRRSPByIdQueryHandler`, `GetRRSPListQueryHandler`, `GetRSPIQueryHandler`, `GetRegSPIQueryHandler`, `GetPARByIdQueryHandler`, `GetSMIRByIdQueryHandler`, `GetRPCPPEQueryHandler`, `GetPropertyIncidentReportByIdQueryHandler`, `GetPTRQueryHandler`, `GetRSPIQueryHandler`, `GetRegSPIQueryHandler`, `GetUnserviceablePropertyReportByIdQueryHandler`, `RegisterTangibleItemCommandHandler`, `CreateSemiExpendableItemCommandHandler`, `UpdateSemiExpendableItemCommandHandler`
- **Exception — `ICSExpiryJob.cs` retains `.IgnoreQueryFilters()`**: Hangfire background job runs without tenant context and intentionally processes expired ICS records across all tenants

**Migration:**
- `20260430035559_AddTenantIdToItemEntities` — adds `TenantId` column + tenant-scoped indices to 10 item tables (`PropertyCodeCounters` already had the column from the initial migration; only its index is new)

---

**Final state:** build 0 errors, all 513 tests pass (Architecture 47, Generic 73, Multitenancy 93, Vehicle 8, Auditing 60, Expendable 12, Identity 220).
