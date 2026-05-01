$path = 'e:\AMIS101\PROCUREMENTPLANNING-DOMAIN-OVERHAUL-PLAN.md'
$content = @'
# ProcurementPlanning Domain Overhaul Plan

## Goal

Align the Procurement Planning module with the actual business process:

1. End-user or section prepares the PPMP.
2. Consolidator receives approved PPMP copies and builds the APP.
3. APP is always a consolidation document, not the original planning source.
4. Indicative, Final, and Updated are distinct planning stages.
5. Updated APP versions are rebuilt from the approved PPMP versions currently in force.

## Target Model

### PPMP

- Source planning document per office or section.
- Owns the procurement items.
- Evolves by phase and version:
  - Indicative
  - Final
  - Updated
- Approval happens at PPMP level before consolidation.

### APP

- Consolidation document for a fiscal year and stage.
- Built only from selected approved PPMP versions.
- Never treated as the original source of procurement intent.

### New APP child entities

- `AppSourcePpmp`
  - Records which approved PPMP versions were included in an APP version.
  - Mirrors the manual copies furnished to the consolidator step.
- `AppLineItem`
  - Denormalized copy of procurement rows consolidated from PPMP items.
  - Keeps lineage to the PPMP and PPMP item that produced it.

## Phase Plan

### Phase 1: Introduce the new APP child model

- Add `AppSourcePpmp`.
- Add `AppLineItem`.
- Update `AnnualProcurementPlan` to maintain `SourcePpmps` and `LineItems`.

Status: Complete

### Phase 2: Remove legacy PPMP to APP coupling

- Remove `Ppmp.AppId`.
- Remove APP ownership from PPMP state transitions.
- Keep `Consolidated` as a PPMP status only.

Status: Complete

### Phase 3: Remove optimistic row-version dependency

- Remove active `byte[] Version` reliance from APP and PPMP workflow code paths.
- Replace row-version conflict handling with explicit status and workflow guards.

Status: Complete

### Phase 4: Move APP reads to the new model

- Rewrite APP projections to read from `AppLineItem`.
- Remove APP snapshot dependency from read paths.
- Make totals and item counts come directly from APP-owned rows.

Status: Complete

### Phase 5: Rewrite workflow handlers

- Consolidate using APP-owned child rows.
- Publish and approve without snapshot duplication.
- Delete and availability checks based on `AppSourcePpmp` and `AppLineItem`.

Status: Complete

### Phase 6: Clean schema and migrations

- Drop `AppLineReference`.
- Drop `AppSnapshot` and `AppSnapshotItem`.
- Remove `Ppmp.AppId` and row-version columns from the schema.
- Add migration for the final target schema.

Status: Complete

### Phase 7: UI and client alignment

- Update Blazor screens and API clients to the new APP item shape.
- Expose source PPMP traceability in the APP experience.

Status: Complete

## Business Rules Locked In

- PPMP is the source document.
- APP is the consolidation document.
- Updated APP is a new consolidation of approved PPMP versions currently in force.
- APP lines must keep lineage back to source PPMP and PPMP item.
- The system models the authoritative workflow, while keep file and furnish copy become immutable stored versions and explicit APP source records.

## Work Done In This Checkpoint

- Added `AppSourcePpmp` and `AppLineItem` as the new APP-owned child model.
- Updated `AnnualProcurementPlan` to maintain `SourcePpmps` and `LineItems` during consolidation and APP updates.
- Removed active PPMP reliance on `AppId`.
- Removed active row-version reliance from the current PPMP and APP workflow code paths.
- Moved APP read projection, search, version summaries, publish, approve, consolidate, and delete flows to the new APP-owned model.
- Completed schema cleanup migration to remove legacy snapshot and line-reference tables and obsolete columns.

## Review Notes

- This file is the working implementation plan for the overhaul.
- Update phase status as code lands.
'@
[System.IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::UTF8)
Write-Output 'Plan markdown refreshed.'
