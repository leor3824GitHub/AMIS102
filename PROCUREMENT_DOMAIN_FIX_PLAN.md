# Procurement Planning Domain Fix Plan

**Module:** `Modules.ProcurementPlanning`
**Date:** 2026-04-29
**Author:** Domain Review — AI-assisted
**Status:** Pending Implementation

---

## Context

The manual procurement planning process has three PPMP phases:

1. **Indicative** (July–August) — end user prepares procurement plan based on estimated needs; submitted, approved, and consolidated into the Indicative APP.
2. **Final** (start of year / after budget approval) — end user revises the Indicative for the actual approved budget; submitted, approved, and consolidated into the Final APP.
3. **Updated** (any time) — when quantities, descriptions, amounts change or new unplanned items are added; creates versioned updates (v1, v2…) consolidated into Updated APP.

Each phase produces a **separate, independently approved document**. Neither Final nor Updated "cancels" the previous phase — all three are on file simultaneously.

---

## Issues Found

| # | Issue | Severity |
|---|-------|----------|
| 1 | `PromoteToFinal` supersedes the Indicative — incorrect | Critical |
| 2 | `APP.PromoteToFinal` copies Indicative APP items into Final APP — causes double data | Critical |
| 3 | `ConsolidatePpmps` removes items only by exact `SourcePpmpId` — Updated PPMP can't replace Final PPMP's contribution | Critical |
| 4 | No phase alignment check in `ConsolidatePpmps` — any PPMP can go into any APP phase | High |
| 5 | No `PpmpFamilyId` — can't query full cross-phase history of one PPMP document family | Medium |
| 6 | Domain allows creating Final/Updated PPMP directly without Indicative predecessor | Low (policy) |

---

## Fix 1 — Remove Supersede of Indicative on PromoteToFinal

### Problem
When an Indicative PPMP is promoted to Final, the handler calls `original.Supersede()`. This:
- Changes the Indicative's status to `Superseded`
- Sets `IsCurrentVersion = false`
- Makes the Indicative APP's source PPMPs appear as "superseded" in reports
- Breaks the rule that Indicative remains a valid filed document

Same problem exists on the APP side: `PromoteToFinalAppCommandHandler` calls `original.Supersede()` on the Indicative APP.

### Fix

**File:** `Features/v1/Ppmps/PromoteToFinalPpmp/PromoteToFinalPpmpCommandHandler.cs`

Remove:
```csharp
original.Supersede();
```

The Final PPMP `IsCurrentVersion = true` is its own chain — it does not need to mark the Indicative as superseded.

**File:** `Features/v1/AnnualProcurementPlans/PromoteToFinalApp/PromoteToFinalAppCommandHandler.cs`

Remove:
```csharp
original.Supersede();
```

### Domain method — remove phase guard in `Supersede()`
No change needed to `Ppmp.Supersede()` itself. The `Supersede()` method remains available for the `CreateUpdate` workflow (Updated v1 supersedes Final within the same VersionChainId).

### Migration
No schema change required.

---

## Fix 2 — APP.PromoteToFinal Should Create an Empty APP

### Problem
`AnnualProcurementPlan.PromoteToFinal()` clones all `AppItem`s from the Indicative APP into the new Final APP. Each cloned item has `SourcePpmpId` pointing to an Indicative PPMP (different ID from the corresponding Final PPMP). When the BAC Sec then consolidates Final PPMPs, re-consolidation removes items by `SourcePpmpId` — but since Final PPMPs have new IDs, the cloned Indicative items are NOT removed. Result: Final APP contains both cloned Indicative items AND newly consolidated Final PPMP items.

### Fix

**File:** `Domain/AnnualProcurementPlans/AnnualProcurementPlan.cs`

Change `PromoteToFinal()` to create an empty Final APP — no item cloning:

