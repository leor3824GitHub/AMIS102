# AssetRegister Module — Implementation Plan

> New bounded context that introduces the unified asset domain model from `AMIS101-Unified-Asset-Refactoring_v2.md`, running in parallel with the existing `AssetManagement` module. **`AssetManagement` is not modified.**

## Goal

Collapse the parallel SE-track (ICS / SMIR / RRSP) and PPE-track (PAR / PPEIR / RRP) onto one unified backbone — `AssetRegistry` (master ledger), `PropertyAccountability` (unified ICS+PAR), `PropertyIssuanceReport` (unified SMIR+PPEIR) — while keeping COA-compliant *output* (ICS vs PAR, RPCSEMEX vs RPCPPE, etc.) driven by `AssetType` branching at the edges. `PhysicalCountSession`, `PropertyIncidentReport`, `UnserviceablePropertyReport` are already unified in `AssetManagement` and are recreated cleanly here in the new shape.

## Settled decisions

| # | Decision | Choice |
|---|---|---|
| Module name | Bounded context name | **`AssetRegister`** (aggregate inside it is `AssetRegistry`) |
| Coexistence | Relationship to existing `AssetManagement` | Parallel module, no edits to `AssetManagement` |
| 2 | Property catalog source | **2a** — duplicate a slim catalog inside `AssetRegister` |
| 3 | Counters | Fresh `PropertyCodeCounter` inside `AssetRegister` |
| 4 | Source of new asset rows | **4b** — consume `AssetIARAcceptedEvent` from `AssetProcurement` (event already exists and is published) |
| 5 | Blazor UI location | New folder `Components/Pages/AssetRegister/`, new top-level nav section "Asset Register" |
| 6 | First-cut scope | **6a** — domain + EF + migration skeleton only. No features, no UI |
| 7 | DB schema name | `asset_register` |
| 8 | Tests | Skeleton test project lands in Phase 1; tests filled in alongside features |

## Refinements (second pass) — what changed since first draft

Cross-checked against every COA annex (A.1 SPC, A.2 SPLC, A.3 ICS, A.4 RegSPI, A.5 ITR, A.6 RRSP, A.7 RSPI, A.8 RPCSEMEX, A.9 RLSDDSP, A.10 IIRUSP) and confirmed alignment with existing `AssetManagement` field conventions (`FundCluster`, `UACSObjectCode`).

**A. Aggregate inventory clarified**

- No new aggregates added in second pass. **Inventory Transfer Report (ITR, Annex A.5), RRSP (A.6), SPC (A.1), SPLC (A.2), RegSPI (A.4), RSPI (A.7)** are read-side projections / generated outputs — they are *not* aggregates. They are produced from `AssetRegistry` + `PropertyAccountability` + `PropertyIssuanceReport` events and lifecycle state.
- Returns confirmed folded into `PropertyAccountability` lifecycle. The RRSP form is a generated document from a return event, nothing more.

**B. Cardinality rule**

- **One `AssetRegistry` row = one physical unit with a unique `PropertyNo`.** A purchase of 50 chairs produces 50 rows. This contrasts with existing `TangibleItem` which carries `Quantity` — we deliberately diverge for cleaner accountability tracking and unambiguous lifecycle state. The integration consumer for `AssetIARAcceptedEvent` expands quantity into individual rows.

**C. Concurrency token applied uniformly**

- `byte[] Version` on **all six aggregates** (was three): adds `PhysicalCountSession`, `PropertyIncidentReport`, `UnserviceablePropertyReport`.

**D. Field additions discovered from the annexes**

- `AssetRegistry`: `FundCluster`, `UacsObjectCode`, `AccumulatedDepreciation`, `AccumulatedImpairmentLosses`, `CurrentCondition`, computed `CarryingAmount`.
- `PropertyItemCatalog`: `UacsObjectCode`, `TenantId`.
- `PropertyAccountability` lines: `SnapshotUnit`, `SnapshotItemNo`, `SnapshotResponsibilityCenterCode` (moved from header); header gains `CancellationReason?`, `FundCluster`.
- `PropertyIssuanceReport`: `PostedByEmployeeId?`, `PostedOn?`, `FundCluster`; lines gain `AccountabilityLineId`, `SnapshotResponsibilityCenterCode`.
- `PhysicalCountSession`: `ApprovedByEmployeeId?`, `WitnessedByEmployeeId?`, `FundCluster`; entries gain `SnapshotUnit`, `SnapshotArticle`.
- `PropertyIncidentReport`: `DepartmentOffice`, `AccountableOfficerDesignation`, `AccountableOfficerGovIdType?`, `AccountableOfficerGovIdNo?`, `AccountableOfficerGovIdIssuedOn?`, notarization fields, resolution fields (`AmountSettled?`, `RecoveredOn?`, `ReliefGrantedOn?`), `FundCluster`.
- `UnserviceablePropertyReport`: `RequestedByEmployeeId`, `ApprovedByEmployeeId?`, `InspectedByEmployeeId?`, `InspectedOn?`, `WitnessedByEmployeeId?`, `WitnessedOn?`, `FundCluster`; items gain `SnapshotDateAcquired`, `SnapshotAccumulatedDepreciation`.

**E. Value objects extracted (in Contracts → Domain)**

- `PropertyNumber` — encapsulates COA 2020-006 format `YYYY-AA-BB-NNNN-CC` with parse + validate + `ToString()`. Use everywhere `string PropertyNo` appears today.
- `AssetSnapshot` — frozen `{ PropertyNo, Description, AssetType, UnitCost, Unit, EstimatedUsefulLifeYears, AcquisitionDate, UacsObjectCode }`. Reused as the snapshot block on every line/entry/item to eliminate field drift. Owned-entity-or-property-bag in EF (configured as owned).
- `EmployeeRef` — `{ EmployeeId, SnapshotPrintedName, SnapshotDesignation }`. Signatures on COA forms must survive employee renames; this captures the moment.

**F. Domain services added (interfaces in Domain, implementations land Phase 3 under Data/)**

