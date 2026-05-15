# IAR Stage-Based Workflow — Progress Tracker

Companion to [IAR-WORKFLOW-PLAN.md](IAR-WORKFLOW-PLAN.md).

---

## Phase 1 — Domain + Contracts + Migration (in progress)

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

### In progress

- [ ] **New feature slices (3 of 6)**: `AssignPropertyNo/`, `ExpandLineByQuantity/`, `CancelAssetIAR/`.

### Pending

- [ ] **Update Accept handler**: filter `AssetIARAcceptedEvent` payload to non-Rejected lines.
- [ ] **Permissions + module wiring**:
  - Add to `AssetProcurementModuleConstants.Permissions.AssetIARs`: `SubmitForInspection`, `Inspect`, `AssignPropertyNo`, `Cancel`.
  - Register in `AssetProcurementModule.RegisteredPermissions`.
  - Wire all 6 new endpoints in `MapEndpoints`.
- [ ] **EF configuration**: add 4 audit columns to `AssetIARConfiguration`.
- [ ] **EF migration** `IAR_StageWorkflow` (additive: new audit columns only — line items are JSON, deserialize with defaults).
- [ ] **Domain unit tests**: state-transition matrix + per-method behavior + `Accept` skipping Rejected lines + `ExpandLineByQuantity`.
- [ ] **Build with 0 warnings + run all tests.**

---

## Phase 2 — Inspection page + Submit flow + tabs (not started)

## Phase 3 — Acceptance page + line expansion + property no (not started)

## Phase 4 — Cleanup (not started)
