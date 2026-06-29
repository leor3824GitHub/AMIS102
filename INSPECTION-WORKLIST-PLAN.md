# Unified Inspection Worklist — Plan

> A single **"My Inspections"** queue that shows every item awaiting the current user's inspection across
> modules, and routes each into its existing inspection flow. Companion to `NOTIFICATIONS-PORT-PLAN.md`.
> Authored 2026-06-27.

---

## 1. Goal & shape

Inspection today is **fragmented across 3 surfaces with 3 different UX patterns**. An inspector has no
single place that answers *"what is waiting on me?"* — and notification deep-links must each guess a
module-specific route (which is how the JO link 404'd).

Build an aggregating **worklist** (`/inspections`) — **not** a universal form. Each row routes into the
module's existing, purpose-built inspection UI. The three inspectables already share one model:

| Source | Assigned-inspector field | "Pending" status | Inspect action (deep-link) | UX today |
| --- | --- | --- | --- | --- |
| **Job Order** | `JobOrder.InspectorId` (employee) | `Status == Issued` | `/procurement/job-orders?inspect={id}` | dialog (auto-open ✅ built) |
| **IAR** | `…SummaryDto.AssignedInspectorId` (employee) | `Status == PendingInspection` | `/procurement/inspection-acceptance-reports/{id}/inspect` | dedicated page |
| **Returned Property** | `…SummaryDto.AssignedInspectorEmployeeId` (employee) | `Status == Pending` | `/asset-register/returned-property?inspect={id}` | dialog (needs auto-open ⏳) |

All three key off the **current user's EmployeeId** (`UserProfileState.EmployeeId` client-side; resolved
server-side from the identity in handlers). v1 scope = **assigned-to-me** items (the well-defined case that
also matches the notification model). A "team queue" (unassigned/role-based) is explicitly out of scope.

---

## 2. Design decisions

1. **Worklist, not a universal form.** JO captures invoice/findings/found-in-order; IAR is per-line
   pass/reject; RP is per-item condition. One form can't serve all — the queue routes into each native flow.