- `IPropertyNumberGenerator.Next(AssetType, AcquisitionDate, sub-major, GL account, locationCode) → PropertyNumber`
- `IAccountabilityNumberGenerator.NextIcs(AssetCategory, DateOnly) → string` (formats `SPLV-YYYY-MM-NNNN` for low-valued, `SPHV-YYYY-MM-NNNN` for high-valued)
- `IAccountabilityNumberGenerator.NextPar(DateOnly) → string`
- `IInventoryTransferNumberGenerator.Next(DateOnly) → string` (`ITR-YYYY-MM-NNNN`)
- `IIncidentNumberGenerator.Next(DateOnly) → string` (`RLSDDSP-YYYY-MM-NNNN`)
- `IUnserviceableReportNumberGenerator.Next(UnserviceableReportType, DateOnly) → string` (IIRUSP/IIRUP series)
- `ICurrentReplacementCostCalculator.Compute(AssetRegistry, DateOnly asOf) → decimal` — used by incident report snapshotting (COA 2022-004 §4.19).

All number generators operate against `PropertyCodeCounter` with optimistic concurrency + retry; documented in their contract.

**G. Aggregate-wide policies (carry forward to every entity below)**

- Child collections expressed as `IReadOnlyCollection<T>` over `private readonly List<T>` so only the aggregate root can mutate.
- `TenantId` stamped on parent aggregates only; Finbuckle global query filter applies at root; children inherit via join.
- Soft delete (`IsDeleted`) intentionally **disabled** for the six runtime aggregates — lifecycle/status enums carry the closure semantics. Soft delete remains on `PropertyItemCatalog` only.
- All monetary fields: `decimal(18,2)` in EF config.
- Business dates: `DateOnly`. Audit timestamps and event timestamps: `DateTimeOffset`.
- Every state-changing method on an aggregate raises a domain event captured on the root; no public setters.
- Cross-module integration events (the ones in §5 below) live in `.Contracts` and implement `IIntegrationEvent`. Pure in-process domain events used to coordinate within the module live in `Domain/Events/` and implement the framework's `IDomainEvent`.

**H. Deliberately deferred (NOT in Phase 1)**

- Vehicle profile fields on `AssetRegistry` — instead, Phase 3 adds a `VehicleAccountabilityProfile` owned entity on `PropertyAccountabilityLine` that captures odometer readings at issue/transfer/return. AssetRegistry stays vehicle-agnostic.
- `AssetRepair` history (SPLC repair-history column) — Phase 5 concern; modeled as a child of `AssetRegistry` then.
- Depreciation schedule entities — Phase 5; for now, `AccumulatedDepreciation` is a plain decimal updated by an external (yet-to-exist) depreciation handler. Domain only enforces the invariant `AssetType == SE ⇒ AccumulatedDepreciation == 0`.
- Multi-currency support — kept as plain `decimal`; flagged for upgrade if/when needed.

---

## Aggregates landing in this module

| Aggregate | Replaces (in `AssetManagement`) | Notes |
|---|---|---|
| `AssetRegistry` | `TangibleItem` + `PropertyItemCatalog` + `TangibleInventory` | Master record, adds `LifecycleState`, current custodian/location, `Version` |
| `PropertyAccountability` (+ lines) | `InventoryCustodianSlip`+`ICSItem` and `PropertyAcknowledgementReceipt`+`PARItem` | Enum-discriminated by `AccountabilityType` |
| `PropertyIssuanceReport` (+ lines) | `SemiExpendableIssuanceRecord`+`SMIRItem` and `PPEIssuanceReport`+`PPEIRItem` | Enum-discriminated by `ReportType` |
| `PhysicalCountSession` (+ entries) | same name in `AssetManagement` | Supports `FoundAtStation` with null `AssetRegistryId` |
| `PropertyIncidentReport` (+ items) | same name in `AssetManagement` | Snapshots CRC, marks assets `UnderInvestigation` |
| `UnserviceablePropertyReport` (+ items) | same name in `AssetManagement` | Drives lifecycle → `Disposed` |
| `PropertyItemCatalog` | slim copy of `AssetManagement` catalog | Local SKU table |
| `PropertyCodeCounter` | local counter | Mints `PropertyNo` / `ICS No` / `PAR No` / `ITR No` per COA formats |

Returns are **not** a separate aggregate — folded into the `PropertyAccountability` lifecycle (`ReturnLines`, `ReportLineLost`).

---

# Phase 1 design — Domain + EF + migration skeleton

## 1. Projects

```
src/Modules/AssetRegister/
├── Modules.AssetRegister.Contracts/
│   └── Modules.AssetRegister.Contracts.csproj
└── Modules.AssetRegister/
    └── Modules.AssetRegister.csproj
src/Tests/Modules.AssetRegister.Tests/
    └── Modules.AssetRegister.Tests.csproj   (architecture tests scaffold only)
```

**References**

- Contracts: `Mediator.Abstractions`, `BuildingBlocks/Eventing.Abstractions`, `BuildingBlocks/Shared`.
- Implementation: Contracts, `BuildingBlocks/Caching`, `BuildingBlocks/Persistence`, `BuildingBlocks/Web`, `BuildingBlocks/Eventing`, and `Modules.AssetProcurement.Contracts` (read-only — only the `AssetIARAcceptedEvent` type).

**Host wiring in `Playground.Api/Program.cs`**

- Add representative types (`AssetRegisterModule`, `AssetRegisterContractsMarker`, a placeholder command type) to `Mediator.Assemblies`.
- Append `typeof(AssetRegisterModule).Assembly` to `moduleAssemblies`.
- Add both project references to `Playground.Api.csproj`.
- Update `src/AMIS.Framework.slnx` to include both projects.

## 2. Module constants

```csharp
public static class AssetRegisterModuleConstants
{
    public const string SchemaName = "asset_register";
    public const string MigrationsTable = "__EFMigrationsHistory_AssetRegister";

    public static class Permissions
    {
        public static class Assets         { /* view, register, update, retire */ }
        public static class Accountability { /* view, issue, transfer, return */ }
        public static class Issuance       { /* view, post */ }
        public static class Count          { /* view, create, record, submit, close */ }
        public static class Incident       { /* view, file, resolve */ }
        public static class Unserviceable  { /* view, file, dispose */ }
        public static class Catalog        { /* view, create, update, delete */ }
    }
}
```

## 3. Domain — aggregates and property shapes

