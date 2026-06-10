# AMIS Progress Tracker

> Update this file after every meaningful implementation change.
> Grouped by project and module.

---

## Current Phase

**Phase: AssetRegister module hardening + cross-client polish**

> Last reconciled against the codebase: 2026-06-10. The MAUI client and AssetRegister module
> are both substantially built — see status table below.

---

## Current Goal

Harden the `AssetRegister` bounded context (valuation, reconciliation, print parity) and finish polish items across the Blazor and MAUI clients.

---

## Active Enhancement Workstream

**AssetManagement Additive Overhaul (No Delete/Remove)**

- Add unified current-state layer for tangible assets while preserving separate agency/legal documents (ICS, PAR, SMIR, PPEIR, RRSP, RRP).
- Keep receipt and issuance document structures intact; write-through to `AssetRegistry` + `AssetAssignmentHistory` for current state and audit timeline.
- Implement as additive changes only (no removal of existing entities/endpoints).

---

## Overall Project Status

| Layer                                                     | Status                          |
| --------------------------------------------------------- | ------------------------------- |
| Backend — Core Modules (Identity, Multitenancy, Auditing) | ✅ Complete                     |
| Backend — MasterData Module                               | ✅ Complete                     |
| Backend — Expendable Module                               | ✅ Complete                     |
| Backend — AssetManagement Module                          | 🟡 Print-parity signoff pending |
| Backend — AssetRegister Module (new bounded context)      | ✅ Phases 1–7 landed (slices for Catalog, Assets, Accountability, Issuance, Receiving, Counting incl. freeze, Incidents, Unserviceable, ReturnedProperty, Reports; IAR consumer materializes assets; catalog seeding; 18 Blazor pages) |
| Backend — AssetProcurement Module                         | ✅ Complete                     |
| Backend — Vehicle Module                                  | ✅ Complete                     |
| Backend — Finance Module                                  | ✅ Complete                     |
| Backend — ProcurementPlanning Module                      | ✅ Complete                     |
| Backend — ProcurementAcquisition Module                   | ✅ Complete                     |
| Client — Playground.Blazor                                | ✅ Complete (all modules wired) |
| Client — Playground.Maui                                  | ✅ Phases 1–11 built (login, shell, profile, inventory, scan, asset detail, platform manifests) + PhysicalCount feature (5 pages, OCR, offline sync) |
| AI Guides (.claude/ rules, skills, agents)                | ✅ Complete                     |

---

## Completed

### Infrastructure

- [x] Modular Monolith with 11 modules scaffolded and fully wired to `Playground.Api`
- [x] .NET Aspire orchestration (`AMIS.Playground.AppHost`)
- [x] PostgreSQL migrations via `Migrations.PostgreSQL`
- [x] OpenAPI + Scalar UI + NSwag client generation scripts
- [x] Architecture tests (`src/Tests/Architecture.Tests`)
- [x] AI development guides: rules, skills, agents, CLAUDE.md
- [x] MAUI implementation plan (`MAUI-IMPLEMENTATION-PLAN.md`)

### Module: Identity

- [x] Token issuance — `POST /api/v1/identity/token/issue`
- [x] Token refresh — `POST /api/v1/identity/token/refresh`
- [x] User profile — `GET /api/v1/identity/profile`
- [x] Users CRUD — `GET/POST/PUT/DELETE /api/v1/identity/users`
- [x] Roles CRUD
- [x] Groups
- [x] Sessions + cleanup hosted service

### Module: Multitenancy

- [x] Tenant CRUD — create, list, status, activation toggle
- [x] Tenant upgrade
- [x] Tenant theme (get, update, reset)
- [x] Tenant provisioning (migrations per tenant)
- [x] Hosted services: tenant provisioning + theme seeding

### Module: Auditing

- [x] Get audits (list, by ID, by correlation, by trace)
- [x] Security audits
- [x] Exception audits
- [x] Audit summary

### Module: MasterData

- [x] Departments
- [x] Positions
- [x] Offices
- [x] Employees (CRUD + search)
- [x] Categories
- [x] Property Classes
- [x] Capitalization Thresholds
- [x] Unit of Measures
- [x] Modes of Procurement
- [x] Suppliers
- [x] Lookups
- [x] Organization Profile
- [x] Report Signatories

### Module: Expendable

- [x] Products / Warehouse inventory
- [x] Purchase requests (employee shopping cart)
- [x] Cart management
- [x] Purchases (approvals flow)
- [x] Expendable reports