2. **Client-side aggregation** (Blazor calls each module's "pending-for-me" endpoint and merges). No
   server-side cross-module query → preserves module boundaries (no module depends on another's internals).
3. **One uniform DTO** (`PendingInspectionItem`) in `BuildingBlocks/Shared`, returned by every module, so
   the page concatenates + sorts a single list instead of juggling 3 DTO types.
4. **Dedicated `GetMyPending…` queries**, self-scoped to the caller — cleaner than bolting an inspector
   filter onto the existing `Search…` queries, and mapping stays server-side.
5. **Deep-links stay specific.** Notifications keep linking straight to the action (`?inspect=` / inspect
   page); the worklist is the complementary pull view. Both reinforce each other.
6. **Reuse the `?inspect={id}` auto-open pattern** (already added to the JO page) for Returned Property.
7. **Sort oldest-first** (most overdue on top); show a per-row **type chip**.

---

## 3. Architecture

```
Blazor: /inspections (MyInspectionsPage)
   │  parallel calls, merge + sort client-side
   ├── GET /api/v1/procurement/inspections/pending-for-me     → IReadOnlyList<PendingInspectionItem>   (JO + IAR)
   └── GET /api/v1/assetregister/inspections/pending-for-me   → IReadOnlyList<PendingInspectionItem>   (Returned Property)
        each handler: resolve current user → EmployeeId → filter own pending inspectables → map to PendingInspectionItem
   ▼
   one sorted worklist; each row → navigate(item.ActionRoute) → existing inspect flow
```

`PendingInspectionItem` lives in `BuildingBlocks/Shared` (every module's `.Contracts` already references
Shared); the Blazor page references the module contracts it aggregates (it already references both).

---

## 4. Shared contract (`BuildingBlocks/Shared`)

```csharp
namespace AMIS.Framework.Shared.Inspections;

/// <summary>One item awaiting the current user's inspection, in a module-agnostic shape so a unified
/// worklist can merge items from any source. Producers map their own pending records to this.</summary>
public sealed record PendingInspectionItem(
    string SourceType,            // "JobOrder" | "IAR" | "ReturnedProperty" — drives the type chip/icon
    Guid SourceId,
    string Reference,             // JO #, IAR #, Receipt #
    string Title,                 // supplier / short description for the row
    DateTimeOffset RequestedOnUtc,// when it became pending (Issued / SubmittedForInspection / created)
    string ActionRoute);          // SPA deep-link that opens the inspect flow
```

> Keep `SourceType` a string (not an enum) so a new producer needs **zero** changes to Shared — the worklist
> maps known values to icons and falls back gracefully for unknown ones.

---

## 5. Producer side — per module

### 5.1 ProcurementAcquisition (JO + IAR — one endpoint)

`Modules.ProcurementAcquisition.Contracts`:

```csharp
public sealed record GetMyPendingProcurementInspectionsQuery
    : IQuery<IReadOnlyList<PendingInspectionItem>>;
```

Handler (`Features/v1/Inspections/GetMyPendingInspections/`):
1. Resolve current user → EmployeeId (mirror `InspectJobOrderCommandHandler`:
   `GetEmployeeReferenceByIdentityUserIdQuery`). If no employee → return empty.
2. JOs: `Status == Issued && InspectorId == me` → map
   (`SourceType:"JobOrder"`, `Reference: JoNumber`, `Title: SupplierName`,
   `RequestedOnUtc: IssuedOnUtc`, `ActionRoute: $"/procurement/job-orders?inspect={Id}"`).
3. IARs: `Status == PendingInspection && AssignedInspectorId == me` → map
   (`SourceType:"IAR"`, `Reference: IarNumber`, `Title: SupplierName`,
   `RequestedOnUtc: SubmittedForInspectionOnUtc`,
   `ActionRoute: $"/procurement/inspection-acceptance-reports/{Id}/inspect"`).
4. Concatenate, return.

Endpoint: `GET /api/v1/procurement/inspections/pending-for-me`
`WithName("Procurement_GetMyPendingInspections")` · `.RequireAuthorization()` (self-scoped; no extra perm).

> Note: `JobOrderSummaryDto` has no `InspectorId`, so query the JO entities directly (or project) in the
> handler rather than reusing `SearchJobOrdersQuery`. `InspectionAcceptanceReportSummaryDto` exposes
> `AssignedInspectorId` but **not** `SubmittedForInspectionOnUtc` (that's on the full `…ReportDto` only) — so
> query IAR entities directly too, to get the `RequestedOnUtc` timestamp.
>
> Employee resolution: mirror `InspectJobOrderCommandHandler` — `currentUser.GetUserId()` →
> `GetEmployeeReferenceByIdentityUserIdQuery` (from `MasterData.Contracts`, already referenced). No employee → empty.

### 5.2 AssetRegister (Returned Property)

`Modules.AssetRegister.Contracts`:

```csharp
public sealed record GetMyPendingReturnedPropertyInspectionsQuery
    : IQuery<IReadOnlyList<PendingInspectionItem>>;
```

Handler: **reuse the existing `CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, ct)`** already
used by `InspectReturnedPropertyReceiptCommandHandler` (AssetRegister already references `MasterData.Contracts`
— no new dependency). Query entities: `Status == Pending && AssignedInspector.EmployeeId == me` (the inspector
is an **owned `EmployeeRef`**, so filter `.AssignedInspector.EmployeeId`, not a flat column) → map:
- `SourceType:"ReturnedProperty"`
- `Reference:` **`AccountabilityDocumentNo` — NOT `ReceiptNo`.** `ReceiptNo` is `string?` and is only assigned
  on *acceptance*; a *pending* row has none, so using it would yield blank references. The accountability
  document number is the only stable identifier at the Pending stage.
- `Title:` `$"{ReceiptType} · {ItemCount} item(s)"` (or the returner's name) — `AccountabilityDocumentNo` is
  already the Reference, so don't repeat it here.
- `RequestedOnUtc:` the entity's creation timestamp (`CreatedOnUtc`). The summary's `Date` is a `DateOnly`
  (the return date), not the pending-since instant — query the entity for the `DateTimeOffset`.
- `ActionRoute: $"/asset-register/returned-property?inspect={Id}"`.

Endpoint: `GET /api/v1/assetregister/inspections/pending-for-me`
`WithName("AssetRegister_GetMyPendingInspections")`.

Also: add the **`?inspect={id}` auto-open** to `ReturnedPropertyPage.razor` (mirror the JO page change —
`[SupplyParameterFromQuery]` + open the inspect dialog once in `OnAfterRenderAsync`).

---

## 6. Blazor — the worklist

| Piece | File | Note |
| --- | --- | --- |
| Manual API clients | `ApiClient/InspectionsClient.cs` | `IInspectionsClient` with `GetMyPendingProcurementAsync()` + `GetMyPendingReturnedPropertyAsync()`; returns `IReadOnlyList<PendingInspectionItem>`. Mirror `NotificationsClient` (manual, `JsonSerializerDefaults.Web`). Register in `ApiClientRegistration.cs`. |
| Page | `Components/Pages/Inspections/MyInspectionsPage.razor` (`@page "/inspections"`) | Parallel-await both clients, merge, sort by `RequestedOnUtc` asc; group by `SourceType` (sections) or show a type chip. Each row → `Navigation.NavigateTo(item.ActionRoute)`. Empty state "Nothing awaiting your inspection." Pull-to-refresh button. |
| Nav item | `Components/Layout/NavMenu.razor` | "My Inspections" link (icon `FactCheck`/`Checklist`) under **Procurement Acquisition**, or a top-level **My Work** group. Optional unread-style **count badge** of pending items. |
| (Optional) count state | `Services/Inspections/InspectionQueueState.cs` | Mirror `INotificationState`; load count once, refresh on page visit. v1 can skip and just show a count fetched on nav render. |

Permissions: the worklist is visible to authenticated users (each query returns only the caller's
assignments, so a non-inspector simply sees an empty list). Per-action permissions remain enforced by each
module's inspect endpoint.

---

## 7. Notifications integration

- **Keep** notification deep-links specific (they open the action directly): JO `?inspect=`, IAR inspect
  page, RP `?inspect=`. These are the best per-event UX.
- The worklist is the **pull** view ("everything on my plate"); notifications are the **push** nudge.
- Optional polish: when the bell badge is non-zero, a "View all inspections" affordance can jump to
  `/inspections`. And the inspection count badge can refresh off the same `NotificationCreated` hub push
  (an `InspectionRequested`-typed notification arriving means a new pending item) — reuse, don't rebuild.

---

## 8. Phase checklist

- [x] **P1** `BuildingBlocks/Shared` → `PendingInspectionItem` (`Shared/Inspections/PendingInspectionItem.cs`).
- [x] **P2** ProcurementAcquisition: `GetMyPendingProcurementInspectionsQuery` + handler (JO + IAR) + endpoint
      (`/api/v1/procurement/inspections/pending-for-me`, `WithName("Procurement_GetMyPendingInspections")`).
- [x] **P3** AssetRegister: `GetMyPendingReturnedPropertyInspectionsQuery` + handler + endpoint
      (`/api/v1/asset-register/inspections/pending-for-me`); added `?inspect={id}` auto-open to `ReturnedPropertyPage`.
- [x] **P4** Blazor: `InspectionWorklistClient` (renamed from `InspectionsClient` — name clashed with a pre-existing
      NSwag-generated `InspectionsClient`) + registration, `MyInspectionsPage` (`/inspections`), top-level "My Inspections" nav item.
- [x] **P5** Tests: `GetMyPendingProcurementInspectionsTests` (2) + `GetMyPendingReturnedPropertyInspectionsTests` (2)
      — filter to caller's pending items only, correct routes, empty when unlinked. Build 0 errors; 47 architecture tests pass.

---

## 9. Smoke test

1. Assign the logged-in user as inspector on: a JO (→ Issue it), an IAR (→ submit for inspection), and a
   Returned Property request.
2. Open **My Inspections** → all three appear, oldest-first, each with the right type chip.
3. Click each → lands in that type's inspect flow (JO dialog, IAR page, RP dialog).
4. Complete one inspection → it drops off the worklist on refresh.
5. A JO assigned to a **different** inspector does **not** appear in your list.

---

## 10. Out of scope (v1)

- **Team/role queue** (unassigned items any inspector can pick up) — v1 is assigned-to-me only.
- A universal inspection **form** (each type keeps its own).
- Reassignment from within the worklist (use each module's existing reassign flow).
- Live count via SignalR (optional; reuse the notification hub if added).

---

## 11. Reference index

| Topic | Path |
| --- | --- |
| JO contracts (InspectorId, Status, summary) | `Modules.ProcurementAcquisition.Contracts/v1/JobOrders/JobOrderContracts.cs` |
| IAR contracts (AssignedInspectorId, PendingInspection) | `Modules.ProcurementAcquisition.Contracts/v1/InspectionAcceptanceReports/InspectionAcceptanceReportContracts.cs` |
| Returned Property contracts (AssignedInspectorEmployeeId, Pending) | `Modules.AssetRegister.Contracts/v1/ReturnedProperty/ReturnedPropertyContracts.cs` |
| Current-user → employee resolution | `…/JobOrders/InspectJobOrder/InspectJobOrderCommandHandler.cs` (`GetEmployeeReferenceByIdentityUserIdQuery`) |
| JO `?inspect=` auto-open (pattern to reuse) | `Components/Pages/Procurement/JobOrdersPage.razor` |
| IAR inspect page (existing route) | `Components/Pages/InspectionAcceptanceReports/InspectionAcceptanceReportInspectionPage.razor` |
| Manual client + state patterns to mirror | `ApiClient/NotificationsClient.cs`, `Services/Notifications/NotificationState.cs` |
| EmployeeId in session | `Services/UserProfileState.cs` (`EmployeeId`) |
```
