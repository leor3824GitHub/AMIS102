# IAR UI Overhaul Plan (Minimal Input, Role-Based)

## 1. Problem Statement

Current IAR screens still ask users to encode data that is already available from Purchase Order, line items, and master records. This creates repetitive work, slower processing, and more encoding errors.

This overhaul shifts IAR into strict role-based screens with minimal manual input.

## 2. Goals

1. Remove repeated data entry across Draft, Inspection, and Acceptance.
2. Keep each role focused on only the decisions they own.
3. Auto-hydrate header and line details from existing data sources.
4. Ensure only inspected and passed lines proceed to Acceptance and property generation.
5. Enable Property Custodian or Supply Officer to generate property numbers and print Exhibit 3 for signature.

## 3. Roles and Ownership

- Requesting user (Property Custodian or Supply Officer): creates Request for Inspection from PO.
- Inspector: records inspection decision per selected line.
- Property Custodian or Supply Officer: final acceptance, property number generation, Exhibit 3 print.

## 4. Target UX Architecture (3 Dedicated UIs)

### A. Request for Inspection UI (new/overhauled Draft)

Purpose: create inspection request only, not full manual IAR encoding.

Required inputs:

- Open PO (autocomplete)
- Inspector (employee autocomplete from MasterData)
- Selected lines from PO for inspection (checkbox/select rows)

Read-only/auto-filled from PO and related records:

- Supplier
- PO number and PO date
- Office/department and responsibility center code (if available in PO/PR context)
- Item description, unit, quantity, unit cost, brand/model/specs where available

Rules:

- No manual line typing in this screen.
- No property number fields here.
- Only selected PO lines are included in inspection request.
- Save as Draft and Submit for Inspection actions remain.

Primary file targets:

- src/Playground/Playground.Blazor/Components/Pages/AssetProcurement/AssetIARDraftDialog.razor
- src/Playground/Playground.Blazor/ApiClient/AssetProcurementClient.cs

### B. Inspector UI (minimal decision UI)

Purpose: inspector only confirms inspection outcome on selected lines.

Allowed inputs:

- Per line decision: Passed or Rejected
- Remarks only when Rejected (required)
- Optional inspection date override only if business requires; otherwise use system timestamp

Read-only:

- PO, supplier, item details, quantities
- Assigned inspector identity
- All procurement details

Rules:

- No editing of header and item master fields.
- Bulk action supported: Pass All, Reset All.
- Submit records inspection state and locks inspector editing unless reopened by authorized flow.

Primary file target:

- src/Playground/Playground.Blazor/Components/Pages/AssetProcurement/AssetIARInspectionPage.razor

### C. Acceptance UI (PC/SO minimal + property generation)

Purpose: finalize acceptance using only inspected lines.

Shown data:

- Only lines from the chosen PO and IAR that were inspected.
- Separate groups:
  - Passed lines: actionable
  - Rejected lines: read-only for audit trail

Allowed inputs/actions:

- Property number generation for passed lines
- Quantity expansion when needed (one line per unit)
- Final Accept action
- Print Exhibit 3 action

Read-only:

- Supplier/PO/header details
- Inspector decisions and remarks

Rules:

- Accept disabled until all passed lines have valid property numbers.
- Rejected lines are never included in asset registration payload.
- Print output follows Exhibit 3 structure and signature blocks.

Primary file target:

- src/Playground/Playground.Blazor/Components/Pages/AssetProcurement/AssetIARAcceptancePage.razor

## 5. Data Hydration Strategy (No Redundant Encoding)

### Source of truth

- PO header + PO lines: procurement module records
- Inspector list: employee master data (with permission/role filtering)
- Existing IAR status and line inspection records

### UI behavior

- Selecting PO triggers automatic load of all display fields.
- User only selects lines and inspector in Request for Inspection.
- Later stages never ask for data already persisted in earlier stages.

### Validation boundaries

- Request stage validates at least one selected line and assigned inspector.
- Inspection validates decision for each included line.
- Acceptance validates property number completeness for passed lines only.

## 6. API and Contract Adjustments

1. Add or confirm endpoint to fetch PO line candidates for inspection selection.
2. Add or confirm employee lookup endpoint filtered for inspector-eligible users.
3. Ensure inspection record endpoint accepts only per-line decisions and remarks.
4. Ensure acceptance endpoint processes passed lines only for registration event payload.
5. Add or confirm Exhibit 3 print endpoint/model (HTML or PDF render).

## 7. Exhibit 3 Printing Plan

### Output requirements

- Render Inspection and Acceptance Report (Exhibit 3) with:
  - Supplier
  - PO no/date
  - IAR no/date
  - Invoice no/date (if available)
  - Table of inspected items
  - Inspection section with inspector signature block
  - Acceptance section with Supply and/or Property Custodian signature block

### Technical approach

- Preferred: server-side HTML template rendered to PDF for consistent print layout.
- Fallback: print-optimized Razor page using browser print CSS.

### Trigger points

- Print button appears in Acceptance UI once status is Inspected or Accepted.
- Recommended final flow: Accept then Print Exhibit 3.

## 8. UX Simplification Checklist

- Remove free-form line entry from Request for Inspection.
- Remove duplicate header fields across stages.
- Keep all non-decision fields read-only in Inspector UI.
- Keep Acceptance focused on property generation and final confirmation only.
- Persist context in URL and state to avoid repeated lookups and reselection.

## 9. Implementation Phases

### Phase 1: Request for Inspection refactor

- Replace manual line editor with PO line selector table.
- Keep Save Draft and Submit for Inspection.
- Integrate inspector autocomplete from employee master data.

### Phase 2: Inspector UI hard-minimize

- Restrict editable controls to decision and rejection remarks.
- Keep bulk decision tools.
- Tighten permission and assignment checks.

### Phase 3: Acceptance UI finalize

- Display only inspected lines for selected PO/IAR.
- Enable property generation flow for passed lines.
- Keep rejected lines visible but non-actionable.

### Phase 4: Exhibit 3 print

- Implement print model and template.
- Add Print Exhibit 3 button in Acceptance UI.
- Validate output format against provided Exhibit 3 reference.

### Phase 5: Regression and UAT

- Validate end-to-end role handoff using real PO data.
- Confirm no duplicate data entry is required.
- Confirm asset registration receives only passed lines.

## 10. Acceptance Criteria

1. Request for Inspection requires only PO selection, line selection, and inspector selection.
2. Inspector screen does not allow editing PO/header/item master data.
3. Acceptance screen only acts on inspected items for the selected PO.
4. Property number generation is done in Acceptance by PC/SO.
5. Exhibit 3 can be printed with populated data and signature areas.
6. End-to-end flow eliminates repetitive encoding of already stored values.

## 11. Risks and Mitigations

- Risk: Missing PO metadata required by Exhibit 3.
  - Mitigation: define fallback mappings and optional fields in print template.
- Risk: Inspector lookup quality if role assignment is inconsistent.
  - Mitigation: support permission-based filter plus searchable employee fallback.
- Risk: Users rely on old all-in-one draft behavior.
  - Mitigation: staged rollout with quick guide and role-specific labels.

## 12. Suggested Delivery Artifacts

- UI updates in three pages
- API client updates for PO line selection and print
- Print template for Exhibit 3
- UAT checklist focused on data reuse and reduced encoding