All inherit `AggregateRoot<Guid>`, implement `IHasTenant` and `IAuditableEntity`. Properties use `private set`; mutation goes through factory + behavior methods. Concurrency token (`byte[] Version`) on `AssetRegistry`, `PropertyAccountability`, `PropertyIssuanceReport`.

### 3.1 `PropertyItemCatalog` (slim, owned by this module)

```
Id, TenantId
Code                                       (string, unique per tenant)
Description
DefaultPropertyClass, DefaultCategoryCode
DefaultUnit                                (e.g. "piece", "set")
UacsObjectCode?                            (chart-of-accounts code, snapshotted into AssetRegistry at registration)
EstimatedUsefulLifeYears                   (per COA 2022-004 §4.13 guide range)
IsActive
+ IAuditableEntity (soft delete allowed here)
```

Methods: `Create`, `Update`, `Deactivate`, `Reactivate`. Domain rule: `EstimatedUsefulLifeYears > 0`.

### 3.2 `PropertyCodeCounter`

```
Id, TenantId
Year (int), Month (int)
CounterKey (string — known values: SPLV, SPHV, PAR, PPE-{sub-major}, ITR, RLSDDSP, IIRUSP, IIRUP, RSPI)
LastSerial (int)
RowVersion (concurrency token)
```

Unique index: `(TenantId, Year, Month, CounterKey)`. `NextSerial()` is a single domain method using optimistic concurrency: on `DbUpdateConcurrencyException`, retry up to N times. Documented contract for the generators in §F.

### 3.3 `AssetRegistry` (aggregate root — master ledger)

```
Id, TenantId, Version (concurrency token)

— identity & classification —
PropertyNo                                (PropertyNumber VO, unique per tenant, COA 2020-006 format)
ItemId                                    (FK → PropertyItemCatalog)
AssetType                                 (SE | PPE)
Category                                  (LowValuedSemi | HighValuedSemi | PPE)
PropertyClass, CategoryCode               (snapshot from catalog defaults at register time)
Description, SerialNo?, Brand?, Model?
Unit                                      (snapshot from catalog)

— accounting —
FundCluster                               (required)
UacsObjectCode                            (snapshot from catalog; required for SPLC postings)
AcquisitionDate                           (DateOnly)
UnitCost                                  (decimal(18,2))
EstimatedUsefulLifeYears                  (snapshot from catalog)
AccumulatedDepreciation                   (decimal(18,2), default 0 — INVARIANT: AssetType == SE ⇒ 0)
AccumulatedImpairmentLosses               (decimal(18,2), default 0)
CarryingAmount                            (computed: UnitCost − AccumulatedDepreciation − AccumulatedImpairmentLosses; not stored)

— lifecycle & assignment —
LifecycleState                            (Available | Assigned | UnderInvestigation | Unserviceable | Disposed)
CurrentCondition                          (InGoodCondition | NeedingRepair | Unserviceable — updated from latest count entry; defaults InGoodCondition)
CurrentCustodianId?                       (Guid → employee)
CurrentLocationId?                        (Guid)
CurrentAccountabilityId?                  (Guid → PropertyAccountability)

— provenance —
SourceIARId?                              (Guid → AssetIARAcceptedEvent.IARId)
SourcePurchaseOrderId?                    (Guid)

+ IAuditableEntity (soft delete DISABLED — disposition is via LifecycleState, not IsDeleted)
```

**Cardinality:** one row = one physical unit with a unique `PropertyNo`. No `Quantity` field.

**Methods (each raises a domain event):**

- `Register(catalog, fundCluster, acquisitionDate, unitCost, propertyNumber, sourceIARId?, sourcePurchaseOrderId?)` → factory; emits `AssetRegisteredEvent`
- `AssignTo(accountabilityId, custodianId, locationId)` → `LifecycleState: Available → Assigned`; emits `AssetIssuedEvent`
- `Transfer(newAccountabilityId, newCustodianId, newLocationId)` → asset must already be `Assigned`; emits `AssetTransferredEvent`
- `ReturnToAvailable()` → from `Assigned`; clears current custodian/location/accountability; emits `AssetReturnedEvent`
- `MarkUnderInvestigation(incidentReportId)` → from `Assigned`; emits `AssetLostEvent`
- `RecordFoundAtStation(sessionId)` → typically transitions a newly-registered asset to `Available` and links provenance to the count session; emits `AssetFoundAtStationEvent`
- `MarkUnserviceable(reportId)` → from any non-Disposed state; emits `AssetUnserviceableEvent`
- `Dispose(reportId, method)` → terminal; emits `AssetDisposedEvent`
- `RecordImpairment(amount, reason)` → adds to `AccumulatedImpairmentLosses`
- `RecordDepreciation(amount)` → guards `AssetType == PPE`, adds to `AccumulatedDepreciation`
- `UpdateCondition(condition)` → called by physical-count reconciliation only

**Invariants:**

1. `PropertyNo` unique per tenant.
2. `AssetType == SE ⇒ AccumulatedDepreciation == 0` (depreciation rule).
3. State transitions follow the lifecycle FSM (table in §10 below).
4. `Disposed` is terminal — no further mutation allowed except read.

### 3.4 `PropertyAccountability` (aggregate root — unifies ICS + PAR)

```
Id, TenantId, Version (concurrency token)

— header —
DocumentNo                                (COA format per type: SPLV-/SPHV-YYYY-MM-NNNN or PAR-YYYY-MM-NNNN)
AccountabilityType                        (SE_ICS | PPE_PAR)
FundCluster
IssuedOn                                  (DateOnly)
ExpiresOn?                                (DateOnly; REQUIRED iff SE_ICS; default = IssuedOn + 3 years per COA 2022-004 §4.1)
Status                                    (Active | Renewed | Returned | Cancelled)
CancellationReason?                       (required when Status == Cancelled)
SupersededByAccountabilityId?             (Guid — set when this row was renewed by a successor)
SupersedesAccountabilityId?               (Guid — back-link: this row renewed an earlier one)

— signatories (EmployeeRef value objects: captures employee + printed name + designation at moment of signing) —
IssuedBy                                  (EmployeeRef — Property/Supply Custodian, signs "Received from")
ReceivedBy                                (EmployeeRef — end-user, signs "Received by")

— lines —
Lines : IReadOnlyCollection<PropertyAccountabilityLine>

+ IAuditableEntity (soft delete disabled)
```

