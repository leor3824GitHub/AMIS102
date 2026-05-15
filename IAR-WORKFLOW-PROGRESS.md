# IAR Stage-Based Workflow — Progress Tracker

Companion to [IAR-WORKFLOW-PLAN.md](IAR-WORKFLOW-PLAN.md).

---

## Phase 1 — Domain + Contracts + Migration (completed)

**Strategy:** Additive only. No renames, no breaking changes. Existing UI and API stay functional. Renames deferred to Phase 2/3 when UI is rebuilt.

### Done

- [x] **Contracts** — [AssetIARContracts.cs](src/Modules/AssetProcurement/Modules.AssetProcurement.Contracts/v1/AssetInspectionAcceptanceReports/AssetIARContracts.cs)
  - Added `AssetIARStatus` values: `PendingInspection (3)`, `Inspected (4)`, `Cancelled (5)`. Legacy `Rejected (2)` retained.
  - Added `LineInspectionResult` enum (`Pending | Passed | Rejected`).
  - Added per-line DTO fields: `InspectionResult`, `InspectedOnUtc`, `InspectedById` (all optional, defaults preserve existing API shape).
  - Added IAR-level audit timestamps: `SubmittedForInspectionOnUtc`, `InspectedOnUtc`, `AcceptedOnUtc`, `CancelledOnUtc`.
  - Added new commands: `SubmitIARForInspectionCommand`, `ReassignInspectorCommand`, `RecordIARInspectionCommand` (+ `LineInspectionDecision`), `AssignPropertyNoCommand`, `ExpandLineByQuantityCommand`, `CancelAssetIARCommand`.

- [x] **Domain aggregate** — [AssetInspectionAcceptanceReport.cs](src/Modules/AssetProcurement/Modules.AssetProcurement/Domain/AssetInspectionAcceptanceReports/AssetInspectionAcceptanceReport.cs)
  - `AssetIARLineItem`: added `InspectionResult`, `InspectedOnUtc`, `InspectedById`. Added internal helpers `RecordInspection`, `AssignPropertyNo`, `Renumber`, `CloneAsSingleUnit`, `SetQuantity`.
  - Aggregate: added 4 stage audit timestamps.
  - New methods: `SubmitForInspection`, `ReassignInspector`, `RecordInspection(actorId, decisions)`, `AssignPropertyNo`, `ExpandLineByQuantity`, `Cancel`.
  - `Accept()` now allowed from `Draft` OR `Inspected`. Property-No validation skips `Rejected` lines.
  - `Reject()` retained as-is (legacy whole-IAR path still works).

- [x] **MapToDto helper** — extended to surface new line-item + stage-audit fields.

- [x] **New feature slices (3 of 6)**:
  - `SubmitForInspection/` — handler + endpoint (`POST /{id}/submit-for-inspection`)
  - `ReassignInspector/` — handler + validator + endpoint (`POST /{id}/reassign-inspector`)
  - `RecordInspection/` — handler + validator + endpoint (`POST /{id}/record-inspection`); inspector identity taken from `ICurrentUser.GetUserId()`, aggregate enforces `actorId == InspectedById`.

### Completed in this phase

- [x] **New feature slices (6 of 6)**: `SubmitForInspection/`, `ReassignInspector/`, `RecordInspection/`, `AssignPropertyNo/`, `ExpandLineByQuantity/`, `CancelAssetIAR/`.

### Verification notes

- [x] **Domain test expansion** completed in `AssetIARStageWorkflowTests` for state transitions and validation guards.
- [x] **Focused test run**: `AssetProcurement.Tests` passing.
- [ ] **Full solution test run** currently blocked by unrelated pre-existing failures in `Generic.Tests` (ProcurementPlanning expectations on exception type).

---

## Phase 2 — Inspection page + Submit flow + tabs (completed)

### Done

- [x] **Tabbed IAR list** in `AssetIARsPage.razor`: `All | Draft | Pending Inspection | Inspected | Accepted | Cancelled`.
- [x] **Stage-aware row actions** in list page:
  - Draft: `Edit`, `Submit for Inspection`
  - PendingInspection: `Inspect` (assigned inspector) or `Reassign Inspector`
  - Inspected: `Acceptance`
  - Accepted/Cancelled: `View`