```csharp
public AnnualProcurementPlan PromoteToFinal(Guid promotedById)
{
    if (Status is not AppStatus.Approved)
        throw new InvalidOperationException("Only Approved APPs can be promoted to Final.");
    if (Phase is not AppPhase.Indicative)
        throw new InvalidOperationException("Only Indicative APPs can be promoted to Final.");

    return new AnnualProcurementPlan
    {
        Id = Guid.NewGuid(),
        AppNumber = AppNumber,
        FiscalYear = FiscalYear,
        Phase = AppPhase.Final,
        Status = AppStatus.Draft,
        VersionNumber = 1,
        IsCurrentVersion = true,
        VersionChainId = Guid.NewGuid(),
        PreviousVersionId = Id,
        AmendedAt = DateTimeOffset.UtcNow,
        AmendedById = promotedById,
        CreatedOnUtc = DateTimeOffset.UtcNow,
        Version = NewVersion()
    };
    // No items — BAC Sec consolidates Final PPMPs fresh into this draft.
}
```

**Note:** `AppItem.Clone()` and `AppItem.Clone` internal method remain needed for `CreateUpdate()` (Updated APP starts from Final APP items, which is correct — Updated APP carries forward Final APP items and then the BAC Sec re-consolidates Updated PPMPs to replace changed office contributions).

### Migration
No schema change required.

---

## Fix 3 — AppItem Needs SourcePpmpVersionChainId for Correct Re-consolidation

### Problem
When a Final PPMP (PPMP-B) for Office X is superseded by an Updated PPMP (PPMP-C), the Updated APP is created from the Final APP (via `CreateUpdate`). The Updated APP's items from Office X have `SourcePpmpId = PPMP-B`. When the BAC Sec consolidates PPMP-C (Updated PPMP) into the Updated APP, `ConsolidatePpmps` removes items where `SourcePpmpId = PPMP-C` — but PPMP-C is new, so nothing is removed. PPMP-B's items remain, and PPMP-C's items are added. Office X now has duplicate items in the Updated APP.

The correct behavior: when PPMP-C (Updated) is consolidated, all items previously contributed by ANY PPMP in the same document family (same `VersionChainId`) should be replaced.

### Changes Required

#### 3a — Add `SourcePpmpVersionChainId` to `AppItem`

**File:** `Domain/AnnualProcurementPlans/AnnualProcurementPlan.cs`

Add field to `AppItem`:
```csharp
public Guid SourcePpmpVersionChainId { get; private set; }
```

Update `AppItem.FromPpmpItem()`:
```csharp
SourcePpmpVersionChainId = ppmp.VersionChainId,
```

Update `AppItem.Clone()`:
```csharp
SourcePpmpVersionChainId = source.SourcePpmpVersionChainId,
```

#### 3b — Update `ConsolidatePpmps()` to remove by version chain

**File:** `Domain/AnnualProcurementPlans/AnnualProcurementPlan.cs`

```csharp
public void ConsolidatePpmps(IEnumerable<Ppmp> ppmps, Guid consolidatedById)
{
    if (Status is not (AppStatus.Draft or AppStatus.Returned))
        throw new InvalidOperationException("Only Draft or Returned APPs can have PPMPs consolidated into them.");

    var ppmpList = ppmps.ToList();

    // Remove all items previously sourced from the same version chain as any incoming PPMP.
    // This ensures an Updated PPMP correctly replaces a Final PPMP's contribution
    // for the same office/document family.
    var incomingChainIds = ppmpList.Select(p => p.VersionChainId).ToHashSet();
    _items.RemoveAll(i => incomingChainIds.Contains(i.SourcePpmpVersionChainId));

    var nextItemNo = _items.Count == 0 ? 1 : _items.Max(i => i.ItemNo) + 1;

    foreach (var ppmp in ppmpList)
    {
        foreach (var item in ppmp.Items)
        {
            _items.Add(AppItem.FromPpmpItem(Id, nextItemNo++, ppmp, item));
        }
    }

    ConsolidatedById = consolidatedById;
    ConsolidatedOn = DateTimeOffset.UtcNow;
    MarkChanged();
}
```

#### 3c — EF Configuration

**File:** `Data/Configurations/AnnualProcurementPlanConfiguration.cs`

In `AppItemConfiguration.Configure()`, add:
```csharp
builder.HasIndex(x => x.SourcePpmpVersionChainId);
```

#### 3d — EF Core Migration

A new column `SourcePpmpVersionChainId` (`uuid NOT NULL`) must be added to the `AppItems` table.