**`PropertyAccountabilityLine`:**

```
Id, AccountabilityId, AssetRegistryId
Snapshot : AssetSnapshot                  (value object: PropertyNo, Description, AssetType,
                                           UnitCost, Unit, EstimatedUsefulLifeYears, AcquisitionDate, UacsObjectCode)
SnapshotItemNo                            (string — the "Item No." column on the COA form)
SnapshotResponsibilityCenterCode?         (per line; falls back to a header default if needed)
IssuedQty                                 (always 1 per cardinality rule, kept for COA-form column)
ReturnedQty                               (0 or 1)
LineStatus                                (Active | Returned | Lost)
ReturnedOn?, ReturnedToConditionAtReturn?
LostOnIncidentId?                         (Guid → PropertyIncidentReport)
```

**Methods:**

- `Issue(type, fundCluster, issuedBy, receivedBy, issuedOn, lines, expiresOn?)` → factory; emits `AssetIssuedEvent` per line
- `Renew(newIssuedOn, newExpiresOn?)` → returns a new `PropertyAccountability` with `SupersedesAccountabilityId = this.Id`; transitions `this.Status` to `Renewed`; only valid when `this.Status == Active`
- `ReturnLines(lineIds, returnedOn, conditionAtReturn)` → flips matching lines to `Returned`; if all lines returned, header → `Returned`; emits `AssetReturnedEvent` per line
- `ReportLineLost(lineId, incidentReportId)` → line → `Lost`; emits `AssetLostEvent`
- `Cancel(reason, cancelledOn)` → only valid when no line has been Returned; emits `AccountabilityCancelledEvent`

**Invariants (factory + each mutator):**

1. Every line's asset's `AssetType` matches `AccountabilityType` (SE → SE_ICS, PPE → PPE_PAR). `DomainException` otherwise — this is the rule #1 "form segregation" guard.
2. Every line's asset must be in `LifecycleState == Available` at issue time.
3. `ExpiresOn` required iff `AccountabilityType == SE_ICS`; must equal `IssuedOn + EstimatedUsefulLifeYears` (capped at 3 years per COA default) unless explicitly overridden.
4. `Renew` only allowed when `Status == Active`.
5. `Cancel` blocked once any line has `LineStatus != Active`.
6. `DocumentNo` unique per tenant.

### 3.5 `PropertyIssuanceReport` (aggregate root — unifies RSPI/SMIR + PPEIR)

```
Id, TenantId, Version (concurrency token)
ReportNo                                  (COA format, e.g. RSPI-YYYY-MM-NNNN or PPEIR-YYYY-MM-NNNN)
ReportType                                (SMIR | PPEIR — asserted on Post() against all line AssetTypes)
FundCluster
PeriodFromDate, PeriodToDate              (DateOnly)
Status                                    (Draft | Posted)

— signatories (EmployeeRef) —
PreparedBy                                (Property/Supply Custodian — "Certified by" on RSPI)
CertifiedBy?
PostedBy?                                 (Accounting Division/Unit designated staff — "Posted by/date" on RSPI A.7)
PostedOn?                                 (DateOnly, mirrors COA "Posted by/date" column)

Lines : IReadOnlyCollection<PropertyIssuanceReportLine>

+ IAuditableEntity (soft delete disabled)
```

**`PropertyIssuanceReportLine`:**

```
Id, ReportId
AccountabilityId                          (source ICS or PAR)
AccountabilityLineId                      (source line — traces to the exact issuance)
AssetRegistryId
Snapshot : AssetSnapshot                  (same VO as accountability line)
SnapshotResponsibilityCenterCode
SnapshotQuantityIssued                    (=1 per cardinality rule, kept for form column)
SnapshotUnitCost                          (frozen at posting time)
SnapshotAmount                            (= SnapshotQuantityIssued × SnapshotUnitCost, denormalized for fast RSPI roll-up)
```

**Methods:**

- `CreateDraft(reportType, fundCluster, period, preparedBy)` → factory
- `AddLines(IEnumerable<sourceAccountabilityLineId>)` → snapshots from the referenced accountability lines
- `RemoveLine(lineId)` → only valid while Draft
- `Post(certifiedBy, postedBy, postedOn)` → invariants below; transitions to `Posted`; emits `IssuanceReportPostedEvent`

**Invariants on `Post()`:**

1. Status must be Draft.
2. All lines' `Snapshot.AssetType` must agree with `ReportType` (SMIR ⇒ all SE; PPEIR ⇒ all PPE).
3. At least one line.
4. `ReportNo` unique per tenant; assigned by `IIssuanceReportNumberGenerator`.
5. Period must be coherent: `PeriodFromDate ≤ PeriodToDate`; lines' source issuance dates fall within the period.

**Posted reports are immutable** — corrections go via a new draft referencing the original (`SupersedesReportId?` — Phase 4 extension if needed).

### 3.6 `PhysicalCountSession` (aggregate root — produces RPCSEMEX / RPCPPE)

```
Id, TenantId, Version (concurrency token)
Code                                      (e.g. PCS-YYYY-NNNN)
Scope                                     (PPEOnly | SEOnly | Both)
Status                                    (Ongoing | Reconciled | Closed)
FundCluster
StartedOn, ClosedOn?                      (DateOnly)
AsAt                                      (DateOnly — the "As at" stamped on RPCSEMEX/RPCPPE)

— signatories (per Annex A.8) —
ConductedBy                               (collection of EmployeeRef — Inventory Committee members)
ApprovedBy?                               (EmployeeRef — Head of Agency or representative)
WitnessedBy?                              (EmployeeRef — COA Representative)

Remarks

Entries : IReadOnlyCollection<PhysicalCountEntry>
```

**`PhysicalCountEntry`:**

