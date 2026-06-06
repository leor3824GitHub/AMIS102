# AssetRegister Module Documentation

> Comprehensive guide to the AssetRegister bounded context and implementation plan.

---

## Overview

The **AssetRegister** module introduces a new unified asset domain model running in parallel with the existing `AssetManagement` module (which remains unmodified). The goal is to collapse the parallel SE-track (ICS / SMIR / RRSP) and PPE-track (PAR / PPEIR / RRP) onto one unified backbone while keeping COA-compliant *output* driven by `AssetType` branching at the edges.

### Key Design Principles

- **One `AssetRegistry` row = one physical unit** with a unique `PropertyNo` (no `Quantity` field, unlike `TangibleItem`)
- **Unified current-state layer** — `AssetRegistry` (master ledger), `PropertyAccountability` (unified ICS+PAR), `PropertyIssuanceReport` (unified SMIR+PPEIR)
- **Additive-only approach** — existing `AssetManagement` module unchanged; this module coexists in parallel
- **COA-compliant output** — separate document entities for different form types (ICS vs PAR, SMIR vs PPEIR) but driven from unified domain

---

## Settled Decisions

| # | Decision | Choice |
|---|----------|--------|
| Module name | Bounded context name | **`AssetRegister`** (aggregate inside it is `AssetRegistry`) |
| Coexistence | Relationship to existing `AssetManagement` | Parallel module, no edits to `AssetManagement` |
| Property catalog source | Catalog location | **2a** — duplicate a slim catalog inside `AssetRegister` |
| Counters | Number generators | Fresh `PropertyCodeCounter` inside `AssetRegister` |
| Source of new asset rows | Integration point | **4b** — consume `AssetIARAcceptedEvent` from `AssetProcurement` |
| Blazor UI location | UI folder organization | New folder `Components/Pages/AssetRegister/`, new top-level nav section "Asset Register" |
| First-cut scope | Phase 1 deliverables | **6a** — domain + EF + migration skeleton only. No features, no UI |
| DB schema name | Database schema | `asset_register` |
| Tests | Test project | Skeleton in Phase 1; tests filled in alongside features |

---

## Aggregates in This Module

| Aggregate | Replaces (in `AssetManagement`) | Notes |
|-----------|-----------------------------------|-------|
| `AssetRegistry` | `TangibleItem` + `PropertyItemCatalog` + `TangibleInventory` | Master record, adds `LifecycleState`, current custodian/location, `Version` |
| `PropertyAccountability` + lines | `InventoryCustodianSlip`+`ICSItem` and `PropertyAcknowledgementReceipt`+`PARItem` | Enum-discriminated by `AccountabilityType` |
| `PropertyIssuanceReport` + lines | `SemiExpendableIssuanceRecord`+`SMIRItem` and `PPEIssuanceReport`+`PPEIRItem` | Enum-discriminated by `ReportType` |
| `PhysicalCountSession` + entries | Same name in `AssetManagement` | Supports `FoundAtStation` with null `AssetRegistryId` |
| `PropertyIncidentReport` + items | Same name in `AssetManagement` | Snapshots CRC, marks assets `UnderInvestigation` |
| `UnserviceablePropertyReport` + items | Same name in `AssetManagement` | Drives lifecycle → `Disposed` |
| `PropertyItemCatalog` | Slim copy of `AssetManagement` catalog | Local SKU table |
| `PropertyCodeCounter` | Local counter | Mints `PropertyNo` / `ICS No` / `PAR No` / `ITR No` per COA formats |

> **Note:** Returns are **not** a separate aggregate — folded into the `PropertyAccountability` lifecycle.

---

## Core Entities (High Level)

### AssetRegistry (Master Ledger)

The unified record for a single physical asset. One row = one unit.