### Module: AssetManagement

- [x] Tangible Items (pre-receipt registration)
- [x] Tangible Inventory (receive items into inventory)
- [x] Semi-Expendable Items + Issuance Records
- [x] ICS — Inventory Custodian Slips (create, list, get, expiring, renew)
- [x] PAR — Property Acknowledgement Receipts (create, list, get)
- [x] PPE Issuance Reports
- [x] Receipt for Returned Properties (RRSP)
- [x] Receipt for Returned PPE (RRP)
- [x] Physical Count
- [x] Property Incident Reports
- [x] Reclassification
- [x] Unserviceable Property Reports
- [x] Reports (property history, etc.)
- [x] ICS expiry background job

### Module: AssetManagement (Additive Overhaul)

- [x] Added unified current-state entities: `AssetRegistry`, `AssetAssignmentHistory`, `Location`
- [x] Added lifecycle/event enums: `AssetLifecycleState`, `AssetAssignmentEventType`, `LocationType`
- [x] Added EF configurations for new entities with tenant-aware indexes and soft-delete filters
- [x] Wired new DbSets into `AssetManagementDbContext`
- [x] Receipt flow writes to registry: `CreateTangibleInventory`
- [x] SE issuance/transfer/return flows write to registry + history: `CreateICS`, `CreateSMIR`, `CreateRRSP`
- [x] PPE issuance/transfer/return flows write to registry + history: `CreatePAR`, `CreatePPEIR`, `CreateRRP`
- [x] Incident flow writes to registry + history: `CreatePropertyIncidentReport`
- [x] Unserviceable flow writes to registry + history: `CreateUnserviceablePropertyReport`
- [x] Reclassification flow writes to registry + history: `ReclassifyProperties`
- [x] ICS renewal/expiry flows write status history to registry timeline: `RenewICS`, `ICSExpiryJob`
- [x] Property history query now reads current custodian from `AssetRegistry` first (legacy fallback retained)
- [x] Add migration for new registry/history/location tables (`20260509113347_AddAssetRegistryAndLocation`)
- [x] Extend registry/history writes to reclassification flow
- [x] Add registry-focused query slices (assets by custodian/location + assignment timeline)
- [x] Add/validate permissions and endpoint groups for new location/registry operations
- [x] AssetManagement module test suite pass after overhaul additions (`AssetManagement.Tests`: 176/176)

### Module: AssetProcurement

- [x] Asset Purchase Requests
- [x] Asset Purchase Orders
- [x] Asset IARs (Inspection and Acceptance Reports)

### Module: Vehicle

- [x] Vehicles CRUD
- [x] Fuel & Odometer records
- [x] Maintenance records
- [x] Repair records
- [x] Vehicle lookups

### Module: Finance

- [x] Disbursement Vouchers
- [x] Budget Utilization Records

### Module: ProcurementPlanning

- [x] Annual Procurement Plans (APPs)
- [x] PPMPs (Project Procurement Management Plans)

### Module: ProcurementAcquisition

- [x] Purchase Requests
- [x] Canvass
- [x] Purchase Orders

### Client: Playground.Blazor

- [x] Authentication (login, session)
- [x] Dashboard / Home
- [x] MasterData pages (Employees, Departments, Positions, etc.)
- [x] Expendable pages
- [x] AssetManagement pages (ICS, PAR, PPE, Physical Count, etc.)
- [x] Vehicle pages
- [x] Finance pages
- [x] Procurement pages (Planning + Acquisition)
- [x] Identity pages (Users, Roles, Groups, Sessions)
- [x] Multitenancy (Tenants)
- [x] Auditing page
- [x] Profile Settings + Theme Settings

### AI Guides (.claude/)

- [x] `rules/architecture.md` — modular monolith + MAUI client layer
- [x] `rules/api-conventions.md` — endpoint patterns
- [x] `rules/modules.md` — module structure
- [x] `rules/persistence.md` — EF Core / repository patterns
- [x] `rules/buildingblocks-protection.md` — protected packages
- [x] `rules/testing-rules.md` — architecture + unit tests
- [x] `rules/maui.md` — MAUI client MVVM + service + caching rules _(new)_
- [x] `skills/add-feature/SKILL.md`
- [x] `skills/add-entity/SKILL.md`
- [x] `skills/add-module/SKILL.md`
- [x] `skills/query-patterns/SKILL.md`
- [x] `skills/testing-guide/SKILL.md`
- [x] `skills/error-handling/SKILL.md`
- [x] `skills/mediator-reference/SKILL.md`
- [x] `skills/maui-feature/SKILL.md` — MAUI Page + ViewModel + API client scaffold _(new)_
- [x] `agents/code-reviewer.md` — backend + MAUI checklist _(updated)_
- [x] `agents/feature-scaffolder.md`
- [x] `agents/module-creator.md`
- [x] `agents/architecture-guard.md` — + MAUI boundary checks _(updated)_
- [x] `agents/migration-helper.md`
- [x] `agents/maui-reviewer.md` — MAUI-specific MVVM review _(new)_
- [x] `progress-tracker.md` — this file _(new)_

