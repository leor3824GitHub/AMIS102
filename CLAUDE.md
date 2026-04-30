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

## Work Done — Branch `April82026` (ProcurementPlanning Refactor)

### Status

- **Phase 1 — Type & invariant cleanup: COMPLETE** (build 0 errors, 507 tests pass)
- **Phase 2 — Phase model & transitions: IN PROGRESS** (contracts done; domain/handlers/endpoints/UI pending — see TODO below)

### The 5-phase plan

| Phase | Scope | Files | Schema | Status |
|---|---|---|---|---|
| 1 | Type & invariant cleanup | ~12 | none | ✅ DONE |
| 2 | Phase model & transitions | ~25 | enum reshape | 🚧 IN PROGRESS |
| 3 | Type sharpening (DateOnly, value objects) & SSoT cleanup | ~25 | yes | pending |
| 4 | `ProcurementProject` master + item lineage | ~40 | yes | pending |
| 5 | Consolidator workflow + manual Updated-APP creation | ~12 | none | pending |

Single regenerated EF migration after Phase 4 — pre-production, no data-migration scripts.

### Decisions locked in

- **Curation policy** (Phase 4): hybrid + department-scoped. `ProcurementProject` carries `OwnerOfficeCode` matching `Ppmp.OfficeCode`; anyone in the same office may create projects; picker scoped per office. Cross-office "promote to shared" is a future concern.
- **Updated APP trigger** (Phase 5): manual. Consolidator invokes `CreateUpdateAppCommand(fiscalYear, ppmpIds[])` → system clones current APP, merges selected Updated PPMPs into a new draft.
- **Per-phase version reset**: `VersionNumber` resets to 1 on `PromoteToFinal`. The Updated phase has its own monotonic counter (Updated v1, v2, v3…).
- **PDF snapshot**: out of scope.

### Phase 1 — DONE

User-id type normalization, `MarkChanged()` helper, `Supersede()` guards, `Update()` no longer accepts phase, `Version` private set.

Files changed:
- `PpmpContracts.cs` — `PpmpDto.AmendedById: string? → Guid?`; dropped `PpmpType` from `UpdatePpmpCommand`
- `AppContracts.cs` — DTO `AmendedById/ConsolidatedById/ApprovedById: string? → Guid?`
- `Ppmp.cs` — `AmendedById: Guid?`; `MarkChanged()` consolidates `LastModifiedOnUtc + Version` bumps; guarded `Supersede()`; `Update()` no longer accepts `PpmpType`; `Version` setter is `private`; `CreateAmendment(string, Guid)`
- `AnnualProcurementPlan.cs` — `AmendedById/ConsolidatedById/ApprovedById: Guid?`; `MarkChanged()` (also fixed bug: APP previously never bumped `Version`); `NewVersion()` added; guarded `Supersede()`; `Approve(Guid)`, `ConsolidatePpmps(..., Guid)`, `CreateAmendment(..., Guid)`
- `PpmpConfiguration.cs`, `AnnualProcurementPlanConfiguration.cs` — dropped `HasMaxLength` on now-Guid columns
- 5 handlers + 1 Blazor page (`PpmpPage.razor`) updated for new types

### Phase 2 — IN PROGRESS

#### Done so far

**`PpmpContracts.cs`** — fully rewritten:
- New enum `PpmpPhase { Indicative=0, Final=1, Updated=2 }` (replaces `PpmpType`)
- DTO field `PpmpType` → `Phase` (on `PpmpDto`, `PpmpSummaryDto`)
- `CreatePpmpCommand.PpmpType` → `Phase`
- `SearchPpmpsQuery.PpmpType` → `Phase`
- Dropped `AmendPpmpCommand`
- Added `PromoteToFinalPpmpCommand(Guid Id)` and `CreateUpdatePpmpCommand(Guid Id, string UpdateReason)`
- `GetAvailablePpmpsForAppQuery` now takes optional `Guid? AppId = null` (linter addition — keep)

**`AppContracts.cs`** — fully rewritten:
- New enum `AppPhase { Indicative=0, Final=1, Updated=2 }`
- DTO `RevisionType` → `Phase` (on `AnnualProcurementPlanDto`, `AnnualProcurementPlanSummaryDto`)
- `CreateAnnualProcurementPlanCommand.RevisionType` → `Phase`
- `SearchAnnualProcurementPlansQuery.RevisionType` → `Phase`
- Dropped `AmendAnnualProcurementPlanCommand`
- Added `PromoteToFinalAppCommand(Guid Id)` and `CreateUpdateAppCommand(Guid Id, string UpdateReason)`
- Added `DeleteAnnualProcurementPlanCommand(Guid Id) : ICommand<Unit>` (linter addition — keep)
- ⚠ Linter kept the legacy `AppRevisionType` enum alongside new `AppPhase`. Plan is to **remove `AppRevisionType` once all references are gone** (Phase 2 step 12 below).

#### TODO — to resume Phase 2

1. **Refactor `Ppmp.cs`**:
   - Rename property `PpmpType` → `Phase` (type `PpmpPhase`)
   - `Create(...)` parameter `PpmpType ppmpType` → `PpmpPhase phase`
   - Add `PromoteToFinal(Guid promotedById)` — guards: `Phase=Indicative && Status=Approved`. Returns new Ppmp row, same chain, `Phase=Final`, `Status=Draft`, `VersionNumber=1`, items cloned. Caller calls `Supersede()` on previous.
   - Add `CreateUpdate(string reason, Guid amendedById)` — guards: `Phase ∈ {Final,Updated} && Status ∈ {Approved,Consolidated}`. Returns new row, `Phase=Updated`, items cloned. If previous was Final → new VersionNumber=1. If previous was Updated → new VersionNumber=prev+1.
   - Drop `CreateAmendment`

