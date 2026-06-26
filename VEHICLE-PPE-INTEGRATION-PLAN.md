# Vehicle = PPE Asset — Integration Plan

> Model a Vehicle as a PPE asset that is **enrolled** from the canonical Asset Register
> (`AssetRegistry`), acquired either by **purchase** or **transfer from Central Office (CO)**.
> A vehicle is issued on a **PAR** (PPE accountability); the **accountable officer** can manage
> fuel/odometer (etc.) only for the vehicle(s) PAR'd to them (accepted-PAR gate).
> Repairs are **PPE-wide (owned by AssetRegister)**: the NFA **Request for Pre/Post Repair Inspection**
> (Exhibit 6) is **mandatory for every PPE repair** — pre-inspection → (optional PR/PO-JO/BUR/DV for
> outsourced) → post-inspection → custodian acceptance. **Repair history is sourced from accepted RPRIs.**
> Status: **PLAN (no code yet)** · Date: 2026-06-25 · Revision: **5**

---

## Rev 5 changelog — repairs are PPE-wide (AssetRegister), RPRI mandatory

- **Repairs move to AssetRegister (PPE-wide)**, keyed by `AssetRegistryId` — any PPE asset, not just
  vehicles. Vehicle's `RepairRecord` is **removed**; Vehicle surfaces its vehicles' repairs from
  AssetRegister. (Supersedes rev 4's "owned by the Vehicle module".)
- **RPRI is MANDATORY for every PPE repair** (NFA SOP: "all PPE shall have RPRI before repairs"). The
  **procurement/finance chain (PO-JO + BUR/DV) is the optional part** (outsourced vs in-house).
- **Repair history is sourced directly from accepted RPRI records** (no separate history table) — since
  every repair requires an RPRI, the accepted-RPRI set is the authoritative per-asset history.

## Rev 4 changelog — repair workflow (NFA Exhibit 6: Request for Pre/Post Repair Inspection)

Grounded in the actual NFA practice + the Exhibit 6 form:

- Repair is **procurement-driven**: driver **PR** → supply **canvass** → **PO / Job Order** → **BUR + DV**
  → repair → **Post-Repair Inspection** → **Property Custodian accepts (Done)**.
- The central artifact is the **Request for Pre/Post Repair Inspection (RPRI, Exhibit 6)** — owned by the
  Vehicle module. The thin `RepairRecord` is **evolved** into this richer aggregate.
- **Inspection & acceptance = the RPRI's Pre-/Post-Repair sections** (vehicle-native), *not* the goods
  IAR. Resolves the earlier IAR-fit question.
- **PO/JO No, Invoice No, Amount per JO / Payable, BUR/DV No are captured on the RPRI** (Post-Repair
  section); the **PropertyNo query** assists *discovery* of the related PO/JO/BUR/DV. Resolves the
  "store id vs query" fork.
- **History = the Accepted RPRI (immutable, lock-in-place)**; a new request's **Nature/Date of Last
  Repair auto-fills from the latest accepted RPRI**. Resolves snapshot-vs-lock.
- **Description of Property** auto-fills from the enrolled vehicle + PPE asset — reinforces Phases 1–3.

## Rev 3.1 changelog — third-pass refinements (reuse existing AssetRegister contracts)

Verified against code; **less new surface than rev 3 implied**:

- **No new "vehicle-class assets" query.** Extend the existing `SearchAssetsQuery` with a `PropertyClass`
  filter; the enroll picker uses `SearchAssetsQuery(AssetType: PPE, PropertyClass: "LT")` →
  existing `AssetRegistrySummaryDto`.
- **No new by-id query for enrollment.** `EnrollVehicle` fetches via existing
  `GetAssetRegistryQuery(Guid Id)` (`AssetRegistryDto` already carries `PropertyClass`/`UnitCost`/
  `AcquisitionDate`/`LifecycleState`).
- **Accountable officer == `AssetRegistry.CurrentCustodianId`** (set at issuance to
  `ReceivedBy.EmployeeId`). So "my vehicles" / authorization derive from one field; the report's officer
  name reuses the resolution already in `AssetScanDetailDto`.
- **`GetMyAccountableAssetIdsQuery` is Mediator-only** (no REST endpoint) and trivially implemented.
- **Permission model confirmed:** `AmisPermission(...) + PermissionConstants.Register(...)`; mirror
  `MyAccountability` (View `IsBasic`, actions registered/role-assigned) for `MyVehicle`.
- **Gate decided: accepted-PAR** — fuel/odometer requires an Active (accepted) PAR line, so
  `GetMyAccountableAssetIdsQuery` filters active PAR lines (not `CurrentCustodianId`); see Phase 8.2.

## Rev 3 changelog (PAR + accountable-officer access)

- **Vehicle gets a PAR** (`AccountabilityType.PPE_PAR`) via the *existing* AssetRegister
  `IssueAccountabilityCommand` — no new accountability mechanism. The PAR line already captures
  plate/engine/chassis/odometer (`IssueAccountabilityLineRequest`); the accountable officer is
  `ReceivedBy` and takes force on `Accept` (PendingAcceptance → Active).
- **Accountable-officer-scoped self-service** added as **Phase 8**: the accountable officer (active PAR
  holder) can view/record fuel & odometer for *their* vehicles only. Mirrors the existing
  `MyAccountability` self-service split (officer-wide vs "mine").
- **Identity resolution reused:** server resolves the current employee via `ICurrentUser` +
  `CurrentEmployeeResolver` (same as `GetMyAccountabilities`) — endpoints carry no employee id.
- **Authorization = live check**, not a mirror (security-sensitive; avoids stale grants).
- **Assignment fields dropped for consistency:** `Vehicle.AssignedDriver/Department/
  AccountableOfficerTitle`, the `AssignTo` method, and the entire `AssignVehicle` slice are **removed**.
  The **PAR holder** is the single accountable officer (resolved live from AssetRegister). The Motor
  Vehicle Inventory keeps its *Accountable Officer* column but sources it from the active PAR.

---

## Second-pass changelog (what changed vs rev 1)

- **Added an ownership/boundary section** resolving the overlap between `Vehicle` (operations) and
  `AssetRegistry` accountability (`VehicleAccountabilityProfile` already stores plate/engine/chassis/
  odometer). This was the biggest gap in rev 1.
- **Confirmed no circular dependency:** AssetRegister has **zero** references to Vehicle, and
  `AssetRegister.Contracts` only references `Eventing.Abstractions` + `Shared`. So `Modules.Vehicle`
  (impl) → `AssetRegister.Contracts` is impl→contracts and safe.
- **Confirmed Mediator wiring:** `Program.cs` already registers `AssetRegisterModule` and
  `VehicleModule`; cross-module `Send` works with no new wiring.
- **Unique index corrected** to composite `(TenantId, AssetRegistryId)` to match the existing
  multitenant index pattern in `VehicleConfiguration`.
- **Concrete contract/command deltas** specified (`EnrollVehicleCommand`, `VehicleDto`,
  `UpdateVehicleCommand`, enrollment-candidate lifecycle filter).
- **Reporting clarified:** the cost "switch" is satisfied by the asset-sourced mirror; no PDF/handler
  change, label stays "Acquisition Cost."
- **Added impacted consumers:** Blazor `VehicleFormDialog`/`VehiclesPage`/`VehicleDetailPage`,
  generated `IVehicleClient`, and `Vehicle.Tests`.

---

## Background & current gap

A vehicle is fundamentally a PPE asset. It enters the books either by **purchase** (PPE Receiving
Report) or **transfer from CO** (PPERR with `ReceiptType.Transfer`, preserving the original
acquisition date for COA depreciation continuity). Both paths already materialize an
`AssetRegistry` PPE row.

Today the two are disconnected:

- `Vehicle` (`src/Modules/Vehicle/Modules.Vehicle/Domain/Vehicles/Vehicle.cs`) is a **standalone
  aggregate** created free-form ("New Vehicle"), with its own `AcquisitionCost` and **no link** to
  the PPE record.
- `AssetRegistry` (`src/Modules/AssetRegister/Modules.AssetRegister/Domain/Assets/AssetRegistry.cs`)
  is the canonical PPE record: PropertyNo, UnitCost, AcquisitionDate, **COA depreciation**,
  lifecycle, accountability, provenance.

**Note:** "transfer from CO" is *not* missing — `ReceiptType = Purchase | Transfer | Donation | Other`
already exists on the Receiving Report, and `CreateReceivingReportCommandHandler` already carries the
original acquisition date for transfers/donations.

---

## Decisions locked

1. **Enrollment style: B** — admin picks an unenrolled PPE motor-vehicle asset; no free-form create.
2. **Vehicle-class detection: auto by property class** — `AssetRegistry.PropertyClass == "LT"`
   (Motor Vehicles, COA Account 10606010).
3. **Strictness: hard-block** — `Vehicle.AssetRegistryId` is **required & unique**; enrollment is the
   only path. Safe because there is no legacy data once the seed is removed and nothing is backfilled.
4. **Existing data:** remove the standalone vehicle seed; seed vehicles **as PPE assets** in
   AssetRegister, then enroll them. Reporting **switches its cost source** to the PPE asset; the report
   **column label stays "Acquisition Cost."**

---

## Ownership & boundary (NEW — read before implementing)

`Vehicle` and `AssetRegistry` both touch "vehicle"; they must not become two sources of truth.
`AssetRegistry` already has a `VehicleAccountabilityProfile` (plate/engine/chassis + odometer at
issue/return) attached to a `PropertyAccountabilityLine`.

| Concern | Owner (source of truth) | Notes |
| ------- | ----------------------- | ----- |
| PropertyNo, UnitCost (acquisition cost), AcquisitionDate | **AssetRegistry** | Mirrored read-only onto Vehicle at enrollment |
| Depreciation, carrying amount, impairment | **AssetRegistry** | Vehicle never computes these |
| Accounting lifecycle (Available/Assigned/Disposed/TransferredOut/Unserviceable/UnderInvestigation) | **AssetRegistry** | Formal property accountability (PAR/custodian) |
| Plate / engine (motor) / chassis no., make/model/year, type, fuel, cylinders, displacement | **Vehicle** | The accountability `VehicleAccountabilityProfile` is a point-in-time **snapshot**, not the master |
| Operational status (Active/UnderRepair/Retired/Decommissioned) | **Vehicle** | Fleet operations only |
| Odometer (running), maintenance, repairs, fuel logs | **Vehicle** | `VehicleAccountabilityProfile.OdometerAtIssue/Return` is a separate point-in-time value |
| **Accountable officer** (who answers for the vehicle) | **AssetRegistry PAR** (`ReceivedBy`, active) | Authorizes self-service fuel/odometer; resolved **live**, not mirrored. Established by issuing a `PPE_PAR` and the officer accepting it |
| Driver / dept assignment | **— removed** | Dropped for consistency: the PAR holder is the *single* accountable officer. `Vehicle.AssignedDriver/Department/AccountableOfficerTitle`, `AssignTo`, and the `AssignVehicle` slice are removed |

**Lifecycle coupling decision:** the two lifecycles are **decoupled in this phase**. Changing
`Vehicle.Status` (retire/decommission) does **not** mutate the PPE asset; disposal/transfer-out remain
AssetRegister actions. *(Future option: Vehicle consumes `AssetRegisterIntegrationEvents.AssetDisposed`
/ `AssetTransferredOut` to auto-decommission — out of scope here.)*

**Enrollment ≠ PAR:** enrolling links the PPE asset to a fleet vehicle (asset may be `Available`, not
yet issued). Issuing a PAR assigns an accountable officer. They are independent steps; a vehicle with
no active PAR simply has no accountable officer (only admins with `FuelOdometer.*` manage it).

---

## Key facts grounding this plan

- Property class `LT` = "Motor Vehicles" (`MasterDataDbInitializer`), 2-char COA code, stored on
  `AssetRegistry.PropertyClass` via `PropertyItemCatalog.DefaultPropertyClass`.
- **Data hygiene risk:** the current AssetRegister catalog seed uses ad-hoc class strings
  (`"ICT"`, `"EQUIP"`, `"FURN"`) — **not** COA 2-char codes — so auto-detection only works if
  vehicle catalog items carry `DefaultPropertyClass = "LT"`.
- `VehicleConfiguration` uses `.IsMultiTenant()` + a **named** `"SoftDelete"` query filter, and
  composite `(TenantId, X)` indexes — new index must follow the same shape.
- Vehicle module currently has **zero** references to AssetRegister. Per architecture rules it MAY
  reference `Modules.AssetRegister.Contracts` but NOT AssetRegister internals (precedent: AssetRegister
  already references MasterData.Contracts + ProcurementAcquisition.Contracts).
- There is intentionally **no cross-module FK**; links are by value (PropertyNo / IAR number).

---

## Phase 1 — AssetRegister.Contracts: expose vehicle-class PPE assets
*(reuse existing contracts; minimal additions)*

- **Extend the existing `SearchAssetsQuery`** with an optional `PropertyClass` filter (query + handler).
  The enrollable list is `SearchAssetsQuery(AssetType: PPE, PropertyClass: "LT")` → existing
  `AssetRegistrySummaryDto` (Id, PropertyNo, Description, UnitCost, AcquisitionDate, LifecycleState,
  CurrentCustodianId) — enough for the picker. **No new query/DTO invented.** (`SearchAssetsQuery`
  already excludes TransferredOut by default; confirm Disposed handling, else filter in Vehicle.)
- `VehicleClassCode = "LT"` as a **named constant** in `AssetRegisterModuleConstants`.
- Single-asset fetch for enrollment reuses existing **`GetAssetRegistryQuery(Guid Id)`** (`AssetRegistryDto`
  carries `PropertyClass`, `AssetType`, `UnitCost`, `AcquisitionDate`, `LifecycleState`).
- *(Optional, deferrable)* acquisition **source** (Purchase vs Transfer-from-CO) is on the
  `ReceivingReport` (`ReceiptType`), not the asset; surface later by joining.

## Phase 2 — Vehicle domain + persistence

- Add to `Vehicle`: `AssetRegistryId` (Guid, required) + cached read-mirrors copied at enrollment:
  `PropertyNo` (string), `AcquisitionCost` (existing field; keep `decimal?` to minimize churn but
  always populated), `AcquisitionDate` (DateOnly).
- Replace `Vehicle.Create(...)` with
  `Vehicle.Enroll(assetRegistryId, propertyNo, acquisitionCost, acquisitionDate, plate, make, model, year, type, odometer, specs…, notes)`.
  Keep raising `VehicleCreatedEvent` (rename to `VehicleEnrolledEvent` optional — defer).
- `Update(...)` no longer accepts acquisition cost (asset-owned, read-only).
- **Remove** assignment state: `AssignedDepartmentId/AssignedDepartment`, `AssignedDriverId/
  AssignedDriver`, `AccountableOfficerTitle`, and the `AssignTo(...)` method.
- `VehicleConfiguration`: add `builder.HasIndex(v => new { v.TenantId, v.AssetRegistryId }).IsUnique();`
  and map `AssetRegistryId` (required), `PropertyNo` (maxlen), `AcquisitionDate`; **drop the assignment
  column mappings**.
- EF migration in `Migrations.PostgreSQL` (Vehicle context): add columns + unique index, **drop
  assignment columns**. Clean migration; dev DB disposable, **no backfill**.

## Phase 3 — Vehicle features

- **New:** `GetEnrollableVehicleAssets` query handler (in Vehicle) → calls `SearchAssetsQuery(AssetType:
  PPE, PropertyClass: "LT")` via Mediator, then **anti-joins** against local `Vehicles.AssetRegistryId`
  to drop already-enrolled assets. ("Enrolled" knowledge lives in Vehicle.)
- **Replace** `CreateVehicle` → `EnrollVehicle`:
  - **Contract:** `EnrollVehicleCommand(Guid AssetRegistryId, string PlateNumber, string Make,
    string Model, int Year, string Type, int Odometer = 0, string? Notes = null, …specs…)`. **No**
    `AcquisitionCost` (server reads it from the asset).
  - **Validator:** `AssetRegistryId` NotEmpty + the existing field rules.
  - **Handler:** fetch the asset via existing `GetAssetRegistryQuery(AssetRegistryId)`; reject if not
    found / not `PPE` / `PropertyClass != "LT"` / lifecycle `Disposed|TransferredOut` / already enrolled
    (local unique check; `DbUpdateException` on the unique index → 409 as race fallback); copy
    `UnitCost`+`AcquisitionDate`+`PropertyNo`; set specs.
  - **Endpoint:** keep local convention `WithName(nameof(EnrollVehicleCommand))` (unique). Reuse
    `VehiclePermissions.Vehicles.Create`.
- `UpdateVehicleCommand`: remove `AcquisitionCost`.
- **Delete** the `AssignVehicle` slice (`Features/v1/Vehicles/AssignVehicle/*`), remove its endpoint
  mapping from `VehicleModule`, and remove `AssignVehicleCommand` from Contracts.
- Drop assignment fields from `VehicleDto`, `VehicleReferenceDto`, `VehicleMapper`, and the `Lookups`
  handler; remove the `AssignedDepartmentId` filter from `SearchVehiclesQuery` +
  `SearchVehicleReferencesQuery` and their handlers.
- `Modules.Vehicle.csproj` → add `ProjectReference` to `Modules.AssetRegister.Contracts`.

## Phase 4 — Reporting (cost source switched, label unchanged)

- `GetMotorVehicleInventoryQueryHandler` already maps `AcquisitionCost: v.AcquisitionCost`. Because the
  mirror now originates from the PPE asset at enrollment, **the source is switched with no handler/PDF
  change**; the DTO field + PDF column label stay **"Acquisition Cost."**
- *(Alternative if you want a live read instead of a mirror: have the report handler join the asset via
  a cross-module query. Not recommended — `UnitCost` is immutable post-registration, so the mirror
  never goes stale, and it keeps the report a single-module query.)*

## Phase 5 — Seeding (remove standalone, seed as PPE)

- `VehicleDbInitializer`: **remove** the 6-vehicle standalone seed.
- `AssetRegisterDbInitializer`:
  - add a **motor-vehicle catalog item** with `DefaultPropertyClass = "LT"`, UACS `10606010`;
  - add **PPERR vehicle line(s)** (optionally one `ReceiptType.Transfer` line to demo transfer-from-CO)
    → materializes PPE assets with `PropertyClass = "LT"`.
- Vehicle seed (rewritten): call `GetEnrollableVehicleAssets`, then **enroll** each seeded asset as a
  Vehicle (plate/engine/specs from the old seed values). Dogfoods the exact enrollment path.
- **Seed ordering:** AssetRegister must seed assets before Vehicle enrolls. Verify hosted-service order
  (`Provisioning/*DbInitializerHostedService`); if not guaranteed, Vehicle seed degrades gracefully
  (no assets → no vehicles, logged).

## Phase 6 — Blazor UI (`Components/Pages/Vehicle`)

- `VehiclesPage.razor`: "New Vehicle" → **"Enroll Vehicle"** (gate on
  `VehiclePermissions.Vehicles.Create`).
- `VehicleFormDialog.razor`:
  - **Create/enroll mode:** step 1 pick an unenrolled `LT` PPE asset (new `IVehicleClient`
    `GetEnrollableVehicleAssetsAsync`) showing PropertyNo/description/cost/date; prefill Make/Model from
    the asset's Brand/Model; step 2 enter plate/engine/specs. Submit `EnrollVehicleCommand`.
  - **Remove** the `AcquisitionCost` numeric field from the form (now asset-owned).
  - Use AMIS compact wrappers; save as **UTF-8**.
- `VehicleDetailPage.razor`: show PropertyNo / Acquisition Cost / Acquisition Date **read-only**
  (asset-owned).
- Regenerated/updated `IVehicleClient`: `CreateVehicleAsync` → `EnrollVehicleAsync` +
  `GetEnrollableVehicleAssetsAsync`.

## Phase 7 — Verify

- `dotnet build src/AMIS.Framework.slnx` (0 warnings) + `dotnet test`.
- Update `Vehicle.Tests` (`CreateVehicleCommand`→`EnrollVehicleCommand`; add enroll-validation +
  anti-join tests). Run `Architecture.Tests` (Vehicle→AssetRegister.Contracts is allowed).
- Endpoint-name uniqueness grep (per `api-conventions.md`).
- Run and confirm: no seeded standalone vehicles; enrollment lists only unenrolled `LT` PPE assets;
  report shows asset cost under the "Acquisition Cost" label.

## Phase 8 — PAR accountability & accountable-officer self-service

### 8.1 PAR issuance (clarification — no new mechanism)
A vehicle's PPE asset is issued on a **PAR** via the existing `IssueAccountabilityCommand`
(`AccountabilityType.PPE_PAR`); the line captures plate/engine/chassis/odometer
(`IssueAccountabilityLineRequest`). Accountable officer = `ReceivedBy`, active after `Accept`. Vehicles
continue to appear in the officer's existing **"My Accountability"** list (the document/acknowledge/
return view).

