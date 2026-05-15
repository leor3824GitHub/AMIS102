# IAR Stage-Based Workflow — Implementation Plan

**Module:** `AssetProcurement` · **Aggregate:** `AssetInspectionAcceptanceReport`
**Goal:** Split the current single-form IAR into a 3-stage workflow (Draft → Inspection → Acceptance) that mirrors the actual NFA paper process and matches each role's mental model.

---

## 0. Roles & terminology

| Role | Identity | Owns stages |
|---|---|---|
| **Property Custodian** (= Supply Officer) | Existing `ReceivedById` → renamed `PropertyCustodianId` | Draft, Acceptance, Register |
| **Inspector** | Assigned per IAR by the Property Custodian at submit time | Inspection only |

- Inspector authorization is a new permission (`IARs.Inspect`). Only users with this permission can be picked as the inspector on an IAR.
- The Property Custodian picks the inspector via an autocomplete on the Draft / Submit-for-Inspection UI — filtered to users with `IARs.Inspect`. If the usual inspector is not around, the custodian simply picks an alternate from the same list. **There is no separate "primary" vs "alternate" slot in the data model — only the assigned inspector for that IAR.**
- Only the assigned inspector can record the inspection. Reassignment is allowed while the IAR is `PendingInspection` (Property Custodian re-opens the IAR and changes the inspector).

---

## 1. Status model

```
Draft ──Submit──▶ PendingInspection ──Record──▶ Inspected ──Accept──▶ Accepted
  │                       │                          │
  └───────────Cancel───────┴──────Cancel─────────────┘
                                                              (Cancelled is terminal)
```

**Removed:** the old whole-IAR `Rejected` status. Rejection is now **per-line at the Inspection stage only** (per your direction). A whole-IAR `Cancel` is kept as a no-reason cleanup for mistaken Drafts/RFIs that never got off the ground.

### Per-line inspection result

```
Pending ──▶ Passed   (inspector decision)
        └─▶ Rejected (with required remarks)
```

- Only `Passed` lines proceed to Acceptance and require a Property No.
- `Rejected` lines remain on the IAR for record-keeping; they are excluded from `AssetIARAcceptedEvent` and never reach the Asset Registry.
- No "Returned to Supplier" document generated (per your direction).

---

## 2. Domain changes

### 2.1 `AssetIARStatus` enum
```csharp
public enum AssetIARStatus
{
    Draft = 0,
    PendingInspection = 1,
    Inspected = 2,
    Accepted = 3,
    Cancelled = 4
    // (legacy: Rejected = 2 — removed; existing rows migrated to Cancelled)
}
```

### 2.2 `LineInspectionResult` enum (new)
```csharp
public enum LineInspectionResult { Pending = 0, Passed = 1, Rejected = 2 }
```

### 2.3 `AssetIARLineItem` — new fields
| Field | Type | Notes |
|---|---|---|
| `InspectionResult` | `LineInspectionResult` | Default `Pending` |
| `InspectedOnUtc` | `DateTimeOffset?` | Set on Record |
| `InspectedById` | `Guid?` | Whoever signed (Primary or Alternate) |

`InspectionRemarks` already exists. When `InspectionResult = Rejected`, remarks are required (validator).

### 2.4 `AssetInspectionAcceptanceReport` — new / changed fields

Renamed:
- `ReceivedById` → **`PropertyCustodianId`** (DB column renamed via migration)
- `InspectedById` → **`AssignedInspectorId`** (semantic clarification — this is the inspector picked by the custodian, not necessarily the one who eventually signs)

New:
- `SubmittedForInspectionOnUtc`, `InspectedOnUtc`, `AcceptedOnUtc`, `CancelledOnUtc` (audit trail per stage)
- A new method `ReassignInspector(Guid newInspectorId)` allowed only from `PendingInspection` (covers the "usual inspector is out, pick someone else" case without re-creating the IAR).

### 2.5 New / changed domain methods

