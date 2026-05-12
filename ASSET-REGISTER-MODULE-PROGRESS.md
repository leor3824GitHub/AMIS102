# AssetRegister Module — Progress Log

> Companion to [ASSET-REGISTER-MODULE-PLAN.md](ASSET-REGISTER-MODULE-PLAN.md). Tracks what has actually shipped, phase by phase.

**Module:** `AssetRegister` (new bounded context, parallel to `AssetManagement`)
**Branch:** `May32026`
**Status:** Phase 1-5 complete; Phase 6 in progress; Phase 7 started (catalog seeding).

---

## Phase 1 — Domain + EF + migration skeleton ✅ COMPLETE

**Completed:** 2026-05-12

### Acceptance criteria

| Criterion                                                                         | Result                                                        |
| --------------------------------------------------------------------------------- | ------------------------------------------------------------- |
| `dotnet build src/FSH.Framework.slnx` → 0 errors                                  | ✅ Build succeeded                                            |
| AssetRegister projects compile with 0 warnings (isolated build)                   | ✅ Confirmed                                                  |
| `dotnet test src/Tests/AssetRegister.Tests`                                       | ✅ **9/9 passed**                                             |
| `dotnet ef migrations script --context AssetRegisterDbContext` produces clean DDL | ✅ 14 tables in `asset_register.*` schema                     |
| `AssetManagement` module untouched                                                | ✅ `git diff src/Modules/AssetManagement/` shows zero changes |
| Architecture test asserts no references to other module implementations           | ✅ Only `Modules.AssetProcurement.Contracts` allowed          |
| Every aggregate carries `byte[] Version` concurrency token                        | ✅ All 6 roots                                                |
| Monetary columns explicit `HasPrecision(18, 2)`                                   | ✅ Verified                                                   |
| Aggregate roots: no public setters on domain state                                | ✅ Reflection test passes                                     |
| `PropertyNumber` value object round-trips a known COA-format sample               | ✅ Unit test passes                                           |

### What landed

#### Projects (3 new, all wired into `src/FSH.Framework.slnx`)

```
src/Modules/AssetRegister/
├── Modules.AssetRegister.Contracts/   (DTOs, enums, value objects, integration events)
└── Modules.AssetRegister/             (domain, EF, module bootstrap)
src/Tests/AssetRegister.Tests/         (architecture + value-object tests)
```

Host wiring updated:

- [Playground.Api/Playground.Api.csproj](src/Playground/Playground.Api/Playground.Api.csproj) — both module project references added
- [Playground.Api/Program.cs](src/Playground/Playground.Api/Program.cs) — `AssetRegisterModule` assembly added to `moduleAssemblies`
- [Migrations.PostgreSQL/Migrations.PostgreSQL.csproj](src/Playground/Migrations.PostgreSQL/Migrations.PostgreSQL.csproj) — implementation project referenced for design-time tooling

#### Domain — 6 aggregate roots