```
Id, SessionId
AssetRegistryId?                          (NULL when Condition == FoundAtStation and the asset is brand new)

Snapshot : AssetSnapshot?                 (NULL when FoundAtStation creates a row from scratch;
                                           otherwise frozen at scan time)
SnapshotArticle                           (string — the "Article" column on RPCSEMEX)
SnapshotUnit                              (string — "Unit of Measure" column)
SnapshotUnitCost                          (decimal(18,2) — frozen)

Condition                                 (InGoodCondition | NeedingRepair | Unserviceable | Missing | FoundAtStation)
ScannedOnUtc?                             (DateTimeOffset — null if entered manually)
PhotoPath?
ScannedByEmployeeId?
LocationId
Remarks

— for FoundAtStation entries only —
ProposedPropertyClass?, ProposedCategoryCode?, ProposedAcquisitionDate?, ProposedUnitCost?
                                          (populated by the inventory committee; used by reconciliation to materialize a new AssetRegistry row)
```

**Methods on session:**

- `Start(scope, fundCluster, asAt, startedOn, conductedBy)` → factory
- `RecordEntry(...)` → for known assets; updates `AssetRegistry.CurrentCondition` post-reconciliation only, not at record time
- `AddFoundAtStationEntry(article, unit, unitCost, location, proposed fields, scannedBy)` → entry with `AssetRegistryId == null`
- `MarkMissing(assetRegistryId, location, remarks)` → entry with `Condition == Missing`
- `Reconcile(numberGenerator)` → only valid from `Ongoing`; for every FoundAtStation entry, raises `AssetFoundAtStationEvent` carrying the proposed snapshot (handler creates the `AssetRegistry` row and back-fills `AssetRegistryId` on the entry). Status → `Reconciled`. For every `Missing` entry, raises `AssetReportedMissingFromCountEvent` (handler opens a draft `PropertyIncidentReport` — Phase 4).
- `Close(approvedBy, witnessedBy)` → from `Reconciled` only; emits `PhysicalCountSessionClosedEvent`. Updates `AssetRegistry.CurrentCondition` per entry conditions.

**Invariants:**

1. An entry with `AssetRegistryId != null` must reference an asset whose `AssetType` falls under the session `Scope`.
2. Reconciliation cannot run twice — Status FSM enforces.
3. Close blocked while any entry still has `Condition == FoundAtStation` and `AssetRegistryId == null`.

### 3.7 `PropertyIncidentReport` (aggregate root — unifies RLSDDSP + Notice of Loss)

```
Id, TenantId, Version (concurrency token)

— header —
IncidentNo                                (COA format: RLSDDSP-YYYY-MM-NNNN)
IncidentType                              (Lost | Stolen | Damaged | Destroyed)
IncidentDate                              (DateOnly)
FundCluster
DepartmentOffice                          (per Annex A.9)
Circumstances                             (long string — the narrative section of the form)

— accountable officer block —
AccountableOfficer                        (EmployeeRef — required by form certification)
AccountableOfficerDesignation
AccountableOfficerGovIdType?              (per "Government Issued ID" field)
AccountableOfficerGovIdNo?
AccountableOfficerGovIdIssuedOn?          (DateOnly)
NotedBy?                                  (EmployeeRef — Immediate Supervisor)

— police block (RLSDDSP) —
PoliceNotified                            (bool)
PoliceStation?
PoliceNotifiedOn?                         (DateOnly)
PoliceBlotterRef?

— notarization (required for RLSDDSP) —
NotarizedOn?                              (DateOnly)
NotaryDocNo?, NotaryPageNo?, NotaryBookNo?, NotarySeriesOf?

— status & resolution —
Status                                    (Filed | UnderInvestigation | Resolved | Closed)
ReliefRequestedOn?                        (DateOnly — Sec 73, PD 1445)
ReliefGrantedOn?, ReliefGrantedRef?       (COA decision reference if relief granted)
AmountSettled?                            (decimal — if accountable officer paid)
SettledOn?
RecoveredOn?                              (if item was eventually produced)

Items : IReadOnlyCollection<PropertyIncidentItem>

+ IAuditableEntity (soft delete disabled)
```

**`PropertyIncidentItem`:**

```
Id, ReportId, AssetRegistryId
Snapshot : AssetSnapshot
SnapshotAcquisitionCost                   (= UnitCost at time of incident)
SnapshotCurrentReplacementCost            (computed by ICurrentReplacementCostCalculator at File() time)
AccountabilityLineId?                     (Guid → PropertyAccountabilityLine that issued this asset)
ItemResolution                            (Pending | Recovered | Paid | ReliefGranted | Derecognized)
ResolvedOn?
```

**Methods:**

- `File(filedByEmployee, items, crcCalculator)` → factory; per item, computes CRC snapshot; sets each asset to `UnderInvestigation`; flips the associated `PropertyAccountabilityLine.LineStatus → Lost`; emits `AssetLostEvent` per item
- `NotifyPolice(station, notifiedOn, blotterRef)`
- `Notarize(notarizedOn, docNo, pageNo, bookNo, seriesOf)`
- `RecordRecovery(itemId, recoveredOn)` → asset returns to `Available` (or `Unserviceable` if damaged on return); flips item to `Recovered`
- `RecordSettlement(itemId, amount, settledOn)` → asset goes to `Disposed` (paid-and-derecognized); flips item to `Paid`
- `GrantRelief(itemId, grantedOn, decisionRef)` → asset → `Disposed`; item → `ReliefGranted`
- `MarkDerecognized(itemId)` → for the COA 2020-006 §8 path (missing PPE without record of accountability) — requires `IDerecognitionAuthorityRef`; item → `Derecognized`
- `Close()` → only when all items have a terminal resolution

**Invariants:**

1. Every referenced asset must have `LifecycleState ∈ { Assigned, Available }` at File time (you can't lose what's already disposed).
2. Notarization is required on RLSDDSP before any resolution method can run.
3. `IncidentNo` unique per tenant.

### 3.8 `UnserviceablePropertyReport` (aggregate root — unifies IIRUSP + IIRUP)

```
Id, TenantId, Version (concurrency token)

— header —
ReportNo                                  (IIRUSP-YYYY-MM-NNNN or IIRUP-YYYY-MM-NNNN)
ReportType                                (IIRUSP | IIRUP — asserted on Submit against item AssetTypes)
AsAt                                      (DateOnly — the "As at" stamp on Annex A.10)
FundCluster
Station                                   (per Annex A.10 header)
Status                                    (Draft | Submitted | InspectionDone | DisposalRecorded | Closed)

— signatories (EmployeeRef) —
AccountableOfficer                        ("Requested by" + designation)
ApprovedBy?
InspectedBy?
InspectedOn?                              (DateOnly)
WitnessedBy?
WitnessedOn?                              (DateOnly)

Items : IReadOnlyCollection<UnserviceablePropertyItem>

+ IAuditableEntity (soft delete disabled)
```