```
dotnet ef migrations add AddAppItemSourcePpmpVersionChainId \
  --project src/Modules/ProcurementPlanning/Modules.ProcurementPlanning \
  --startup-project src/Playground/FSH.Playground.AppHost
```

**Backfill for existing data:**
Existing `AppItem` rows need `SourcePpmpVersionChainId` populated. The migration should join `AppItems` → `Ppmps` on `SourcePpmpId` and copy `Ppmps.VersionChainId`:

```sql
UPDATE procurement_planning."AppItems" ai
SET "SourcePpmpVersionChainId" = p."VersionChainId"
FROM procurement_planning."Ppmps" p
WHERE ai."SourcePpmpId" = p."Id";
```

Include this as a `migrationBuilder.Sql(...)` statement in the migration's `Up()` method.

---

## Fix 4 — Enforce Phase Alignment in ConsolidatePpmps

### Problem
`ConsolidatePpmps()` accepts any Approved PPMP regardless of its phase. This allows Indicative PPMPs to be consolidated into a Final APP.

### Fix

**File:** `Domain/AnnualProcurementPlans/AnnualProcurementPlan.cs`

Add phase guard to `ConsolidatePpmps()` (add after the status guard):

```csharp
// Map AppPhase to PpmpPhase — they share the same ordinal values.
var expectedPpmpPhase = (PpmpPhase)(int)Phase;
if (ppmpList.Any(p => p.Phase != expectedPpmpPhase))
    throw new InvalidOperationException(
        $"Only {Phase} phase PPMPs can be consolidated into a {Phase} APP.");
```

**Note:** `PpmpPhase` and `AppPhase` both use `Indicative=0, Final=1, Updated=2`, so the cast is safe. If the enums ever diverge, add an explicit mapping instead.

### Migration
No schema change required.

---

## Fix 5 — Add PpmpFamilyId for Cross-Phase Lineage Tracking

### Problem
There is no single key that links an Indicative PPMP, its Final PPMP, and all subsequent Updated PPMPs as one "document family." `VersionChainId` only covers Final + Updated (same chain). The Indicative is in a separate chain linked only via `PreviousVersionId` on the Final.

Consequence: the BAC Sec cannot easily query "show me all phases and versions of PPMP-2025-ADMIN-001 for Office X."

### Fix

#### 5a — Add `PpmpFamilyId` to `Ppmp`

**File:** `Domain/Ppmps/Ppmp.cs`

Add property:
```csharp
// Shared across all phases of the same PPMP document family (Indicative → Final → Updated).
public Guid PpmpFamilyId { get; private set; }
```

Update `Ppmp.Create()` — set on first creation:
```csharp
PpmpFamilyId = Guid.NewGuid(),
```

Update `Ppmp.PromoteToFinal()` — inherit from source:
```csharp
PpmpFamilyId = PpmpFamilyId,
```

Update `Ppmp.CreateUpdate()` — inherit from source:
```csharp
PpmpFamilyId = PpmpFamilyId,
```

#### 5b — EF Configuration

**File:** `Data/Configurations/PpmpConfiguration.cs`

Add:
```csharp
builder.HasIndex(x => x.PpmpFamilyId);
```

#### 5c — EF Core Migration

New column `PpmpFamilyId` (`uuid NOT NULL`) on the `Ppmps` table.

```
dotnet ef migrations add AddPpmpFamilyId \
  --project src/Modules/ProcurementPlanning/Modules.ProcurementPlanning \
  --startup-project src/Playground/FSH.Playground.AppHost
```

**Backfill for existing data:**
For existing PPMPs, each document family must be reconstructed. The safest backfill is:
1. For PPMPs with no `PreviousVersionId` AND `Phase = Indicative`: assign a new `PpmpFamilyId = gen_random_uuid()`.
2. For PPMPs with `PreviousVersionId` (Final or Updated): follow the chain back to the root Indicative and use its `PpmpFamilyId`.

Since chain traversal is complex in SQL, include a helper migration script or do it in the `IDbInitializer.MigrateAsync` on first run.

#### 5d — New Query Support