| Method | Allowed from | Effect |
|---|---|---|
| `Create(...)` | — | Existing. Initial status `Draft`. Captures Assigned Inspector + Property Custodian. |
| `Update(...)` | `Draft` | Existing. Limited to Draft only. |
| `SubmitForInspection()` | `Draft` | Validates ≥1 line, all required header fields filled, Assigned Inspector set. → `PendingInspection`. |
| `ReassignInspector(Guid newInspectorId)` | `PendingInspection` | Property Custodian-only. Replaces `AssignedInspectorId`. Status unchanged. |
| `RecordInspection(Guid actorId, IEnumerable<LineInspectionDecision>)` | `PendingInspection` | Validates `actorId == AssignedInspectorId`. Every line must have a non-Pending decision. Rejected lines must have remarks. → `Inspected`. |
| `AssignPropertyNo(int itemNo, string propertyNo)` | `Inspected` | Only on `Passed` lines. Normalizes `Trim().ToUpperInvariant()`. |
| `ExpandLineByQuantity(int itemNo)` | `Inspected` | Splits a `Passed` line with Qty > 1 into N lines of Qty = 1 (NFA "one line per physical unit"). Inspection result + remarks copied to each child. |
| `Accept()` | `Inspected` | Validates every `Passed` line has a Property No (Rejected lines bypass). → `Accepted`. Existing `AssetIARAcceptedEvent` fires with `Passed` items only. |
| `Cancel()` | `Draft` / `PendingInspection` / `Inspected` | → `Cancelled`. No reason field (per your direction). |

### 2.6 Integration event
`AssetIARAcceptedEvent` payload shape unchanged. **Update the handler that builds the event payload to filter `LineItems.Where(li => li.InspectionResult == Passed)`.**

---

## 3. Contracts (`AssetIARContracts.cs`)

### Added

```csharp
public enum AssetIARStatus { Draft, PendingInspection, Inspected, Accepted, Cancelled }
public enum LineInspectionResult { Pending, Passed, Rejected }

public sealed record LineInspectionDecision(int ItemNo, LineInspectionResult Result, string? Remarks);

public sealed record SubmitIARForInspectionCommand(Guid Id) : ICommand<AssetIARDto>;

public sealed record ReassignInspectorCommand(Guid Id, Guid NewInspectorId) : ICommand<AssetIARDto>;

public sealed record RecordIARInspectionCommand(
    Guid Id,
    IReadOnlyList<LineInspectionDecision> Decisions) : ICommand<AssetIARDto>;
// Note: actor (inspector) is taken from current user context, not from the command payload.

public sealed record AssignPropertyNoCommand(Guid Id, int ItemNo, string PropertyNo) : ICommand<AssetIARDto>;

public sealed record ExpandLineByQuantityCommand(Guid Id, int ItemNo) : ICommand<AssetIARDto>;

public sealed record CancelAssetIARCommand(Guid Id) : ICommand<AssetIARDto>;
```

### Changed

```csharp
public sealed record AssetIARLineItemDto(
    int ItemNo, string Description, /* ... existing ... */,
    LineInspectionResult InspectionResult,    // NEW
    DateTimeOffset? InspectedOnUtc,           // NEW
    string? InspectedByName,                  // NEW (resolved from InspectedById)
    string? StockPropertyNo);

public sealed record AssetIARDto(
    /* ... existing fields, but with: */
    Guid AssignedInspectorId, string AssignedInspectorName,         // RENAMED from InspectedBy*
    Guid PropertyCustodianId, string PropertyCustodianName,         // RENAMED from ReceivedBy*
    DateTimeOffset? SubmittedForInspectionOnUtc,                    // NEW
    DateTimeOffset? InspectedOnUtc,                                 // NEW
    DateTimeOffset? AcceptedOnUtc,                                  // NEW
    DateTimeOffset? CancelledOnUtc,                                 // NEW
    /* ... */);

// Existing CreateAssetIARCommand / UpdateAssetIARCommand:
//   InspectedById → AssignedInspectorId
//   ReceivedById  → PropertyCustodianId
```

### Removed
- `RejectAssetIARCommand` — deleted in PR 4 (after UI migration).

---

## 4. Features (vertical slices)

New folders under `src/Modules/AssetProcurement/Modules.AssetProcurement/Features/v1/AssetIARs/`:

| Folder | Files | Notes |
|---|---|---|
| `SubmitForInspection/` | Handler, Validator, Endpoint | Permission: `IARs.SubmitForInspection` |
| `ReassignInspector/` | Handler, Validator, Endpoint | Permission: `IARs.SubmitForInspection` (custodian-side action) |
| `RecordInspection/` | Handler, Validator, Endpoint | Permission: `IARs.Inspect` + handler-level check `actorId == AssignedInspectorId` returning `403` if mismatched |
| `AssignPropertyNo/` | Handler, Validator, Endpoint | Permission: `IARs.AssignPropertyNo` |
| `ExpandLineByQuantity/` | Handler, Endpoint | Permission: `IARs.AssignPropertyNo` |
| `CancelAssetIAR/` | Handler, Endpoint | Permission: `IARs.Cancel` |