### 8.2 AssetRegister.Contracts — self-service query (additive, Mediator-only, no REST endpoint)
- **Gate semantics (DECIDED): accepted-PAR.** The officer must **accept** the PAR (via "My
  Accountability") before they can log fuel/odometer. `CurrentCustodianId` alone is *insufficient* — it
  is set at issuance while the PAR is still `PendingAcceptance`.
- `GetMyAccountableAssetIdsQuery(AssetType? assetType = PPE) → IReadOnlySet<Guid>`: resolve the employee
  via `ICurrentUser` + `CurrentEmployeeResolver` (reused), then return `AssetRegistryId`s from **Active**
  PAR lines — `PropertyAccountability.Status == Active && AccountabilityType == PPE_PAR &&
  ReceivedBy.EmployeeId == employee`, with the line `LineStatus == Active`. Carries no employee id.
- For display, the report's officer name reuses the current-accountability resolution already in
  `AssetScanDetailDto` (`AccountableOfficerId/Name/Designation`) — see 8.5.

### 8.3 Vehicle self-service permissions (Contracts)
- Add `VehiclePermissions.MyVehicle`: `View`, `RecordFuelOdometer` (+ optionally `RecordMaintenance`).
  Mirror the `MyAccountability` convention; bundle into the "Employee" role.
- **Must register these in the permission catalog as `IsBasic`** or `RequirePermission` → 403 for
  everyone (see memory *Permission Must Be In Catalog*).

### 8.4 Vehicle self-service feature slices
- `GetMyVehicles` handler: calls `GetMyAccountableAssetIdsQuery`, intersects with
  `Vehicles.AssetRegistryId` → the officer's vehicles. `RequirePermission(MyVehicle.View)`.
- Scoped fuel/odometer — **dedicated self-service endpoints** (mirroring the My* split):
  `RecordMyVehicleFuelOdometer`, `GetMyVehicleDailyUsage`. Each verifies the target vehicle's
  `AssetRegistryId` ∈ the accountable set; else `CustomException(403 Forbidden)` "This vehicle is not
  assigned to you." Existing admin `FuelOdometer.*` endpoints remain for property staff (officer-wide).

### 8.5 Reporting/display
- `MotorVehicleInventoryItemDto` keeps its `AccountableOfficer` + `AccountableOfficerTitle` columns, but
  `GetMotorVehicleInventoryQueryHandler` sources them from the current accountable officer
  (`CurrentCustodianId` → name/designation) instead of the removed `AssignedDriver`. To avoid N+1 across
  the report, add a small batch query `GetAccountableOfficersByAssetIdsQuery(ids) → Map<Guid,
  EmployeeRefDto>` in AssetRegister.Contracts, reusing the same current-accountability resolution as
  `AssetScanDetailDto`. PDF / FastReporting consumers unchanged (same DTO shape).

### 8.6 Blazor — "My Vehicle" entry point (decision)
**Recommendation: add a dedicated "My Vehicle" menu** for operational logging (fuel/odometer, view
usage), gated by `MyVehicle.*` — **while** vehicles also remain in the existing "My Accountability"
list. Rationale: the two are different concerns (operational logging vs accountability-document
acknowledge/return), use different permissions, and fuel/odometer is a frequent, focused workflow that
would be buried inside an accountability document detail. See the discussion note in the response.

## Phase 9 — PPE Repair workflow: Request for Pre/Post Repair Inspection (RPRI, NFA Exhibit 6)

**Decided:** repairs are **PPE-wide**, owned by **AssetRegister** (keyed by `AssetRegistryId`); the
**RPRI pre/post inspection is MANDATORY for every PPE repair** (NFA SOP — "all PPE shall have RPRI before
repairs"); the **procurement/finance linkage (PO-JO + BUR/DV) is optional** — present for outsourced
repairs, absent for in-house. The Vehicle module's `RepairRecord` is **removed**; Vehicle surfaces its
vehicles' repairs from AssetRegister.

### 9.1 Aggregate — `PropertyRepair` (AssetRegister), keyed by `AssetRegistryId`
Any PPE asset can have a repair. Sections map to Exhibit 6:
- **Description of Property** — from the asset (Type/PropertyClass, Brand/Model, Serial No.,
  **Property No.**, Acquisition Date, Acquisition Cost) + **Nature/Date of Last Repair** (history, 9.4).
  Vehicle-specific **Engine/Chassis No. + Odometer** are *optional* request fields (blank for
  non-vehicle PPE; pre-filled by the Vehicle module for vehicles).
- **Defects/Complaints** — Nature & Scope of Work, Parts to be Supplied/Replaced, Requested By.
- **Pre-Repair Inspection (MANDATORY)** — Findings, Pre-Inspected By (Property Inspector), Noted By
  (Head of Office), dates. **A repair cannot proceed without it** (NFA SOP).
- **Post-Repair Inspection** — Repair Shop/Contractor, **Job Order/Contract No.**, Invoice No.+date,
  **Amount per JO/Payable**, Findings, Post-Inspected By, **Custodian acceptance**.
- **Procurement/finance refs (optional)** — PR/PO-JO/BUR/DV/Invoice numbers; outsourced only.

### 9.2 Lifecycle (RPRI-gated; finance optional)
```
Requested + Pre-Repair Inspection (MANDATORY — all PPE)
   → if outsourced: PR → canvass → PO/JO → BUR + DV   (optional chain)
   → Repair performed
   → Post-Repair Inspection
   → Property Custodian ACCEPTS = DONE (immutable)
   → asset's Repair History entry
```
Status: `Requested → PreInspected → Repaired → PostInspected → Accepted`. Pre-inspection is required to
leave `Requested`. On Accept, **lock-in-place** → it *is* the history entry.

### 9.3 Module placement & coupling
- **AssetRegister owns** `PropertyRepair` (domain, data, features, permissions, RPRI PDF). **No new
  cross-module references needed** — the RPRI just *stores* the captured PR/PO-JO/BUR/DV/Invoice numbers
  and amounts (Exhibit 6 is a fill-in form). AssetRegister does **not** reference BudgetDisbursement.
- **Procurement (PR/canvass/PO-JO) and Finance (BUR/DV) stay fully decoupled.** **Discovery is a
  UI-layer convenience:** the Blazor RPRI form calls the existing BudgetDisbursement / ProcurementAcquisition
  **API clients** with `keyword = PropertyNo` to find/pick the right BUR/DV/PO-JO, then submits the chosen
  numbers to AssetRegister. Convention: those docs' particulars carry the PropertyNo.
  *(If live server-side enrichment — e.g. showing DV "Paid" status on the RPRI — is ever needed, add the
  Contracts ref then.)*
- **Vehicle module** removes its `RepairRecord` domain + repair slices/endpoints/permissions; it
  **surfaces its vehicles' repairs** via AssetRegister contracts (filter by the vehicle's
  `AssetRegistryId`) and pre-fills Engine/Chassis/Odometer on request. **Maintenance schedules +
  fuel/odometer stay in Vehicle** (vehicle-specific).
- RPRI generated as a **printable PDF** (Exhibit 6 layout) to attach to the PR.

### 9.4 Repair history sourced from RPRI (no separate history table)
- **History = a query over Accepted `PropertyRepair` (RPRI) records** for the asset — the RPRI *is* the
  repair history (lock-in-place on acceptance, immutable). Since every PPE repair (in-house *or*
  outsourced) requires an RPRI, the accepted-RPRI set is the complete, authoritative repair history.
  No separate snapshot/history table.
- A new RPRI auto-fills **Nature/Date of Last Repair** from the asset's most recent accepted RPRI
  (its scope-of-work + accepted date) — "determined upon request".
- "Repair History" is exposed per asset (AssetRegister) and surfaced per vehicle (Vehicle module, by
  `AssetRegistryId`).

### 9.5 Roles / permissions (AssetRegister)
- Add `AssetRegisterPermissions.Repair`: `View`, `Request`, `Inspect` (pre/post), `Accept` — register in
  the catalog. Requestor may be a driver via self-service scoped to their accepted-PAR asset (Phase 8).
- **Property Inspector** = pre/post inspection; **Property Custodian** = acceptance (Done).

### 9.6 Domain / data / UI
- New `PropertyRepair` aggregate + EF config + migration in AssetRegister. **Remove** Vehicle
  `RepairRecord` + its slices/endpoints/`Repairs.*` permissions/tests (dev DB disposable).
- AssetRegister.Contracts: repair DTOs + commands (`RequestRepair`, `RecordPreRepairInspection`,
  `RecordPostRepairInspection`, `AcceptRepair`) + queries (`GetAssetRepairHistory`, `GetLastRepair`,
  repair search). PO/JO/BUR/DV captured as numbers/amounts on the command — **no discovery query here**
  (discovery is UI-layer).
- RPRI (Exhibit 6) printable PDF (QuestPdf/FastReporting).
- Blazor: RPRI form (Exhibit 6 layout), per-asset + per-vehicle repair history, discovery picker.
  Vehicle repair pages now read from AssetRegister.

---

## Risks / open items

- **Property-class data hygiene:** real vehicle catalog items must use `DefaultPropertyClass = "LT"` or
  auto-detection silently finds nothing. Follow-up: align existing catalog seeds with COA 2-char codes.
- **Acquisition source (purchase vs transfer):** available but needs a ReceivingReport join (no asset
  FK). Deferred unless required at enrollment time.
- **Lifecycle coupling:** intentionally decoupled now; revisit if disposing/transferring a PPE asset
  should auto-update the fleet record (via integration events).
- **Migration:** dev DB is disposable, so add the column/index in a clean migration without backfill.
- **Gate semantics (DECIDED): accepted-PAR.** Fuel/odometer access requires an Active (accepted) PAR
  line, not just `CurrentCustodianId`. See Phase 8.2.
- **Verify `SearchAssetsQuery` Disposed handling:** it excludes `TransferredOut` by default; confirm
  `Disposed` is also excluded (or filter it in the Vehicle anti-join) so disposed vehicles aren't
  enrollable.
- **PAR plate/engine prefill (optional):** when issuing a PAR for an enrolled vehicle, the Blazor PAR
  page could prefill plate/engine/chassis/odometer from the Vehicle (via HTTP `IVehicleClient`), since
  `IssueAccountabilityLineRequest` takes them as input. Nice-to-have, not required.

---

## File-level change map (anticipated)

| Area | File(s) | Change |
| ---- | ------- | ------ |
| Contracts | `Modules.AssetRegister.Contracts/v1/Assets/AssetContracts.cs` | Add `PropertyClass` filter to `SearchAssetsQuery` (reuse `AssetRegistrySummaryDto`); reuse `GetAssetRegistryQuery` for enroll fetch |
| AssetRegister | `Features/v1/Assets/SearchAssets/*` (handler) | Apply `PropertyClass` filter |
| AssetRegister | `AssetRegisterModuleConstants.cs` | Add `VehicleClassCode = "LT"` |
| AssetRegister | `Data/AssetRegisterDbInitializer.cs` | Add motor-vehicle catalog + PPERR vehicle line(s) |
| Vehicle | `Domain/Vehicles/Vehicle.cs` | `AssetRegistryId` + mirrors; `Create`→`Enroll` |
| Vehicle | `Data/Configurations/VehicleConfiguration.cs` | Map new cols; unique `(TenantId, AssetRegistryId)` |
| Vehicle | `Contracts/v1/Vehicles/VehicleContracts.cs` | `EnrollVehicleCommand`; `VehicleDto` += `AssetRegistryId`,`PropertyNo`,`AcquisitionDate`, − assignment fields; drop cost from `UpdateVehicleCommand`; **remove `AssignVehicleCommand`** + `SearchVehiclesQuery.AssignedDepartmentId` |
| Vehicle | `Domain/Vehicles/Vehicle.cs` | (also) remove assignment fields + `AssignTo` |
| Vehicle | `Features/v1/Vehicles/EnrollVehicle/*` | Replace `CreateVehicle` slice |
| Vehicle | `Features/v1/Vehicles/AssignVehicle/*` | **Delete** slice (command/handler/validator/endpoint) |
| Vehicle | `Features/v1/Vehicles/GetEnrollableVehicleAssets/*` | New query (anti-join) |
| Vehicle | `Features/v1/Vehicles/UpdateVehicle/*` | Remove `AcquisitionCost` entry |
| Vehicle | `Features/v1/Vehicles/GetMotorVehicleInventory/*` | Cost from mirror (label unchanged); Accountable Officer from PAR (8.2) |
| Vehicle | `Features/v1/Vehicles/SearchVehicles/*`, `VehicleMapper.cs`, `Lookups/VehicleLookupHandlers.cs` | Drop assignment field mapping/filter |
| Vehicle | `Contracts/v1/References/VehicleReferenceContracts.cs` | Drop `AssignedDriver/Department` fields + filter |
| Vehicle | `VehicleModule.cs` | Remove `AssignVehicle` endpoint mapping |
| Vehicle | `Data/VehicleDbInitializer.cs` | Remove standalone seed; enroll seeded PPE assets |
| Vehicle | `Modules.Vehicle.csproj` | Ref `Modules.AssetRegister.Contracts` |
| Migrations | `Migrations.PostgreSQL` | Vehicle: add `AssetRegistryId`,`PropertyNo`,`AcquisitionDate` + index |
| Blazor | `Components/Pages/Vehicle/VehicleFormDialog.razor` | 2-step enroll; remove cost field |
| Blazor | `Components/Pages/Vehicle/VehiclesPage.razor` | "New Vehicle" → "Enroll Vehicle" |
| Blazor | `Components/Pages/Vehicle/VehicleDetailPage.razor` | Read-only acquisition info |
| Blazor | `Services/Api/IVehicleClient` (generated) | `EnrollVehicleAsync` + `GetEnrollableVehicleAssetsAsync` |
| Tests | `src/Tests/Vehicle.Tests/*` | `Create`→`Enroll`; enroll validation + anti-join tests; **delete** `AssignVehicleCommandValidatorTests`; update `VehicleDomainTests` (remove `AssignTo`) |
| **Phase 8** | | |
| Contracts | `Modules.AssetRegister.Contracts/v1/Assets/AssetContracts.cs` | `GetMyAccountableAssetIdsQuery` (Mediator-only) + `GetAccountableOfficersByAssetIdsQuery` (report) |
| AssetRegister | `Features/v1/Assets/GetMyAccountableAssetIds/*`, `GetAccountableOfficersByAssetIds/*` | New handlers (`CurrentCustodianId` / current-accountability resolution; reuse `CurrentEmployeeResolver`) |
| Vehicle | `Contracts/Permissions/VehiclePermissions.cs` | Add `MyVehicle` (View, RecordFuelOdometer, …) |
| Vehicle | `VehicleModule.RegisteredPermissions` | Register `MyVehicle.*` (View `IsBasic`; mirror `MyAccountability`) |
| Vehicle | `Features/v1/MyVehicle/*` | `GetMyVehicles`, `RecordMyVehicleFuelOdometer`, `GetMyVehicleDailyUsage` (ownership-guarded) |
| Vehicle | `Features/v1/Vehicles/GetMotorVehicleInventory/*` | Accountable officer from PAR holder (batch query) |
| Blazor | `Components/Pages/Vehicle/MyVehiclePage.razor` (new) + nav menu | Self-service fuel/odometer; gated by `MyVehicle.*` |
| **Phase 9** | | |
| AssetRegister | `Domain/Repairs/PropertyRepair.cs` (new) | RPRI aggregate keyed by `AssetRegistryId`: description-of-property, pre/post inspection, optional vehicle fields + PO-JO/BUR/DV/Invoice nos, status machine, **RPRI-mandatory gate**, lock-on-accept |
| AssetRegister | `Data/Configurations/PropertyRepairConfiguration.cs` + migration | RPRI table |
| Contracts | `Modules.AssetRegister.Contracts/v1/Repairs/*` | RPRI DTOs + commands (`RequestRepair`, `RecordPreRepairInspection`, `RecordPostRepairInspection`, `AcceptRepair`) + queries (`GetAssetRepairHistory`, `GetLastRepair`, search). PO/JO/BUR/DV as captured numbers — no discovery query |
| AssetRegister | `Features/v1/Repairs/*` | New slices; history = query over accepted RPRIs |
| AssetRegister | `AssetRegisterModule.RegisteredPermissions` + `AssetRegisterPermissions.cs` | Add `Repair` (View/Request/Inspect/Accept) |
| Blazor (discovery) | RPRI form | Calls existing BudgetDisbursement/Procurement API clients with `keyword = PropertyNo`; AssetRegister adds **no** finance reference |
| Vehicle (remove) | `Domain/Repairs/*`, `Features/v1/Repairs/*`, `VehiclePermissions.Repairs.*`, `VehicleModule` repair mappings, `Vehicle.Tests` repairs | **Delete** — repairs move to AssetRegister |
| Vehicle (surface) | repair UI / a thin query | Read vehicles' repairs from AssetRegister by `AssetRegistryId`; pre-fill engine/chassis/odometer on request |
| Reporting | QuestPdf/FastReporting `Repair` feature | RPRI (Exhibit 6) printable PDF |
| Blazor | repair pages | RPRI form (Exhibit 6 layout), per-asset/per-vehicle repair history, PO/JO/BUR/DV discovery picker |