---

## In Progress

### Backend: AssetManagement Additive Overhaul

- [x] Migration generation and snapshot update (`Migrations.PostgreSQL/AssetManagement`)
- [x] Reclassification integration with `AssetRegistry`
- [x] Registry query slices + endpoint registration
- [x] RSPI/RegSPI report projections enriched with employee display fields (name/position/office) while preserving ID fields
- [x] PTR report projection enriched with officer display fields (name/position/office) while preserving ID fields
- [x] RSPI/RegSPI report projections enriched with additive totals metadata for report summary rows
- [x] RSPI/RegSPI report projections enriched with additive signatory block data from MasterData report signatories
- [x] RSPI/RegSPI report projections enriched with additive ICS section-group metadata and deterministic print ordering
- [x] RSPI/RegSPI report handler regression tests added (ordering, sections, signatories, totals)
- [x] PTR report handler regression test added (officer display projection + item ordering)
- [x] RSPI/RegSPI/PTR query validators added (pagination/date-range guardrails)
- [x] RSPI/RegSPI/PTR employee lookup optimized to single bulk MasterData query (N+1 removed)
- [x] Assignment history event semantics standardized across ICS/PAR/PPEIR (Assigned vs Transferred by prior custodian)
- [x] PPEIR transfer guard hardened: requires issued item + existing registry + current custodian
- [x] Ambiguous behavior documented: ICS expiry backfill and reclassification pre-change snapshot intent
- [ ] Final visual print-layout parity signoff against approved ICS/PAR/SMIR/PPEIR templates (API/data-level cross-check completed; see `ASSETMANAGEMENT-REPORT-ALIGNMENT-CHECKLIST.md`)
- [x] Full solution build gate revalidated after Vehicle compile fix

### Client: Playground.Maui

> Implementation plan: `MAUI-IMPLEMENTATION-PLAN.md` — **all 11 phases are built** (verified against the codebase 2026-06-10):
> backend endpoints (`employees/me`, `tangible-inventory-items/by-property-no`), project setup, auth infrastructure
> (token storage, authenticated handler, SQLite cache), login, shell navigation, profile, inventory (ICS/PAR + details),
> scan (ZXing + manual entry), asset detail, and platform manifests.
> **Beyond the plan:** a full PhysicalCount feature was added — session list, walkthrough, scan, mark-entry,
> found-at-station pages, `OcrService`, `PropertyNumberExtractor`, and `PhysicalCountSyncService` for offline sync.

- [ ] On-device validation pass on a physical low-end Android device (per `.claude/rules/maui.md` performance rule)

### Backend: AssetRegister

> The archived plan/progress docs in `_ARCHIVED_DOCUMENTATION/` are out of date. Verified state 2026-06-10:
> Phases 1–7 all landed — ~50 vertical slices (Catalog, Assets, Accountability, Issuance, Receiving,
> Counting incl. freeze workflow, Incidents, Unserviceable, ReturnedProperty, Reports), the
> `AssetIARAcceptedEventConsumer` fully materializes assets (catalog-id–based, idempotent), catalog seeding
> runs per tenant, permissions are registered, and 18 Blazor pages exist under `Components/Pages/AssetRegister/`.

- [x] Real Current Replacement Cost calculator (COA 2022-004 §4.19): latest similar-acquisition price via `ReplacementCostPolicy` + dedicated `CurrentReplacementCostCalculator` service (replaced the acquisition-cost placeholder; 6 policy unit tests added) — 2026-06-10
- [x] Fixed ambiguous `CustomException` ctor call in `CountFreezeGuard` that broke the module build — 2026-06-10
- [x] **2nd pass:** `ICurrentReplacementCostCalculator.ComputeAsync` now takes the already-loaded `AssetRegistry` (aligns with `ICountFreezeGuard`), dropping a redundant per-item PK re-fetch in `FileIncidentReport` — 2026-06-10
- [x] **2nd pass:** Closed freeze-guard consistency gap — `FileIncidentReport` now calls `EnsureMovementAllowedAsync` before flipping assets to UnderInvestigation/accountability lines to Lost (every other asset-mutating handler already did). User-approved behavior: filing an RLSDDSP is blocked while a covering count is frozen — 2026-06-10