**Key fields:**
- `PropertyNo` (unique per tenant, COA 2020-006 format)
- `AssetType` (SE | PPE), `Category`, `Description`
- `FundCluster`, `UacsObjectCode`
- `AcquisitionDate`, `UnitCost`, `EstimatedUsefulLifeYears`
- `AccumulatedDepreciation` (INVARIANT: AssetType == SE ⇒ 0)
- `LifecycleState` (Available | Assigned | UnderInvestigation | Unserviceable | Disposed)
- `CurrentCustodianId`, `CurrentLocationId`, `CurrentAccountabilityId`
- `SourceIARId`, `SourcePurchaseOrderId` (provenance tracking)

**State transitions:**
```
Available ──(Assign)──► Assigned ──(Transfer)──► Assigned
	│                       │
	│                   (Return)
	▼                       ▼
   ...                  Available
							│
						(Investigate)
							▼
					  UnderInvestigation ──(Recover/Settle/Grant Relief)──► Available or Disposed
							│
						(Dispose)
							▼
						  Disposed (terminal)
```

**Methods:**
- `Register()` — factory; emits `AssetRegisteredEvent`
- `AssignTo()` — Available → Assigned; emits `AssetIssuedEvent`
- `Transfer()` — Assigned → Assigned; emits `AssetTransferredEvent`
- `ReturnToAvailable()` — clears custodian/location/accountability; emits `AssetReturnedEvent`
- `MarkUnderInvestigation()` — Assigned → UnderInvestigation; emits `AssetLostEvent`
- `MarkUnserviceable()` — any non-Disposed → Unserviceable; emits `AssetUnserviceableEvent`
- `Dispose()` — terminal; emits `AssetDisposedEvent`

### PropertyAccountability (Unified ICS + PAR)

Represents a single slip (ICS or PAR) and all line items issued under it.

**Key fields:**
- `DocumentNo` (COA format: SPLV-/SPHV-YYYY-MM-NNNN or PAR-YYYY-MM-NNNN)
- `AccountabilityType` (SE_ICS | PPE_PAR)
- `IssuedOn`, `ExpiresOn` (REQUIRED iff SE_ICS; default = IssuedOn + 3 years)
- `Status` (Active | Renewed | Returned | Cancelled)
- `IssuedBy`, `ReceivedBy` (EmployeeRef value objects capturing name + designation at signing time)
- `Lines` collection (one per asset issued under this slip)

**Line status:** Active | Returned | Lost

**Methods:**
- `Issue()` — factory; emits `AssetIssuedEvent` per line
- `Renew()` — creates new accountability superseding this one; transitions `this.Status` → Renewed
- `ReturnLines()` — marks lines as Returned; if all returned, header → Returned
- `ReportLineLost()` — marks line as Lost; emits `AssetLostEvent`
- `Cancel()` — only valid if no line has been Returned

**INVARIANT:** Every line's asset `AssetType` must match `AccountabilityType` (SE → SE_ICS, PPE → PPE_PAR).

### PropertyIssuanceReport (Unified RSPI/SMIR + PPEIR)

Represents a monthly or periodic issuance roll-up (RSPI for SE, PPEIR for PPE).

**Key fields:**
- `ReportNo` (COA format: RSPI-YYYY-MM-NNNN or PPEIR-YYYY-MM-NNNN)
- `ReportType` (SMIR | PPEIR — validated on Post)
- `FundCluster`
- `PeriodFromDate`, `PeriodToDate`
- `Status` (Draft | Posted)
- `PreparedBy`, `CertifiedBy`, `PostedBy`, `PostedOn`
- `Lines` (each line traces to a source `PropertyAccountabilityLine`)

**Methods:**
- `CreateDraft()` — factory
- `AddLines()` — snapshots from accountability lines
- `RemoveLine()` — only while Draft
- `Post()` — validates all lines match `ReportType`; immutable thereafter

**INVARIANT on `Post()`:** All lines' `Snapshot.AssetType` must agree with `ReportType` (SMIR ⇒ all SE; PPEIR ⇒ all PPE).

### PhysicalCountSession (Asset Counting & Reconciliation)

Models the periodic physical inventory count (produces RPCSEMEX / RPCPPE).

