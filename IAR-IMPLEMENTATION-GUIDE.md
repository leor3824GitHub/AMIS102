# IAR Implementation Guide

> Comprehensive guide to the Item Acceptance Report (IAR) stage-based workflow, UI implementation, and progress tracking.

---

## Quick Links

- **[IAR-WORKFLOW-PLAN.md](IAR-WORKFLOW-PLAN.md)** — Complete workflow design, 3-stage model, domain changes, phase breakdown
- **[IAR-WORKFLOW-PROGRESS.md](IAR-WORKFLOW-PROGRESS.md)** — Real-time progress tracker with completed phases and current status
- **[IAR-UI-OVERHAUL-PLAN.md](IAR-UI-OVERHAUL-PLAN.md)** — Blazor UI redesign, tabbed views, inspection page

---

## Overview

The IAR Stage-Based Workflow splits the current single-form IAR into a **3-stage workflow** (Draft → Inspection → Acceptance) that mirrors the actual NFA paper process and matches each role's mental model.

### Key Improvements

✅ **Clear role separation:**
- Property Custodian owns Draft, Acceptance, Register stages
- Inspector owns Inspection stage only
- Inspector is assigned per IAR and can be reassigned while pending

✅ **Per-line inspection results:**
- Passed → proceeds to Acceptance
- Rejected → marked as rejected, excluded from asset registry
- No "Returned to Supplier" document (per direction)

✅ **Stage audit trail:**
- `SubmittedForInspectionOnUtc`, `InspectedOnUtc`, `AcceptedOnUtc`, `CancelledOnUtc`
- Links to responsible employees at each stage

✅ **New IAR UI page:**
- Tabbed list view (All | Draft | Pending Inspection | Inspected | Accepted | Cancelled)
- Stage-aware row actions
- Draft dialog with inspector autocomplete
- Inspection page with line-by-line results
- Acceptance form with Property No assignment

---

## 3-Stage Workflow

```
Draft ──Submit──► PendingInspection ──Record──► Inspected ──Accept──► Accepted
  │                       │                          │
  └───────────Cancel───────┴──────Cancel─────────────┘
													(Cancelled is terminal)
```

### Status Model

**IAR-level statuses:**
- `Draft = 0` — Initial state, property custodian editing
- `PendingInspection = 1` — Awaiting inspector assignment and review
- `Inspected = 2` — Inspector recorded results, awaiting custodian acceptance
- `Accepted = 3` — Accepted by custodian, materializes AssetRegistry rows
- `Cancelled = 4` — Cancelled (terminal, no reason required)

**Per-line inspection results:**
- `Pending` — awaiting inspection
- `Passed` — inspector approved, will proceed to registry
- `Rejected` — inspector rejected with remarks, excluded from registry

---

## Roles & Permissions

| Role | Identity | Owns Stages | New Permission |
|------|----------|-------------|-----------------|
| Property Custodian | `ReceivedById` (renamed: `PropertyCustodianId`) | Draft, Acceptance, Register | `IARs.Manage` |
| Inspector | Assigned per IAR | Inspection only | `IARs.Inspect` |

**Assignment:** Property Custodian picks inspector via autocomplete at submit time. Inspector can be reassigned while IAR is `PendingInspection`.

---

## Domain Changes (Phase 1 ✅)

### New Enum Values
- `AssetIARStatus`: Added `PendingInspection (3)`, `Inspected (4)`, `Cancelled (5)`. Legacy `Rejected (2)` retained.
- `LineInspectionResult`: New enum (`Pending | Passed | Rejected`)

### New Fields on AssetInspectionAcceptanceReport
- `SubmittedForInspectionOnUtc`
- `InspectedOnUtc`
- `AcceptedOnUtc`
- `CancelledOnUtc`

### New Fields on AssetIARLineItem
- `InspectionResult` (LineInspectionResult, optional)
- `InspectedOnUtc` (DateTimeOffset, optional)
- `InspectedById` (Guid, optional)

### New Domain Methods
- `SubmitForInspection(inspectorId)` — Draft → PendingInspection
- `ReassignInspector(newInspectorId)` — valid while PendingInspection
- `RecordInspection(actorId, decisions)` — PendingInspection → Inspected, per-line results
- `AssignPropertyNo(assetRegistryId, propertyNo)` — Inspected, per line
- `ExpandLineByQuantity(lineId, newQuantity)` — splits 1 line into N
- `Cancel()` — Draft/PendingInspection/Inspected → Cancelled

---

## Blazor UI Implementation (Phases 2-3 ✅)

### Pages & Components

**AssetIARsPage.razor** — Main list view
- Tabbed interface: All | Draft | Pending Inspection | Inspected | Accepted | Cancelled
- Stage-aware row actions:
  - Draft: `Edit`, `Submit for Inspection`
  - PendingInspection: `Inspect` (assigned only) or `Reassign Inspector`
  - Inspected: `Acceptance`
  - Accepted/Cancelled: `View`

**AssetIARDraftDialog.razor** — Draft creation/editing
- PO selection with line items pre-fill
- Inspector autocomplete (filters to `IARs.Inspect` permission)
- Required IAR header fields
- Line item grid with add/edit/delete
- Buttons: `Save Draft`, `Save & Submit for Inspection`