| Aggregate                               | Location                                                                                                         | Notes                                                                                       |
| --------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| `AssetRegistry`                         | [Domain/Assets/AssetRegistry.cs](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Assets/AssetRegistry.cs) | Master ledger. Full lifecycle FSM enforced. Cardinality 1 row = 1 physical unit.            |
| `PropertyAccountability` (+ lines)      | [Domain/Accountability/](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Accountability/)                 | Unifies ICS + PAR. SE↔ICS / PPE↔PAR form segregation (rule #1) enforced in `Issue` factory. |
| `PropertyIssuanceReport` (+ lines)      | [Domain/Issuance/](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Issuance/)                             | Unifies SMIR + PPEIR. Posted reports are immutable.                                         |
| `PhysicalCountSession` (+ entries)      | [Domain/Counting/](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Counting/)                             | Drives RPCSEMEX/RPCPPE. `FoundAtStation` entries supported.                                 |
| `PropertyIncidentReport` (+ items)      | [Domain/Incidents/](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Incidents/)                           | RLSDDSP + Notice of Loss unified. Notarization-before-resolution invariant.                 |
| `UnserviceablePropertyReport` (+ items) | [Domain/Unserviceable/](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Unserviceable/)                   | IIRUSP + IIRUP unified. Inspection-before-disposal invariant.                               |

Plus supporting aggregates: [`PropertyItemCatalog`](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Catalog/PropertyItemCatalog.cs), [`PropertyCodeCounter`](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Catalog/PropertyCodeCounter.cs).

#### Value objects (in Contracts so they cross the wire later)

| VO               | Location                                                                                                                                   | Purpose                                                                                |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------- |
| `PropertyNumber` | [Contracts/v1/ValueObjects/PropertyNumber.cs](src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/ValueObjects/PropertyNumber.cs) | COA 2020-006 format `YYYY-AA-BB-NNNN-CC`, with `Create`/`Parse`/`TryParse`             |
| `AssetSnapshot`  | [Contracts/v1/ValueObjects/AssetSnapshot.cs](src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/ValueObjects/AssetSnapshot.cs)   | Frozen subset of `AssetRegistry` at issue/count/incident/disposal time — EF owned type |
| `EmployeeRef`    | [Contracts/v1/ValueObjects/EmployeeRef.cs](src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/ValueObjects/EmployeeRef.cs)       | Signatory + printed name + designation; survives employee renames — EF owned type      |

#### Enums

19 enums in [Contracts/v1/Enums.cs](src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/Enums.cs): `AssetType`, `AssetCategory`, `AssetCondition`, `LifecycleState`, `AccountabilityType`, `AccountabilityStatus`, `AccountabilityLineStatus`, `IssuanceReportType`, `IssuanceReportStatus`, `PhysicalCountScope`, `PhysicalCountStatus`, `PhysicalCountCondition`, `PropertyIncidentType`, `PropertyIncidentStatus`, `IncidentItemResolution`, `UnserviceableReportType`, `UnserviceableReportStatus`, `DisposalMethod`.

#### Domain services (interfaces only — implementations land in Phase 3)

In [Domain/Services/NumberGenerators.cs](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Services/NumberGenerators.cs):

- `IPropertyNumberGenerator`
- `IAccountabilityNumberGenerator` (NextIcs / NextPar)
- `IInventoryTransferNumberGenerator`
- `IIncidentNumberGenerator`
- `IIssuanceReportNumberGenerator`
- `IUnserviceableReportNumberGenerator`
- `ICurrentReplacementCostCalculator`

#### Events

**15 in-process domain events** in [Domain/Events/AssetRegisterDomainEvents.cs](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Events/AssetRegisterDomainEvents.cs): `AssetRegisteredEvent`, `AssetIssuedEvent`, `AssetReturnedEvent`, `AssetTransferredEvent`, `AssetFoundAtStationEvent`, `AssetReportedMissingFromCountEvent`, `AssetLostEvent`, `AssetRecoveredEvent`, `AssetUnserviceableEvent`, `AssetDisposedEvent`, `AccountabilityCancelledEvent`, `IssuanceReportPostedEvent`, `PhysicalCountSessionClosedEvent`, `IncidentReportFiledEvent`, `UnserviceableReportSubmittedEvent`.

**6 cross-module integration event types** declared (not yet published) in [Contracts/v1/IntegrationEvents/AssetRegisterIntegrationEvents.cs](src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/IntegrationEvents/AssetRegisterIntegrationEvents.cs): `AssetRegistered`, `AssetIssued`, `AssetDisposed`, `IssuanceReportPosted`, `IncidentReportFiled`, `UnserviceableReportClosed`.

#### Persistence

- [`AssetRegisterDbContext`](src/Modules/AssetRegister/Modules.AssetRegister/Data/AssetRegisterDbContext.cs) — 8 `DbSet<>`s, inherits `BaseDbContext`
- [`AssetRegisterDbContextFactory`](src/Modules/AssetRegister/Modules.AssetRegister/Data/AssetRegisterDbContextFactory.cs) — design-time factory for EF tooling
- [`AssetRegisterDbInitializer`](src/Modules/AssetRegister/Modules.AssetRegister/Data/AssetRegisterDbInitializer.cs) — runs `Database.MigrateAsync`; `SeedAsync` is a no-op
- [`AssetRegisterDbInitializerHostedService`](src/Modules/AssetRegister/Modules.AssetRegister/Provisioning/AssetRegisterDbInitializerHostedService.cs) — invokes migrate-on-startup
- 9 entity configurations in [Data/Configurations/](src/Modules/AssetRegister/Modules.AssetRegister/Data/Configurations/):
  - `PropertyItemCatalogConfiguration`
  - `PropertyCodeCounterConfiguration`
  - `AssetRegistryConfiguration` (with `PropertyNumber` value converter)
  - `PropertyAccountabilityConfiguration` + line config
  - `PropertyIssuanceReportConfiguration` + line config
  - `PhysicalCountSessionConfiguration` + entry config
  - `PropertyIncidentReportConfiguration` + item config
  - `UnserviceablePropertyReportConfiguration` + item config
  - `OwnedTypeConfigurations` — DRY helpers for `AssetSnapshot` and `EmployeeRef` flattening

#### Initial migration

[`Migrations.PostgreSQL/AssetRegister/20260512091248_AssetRegister_Initial.cs`](src/Playground/Migrations.PostgreSQL/AssetRegister/) — generates 14 tables under `asset_register.*`:

```
AssetRegistries                          PropertyAccountabilities
PropertyAccountabilityLines              PropertyIssuanceReports
PropertyIssuanceReportLines              PhysicalCountSessions
PhysicalCountSessionConductors           PhysicalCountEntries
PropertyIncidentReports                  PropertyIncidentItems
UnserviceablePropertyReports             UnserviceablePropertyItems
PropertyItemCatalog                      PropertyCodeCounters
```

#### Module bootstrap

[`AssetRegisterModule`](src/Modules/AssetRegister/Modules.AssetRegister/AssetRegisterModule.cs) implements `IModule`:

- Registers **29 permissions** across 7 resource groups (Assets, Accountability, Issuance, Count, Incident, Unserviceable, Catalog)
- Calls `AddHeroDbContext<AssetRegisterDbContext>()`
- Registers `IDbInitializer` and hosted service
- Registers `AssetIARAcceptedEventConsumer` (Phase 1 stub that logs and ignores)
- Reserves API group `api/v1/asset-register` with tag `"Asset Register"` (no routes yet — Phase 3)

Module discovery: [AssemblyInfo.cs](src/Modules/AssetRegister/Modules.AssetRegister/AssemblyInfo.cs) carries `[FshModule(typeof(AssetRegisterModule), 760)]`.

#### Tests

[`src/Tests/AssetRegister.Tests/`](src/Tests/AssetRegister.Tests/) — 9 tests across:

- `Architecture/ModuleBoundaryTests.cs` — 3 tests:
  - No references to other module implementations
  - Only `AssetProcurement.Contracts` allowed for cross-module
  - Aggregate roots have no public setters on domain state
- `ValueObjects/PropertyNumberTests.cs` — 6 tests covering format round-trip and malformed input

### Decisions and deviations from plan

| Plan said                                                                                                           | What we did                                                             | Why                                                                                                                                                                              |
| ------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `DomainException` for invariant violations                                                                          | `InvalidOperationException`                                             | No `DomainException` class exists in BuildingBlocks — followed existing modules' convention                                                                                      |
| Add `typeof(AssetRegisterModule)` + `typeof(AssetRegisterContractsMarker)` to `Mediator.Assemblies` in `Program.cs` | Omitted in Phase 1                                                      | Mediator source generator errors when a listed assembly contains zero `ICommand`/`IQuery` types. Re-add in Phase 2 alongside first handler (e.g. `typeof(RegisterAssetCommand)`) |
| EF owned types use `snapshot_*` prefix                                                                              | Used `Snapshot_*` / `IssuedBy_*` / `ReceivedBy_*` / `PreparedBy_*` etc. | More readable column names; PostgreSQL is case-insensitive anyway when quoted consistently                                                                                       |
| `PropertyNumber` value converter only on `AssetRegistry.PropertyNo`                                                 | Same                                                                    | Snapshots carry `PropertyNo` as plain `string` (frozen text), correct per plan                                                                                                   |

### What does NOT exist after Phase 1 (per plan §"What does NOT exist after Phase 1")

- No `RegisterAssetCommand` / `IssueAccountabilityCommand` / any handler
- No Blazor pages
- No endpoints (group exists, zero routes)
- No seeding
- No actual integration-event consumption logic (handler exists, body is logging stub)
- No domain-service implementations (interfaces declared; no DI registration)
- No integration-event publication

All deferred to subsequent phases per the plan.

---

## Connection to upstream modules

Per the [audit done before Phase 1](#) (see `git log`):

- **`AssetIARAcceptedEvent`** is published today by `AssetProcurement`'s [AcceptAssetIARCommandHandler.cs:39](src/Modules/AssetProcurement/Modules.AssetProcurement/Features/v1/AssetIARs/AcceptAssetIAR/AcceptAssetIARCommandHandler.cs#L39). AssetRegister now has a registered consumer (currently a stub).
- **No other upstream wiring exists in the codebase** — ProcurementPlanning → ProcurementAcquisition → Finance share `Guid` references but no events. Those gaps are outside this module's scope.

### Known gap to address before Phase 3

The current `AssetIARAcceptedEvent` payload ([AssetIARContracts.cs:128-152](src/Modules/AssetProcurement/Modules.AssetProcurement.Contracts/v1/AssetInspectionAcceptanceReports/AssetIARContracts.cs#L128-L152)) does **not** carry:

- `AcquisitionDate`
- `FundCluster`
- `UacsObjectCode`
- `EstimatedUsefulLifeYears`

These are required to materialize an `AssetRegistry` row. Either:

1. Amend the event payload in `AssetProcurement.Contracts` (cheap, one-time change), or
2. Have the AssetRegister consumer fetch the missing fields from the catalog + tenant defaults at consume time.

Decide before starting Phase 3.

---

## Roadmap (from plan, status added)

| Phase | Deliverable                                                                                                                                                                                        | Status                                                             |
| ----- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| 1     | Project skeleton + 6 aggregates + enums + EF + initial migration + tests skeleton                                                                                                                  | ✅ **Done 2026-05-12**                                             |
| 2     | Contracts (DTOs, commands, queries, domain events) wired through Mediator                                                                                                                          | ⏳ Pending                                                         |
| 3     | Vertical slices for `AssetRegistry` + `PropertyAccountability`; integration consumer for `AssetIARAcceptedEvent` materializes assets; validators enforce SE↔ICS and PPE↔PAR; vehicle odometer rule | ⏳ Pending                                                         |
| 4     | `PropertyIssuanceReport`, `PhysicalCountSession`, `PropertyIncidentReport`, `UnserviceablePropertyReport` slices; FoundAtStation reconciliation workflow                                           | ⏳ Pending                                                         |
| 5     | Reports — ICS, PAR, RPCSEMEX, RPCPPE, RegSPI, RSPI, IIRUSP/IIRUP — branched off `AssetType`                                                                                                        | ✅ **API endpoints + query handlers landed**                       |
| 6     | Blazor UI under `Components/Pages/AssetRegister/`                                                                                                                                                  | 🔄 **In progress** (overview + parameterized reports launcher + nav group added) |
| 7     | Seeding, role/permission seed updates, final cleanup                                                                                                                                               | 🔄 **Started** (`AssetRegisterDbInitializer` now seeds default property catalog per tenant) |

---

## Verification commands

```powershell
# Full build (should report 0 errors)
dotnet build src/FSH.Framework.slnx

# AssetRegister tests
dotnet test src/Tests/AssetRegister.Tests/AssetRegister.Tests.csproj

# Generate DDL preview for the AssetRegister schema
dotnet ef migrations script `
    --project src/Playground/Migrations.PostgreSQL `
    --startup-project src/Playground/Playground.Api `
    --context AssetRegisterDbContext

# Confirm AssetManagement was not touched
git diff --stat src/Modules/AssetManagement/
```