**Key fields:**
- `Code` (e.g. PCS-YYYY-NNNN)
- `Scope` (PPEOnly | SEOnly | Both)
- `Status` (Ongoing | Reconciled | Closed)
- `FundCluster`
- `StartedOn`, `ClosedOn`, `AsAt` (DateOnly)
- `ConductedBy`, `ApprovedBy`, `WitnessedBy` (EmployeeRef collection)
- `Entries` (one per asset scanned or found at station)

**Entry conditions:** InGoodCondition | NeedingRepair | Unserviceable | Missing | FoundAtStation

**Methods:**
- `Start()` — factory
- `RecordEntry()` — for known assets
- `AddFoundAtStationEntry()` — entry with `AssetRegistryId == null` (new asset found during count)
- `MarkMissing()` — entry with `Condition == Missing`
- `Reconcile()` — FoundAtStation entries materialize new `AssetRegistry` rows; Missing entries trigger draft `PropertyIncidentReport`; Status → Reconciled
- `Close()` — from Reconciled only; updates `AssetRegistry.CurrentCondition` per entry

### PropertyIncidentReport (Loss/Theft/Damage Documentation)

Models RLSDDSP and related incident tracking for lost, stolen, damaged, or destroyed assets.

**Key fields:**
- `IncidentNo` (COA format: RLSDDSP-YYYY-MM-NNNN)
- `IncidentType` (Lost | Stolen | Damaged | Destroyed)
- `IncidentDate`
- `FundCluster`
- `AccountableOfficer` (EmployeeRef)
- `PoliceNotified`, `PoliceStation`, `PoliceBlotterRef`
- `NotarizedOn`, `NotaryDocNo` (required for RLSDDSP)
- `Status` (Filed | UnderInvestigation | Resolved | Closed)
- `ReliefGrantedOn`, `AmountSettled`, `RecoveredOn`
- `Items` (each item references an `AssetRegistry` and tracks its resolution)

**Item resolution:** Pending | Recovered | Paid | ReliefGranted | Derecognized

**Methods:**
- `File()` — factory; marks assets `UnderInvestigation`; flips accountability lines to `Lost`
- `NotifyPolice()`, `Notarize()`
- `RecordRecovery()` — asset returns to Available/Unserviceable; item → Recovered
- `RecordSettlement()` — asset → Disposed; item → Paid
- `GrantRelief()` — asset → Disposed; item → ReliefGranted
- `Close()` — all items resolved

### UnserviceablePropertyReport (Equipment Disposal)

Models IIRUSP and IIRUP — items identified as no longer serviceable and scheduled for disposal.

**Key fields:**
- `ReportNo` (IIRUSP-YYYY-MM-NNNN or IIRUP-YYYY-MM-NNNN)
- `ReportType` (IIRUSP | IIRUP)
- `AsAt` (DateOnly)
- `FundCluster`, `Station`
- `Status` (Draft | Submitted | InspectionDone | DisposalRecorded | Closed)
- `PreparedBy`, `InspectedBy`, `ApprovedBy` (EmployeeRef)
- `Items` (each references an `AssetRegistry`)

**Methods:**
- `CreateDraft()` — factory
- `AddItems()`, `RemoveItem()` — while Draft
- `Submit()` — transitions to Submitted; emits `UnserviceableReportSubmittedEvent`
- `RecordInspection()` — Submitted → InspectionDone
- `RecordDisposal()` — InspectionDone → DisposalRecorded; marks all items `Disposed`

---

## Value Objects

### PropertyNumber

Encapsulates COA 2020-006 format `YYYY-AA-BB-NNNN-CC`:
- `YYYY` = fiscal year
- `AA` = sub-major object code (e.g., 01 for PPE general, 02 for vehicles)
- `BB` = GL account code
- `NNNN` = sequential number within the year
- `CC` = checksum or location code

Supports parse, validate, and `ToString()`.

### AssetSnapshot

Frozen record of an asset's state at a point in time:
```
PropertyNo
Description
AssetType
UnitCost
Unit
EstimatedUsefulLifeYears
AcquisitionDate
UacsObjectCode
```

Used as owned entity on every accountability line, issuance report line, count entry, and incident item to eliminate field drift across the lifecycle.