**AssetIARInspectionPage.razor** — Inspection workflow
- Path: `/asset-procurement/iars/{Id:guid}/inspect`
- Show IAR header, assigned inspector info
- Per-line inspection controls:
  - Radio: Passed | Rejected
  - Remarks field (required if Rejected)
  - Condition dropdown (InGoodCondition | NeedingRepair | Unserviceable)
- Buttons: `Save Progress`, `Complete Inspection`
- After completion, custodian must accept

**AssetIARAcceptancePage.razor** — Acceptance & Property assignment
- Show IAR header with line items filtered to `Passed` status
- Per-line Property No field using existing `<PropertyNoField>` component
- Acceptance date
- Buttons: `Accept`, `Reject Inspection (back to Inspector)`
- On Accept, materializes AssetRegistry + ReceivingReport

---

## Implementation Progress

### Phase 1: Domain + Contracts + Migration ✅ COMPLETE

**Completed:**
- ✅ New enum values and contract fields
- ✅ Domain aggregate methods for all 6 stage operations
- ✅ EF Core migration for new columns
- ✅ 3 feature slices: SubmitForInspection, ReassignInspector, RecordInspection
- ✅ 3 more feature slices: AssignPropertyNo, ExpandLineByQuantity, CancelAssetIAR
- ✅ Domain tests for all state transitions and validation guards
- ✅ AssetProcurement.Tests: 176+ passing

### Phase 2: Blazor UI - Draft & List ✅ COMPLETE

**Completed:**
- ✅ Tabbed IAR list page with stage-aware row actions
- ✅ Draft dialog for creation/editing with PO pre-fill
- ✅ Inspector autocomplete with permission filtering
- ✅ Save Draft and Save & Submit functionality

### Phase 3: Blazor UI - Inspection & Acceptance ✅ COMPLETE

**Completed:**
- ✅ Inspection page with per-line results (Passed/Rejected)
- ✅ Remarks field for rejected items
- ✅ Condition dropdown per line
- ✅ Acceptance page with Property No assignment
- ✅ PropertyNoField component integration (no duplication — reuses existing component)
- ✅ Accept/Reject back-to-inspector flow

### Phase 4: Reports & Analytics 🔄 IN PROGRESS

- 🔄 IAR analytics dashboard
- 🔄 Inspector performance metrics
- 🔄 Stage cycle-time tracking

### Phase 5: Advanced (Deferred)

- ⏳ Bulk operations (accept multiple IARs)
- ⏳ Workflow notifications
- ⏳ Mobile/MAUI support

---

## SOP GS-PD26 Alignment

The workflow now implements the official paper form (SOP GS-PD26) stages:

| Form Section | IAR Stage | Handler |
|--------------|-----------|---------|
| Header + Supplier Info | Draft | CreateAssetIAR |
| Line items from RFI | Draft → PendingInspection | SubmitForInspection |
| Inspection block | Inspected | RecordInspection |
| Per-line Stock/Property No | Inspected → Accepted | AssignPropertyNo |
| Acceptance signatures | Accepted | AcceptAssetIAR |

---

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Inspector selection | Assigned per IAR | Matches paper process; allows reassignment if primary unavailable |
| Rejection | Per-line only | Allows partial acceptance; no whole-IAR rejection |
| PropertyNo assignment | At acceptance time | Matches property custodian's explicit workflow; human-in-the-loop |
| Returned to Supplier | None (per direction) | Simplifies workflow; focus on acceptance |

---

## Testing & Verification

### Unit Tests ✅
- Domain state transitions (all 6 paths)
- Validator guards (inspector permissions, line counts, etc.)
- Event emission on each stage transition

### Integration Tests ✅
- Cross-module: AssetProcurement ↔ AssetRegister event flow
- Blazor: Draft → Submit → Inspect → Accept full cycle
- Database: EF migrations and concurrency handling

### Manual Smoke Tests ✅
- Draft creation and editing
- Submit with inspector assignment
- Inspector reassignment (while PendingInspection)
- Record inspection with Passed/Rejected lines
- Accept with Property No assignment
- Cancel flow

---

## Related Documentation

- **[ASSETMANAGEMENT-DOCUMENTATION.md](ASSETMANAGEMENT-DOCUMENTATION.md)** — Gap 2 (PropertyNo generator) details
- **[ASSET-REGISTER-DOCUMENTATION.md](ASSET-REGISTER-DOCUMENTATION.md)** — AssetRegistry integration
- **[CLAUDE.md](CLAUDE.md)** — Development patterns and conventions

---

## Support

For questions on specific sections, refer to the primary documentation files:
- Workflow design: [IAR-WORKFLOW-PLAN.md](IAR-WORKFLOW-PLAN.md)
- Current progress: [IAR-WORKFLOW-PROGRESS.md](IAR-WORKFLOW-PROGRESS.md)
- UI implementation: [IAR-UI-OVERHAUL-PLAN.md](IAR-UI-OVERHAUL-PLAN.md)