2. **Refactor `AnnualProcurementPlan.cs`**:
   - Rename property `RevisionType` → `Phase` (type `AppPhase`)
   - `Create(...)` parameter `AppRevisionType` → `AppPhase`
   - Add `PromoteToFinal(Guid promotedById)` — guards: `Phase=Indicative && Status=Approved`. **Final APP starts empty** (does NOT clone Indicative items) and must be re-consolidated from Final-phase PPMPs. Per-phase version reset.
   - Add `CreateUpdate(string reason, Guid amendedById)` — guards: `Phase ∈ {Final,Updated} && Status=Approved`. Updated APP **does** clone items (it iterates on the prior Final/Updated APP).
   - Add **phase-matched consolidation guard** in `ConsolidatePpmps`: whitelist
     - Indicative APP ↔ Indicative PPMPs only
     - Final APP ↔ Final PPMPs only
     - Updated APP ↔ Final or Updated PPMPs
   - Drop `CreateAmendment`

3. **PPMP slices to ADD** under `Features/v1/Ppmps/`:
   - `PromoteToFinalPpmp/` (Handler, Endpoint, Validator — Id only)
   - `CreateUpdatePpmp/` (Handler, Endpoint, Validator — Id, UpdateReason ≤ 1000 chars)

4. **PPMP slice to DELETE**: `Features/v1/Ppmps/AmendPpmp/`

5. **APP slices to ADD** under `Features/v1/AnnualProcurementPlans/`:
   - `PromoteToFinalApp/` (Handler, Endpoint, Validator)
   - `CreateUpdateApp/` (Handler, Endpoint, Validator)

6. **APP slice to DELETE**: `Features/v1/AnnualProcurementPlans/AmendAnnualProcurementPlan/`

7. **Update existing handlers**:
   - `CreatePpmpCommandHandler.cs` — `command.PpmpType` → `command.Phase`
   - `CreateAnnualProcurementPlanCommandHandler.cs` — `command.RevisionType` → `command.Phase`
   - `SearchPpmpsQueryHandler.cs` — `query.PpmpType` → `query.Phase`; `x.PpmpType` → `x.Phase`
   - `SearchAnnualProcurementPlansQueryHandler.cs` — same renames

8. **Update mappers**:
   - `PpmpMapper.cs` — `ppmp.PpmpType` → `ppmp.Phase`
   - `AppMapper.cs` — `app.RevisionType` → `app.Phase`

9. **Update `ProcurementPlanningModule.cs`**:
   - Remove usings/registrations for `AmendPpmpEndpoint`, `AmendAnnualProcurementPlanEndpoint`
   - Add usings/registrations for `PromoteToFinalPpmpEndpoint`, `CreateUpdatePpmpEndpoint`, `PromoteToFinalAppEndpoint`, `CreateUpdateAppEndpoint`

10. **Update `ProcurementPlanningModuleConstants.cs`**:
    - Add permission constants for `PromoteToFinal` and `CreateUpdate` on both Ppmps and AnnualProcurementPlans (or repurpose existing `Amend` permission). Endpoints `RequirePermission(...)` accordingly.
    - Update `RegisteredPermissions` list in `ProcurementPlanningModule.cs`.

11. **Update Blazor consumers**:
    - `ProcurementPlanningClient.cs` — `PpmpType?` parameter → `PpmpPhase?` in `PpmpClient.SearchAsync`; `AppRevisionType?` → `AppPhase?` in `AppClient.SearchAsync`. Replace `AmendAsync` with `PromoteToFinalAsync` + `CreateUpdateAsync` for both clients.
    - `PpmpPage.razor` — `_form.PpmpType` → `_form.Phase`; replace amend UI with promote-to-final + create-update buttons (visibility depends on row's `Phase` and `Status`).
    - `AppPage.razor` — `_form.RevisionType` → `_form.Phase`; same UI replacement.

12. **Drop legacy `AppRevisionType` enum** from `AppContracts.cs` once all references are gone.

13. **Build + test**: `dotnet build src/FSH.Framework.slnx` (zero new warnings) and `dotnet test src/FSH.Framework.slnx` (all pass).

#### Workflow being implemented (one office's PPMP chain)

```
Indicative v1 (Approved)──PromoteToFinal──▶ Final v1 (Draft → Approved)
                                                  │
                                                  ├─CreateUpdate("budget revision")──▶ Updated v1 (Draft → Approved)
                                                  │                                          │
                                                  │                                          └─CreateUpdate("urgent item")──▶ Updated v2 ...
                                                  │
                                                  └─ Supersede on each transition (IsCurrentVersion=false)
```

All rows share one `VersionChainId`. Only the latest draft has `IsCurrentVersion=true`; previous rows are marked Superseded but their content (items, ApprovedAt, Phase) is preserved as the filed audit copy.

### Merge conflicts resolved (April 2026 — historical)

- `AppHost.cs` — kept 223 suffix for volume/database names
- `appsettings.json` and `appsettings.Development.json` — kept `AMIS224` database name
- `ProcurementPlanningClient.cs` — kept `EnsureApiSuccessAsync`; removed stale `TryGetApiMessage`
- Migration `20260428001944` — deleted; `20260428161849` is the canonical initial migration
- AssetManagement migration `20260428002013` — created with full 24-table Up()/Down()

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