### EmployeeRef

Captures employee identity + printed name + designation at a point in time (for signature blocks):
```
EmployeeId
SnapshotPrintedName
SnapshotDesignation
```

Ensures form printing is stable even if the employee's name or title changes later.

---

## Domain Services (Interfaces in Domain, Implementations in Data/)

- **`IPropertyNumberGenerator`** — Next(AssetType, AcquisitionDate, sub-major, GL account, locationCode) → PropertyNumber
- **`IAccountabilityNumberGenerator`** — NextIcs(AssetCategory, DateOnly) → string (SPLV/SPHV format); NextPar(DateOnly) → string
- **`IInventoryTransferNumberGenerator`** — Next(DateOnly) → string (ITR format)
- **`IIncidentNumberGenerator`** — Next(DateOnly) → string (RLSDDSP format)
- **`IUnserviceableReportNumberGenerator`** — Next(UnserviceableReportType, DateOnly) → string
- **`ICurrentReplacementCostCalculator`** — Compute(AssetRegistry, DateOnly asOf) → decimal (per COA 2022-004 §4.19)

All number generators operate against `PropertyCodeCounter` with optimistic concurrency + retry.

---

## Phased Implementation Plan

### Phase 1: Domain + EF + Migration Skeleton (✅ Current)

**Deliverables:**
- All six aggregate roots and supporting value objects
- EF Core configurations, `AssetRegisterDbContext`
- Initial migration for schema creation
- Test project skeleton
- Module integration into Playground.Api

**Status:** Domain + EF complete; migration skeleton ready.

### Phase 2: Feature Slices for Registry Management

CRUD and search operations for `AssetRegistry`:
- Create asset (manual entry or from IAR event)
- Get by PropertyNo / by Custodian / by Location
- Update lifecycle state
- Search/filter/pagination

### Phase 3: Accountability & Issuance Features

- Issue accountability (ICS or PAR)
- Return lines
- Renew accountability
- Create issuance reports (RSPI/PPEIR)
- Post issuance reports

### Phase 4: Count, Incident & Unserviceable Reports

- Physical count sessions
- Incident reporting & resolution
- Unserviceable property marking & disposal

### Phase 5: Advanced (Deferred)

- Vehicle profile tracking (odometer at issue/transfer/return)
- Repair history (SPLC column)
- Depreciation schedule integration
- Multi-currency support

---

## Important Guardrails

1. **No deletion/removal** of existing entities/endpoints in `AssetManagement`.
2. **Preserve existing API contracts** and route groups.
3. **Soft delete disabled** on all six runtime aggregates — lifecycle/status enums carry the closure semantics.
4. **Soft delete remains** on `PropertyItemCatalog` only.
5. **All monetary fields:** `decimal(18,2)` in EF.
6. **Business dates:** `DateOnly`. Audit timestamps: `DateTimeOffset`.
7. **Concurrency token** (`byte[] Version`) on `AssetRegistry`, `PropertyAccountability`, `PropertyIssuanceReport`.
8. **TenantId** stamped on parent aggregates; Finbuckle global query filter applies at root.

---

## Build & Test Status

- ✅ Domain + EF + migration complete
- ✅ Full solution builds with zero warnings
- 🔄 Phase 1 integration ready for feature development

---

## Related Documentation

- **Asset Acquisition Flow** — See `ASSETMANAGEMENT-DOCUMENTATION.md` for end-to-end procurement → registry flow
- **Report Alignment** — See `ASSETMANAGEMENT-DOCUMENTATION.md` Part 3 for ICS/PAR/SMIR/PPEIR form validation
- **Development Conventions** — See `CLAUDE.md` for patterns, validators, handlers, and module registration
- **Blazor UI** — UI folder `Components/Pages/AssetRegister/` (Phase 2+)

---

## References

- COA 2020-006 (Property Numbering)
- COA 2022-004 (Accounting Policies & Guidelines)
- SOP GS-PD26 (Inspection & Acceptance Report)
- Annexes A.1–A.10 (Forms referenced in design)