Add to contracts `PpmpContracts.cs`:
```csharp
public sealed record GetPpmpFamilyQuery(Guid PpmpFamilyId) : IQuery<IReadOnlyList<PpmpSummaryDto>>;
```

Add handler `Features/v1/Ppmps/GetPpmpFamily/GetPpmpFamilyQueryHandler.cs`:
```csharp
public async ValueTask<IReadOnlyList<PpmpSummaryDto>> Handle(
    GetPpmpFamilyQuery query, CancellationToken ct)
{
    return await dbContext.Ppmps
        .AsNoTracking()
        .Where(x => x.PpmpFamilyId == query.PpmpFamilyId)
        .OrderBy(x => x.Phase).ThenBy(x => x.VersionNumber)
        .Select(x => new PpmpSummaryDto(...))
        .ToListAsync(ct);
}
```

---

## Fix 6 — Policy Decision on Direct Final/Updated PPMP Creation (Low Priority)

### Problem
`Ppmp.Create()` accepts any `PpmpPhase` including Final and Updated. In strict procurement policy, Final always derives from an Indicative, and Updated derives from a Final.

### Options

**Option A — Enforce in domain (strict):**
Add a guard to `Ppmp.Create()`:
```csharp
if (phase is not PpmpPhase.Indicative)
    throw new InvalidOperationException(
        "A new PPMP must start as Indicative. Use PromoteToFinal or CreateUpdate for other phases.");
```

**Option B — Allow direct creation (flexible):**
Some agencies may not use the Indicative phase (e.g., for supplemental procurement mid-year). Keep the current behavior but document the policy decision in the `Create()` XML doc comment.

**Recommendation:** Discuss with the BAC Secretariat whether direct Final/Updated creation is a valid scenario. If the agency always follows Indicative → Final → Updated strictly, implement Option A.

---

## Implementation Order

Dependencies exist between fixes. Implement in this sequence:

```
Fix 2 (empty APP.PromoteToFinal)         — no dependencies, no migration
Fix 1 (remove Supersede from handlers)  — no dependencies, no migration
Fix 4 (phase alignment guard)           — no dependencies, no migration
Fix 3 (SourcePpmpVersionChainId)        — requires migration + backfill
Fix 5 (PpmpFamilyId)                    — requires migration + backfill + new query
Fix 6 (policy decision)                 — discuss first, then implement
```

Fixes 1–4 can be batched into a single PR (no migration needed).
Fixes 3 and 5 each require a separate migration.

---

## Files Affected Summary

| File | Change |
|------|--------|
| `Domain/AnnualProcurementPlans/AnnualProcurementPlan.cs` | Fix 2: empty `PromoteToFinal`; Fix 3: `SourcePpmpVersionChainId` on `AppItem`, update `ConsolidatePpmps`; Fix 4: phase guard |
| `Domain/Ppmps/Ppmp.cs` | Fix 5: add `PpmpFamilyId`, propagate through `PromoteToFinal` and `CreateUpdate` |
| `Data/Configurations/AnnualProcurementPlanConfiguration.cs` | Fix 3: index on `SourcePpmpVersionChainId` |
| `Data/Configurations/PpmpConfiguration.cs` | Fix 5: index on `PpmpFamilyId` |
| `Features/v1/Ppmps/PromoteToFinalPpmp/PromoteToFinalPpmpCommandHandler.cs` | Fix 1: remove `original.Supersede()` |
| `Features/v1/AnnualProcurementPlans/PromoteToFinalApp/PromoteToFinalAppCommandHandler.cs` | Fix 1: remove `original.Supersede()` |
| `Contracts/v1/Ppmps/PpmpContracts.cs` | Fix 5: add `GetPpmpFamilyQuery` |
| **NEW** `Features/v1/Ppmps/GetPpmpFamily/GetPpmpFamilyQueryHandler.cs` | Fix 5: new query handler |
| **NEW** `Features/v1/Ppmps/GetPpmpFamily/GetPpmpFamilyEndpoint.cs` | Fix 5: new endpoint |
| `ProcurementPlanningModule.cs` | Fix 5: register new endpoint |
| **NEW** EF migration: `AddAppItemSourcePpmpVersionChainId` | Fix 3 |
| **NEW** EF migration: `AddPpmpFamilyId` | Fix 5 |