**`UnserviceablePropertyItem`:**

```
Id, ReportId, AssetRegistryId
Snapshot : AssetSnapshot
SnapshotDateAcquired                      (DateOnly)
SnapshotAcquisitionCost
SnapshotAccumulatedDepreciation           (frozen at submission)
SnapshotAccumulatedImpairmentLosses
SnapshotCarryingAmount                    (= SnapshotAcquisitionCost − SnapshotAccumulatedDepreciation − SnapshotAccumulatedImpairmentLosses)
Remarks

— inspection & disposal columns (Annex A.10) —
DisposalMethod?                           (Sale | Transfer | Destruction | Other)
DisposalOtherSpecify?                     (when DisposalMethod == Other)
AppraisedValue?
DisposalRecordedOn?                       (DateOnly)

— record of sales (only when DisposalMethod == Sale) —
SaleORNo?
SaleAmount?
```

**Methods:**

- `CreateDraft(reportType, fundCluster, station, asAt, accountableOfficer)` → factory
- `AddItem(assetRegistry)` → asset must be in `Unserviceable` lifecycle state (or transition will happen via `MarkUnserviceable` flow)
- `Submit(approvedBy)` → Status → `Submitted`; freezes all snapshots
- `RecordInspection(inspectedBy, inspectedOn, witnessedBy, witnessedOn, perItemDisposalDecisions)` → Status → `InspectionDone`
- `RecordDisposal(perItemDisposalRecord)` → for each item: sets DisposalRecordedOn, sale OR + amount if applicable; flips asset → `Disposed`; emits `AssetDisposedEvent`; Status → `DisposalRecorded`
- `Close()` → final state

**Invariants:**

1. `ReportType == IIRUSP ⇒ every item.Snapshot.AssetType == SE`; `IIRUP ⇒ PPE`.
2. Cannot add items once Status > Draft.
3. Cannot dispose without prior inspection (`InspectionDone`).
4. `ReportNo` unique per tenant.

## 4. Enums (exact members)

```csharp
public enum AssetType                  { SE, PPE }
public enum AssetCategory              { LowValuedSemi, HighValuedSemi, PPE }
public enum AssetCondition             { InGoodCondition, NeedingRepair, Unserviceable }
public enum LifecycleState             { Available, Assigned, UnderInvestigation, Unserviceable, Disposed }
public enum AccountabilityType         { SE_ICS, PPE_PAR }
public enum AccountabilityStatus       { Active, Renewed, Returned, Cancelled }
public enum AccountabilityLineStatus   { Active, Returned, Lost }
public enum IssuanceReportType         { SMIR, PPEIR }
public enum IssuanceReportStatus       { Draft, Posted }
public enum PhysicalCountScope         { PPEOnly, SEOnly, Both }
public enum PhysicalCountStatus        { Ongoing, Reconciled, Closed }
public enum PhysicalCountCondition     { InGoodCondition, NeedingRepair, Unserviceable, Missing, FoundAtStation }
public enum PropertyIncidentType       { Lost, Stolen, Damaged, Destroyed }
public enum PropertyIncidentStatus     { Filed, UnderInvestigation, Resolved, Closed }
public enum IncidentItemResolution     { Pending, Recovered, Paid, ReliefGranted, Derecognized }
public enum UnserviceableReportType    { IIRUSP, IIRUP }
public enum UnserviceableReportStatus  { Draft, Submitted, InspectionDone, DisposalRecorded, Closed }
public enum DisposalMethod             { Sale, Transfer, Destruction, Other }
```

`AssetCondition` is the subset of `PhysicalCountCondition` that maps onto a stored asset (excludes `Missing` and `FoundAtStation`, which are session-only markers).

## 4a. `AssetRegistry` lifecycle FSM (referenced from §3.3 invariants)

| From / To                | Available | Assigned | UnderInvestigation | Unserviceable | Disposed |
|--------------------------|:---------:|:--------:|:------------------:|:-------------:|:--------:|
| **Available**            | —         | AssignTo | MarkUnderInvestigation | MarkUnserviceable | (only via Unserviceable) |
| **Assigned**             | ReturnToAvailable | Transfer | MarkUnderInvestigation | MarkUnserviceable | — |
| **UnderInvestigation**   | RecordRecovery (item) | — | — | RecordRecovery if damaged | RecordSettlement / GrantRelief / MarkDerecognized |
| **Unserviceable**        | — | — | — | — | Dispose (via UnserviceablePropertyReport) |
| **Disposed**             | —         | —        | —                  | —             | terminal |

Any attempted transition not in this matrix throws `DomainException`. The lifecycle handler that consumes lifecycle events (`AssetIssuedEvent`, `AssetLostEvent`, etc.) is responsible for the actual mutation; aggregate methods are the only entry point.

## 4b. Value objects (in `Domain/ValueObjects/`)

```csharp
// Immutable record, validates COA 2020-006 format on construction.
public sealed record PropertyNumber
{
    public string Value { get; }
    public int Year { get; }
    public string SubMajor { get; }     // 2-digit
    public string Glaccount { get; }    // 2-digit
    public string Serial { get; }       // 4-digit
    public string Location { get; }     // 2-digit
    public static PropertyNumber Parse(string s);
    public static bool TryParse(string s, out PropertyNumber pn);
    public override string ToString() => Value;
}

// Frozen subset of AssetRegistry at the moment of issue/count/incident/disposal.
// Configured as an EF owned type on every line/entry/item that references it.
public sealed record AssetSnapshot(
    string PropertyNo,
    string Description,
    AssetType AssetType,
    decimal UnitCost,
    string Unit,
    int EstimatedUsefulLifeYears,
    DateOnly AcquisitionDate,
    string? UacsObjectCode,
    string? SerialNo,
    string? Brand,
    string? Model);

// Captures signatory identity + printed name + designation at the moment of signing,
// so the audit trail survives employee renames or role changes.
public sealed record EmployeeRef(
    Guid EmployeeId,
    string PrintedName,
    string? Designation);
```