---

## Next Up

1. AssetManagement: final visual print-layout parity signoff (ICS/PAR/SMIR/PPEIR templates)
2. MAUI: validation pass on a physical low-end Android device (requires hardware — cannot be automated)
3. Blazor sizing Phase 3: ~40 pages still below 50% Dense coverage — see worklist in `MUDBLAZOR-SIZING-STANDARDIZATION-PLAN.md` "Status Reconciliation" section (reconciled 2026-06-10; VehiclesPage create form migrated same day)
4. ~~Fix `AMISSelect` two-way binding~~ — ✅ Fixed 2026-06-10 (user-approved BuildingBlocks change): now propagates via `OnMudValueChanged`, same pattern as `AMISAutocomplete`. Wrapper adoption is unblocked for Phase 3 migrations.
5. Set the real production API host in `Playground.Maui/appsettings.Production.json` before first release

---

## Open Questions — RESOLVED 2026-06-10 (user decisions)

- **Multi-tenant:** Tenant picker at login. Already existed on `LoginPage`; tenant is now also persisted via `Preferences` (`ApiClientOptions.TenantPreferenceKey`) so a session resumed from stored tokens sends the right `tenant` header, and the login form pre-fills the last-used tenant.
- **ICS/PAR detail offline caching:** Keep online-only (per `.claude/rules/maui.md` cache table).
- **Push notifications for ICS expiry:** Not now; revisit on demand signal (server-side expiry job already exists).
- **Environment configs:** Dev + Prod. `appsettings.json` = dev defaults; `appsettings.Production.json` overlays in Release builds (BaseUrl placeholder `https://amis-api.example.gov.ph` — set the real host before first release). Environment variables still override both.

---

## Architecture Decisions

| Decision               | What                                                      | Why                                                                    |
| ---------------------- | --------------------------------------------------------- | ---------------------------------------------------------------------- |
| MAUI as second client  | Separate `Playground.Maui` project, no BFF                | Blazor uses BFF; MAUI calls API directly with bearer token             |
| Token storage          | `SecureStorage` (Android/iOS) + `PasswordVault` (Windows) | `Preferences` is unencrypted — not safe for tokens                     |
| Employee ID resolution | New `/employees/me` endpoint (MasterData)                 | JWT only carries Identity UserId, not MasterData EmployeeId            |
| QR scan target         | `PropertyNo` from existing property stickers              | No new QR generation needed — stickers already exist                   |
| Offline caching        | SQLite (sqlite-net-pcl) — ICS + PAR lists only            | Field staff need offline read; detail pages require real-time accuracy |
| Cache strategy         | Stale-while-revalidate                                    | Show cached data instantly, refresh silently in background             |
| Scan fallback          | Manual PropertyNo entry always visible                    | Covers Windows, damaged stickers, and accessibility                    |
| Barcode formats        | QrCode + Code128 + Code39 + DataMatrix                    | All formats used on Philippine government property stickers            |
| Navigation             | Shell-only (`Routing.RegisterRoute`)                      | Consistent back-stack and deep-link support across platforms           |

---

## Session Notes

- All backend modules are complete and deployed to `Playground.Api`.
- `Playground.Blazor` is the working web client; all modules are wired and tested there.
- `Playground.Maui` is fully scaffolded and feature-complete per the plan (plus PhysicalCount); treat `MAUI-IMPLEMENTATION-PLAN.md` checklists as historical.
- `_ARCHIVED_DOCUMENTATION/ASSET-REGISTER-MODULE-PROGRESS.md` is stale and self-contradictory; trust this file and the code.
- The `.claude/` AI guides have been fully updated for MAUI: `rules/maui.md`, `skills/maui-feature/SKILL.md`, `agents/maui-reviewer.md`, plus updates to `code-reviewer.md`, `architecture-guard.md`, `CLAUDE.md`, and `architecture.md`.
- Use `/maui-feature` skill when adding any new MAUI screen.
- Use `maui-reviewer` agent after any MAUI code changes.
- Reference `MAUI-IMPLEMENTATION-PLAN.md` for full technical spec including DTO shapes, caching tables, and scan UX layout.