Existing `RejectAssetIAR/` → **deleted in PR 4**.

`AcceptAssetIAR/` handler → updated to validate Property No only on `Passed` lines.

### Permissions (add to `AssetProcurementPermissions.IARs`)
```csharp
public const string SubmitForInspection = "assetprocurement.iars.submitforinspection";
public const string Inspect             = "assetprocurement.iars.inspect";
public const string AssignPropertyNo    = "assetprocurement.iars.assignpropertyno";
public const string Cancel              = "assetprocurement.iars.cancel";
// Existing: View, Create, Update, Accept stay
```

### New seeded role (Identity module seed)
`Inspector` role — granted: `IARs.View`, `IARs.Inspect`. Property Custodians get the rest.

---

## 5. Persistence & migration

### 5.1 EF configuration
- `AssetIARLineItemConfiguration`: add `InspectionResult` (int, default 0, NOT NULL), `InspectedOnUtc`, `InspectedById`.
- `AssetIARConfiguration`: rename columns `ReceivedById` → `PropertyCustodianId`, `InspectedById` → `AssignedInspectorId`. Add the four `*OnUtc` audit columns.

### 5.2 Migration `IAR_StageWorkflow`
1. Rename columns (`ReceivedById`, `InspectedById`).
2. Add new columns.
3. Backfill:
   - `Status = Accepted` (1) → keep, set all lines to `InspectionResult = Passed`, `AcceptedOnUtc = LastModifiedOnUtc`.
   - `Status = Rejected` (2) → set to `Cancelled` (4), `CancelledOnUtc = LastModifiedOnUtc`.
   - `Status = Draft` (0) → no change.
4. Drop the old `RejectionReason` column (lossy — accept this; old rejections are rare and rejection logic is gone).

> **Run** `dotnet ef migrations add IAR_StageWorkflow --project src/Playground/Migrations.PostgreSQL --context AssetProcurementDbContext`.

---

## 6. Blazor UI

### 6.1 Tabbed list page (`AssetIARsPage.razor`)
- Replace status dropdown with **MudTabs**: `All | Draft | Pending Inspection | Inspected | Accepted | Cancelled`.
- Row primary action button changes by status:
  - **Draft** → "Edit" (opens draft dialog) + "Submit for Inspection"
  - **Pending Inspection** → "Inspect" (navigates to inspection page) — visible only to the user matching `AssignedInspectorId`. The Property Custodian sees a "Reassign Inspector" action instead.
  - **Inspected** → "Acceptance" (navigates to acceptance page)
  - **Accepted** → "View / Print"
  - **Cancelled** → "View"

### 6.2 Three stage-specific UIs (replace single mega-dialog)

| File | Type | Audience | Purpose |
|---|---|---|---|
| `AssetIARDraftDialog.razor` | MudDialog | Property Custodian | Existing form **stripped down**: PO selector, IAR No (auto), IAR Date, **Assigned Inspector** (required, autocomplete filtered to users with `IARs.Inspect`), Property Custodian (required), Delivery Receipt No, Delivery Date, Remarks, Line Items grid (description / unit / qty / unit cost / brand / model / serial). **No** property no, **no** inspection remarks, **no** inspection result. Buttons: `Save Draft`, `Save & Submit for Inspection`. The inspector autocomplete is the place where the custodian "picks an alternate" if the usual inspector is unavailable — it is not a separate field. |
| `AssetIARInspectionPage.razor` | `@page "/asset-procurement/iars/{Id:guid}/inspect"` | Inspector | Header card (read-only): IAR No, PO, Supplier, dates, Assigned Inspector. Line table with per-row toggle (Pass / Reject) + Remarks textarea (required when Reject). Bulk actions: "Pass all", "Reset all". Submit button → `RecordInspection` → navigate back to list with snackbar. Server-side guard: 403 if current user ≠ `AssignedInspectorId`. |
| `AssetIARAcceptancePage.razor` | `@page "/asset-procurement/iars/{Id:guid}/accept"` | Property Custodian | Two collapsible sections: **Passed lines** (active) and **Rejected lines** (read-only, collapsed). For each Passed line: "Expand to N units" button (when Qty > 1) — splits inline; Property No input with existing ✨ generator, validated + normalized on blur. Bottom bar: `Accept & Generate IAR` (disabled until every Passed line has a Property No). |

