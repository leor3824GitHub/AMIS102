# AssetManagement Additive Overhaul Plan

## Objective

Implement an additive-only overhaul for AssetManagement that:

1. Keeps agency/legal document entities separate (ICS, PAR, SMIR, PPEIR, RRSP, RRP).
2. Adds a unified current-state layer for all tangible assets.
3. Avoids delete/remove of existing entities, endpoints, and workflows.

## Design Position

Given agency forms are distinct, the system should be:

1. Unified by asset identity and current state.
2. Separated by legal document and compliance output.

This means:

1. `AssetRegistry` is the source of truth for current state.
2. Existing document aggregates remain immutable history and printable records.
3. `AssetAssignmentHistory` links document events to accountability transitions.

## Implemented (Current)

### New Domain Types

1. `AssetRegistry`
2. `AssetAssignmentHistory`
3. `Location`
4. `AssetLifecycleState`
5. `AssetAssignmentEventType`
6. `LocationType`

### Persistence Wiring

1. New entity configurations added for registry/history/location.
2. New DbSets added to `AssetManagementDbContext`.

### Write-Through Integration

The following handlers now write to the registry/history in addition to existing document tables:

1. `CreateTangibleInventory`
2. `CreateICS`
3. `CreatePAR`
4. `CreateSMIR`
5. `CreatePPEIR`
6. `CreateRRSP`
7. `CreateRRP`
8. `CreatePropertyIncidentReport`
9. `CreateUnserviceablePropertyReport`
10. `ReclassifyProperties`
11. `RenewICS` (status-history sync)
12. `ICSExpiryJob` (status-history sync)

### Read Integration

1. `GetPropertyHistory` now reads current custodian from `AssetRegistry` first.
2. Existing transaction-based fallback remains for compatibility.

## Remaining Work

### Phase 1: Schema and Migration

1. Create EF migration for `AssetRegistry`, `AssetAssignmentHistory`, and `Locations`.
2. Update model snapshot in `Migrations.PostgreSQL/AssetManagement`.
3. Validate unique/index strategy in staging.
4. Completed migration: `20260509113347_AddAssetRegistryAndLocation`.

### Phase 2: Complete Document Coverage

Add registry/history updates in:

1. Completed for current documented flows.

### Phase 3: Registry Feature Slices

Add additive query/command slices:

1. Completed: Get assets by current custodian (`/asset-registry/by-custodian/{custodianId}`).
2. Completed: Get assets by location (`/asset-registry/by-location/{locationId}`).
3. Completed: Get assignment history by asset (`/asset-registry/{assetRegistryId}/assignment-history`).
4. Completed: Location CRUD (`/locations`) with permission wiring.

### Phase 4: Reporting Alignment

1. Keep legal reports sourced from existing document entities.
2. Use `AssetRegistry` for current-state dashboards and accountability views.
3. Ensure values match official forms for ICS/PAR/SMIR/PPEIR.
4. Completed (partial): RSPI/RegSPI projections enriched with employee display metadata via MasterData contracts.
5. Completed (partial): PTR projections enriched with officer display metadata via MasterData contracts.
6. Completed (partial): RSPI/RegSPI responses enriched with additive totals metadata for form summary rows.
7. Completed (partial): RSPI/RegSPI responses enriched with additive signatory projection via MasterData report signatories.
8. Completed (partial): RSPI/RegSPI responses enriched with additive per-ICS sections and deterministic line ordering for print rendering.
9. Completed (partial): API/data-level parity cross-check documented in `ASSETMANAGEMENT-REPORT-ALIGNMENT-CHECKLIST.md`; remaining work is final visual parity signoff against approved templates.

## Classification Rules

1. `TangibleInventoryItem.AssetType` is snapshotted at receipt from capitalization threshold.
2. ICS and SMIR are SE-only document flows.
3. PAR and PPEIR are PPE-only document flows.
4. Reclassification updates state/history, not identity.

## Guardrails

1. No deletion/removal of existing entities/endpoints.
2. Preserve existing API contracts and route groups.
3. Preserve separate document numbering and permissions.
4. Keep additive path safe for phased rollout.

## Build Notes

1. AssetManagement module build succeeds with warnings.
2. Full solution build succeeds after fixing Vehicle compile error in `Vehicle.Create`.
3. AssetManagement tests pass (`AssetManagement.Tests`: 176 passed, 0 failed).
4. RSPI/RegSPI report DTO enrichment build+tests validated (176/176 passing).
5. PTR + report totals metadata enrichment build+tests validated (176/176 passing).
6. RSPI/RegSPI signatory projection enrichment build+tests validated (176/176 passing).
7. RSPI/RegSPI section-group + deterministic ordering enrichment build+tests validated (176/176 passing).
8. Added RSPI/RegSPI report handler regression tests; `AssetManagement.Tests` now at 178/178 passing.
9. Added PTR report handler regression test (officer display projection + item ordering); `AssetManagement.Tests` now at 179/179 passing.
10. Added report query validators for RSPI/RegSPI/PTR with explicit pagination/date-range guardrails.
11. Replaced RSPI/RegSPI/PTR employee reference N+1 mediator lookups with one bulk MasterData query.
12. Standardized assignment history event semantics for ICS/PAR/PPEIR: first custody = Assigned, custody change = Transferred.
13. Hardened PPEIR transfer semantics: item must already be issued and have a current custodian in AssetRegistry.
14. Clarified intentional backfill/snapshot semantics in `ICSExpiryJob` and `ReclassifyPropertiesCommandHandler` comments.

## Suggested Next Commands

1. Apply migration to target environments and validate indexes/constraints against data volume.
2. Cross-check report outputs against official forms (ICS/PAR/SMIR/PPEIR) using registry as current-state source.
   - Baseline checklist: `ASSETMANAGEMENT-REPORT-ALIGNMENT-CHECKLIST.md`
3. Integrate registry/history writes into any newly introduced document handlers.
4. Run module tests, then full solution validation as part of release readiness.