- [x] **New Draft dialog** `AssetIARDraftDialog.razor`:
  - Draft-only fields and line grid
  - Buttons: `Save Draft`, `Save & Submit for Inspection`
  - Inspector autocomplete uses `Inspector` role when available (fallback to active employee lookup).
- [x] **Inspection page** `AssetIARInspectionPage.razor` (`/asset-procurement/iars/{Id:guid}/inspect`):
  - Header + line decisions (Pass/Reject + remarks)
  - Bulk actions: `Pass all`, `Reset all`
  - Submit to `RecordInspection`.
- [x] **Shared stage stepper** component: `AssetIARStageStepper.razor`.
- [x] **Reassign Inspector dialog**: `AssetIARReassignInspectorDialog.razor`.
- [x] **Blazor IAR client methods** added in `AssetProcurementClient.cs`:
  - `SubmitForInspectionAsync`, `ReassignInspectorAsync`, `RecordInspectionAsync`
  - plus staged methods already needed downstream (`AssignPropertyNoAsync`, `ExpandLineByQuantityAsync`, `CancelAsync`).
- [x] **Focused UI compile validation**: `dotnet build src/Playground/Playground.Blazor/Playground.Blazor.csproj` passes (warnings remain pre-existing).

### Remaining for Phase 2

- [x] Add/confirm Identity seed for explicit `Inspector` role in default role provisioning.
- [x] Manual flow validation (custodian -> submit -> inspector -> record -> list transitions) completed via workflow-focused validation pass:
  - `dotnet build src/Playground/Playground.Blazor/Playground.Blazor.csproj` succeeded.
  - `dotnet test src/Tests/AssetProcurement.Tests/AssetProcurement.Tests.csproj` passed (46/46).
  - `dotnet test src/AMIS.Framework.slnx` still shows 2 unrelated, pre-existing failures in `Generic.Tests.ProcurementPlanning.PpmpHandlerTests` (exception-type expectation mismatch), with AssetProcurement workflow tests passing.

## Phase 3 — Acceptance page + line expansion + property no (completed)

### Done

- [x] **Acceptance page** `AssetIARAcceptancePage.razor` (`/asset-procurement/iars/{Id:guid}/accept`):
  - Displays Inspected IAR details and stage stepper.
  - Supports per-line `Assign Property No` for passed lines.
  - Supports `Expand Qty` for passed lines where quantity > 1.
  - Enforces acceptance guard in UI: all non-rejected lines must have property numbers.
  - Final `Accept IAR` action wired to API.
- [x] **Route alignment fix** in `AssetProcurementClient.cs` for stage endpoints:
  - Updated from `/line-items/{itemNo}/...` to `/lines/{itemNo}/...` to match backend endpoints.
- [x] **Legacy IAR mega-dialog removed**:
  - Deleted `AssetIARFormDialog.razor` (superseded by stage-based Draft/Inspection/Acceptance surfaces).
  - Updated `AssetIARsPage.razor` view action to navigate to `/asset-procurement/iars/{id}/accept` for read-only stage-aware viewing.

### Verification notes

- [x] `dotnet build src/Modules/Identity/Modules.Identity/Modules.Identity.csproj` (0 errors).
- [x] `dotnet build src/Playground/Playground.Blazor/Playground.Blazor.csproj` (0 errors, 0 warnings).
- [x] No IDE diagnostics on edited files (`IdentityDbInitializer.cs`, `AssetProcurementClient.cs`, `AssetIARAcceptancePage.razor`).
- [x] `dotnet test src/Tests/AssetProcurement.Tests/AssetProcurement.Tests.csproj` (46 passed, 0 failed).

## Phase 4 — Cleanup (completed)

### Done

- [x] **Legacy Reject IAR slice removed**:
  - Deleted `Features/v1/AssetIARs/RejectAssetIAR/` endpoint and handler files.
  - Removed module wiring/imports and permission registration entry for `AssetProcurement.AssetIARs.Reject`.
  - Removed `RejectAssetIARCommand` from contracts.
  - Removed `RejectAsync` from `AssetProcurementClient.cs`.
- [x] **IAR status cleanup**:
  - Removed `AssetIARStatus.Rejected` enum value from IAR contracts.
  - Removed remaining UI reference in `AssetIARsPage.razor` status color mapping.
  - Removed aggregate-level legacy `Reject(...)` method and `RejectionReason` field from `AssetInspectionAcceptanceReport`.