## 4c. Domain services (interfaces declared in `Domain/Services/`, implementations under `Data/Services/` in Phase 3)

```csharp
public interface IPropertyNumberGenerator
{
    Task<PropertyNumber> NextAsync(
        AssetType assetType,
        string subMajorAccount,
        string generalLedgerAccount,
        string locationCode,
        DateOnly acquisitionDate,
        CancellationToken ct);
}

public interface IAccountabilityNumberGenerator
{
    Task<string> NextIcsAsync(AssetCategory category, DateOnly issueDate, CancellationToken ct);
    Task<string> NextParAsync(DateOnly issueDate, CancellationToken ct);
}

public interface IInventoryTransferNumberGenerator
{
    Task<string> NextAsync(DateOnly date, CancellationToken ct);
}

public interface IIncidentNumberGenerator
{
    Task<string> NextAsync(DateOnly incidentDate, CancellationToken ct);
}

public interface IIssuanceReportNumberGenerator
{
    Task<string> NextAsync(IssuanceReportType type, DateOnly periodStart, CancellationToken ct);
}

public interface IUnserviceableReportNumberGenerator
{
    Task<string> NextAsync(UnserviceableReportType type, DateOnly asAt, CancellationToken ct);
}

public interface ICurrentReplacementCostCalculator
{
    Task<decimal> ComputeAsync(Guid assetRegistryId, DateOnly asOf, CancellationToken ct);
}
```

All number generators operate against `PropertyCodeCounter` with optimistic concurrency + bounded retry (default 3). The interfaces are declared in Phase 1 but the implementations (and the actual consumers) land Phase 3.

## 5. Events

### 5a. In-process domain events (in `Domain/Events/`, implement `IDomainEvent`)

These coordinate state within the module and are dispatched on `SaveChanges`:

- `AssetRegisteredEvent`
- `AssetIssuedEvent`
- `AssetReturnedEvent`
- `AssetTransferredEvent`
- `AssetFoundAtStationEvent`
- `AssetReportedMissingFromCountEvent`
- `AssetLostEvent`
- `AssetRecoveredEvent`
- `AssetUnserviceableEvent`
- `AssetDisposedEvent`
- `AccountabilityCancelledEvent`
- `IssuanceReportPostedEvent`
- `PhysicalCountSessionClosedEvent`
- `IncidentReportFiledEvent`
- `UnserviceableReportSubmittedEvent`

Each event carries `{ TenantId, AssetRegistryId(s), OccurredOnUtc, CorrelationId }` plus event-specific payload.

### 5b. Cross-module integration events (in `.Contracts/v1/IntegrationEvents/`, implement `IIntegrationEvent`)

Published on the message bus for other modules (Finance, Auditing, MasterData) to react to:

- `AssetRegisterIntegrationEvents.AssetRegistered`
- `AssetRegisterIntegrationEvents.AssetIssued`
- `AssetRegisterIntegrationEvents.AssetDisposed`
- `AssetRegisterIntegrationEvents.IssuanceReportPosted` — Finance consumes for JEV preparation
- `AssetRegisterIntegrationEvents.IncidentReportFiled` — Finance consumes for setting up receivables
- `AssetRegisterIntegrationEvents.UnserviceableReportClosed`

Phase 1 declares the event types but **does not publish them**; the actual publication wiring lands Phase 3.

### 5c. Inbound integration consumer

Handler for `Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports.AssetIARAcceptedEvent`, registered in DI. **Handler stub is empty in Phase 1** (logged + ignored) — actual auto-registration logic that materializes one `AssetRegistry` row per accepted item lands Phase 3 with `RegisterAssetCommand`.

## 6. EF Core — `AssetRegisterDbContext`

- Inherits `BaseDbContext`.
- `DbSet<>` for: `PropertyItemCatalog`, `PropertyCodeCounter`, `AssetRegistry`, `PropertyAccountability`, `PropertyIssuanceReport`, `PhysicalCountSession`, `PropertyIncidentReport`, `UnserviceablePropertyReport`.
- Child entities (`PropertyAccountabilityLine`, `PropertyIssuanceReportLine`, `PhysicalCountEntry`, `PropertyIncidentItem`, `UnserviceablePropertyItem`) configured via `IEntityTypeConfiguration<T>` under `Data/Configurations/` as **separate tables** (not owned), each with FK + cascade-on-delete-from-root.
- **Value objects:**
  - `PropertyNumber` configured as a value converter on `AssetRegistry.PropertyNo` (`HasConversion(pn => pn.Value, s => PropertyNumber.Parse(s))`).
  - `AssetSnapshot` configured as an **EF owned type** on every line/entry/item that holds one (snapshot columns flattened into the parent table with `snapshot_*` prefix).
  - `EmployeeRef` configured as an **EF owned type** on every signatory property (flattened with appropriate prefix, e.g. `issued_by_employee_id`, `issued_by_printed_name`, `issued_by_designation`).
- All tables in schema `asset_register`.
- **Unique indexes:**
  - `(TenantId, PropertyNo)` on `AssetRegistry`
  - `(TenantId, DocumentNo)` on `PropertyAccountability`
  - `(TenantId, ReportNo)` on `PropertyIssuanceReport`, `UnserviceablePropertyReport`
  - `(TenantId, IncidentNo)` on `PropertyIncidentReport`
  - `(TenantId, Code)` on `PhysicalCountSession` and `PropertyItemCatalog`
  - `(TenantId, Year, Month, CounterKey)` on `PropertyCodeCounter`
- **Filtered/regular indexes (query performance):**
  - `AssetRegistry (TenantId, LifecycleState)`
  - `AssetRegistry (TenantId, CurrentCustodianId) WHERE LifecycleState = 'Assigned'`
  - `AssetRegistry (TenantId, ItemId)`
  - `PropertyAccountability (TenantId, Status, ExpiresOn)` for expiring-ICS queries
  - `PropertyAccountabilityLine (AccountabilityId, LineStatus)`
