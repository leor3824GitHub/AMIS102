# AMIS Progress Tracker

> Update this file after every meaningful implementation change.
> Grouped by project and module.

---

## Current Phase

**Phase: MAUI Client — Planning & AI Guide Setup**

---

## Current Goal

Scaffold `Playground.Maui` — the .NET MAUI mobile/desktop client (Android · iOS · Windows) that consumes the existing REST API, providing login, profile, ICS/PAR inventory view, and QR/barcode asset lookup.

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
| Backend — AssetManagement Module                          | 🟡 Enhancement in progress      |
| Backend — AssetProcurement Module                         | ✅ Complete                     |
| Backend — Vehicle Module                                  | ✅ Complete                     |
| Backend — Finance Module                                  | ✅ Complete                     |
| Backend — ProcurementPlanning Module                      | ✅ Complete                     |
| Backend — ProcurementAcquisition Module                   | ✅ Complete                     |
| Client — Playground.Blazor                                | ✅ Complete (all modules wired) |
| Client — Playground.Maui                                  | 🔲 Not started                  |
| AI Guides (.claude/ rules, skills, agents)                | ✅ Complete                     |

---

## Completed

### Infrastructure

- [x] Modular Monolith with 11 modules scaffolded and fully wired to `Playground.Api`
- [x] .NET Aspire orchestration (`FSH.Playground.AppHost`)
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

> Implementation plan: `MAUI-IMPLEMENTATION-PLAN.md`

- [ ] **Phase 1** — Backend: `GET /api/v1/master-data/employees/me`
  - `GetMyEmployeeQuery.cs`
  - `GetMyEmployeeHandler.cs`
  - `GetMyEmployeeEndpoint.cs`

- [ ] **Phase 2** — Backend: `GET /api/v1/asset-management/tangible-inventory-items/by-property-no/{propertyNo}`
  - `GetTangibleInventoryItemByPropertyNoQuery.cs`
  - `GetTangibleInventoryItemByPropertyNoHandler.cs`
  - `GetTangibleInventoryItemByPropertyNoEndpoint.cs`

- [ ] **Phase 3** — MAUI Project Setup
  - `Playground.Maui.csproj` (Android · iOS · Windows)
  - Add to `FSH.Framework.slnx`
  - NuGet: CommunityToolkit.Maui, CommunityToolkit.Mvvm, ZXing.Net.MAUI, sqlite-net-pcl
  - `appsettings.json` (Api:BaseUrl, Api:TenantId)
  - `MauiProgram.cs`

- [ ] **Phase 4** — Auth Infrastructure
  - `ApiClientOptions.cs`
  - `ITokenStorageService.cs` / `TokenStorageService.cs`
  - `AuthStateService.cs`
  - `AuthenticatedHttpHandler.cs`
  - `ICacheService.cs` / `CacheService.cs`
  - `Data/LocalDb.cs` + SQLite model classes

- [ ] **Phase 5** — Login Screen
  - `Features/Auth/LoginPage.xaml` + `.cs`
  - `Features/Auth/LoginViewModel.cs`
  - Startup token check in `App.xaml.cs`

- [ ] **Phase 6** — AppShell Navigation
  - `AppShell.xaml` + `.cs` (tabs: Inventory | Scan | Profile)
  - Route registration (ICSDetailPage, PARDetailPage, AssetDetailPage)

- [ ] **Phase 7** — Profile Screen
  - `Features/Profile/ProfilePage.xaml` + `.cs`
  - `Features/Profile/ProfileViewModel.cs`

- [ ] **Phase 8** — Inventory Screen (ICS + PAR)
  - `Features/Inventory/InventoryPage.xaml` + `.cs`
  - `Features/Inventory/InventoryViewModel.cs`
  - `Features/Inventory/ICSDetailPage.xaml` + `.cs`
  - `Features/Inventory/ICSDetailViewModel.cs`
  - `Features/Inventory/PARDetailPage.xaml` + `.cs`
  - `Features/Inventory/PARDetailViewModel.cs`

- [ ] **Phase 9** — Scan Screen
  - `Features/Scan/ScanPage.xaml` + `.cs`
  - `Features/Scan/ScanViewModel.cs`
  - Camera (ZXing.Net.MAUI) + manual PropertyNo entry

- [ ] **Phase 10** — Asset Detail Screen
  - `Features/Asset/AssetDetailPage.xaml` + `.cs`
  - `Features/Asset/AssetDetailViewModel.cs`

- [ ] **Phase 11** — Polish
  - Platform manifests (camera + internet permissions)
  - Loading overlays, empty states, error toasts
  - Windows: camera fallback to manual entry

---

## Next Up

1. Implement `GET /api/v1/master-data/employees/me` (Phase 1)
2. Implement `GET /api/v1/asset-management/tangible-inventory-items/by-property-no/{propertyNo}` (Phase 2)
3. Create `Playground.Maui.csproj` and add to solution (Phase 3)

---

## Open Questions

- Should the MAUI app support multiple tenants (tenant switcher), or always default to `root`?
- Should ICS/PAR detail pages be cached offline (currently planned as online-only)?
- Should the app support push notifications for ICS expiry warnings?
- Which environment configs are needed — dev / staging / prod (`appsettings.{env}.json`)?

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
- `Playground.Maui` does NOT exist yet — start from Phase 1 (two new backend endpoints) + Phase 3 (project scaffold) in parallel.
- The `.claude/` AI guides have been fully updated for MAUI: `rules/maui.md`, `skills/maui-feature/SKILL.md`, `agents/maui-reviewer.md`, plus updates to `code-reviewer.md`, `architecture-guard.md`, `CLAUDE.md`, and `architecture.md`.
- Use `/maui-feature` skill when adding any new MAUI screen.
- Use `maui-reviewer` agent after any MAUI code changes.
- Reference `MAUI-IMPLEMENTATION-PLAN.md` for full technical spec including DTO shapes, caching tables, and scan UX layout.