---

## Open Questions Before Implementation

1. **Fix 6 — Policy:** Should `Ppmp.Create()` block Phase=Final or Phase=Updated? Confirm with BAC Sec.
2. **Fix 5 — Backfill:** For existing PPMP data, is a SQL backfill script sufficient, or does the system need a one-time migration job in `IDbInitializer`?
3. **Fix 4 — Phase cast safety:** Confirm that `PpmpPhase` and `AppPhase` ordinal values will always stay in sync (both use Indicative=0, Final=1, Updated=2). If not, add an explicit mapping function.
4. **APP PpmpFamilyId equivalent?** Should `AnnualProcurementPlan` also get an `AppFamilyId` for the same cross-phase history tracking on the APP side? (Indicative APP → Final APP → Updated APP for the same fiscal year.)

---

## Normalization Analysis

### Tables Overview

The module maps to four physical database tables:

| Table | Entity | Role |
|-------|--------|------|
| `Ppmps` | `Ppmp` | Aggregate root — one row per PPMP version |
| `PpmpItems` | `PpmpItem` | Child entity — line items owned by a PPMP |
| `AnnualProcurementPlans` | `AnnualProcurementPlan` | Aggregate root — one row per APP version |
| `AppItems` | `AppItem` | Snapshot child — frozen copy of PPMP items at consolidation |

---

### Table: `Ppmps`

**Columns:** `Id`, `PpmpNumber`, `FiscalYear`, `Phase`, `OfficeCode`, `EndUserUnit`, `Status`, `VersionNumber`, `IsCurrentVersion`, `VersionChainId`, `PreviousVersionId`, `AmendmentReason`, `AmendedAt`, `AmendedById`, `PreparedById`, `SubmittedAt`, `ApprovedAt`, `ApprovedById`, `ReturnReason`, `ReturnedAt`, `ReturnedById`, `AppId`, `Version`, `[audit fields]`

| Normal Form | Status | Notes |
|-------------|--------|-------|
| **1NF** | ✅ | All columns are single-valued and atomic |
| **2NF** | ✅ | Single-column PK — 2NF is automatic |
| **3NF** | ⚠️ | Two violations below |
| **BCNF** | ⚠️ | Same violations |

**Violation A — PpmpNumber encodes OfficeCode and FiscalYear (3NF / BCNF)**

`PpmpNumber` is generated as `PPMP-{FiscalYear}-{OfficeCode}-{Seq}`, creating transitive dependencies:

```
Id → PpmpNumber → FiscalYear   (transitive)
Id → PpmpNumber → OfficeCode   (transitive)
```

Both `FiscalYear` and `OfficeCode` are also stored as independent columns, making them redundant. If `OfficeCode` is ever corrected, `PpmpNumber` will contradict it. `PpmpNumber` is not a superkey, so this also violates BCNF.

**Fix:** Generate `PpmpNumber` as a simple sequential identifier (`PPMP-{Seq}`) without embedding structural data. Rely on `FiscalYear` and `OfficeCode` columns for filtering.

**Violation B — `IsCurrentVersion` is a derived attribute**

`IsCurrentVersion` is always determinable by querying: "does any row in the same `VersionChainId` have a higher `VersionNumber`?" Storing it as a column is a **performance denormalization**. It avoids a subquery on every list read but introduces an update anomaly — if domain methods fail to maintain it, the data becomes inconsistent.

**Decision:** Leave as-is. The domain methods (`Supersede()`, `PromoteToFinal()`, `CreateUpdate()`) maintain it correctly. The performance gain on `WHERE IsCurrentVersion = true` queries justifies the trade-off.

**Conditional column groups (entity-subtype pattern without subtypes)**

The table contains five groups of nullable columns that are only meaningful when `Status` is in a particular state:

| Status | Active columns |
|--------|---------------|
| Submitted | `SubmittedAt` |
| Approved | `ApprovedAt`, `ApprovedById` |
| Returned | `ReturnReason`, `ReturnedAt`, `ReturnedById` |
| Consolidated | `AppId` |
| Amended | `AmendmentReason`, `AmendedAt`, `AmendedById` |