### 6.3 Shared stepper component (`AssetIARStageStepper.razor`)
Top of each detail page:
```
●─Draft─●─Inspection─○─Acceptance─○─Registered
```
Reusable, takes `AssetIARStatus` and renders the highlighted step.

### 6.4 API client (`IAssetIarClient`)
Add methods: `SubmitForInspectionAsync`, `ReassignInspectorAsync`, `RecordInspectionAsync`, `AssignPropertyNoAsync`, `ExpandLineByQuantityAsync`, `CancelAsync`. Drop `RejectAsync` in PR 4.

Also add a lookup method (or reuse existing employee lookup with a permission filter) that returns only employees with the `IARs.Inspect` permission, for the inspector autocomplete on the Draft dialog.

---

## 7. Testing

| Layer | Tests added |
|---|---|
| Domain (`Modules.AssetProcurement.Tests`) | State-transition matrix (each method × each status); `RecordInspection` requires actor == `AssignedInspectorId`; `ReassignInspector` only allowed in `PendingInspection`; Rejected line requires remarks; `Accept` skips Rejected lines for Property No validation; `ExpandLineByQuantity` splits Qty=N into N lines of Qty=1 with copied inspection result. |
| Handlers | One test per new command (happy path + main failure path). |
| Architecture | No change. |

---

## 8. Phasing — one PR per phase

### Phase 1 — Domain + contracts + migration (no UI behavior change)
- Update enums, aggregate, line item, configurations.
- Add new commands + handlers + endpoints (`Submit`, `Record`, `AssignPropertyNo`, `Expand`, `Cancel`).
- Update `Accept` handler (Rejected-line bypass).
- Update `AssetIARAcceptedEvent` payload builder to filter Passed.
- Migration `IAR_StageWorkflow` with backfill.
- Domain + handler tests.
- **No UI changes** — existing dialog still works against renamed fields. Old behavior preserved via the existing `Accept` path on Draft (we'll skip the new stages until UI is migrated, by treating Draft → Accept as a shortcut handled in the existing handler).
- ✅ Build green, 0 warnings, all tests pass.

### Phase 2 — Inspection page + Submit flow + tabs
- Tabbed list page with stage-aware action buttons.
- `AssetIARDraftDialog.razor` (stripped form + Submit button).
- `AssetIARInspectionPage.razor` + route registration.
- Stepper component.
- API client methods for Submit + Record.
- Inspector role seed.
- Manual test: Custodian creates Draft → Submits → Inspector sees in queue → records pass/fail → IAR moves to Inspected.

### Phase 3 — Acceptance page + line expansion + property no
- `AssetIARAcceptancePage.razor` with expand + property no + accept.
- API client methods for AssignPropertyNo, Expand, Accept.
- Delete the old mega-dialog.
- Wire `AssetIARAcceptedEvent` end-to-end (already works server-side from Phase 1).
- Manual test: end-to-end flow lands assets in the Asset Registry; Rejected lines are not registered.

### Phase 4 — Cleanup
- Delete `RejectAssetIAR/` slice + contract + client method.
- Remove `Rejected` enum value from `AssetIARStatus` (after confirming no Cancelled rows still reference it as int 2 — they were migrated to 4 in Phase 1).
- Delete `RejectionReason` column (migration `IAR_DropRejectionReason`).
- Documentation pass.

---

## 9. Out of scope (explicit)

- Returned-to-supplier printable document
- Whole-IAR rejection (replaced by per-line Reject + Cancel)
- Cancel reason field
- Re-inspection workflow for previously rejected lines (treated as a new IAR)
- Multi-inspector co-sign (one inspector signs per IAR; reassignment is the substitute mechanism)

---

## 10. Open items (none — ready for Phase 1)

All questions from the discussion are resolved:
1. ✅ Reject is per-line at Inspection only.
2. ✅ No Returned-to-Supplier doc.
3. ✅ Inspector role exists. Property Custodian assigns the inspector at Submit-for-Inspection time (filtered autocomplete). Reassignment supported while `PendingInspection`. No "alternate" slot in the data model — the alternate is whoever the custodian picks instead of the usual person.
4. ✅ Phased delivery (4 PRs).