- **Concurrency token (`byte[] Version`)** configured on all six aggregates: `AssetRegistry`, `PropertyAccountability`, `PropertyIssuanceReport`, `PhysicalCountSession`, `PropertyIncidentReport`, `UnserviceablePropertyReport`.
- **Decimal precision:** all monetary columns explicitly `HasPrecision(18, 2)`.
- **Date types:** all business-date columns mapped as `date` (PostgreSQL); audit timestamps as `timestamptz`.
- **Soft-delete query filter** applied to `PropertyItemCatalog` only (the other six aggregates do not implement soft delete on their lifecycle).
- `OnModelCreating` calls `ApplyConfigurationsFromAssembly`.

## 7. DbContextFactory and Initializer

- `AssetRegisterDbContextFactory : IDesignTimeDbContextFactory<AssetRegisterDbContext>` — same dev connection string source as existing modules.
- `AssetRegisterDbInitializer : IDbInitializer` — Phase 1 `MigrateAsync` runs `Database.MigrateAsync`; `SeedAsync` is a no-op.

## 8. Initial migration

Single migration: **`AssetRegister_Initial`**. Generated to mirror the existing AssetManagement migrations layout (location confirmed before running). Command pattern:

```bash
dotnet ef migrations add AssetRegister_Initial \
  --project src/Playground/Migrations.PostgreSQL \
  --startup-project src/Playground/Playground.Api \
  --context AssetRegisterDbContext
```

## 9. Module class

`AssetRegisterModule : IModule`

- `ConfigureServices`: register permissions, `AddHeroDbContext<AssetRegisterDbContext>`, register `AssetRegisterDbInitializer`, register the (empty) integration event consumer.
- `MapEndpoints`: API version set + group `api/v{version:apiVersion}/asset-register` with tag `"Asset Register"`. **No endpoints in Phase 1** — group exists so OpenAPI lists the module.

## 10. Tests project (skeleton)

`Modules.AssetRegister.Tests`: one architecture test asserting the module does not reference `Modules.AssetManagement` or any other `Modules.*` implementation assembly. Nothing else.

## What does NOT exist after Phase 1

- No `RegisterAssetCommand` / `IssueAccountabilityCommand` / any other handler. Aggregates exist; no one calls them yet.
- No Blazor pages.
- No endpoints (the API version group exists in `MapEndpoints` but maps zero routes).
- No seeding.
- No integration-event consumption logic (handler class exists, `Handle` body is empty with a TODO).
- No domain-service implementations (interfaces declared; no DI registration yet).
- No integration-event publication (event types declared in `.Contracts`, no publisher wired).

All those land in Phase 3.

## Acceptance criteria — Phase 1

- [ ] `dotnet build src/AMIS.Framework.slnx` → 0 warnings, 0 errors.
- [ ] `dotnet test` → existing tests pass; new architecture test passes.
- [ ] `dotnet ef migrations script --context AssetRegisterDbContext` produces clean DDL for `asset_register.*` tables.
- [ ] `AssetManagement` module untouched (`git diff` shows zero changes under `src/Modules/AssetManagement/**`).
- [ ] Architecture test asserts the module references neither `Modules.AssetManagement` nor any other `Modules.*` implementation assembly (only `Modules.AssetProcurement.Contracts` is allowed).
- [ ] Every aggregate carries a `Version` concurrency token configured in EF.
- [ ] Every monetary column has `HasPrecision(18, 2)`; verified by reflection test.
- [ ] All six aggregate roots have **no** public setters on domain state (only navigation property collections via `IReadOnlyCollection`); verified by a small architecture test using reflection.
- [ ] `PropertyNumber` value object round-trips a known COA-format sample (unit test in scaffold).

---

# Phasing (overall roadmap)

| Phase | Deliverable | Status |
|---|---|---|
| 1 | Project skeleton + 6 aggregates + enums + EF configs + DbContext + initial migration + tests skeleton | This document |
| 2 | Contracts (DTOs, commands, queries, domain events) wired through Mediator | Pending |
| 3 | Vertical slices for `AssetRegistry` + `PropertyAccountability`; integration consumer for `AssetIARAcceptedEvent` materializes assets; validators enforce SE↔ICS and PPE↔PAR; vehicle odometer rule | Pending |
| 4 | `PropertyIssuanceReport`, `PhysicalCountSession`, `PropertyIncidentReport`, `UnserviceablePropertyReport` slices; FoundAtStation reconciliation workflow | Pending |
| 5 | Reports — ICS, PAR, RPCSEMEX, RPCPPE, RegSPI, RSPI, IIRUSP/IIRUP — branched off `AssetType` from one underlying query stream | Pending |
| 6 | Blazor UI under `Components/Pages/AssetRegister/` — `AssetRegistryPage`, `PropertyAccountabilityPage`, `PropertyIssuanceReportsPage`, plus count/incident/unserviceable/reports pages | Pending |
| 7 | Seeding, role/permission seed updates, final cleanup | Pending |

# Strict business rules (carried from refactoring guide)

1. **Form segregation:** system MUST NEVER generate a PAR for an SE item, or an ICS for a PPE item. Branching by `AssetType` on output documents.
2. **Depreciation enforcement:** PPE depreciates; SE does not. Application logic blocks depreciation when `AssetType == SE`.
3. **Audit-trail snapshots:** when an item is issued, counted, transferred, or reported lost, freeze `UnitCost`, `AssetType`, and `EstimatedUsefulLifeYears` into the line item. Do not rely solely on joining to current `AssetRegistry` state.
4. **Vehicle odometer rule:** on vehicle transfer, `endReading > startReading` strictly enforced (`DomainException` otherwise). Lands in Phase 3 alongside `PropertyAccountability` slices.
5. **Derecognition:** missing PPEs without available records of accountability can only be derecognized upon the grant of specific authority by COA (COA Circular 2020-006 Sec 8) — surfaced via `PropertyIncidentReport` workflow.

# Out of scope (this module)

- `AssetManagement` module — untouched.
- `AssetProcurement`, `Vehicle`, `Finance`, `MasterData`, `ProcurementPlanning`, `ProcurementAcquisition` — untouched (this module consumes one event from `AssetProcurement`, that's all).
- MAUI client — separate effort.
- Data migration from existing `AssetManagement` tables to `AssetRegister` — not in scope; if needed later it becomes its own phase.