In strict 3NF these would be separate tables (e.g., `PpmpApprovals`, `PpmpReturnEvents`). The flat design means only the **last** event of each type is stored — if a PPMP is returned twice, the first return reason is overwritten. This is an **audit history loss** risk, not a normalization violation per se, but it originates from the flat model.

**Consider:** Add a `PpmpEvents` audit table to preserve full workflow history if regulators or auditors require it.

---

### Table: `PpmpItems`

**Columns:** `Id`, `PpmpId`, `ItemNo`, `GeneralDescription`, `ProjectType`, `Quantity`, `Unit`, `ModeOfProcurement`, `PreProcurementConference`, `ProcurementStart`, `ProcurementEnd`, `ExpectedDelivery`, `SourceOfFunds`, `EstimatedBudget`, `SupportingDocuments`, `Remarks`

| Normal Form | Status | Notes |
|-------------|--------|-------|
| **1NF** | ⚠️ | `SupportingDocuments` may be multi-valued |
| **2NF** | ✅ | Single-column PK |
| **3NF** | ✅ | All columns depend directly on `Id` |
| **BCNF** | ✅ | No non-trivial non-superkey determinants |

**1NF Risk — `SupportingDocuments` is potentially multi-valued**

The field is named `SupportingDocuments` (plural) and configured as `varchar(500)`. If it stores multiple file paths or references as a delimited string, it violates 1NF. The normalized form is a separate `PpmpItemDocuments` table with one row per file.

**Action needed:** Clarify with the development team whether this field ever stores more than one value. If yes, normalize to a child table.

**Wrong type — Dates stored as strings**

`ProcurementStart`, `ProcurementEnd`, and `ExpectedDelivery` are `varchar(10)` (max-length 10 implies `yyyy-MM-dd` format). They should be `DateOnly`. As strings they:
- Cannot be compared with `<` / `>` in queries without casting
- Accept invalid values (`"not-set"`, `"2025/1/5"`)
- Sort incorrectly for any locale that does not use ISO 8601

**Fix:** Change column type to `date` via an EF migration. Update domain property type from `string` to `DateOnly`.

**Free-text reference fields without lookup tables**

`Unit` (`varchar 64`) and `SourceOfFunds` (`varchar 256`) are free text with no referential integrity. The same concept can be entered as `"pcs"`, `"Pcs"`, `"pieces"` — no constraint prevents inconsistency. These are candidates for lookup tables, especially `SourceOfFunds` which should come from the agency's budget coding structure (UACS/fund source codes).

**Consider:** Source `Unit` and `SourceOfFunds` from the MasterData module via reference codes.

---

### Table: `AnnualProcurementPlans`

**Columns:** `Id`, `AppNumber`, `FiscalYear`, `Phase`, `Status`, `VersionNumber`, `IsCurrentVersion`, `VersionChainId`, `PreviousVersionId`, `AmendmentReason`, `AmendedAt`, `AmendedById`, `ConsolidatedById`, `ConsolidatedOn`, `ApprovedById`, `ApprovedOn`, `ReturnReason`, `ReturnedAt`, `ReturnedById`, `Version`, `[audit fields]`

| Normal Form | Status | Notes |
|-------------|--------|-------|
| **1NF** | ✅ | All columns atomic |
| **2NF** | ✅ | Single-column PK |
| **3NF** | ⚠️ | Same pattern as `Ppmps` |
| **BCNF** | ⚠️ | Same as `Ppmps` |

**Violation — AppNumber encodes FiscalYear**

`AppNumber` is generated as `APP-{FiscalYear}-{Seq}`, creating:
```
Id → AppNumber → FiscalYear   (transitive)
```
`FiscalYear` is also stored as an independent column — redundant, same fix as `PpmpNumber`.

**`IsCurrentVersion`** — same derived-attribute denormalization as `Ppmps`. Leave as-is for the same reasons.

**Conditional workflow columns** — same flat entity-subtype pattern as `Ppmps` with the same audit history loss risk.

---

### Table: `AppItems` — Intentionally Denormalized