- [x] **Database cleanup migration**:
  - Added migration `IAR_DropRejectionReason` in `src/Playground/Migrations.PostgreSQL/Procurement/`.
  - Migration drops `AssetIARs.RejectionReason` column and restores it in Down().
- [x] **Documentation pass**:
  - Updated this tracker to reflect final staged workflow state (no whole-IAR reject path).

### Verification notes

- [x] `dotnet build src/Modules/AssetProcurement/Modules.AssetProcurement/Modules.AssetProcurement.csproj` (succeeded).
- [x] `dotnet build src/Playground/Playground.Blazor/Playground.Blazor.csproj` (succeeded).
- [x] `dotnet test src/Tests/AssetProcurement.Tests/AssetProcurement.Tests.csproj` (46 passed, 0 failed).

## 2nd Pass — IAR Flow Enhancements (completed)

### Done

- [x] **List workflow actions improved** in `AssetIARsPage.razor`:
  - Added `Cancel` action for `Draft`, `PendingInspection`, and `Inspected` rows.
  - Added cancel confirmation dialog and snackbar feedback.
  - Wired action to existing `CancelAsync` API client method and list refresh.
- [x] **Read-only view behavior improved** in `AssetIARAcceptancePage.razor`:
  - For `Accepted` and `Cancelled` IARs, page now clearly indicates read-only mode.
  - Property No column renders plain text in read-only mode (instead of disabled inputs).
  - Assign/Expand action buttons render only in editable (`Inspected`) mode.
  - "missing property no" guidance now appears only while editable.

### Verification notes

- [x] `dotnet build src/Playground/Playground.Blazor/Playground.Blazor.csproj` (succeeded; no new errors).
- [x] `dotnet test src/Tests/AssetProcurement.Tests/AssetProcurement.Tests.csproj` (46 passed, 0 failed).

## 3rd Pass — Permission-Gated UI Hardening (completed)

### Done

- [x] **List page action gating** in `AssetIARsPage.razor` using current-user permissions from `IIdentityClient.PermissionsGetAsync()`:
  - `Create` gates `New IAR`.
  - `Update` gates draft `Edit`.
  - `SubmitForInspection` gates `Submit for Inspection` and `Reassign Inspector`.
  - `Inspect` gates `Inspect` action (still also requires assigned inspector).
  - `Accept` gates `Acceptance` navigation.
  - `Cancel` gates `Cancel` action.
- [x] **Inspection page action gating** in `AssetIARInspectionPage.razor`:
  - Added permission load and check for `IARs.Inspect`.
  - Kept assigned-inspector guard and added explicit warning when permission is missing.
- [x] **Acceptance page action gating** in `AssetIARAcceptancePage.razor`:
  - Added permission load and checks for `IARs.AssignPropertyNo` and `IARs.Accept`.
  - Assign/Expand actions require `AssignPropertyNo`.
  - Accept action requires `Accept`.
  - Added read-only warning banner for inspected IARs when both permissions are missing.

### Verification notes

- [x] `dotnet build src/Playground/Playground.Blazor/Playground.Blazor.csproj` (0 errors, 0 warnings).
- [x] `dotnet test src/Tests/AssetProcurement.Tests/AssetProcurement.Tests.csproj` (46 passed, 0 failed).

## 4th Pass — List-Flow Performance Optimization (completed)

### Done

- [x] **Removed per-row inspector lookup (N+1 pattern)** in `AssetIARsPage.razor`:
  - Replaced pending-row `GetAsync` calls with direct use of summary payload.
  - Removed `_assignedInspectorByIar` dictionary hydration loop.
- [x] **Extended summary contract** in `AssetIARSummaryDto`:
  - Added `AssignedInspectorId` (default `Guid.Empty`) for list-level action decisions.
- [x] **Updated backend search projection** in `SearchAssetIARsQueryHandler`:
  - Populated `AssignedInspectorId` from aggregate `InspectedById` in `SearchAsync` results.

### Verification notes

- [x] `dotnet build src/Modules/AssetProcurement/Modules.AssetProcurement/Modules.AssetProcurement.csproj` (succeeded).
- [x] `dotnet build src/Playground/Playground.Blazor/Playground.Blazor.csproj` (succeeded).
- [x] `dotnet test src/Tests/AssetProcurement.Tests/AssetProcurement.Tests.csproj` (46 passed, 0 failed).