**Columns:** `Id`, `AppId`, `SourcePpmpId`, `SourcePpmpItemId`, `OfficeCode`, `EndUserUnit`, `ItemNo`, `GeneralDescription`, `ProjectType`, `Quantity`, `Unit`, `ModeOfProcurement`, `PreProcurementConference`, `ProcurementStart`, `ProcurementEnd`, `ExpectedDelivery`, `SourceOfFunds`, `EstimatedBudget`, `Remarks`

| Normal Form | Status | Notes |
|-------------|--------|-------|
| **1NF** | ✅ | All values atomic |
| **2NF** | ✅ | Single-column PK |
| **3NF** | ❌ | Two functional dependencies not through `Id` |
| **BCNF** | ❌ | Non-superkey determinants exist |

**BCNF Violation A — Item data depends on `SourcePpmpItemId`, not `Id`**

```
SourcePpmpItemId → { GeneralDescription, ProjectType, Quantity, Unit,
                     ModeOfProcurement, PreProcurementConference,
                     ProcurementStart, ProcurementEnd, ExpectedDelivery,
                     SourceOfFunds, EstimatedBudget }
```

`SourcePpmpItemId` is a foreign key, not the primary key. All 11 item-data columns are functionally determined by it.

**BCNF Violation B — `OfficeCode` and `EndUserUnit` depend on `SourcePpmpId`**

```
SourcePpmpId → { OfficeCode, EndUserUnit }
```

`SourcePpmpId` is a foreign key, not the primary key.

**This is intentional — snapshot pattern.**

`AppItem` is a **point-in-time frozen copy** of a `PpmpItem` at the moment of consolidation — the same way an invoice line item freezes the unit price at order time. If the source PPMP is later amended, the APP still shows the values that were actually consolidated and approved. The code comment documents this explicitly:

> *"AppItem is a point-in-time snapshot of a PpmpItem at consolidation. It is intentionally denormalized — if the source PPMP is later amended, AppItem preserves the original values."*

The `SourcePpmpId` and `SourcePpmpItemId` foreign keys provide traceability back to the source without coupling the APP's content to live PPMP data.

**Decision:** Leave `AppItems` denormalized. The snapshot model is the correct design for an official government procurement document that must be immutable after approval.

**Gap:** `AppItem` does not copy `SupportingDocuments` from `PpmpItem`. If auditors need to trace supporting documents from the APP back to the PPMP, they must follow `SourcePpmpItemId`. This is acceptable but should be documented in the API.

---

### Normalization Summary

| Table | 1NF | 2NF | 3NF | BCNF | Verdict |
|-------|:---:|:---:|:---:|:----:|---------|
| `Ppmps` | ✅ | ✅ | ⚠️ | ⚠️ | Mostly normalized; two fixable violations |
| `PpmpItems` | ⚠️ | ✅ | ✅ | ✅ | Good; fix date types and clarify SupportingDocuments |
| `AnnualProcurementPlans` | ✅ | ✅ | ⚠️ | ⚠️ | Same issues as Ppmps |
| `AppItems` | ✅ | ✅ | ❌ | ❌ | Intentional denormalization — correct by design |

### Normalization Fix Actions

| Issue | Table | Action | Priority |
|-------|-------|--------|----------|
| `PpmpNumber` encodes `FiscalYear` + `OfficeCode` | `Ppmps` | Generate as `PPMP-{Seq}` only | Medium |
| `AppNumber` encodes `FiscalYear` | `AnnualProcurementPlans` | Generate as `APP-{Seq}` only | Medium |
| `IsCurrentVersion` stored redundantly | Both | Leave — performance justification | — |
| `SupportingDocuments` may be multi-valued | `PpmpItems` | Clarify; normalize to `PpmpItemDocuments` if needed | High |
| Dates stored as `varchar` | `PpmpItems`, `AppItems` | Change to `DateOnly` — requires migration | High |
| `Unit` / `SourceOfFunds` as free text | `PpmpItems` | Add reference lookup tables from MasterData | Medium |
| Single return/approval stored (history loss) | `Ppmps`, `AnnualProcurementPlans` | Add `PpmpEvents` audit table if full trail needed | Low |
| `AppItems` BCNF violations | `AppItems` | Leave — intentional snapshot pattern | — |
