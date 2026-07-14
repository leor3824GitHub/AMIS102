# AMIS Progress Tracker

> Update this file after every meaningful implementation change.
> Grouped by project and module.

---

## Current Phase

**Phase: RAG assistant PoC (`Modules.Assistant`) — design approved, implementation not started**

> Last reconciled against the codebase: **2026-07-14**. The previous reconciliation (2026-06-10) had gone
> badly stale: it tracked an `AssetManagement` module that has since been deleted, and listed
> `AssetProcurement` / `Finance` under names they no longer carry. Everything below was re-verified
> against HEAD.

---

## Current Goal

Build the cross-module AI assistant PoC: a new stateless `Modules.Assistant` that answers natural-language
questions across Expendable, AssetRegister, and MasterData, plus semantic product search over Expendable.

Design (approved, both docs currently **uncommitted**):
- [`RAG-Integration-PoC-Expendable-Module.md`](../RAG-Integration-PoC-Expendable-Module.md) — backend, phases 0–6
- [`RAG-PoC-UI-Design.md`](../RAG-PoC-UI-Design.md) — Blazor surface (replaces the backend plan's Phase 6)

---

## Overall Project Status

15 modules in `src/Modules/`, all wired into `AMIS.Api`.

| Layer                                                     | Status |
| --------------------------------------------------------- | ------ |
| Backend — Core (Identity, Multitenancy, Auditing)         | ✅ Complete |
| Backend — MasterData                                      | ✅ Complete |
| Backend — Expendable                                      | ✅ Complete |
| Backend — AssetRegister (canonical SE + PPE)              | ✅ Complete — ~50 slices, PPE depreciation engine, 53 Blazor pages |
| Backend — Vehicle                                         | ✅ Complete |
| Backend — BudgetDisbursement (was "Finance")              | ✅ Complete |
| Backend — ProcurementPlanning                             | ✅ Complete |
| Backend — ProcurementAcquisition (was "AssetProcurement") | ✅ Complete |
| Backend — Notifications · Chat                            | ✅ Complete |
| Backend — Reporting (FastReporting, QuestPdfReporting, RdlcReporting) | ✅ Complete |
| Backend — **Assistant (RAG PoC)**                         | ⬜ **Not started** — designed only |
| Client — AMIS.Blazor                                      | ✅ Complete (all modules wired; UI improvement plan phases 1–5 done) |
| Client — AMIS.Maui                                        | ✅ Feature-complete (phases 1–11 + PhysicalCount); rewired to AssetRegister endpoints |
| AI Guides (.claude/ rules, skills, agents)                | ✅ Complete |

**No `TODO` / `FIXME` / `HACK` markers exist anywhere in `src/`.**

---

## In Progress

### RAG Assistant PoC — `Modules.Assistant`

Nothing is built yet. There is no `src/Modules/Assistant/`. Phases, per the plan:

- [ ] **Phase 0** — pgvector-enabled Postgres image in AppHost; `UseVector()` in the Postgres branch of
      `OptionsBuilderExtensions.ConfigureHeroDatabase` (**requires user-approved BuildingBlocks change**)
- [ ] **Phase 1** — `Modules.Assistant` + `Modules.Assistant.Contracts` scaffold (stateless: no DbContext,
      no DbInitializer, no migration); `IAssistantToolProvider` push interface; `AmisModule` assembly attribute
- [ ] **Phase 2** — per-tool authorization: `AssistantTool.RequiredPermission`, tool list filtered per caller
      *before the model sees it*, re-checked inside each delegate (`ICurrentUser` + `IUserService.HasPermissionAsync`)
- [ ] **Phase 3** — tool providers in Expendable, AssetRegister, MasterData (the flagship cross-module answer
      chains `find_employee` → `get_employee_issuances` → `search_accountabilities`)
- [ ] **Phase 4** — `IChatClient` function-invocation loop (`Microsoft.Extensions.AI`), Anthropic default
- [ ] **Phase 5** — semantic product search: `IEmbeddingService` (local ONNX), `expendable.ProductEmbeddings`
      `vector(384)`, embedding upserts + rebuild endpoint
- [ ] **Phase 6 (UI)** — chat panel, semantic search surface, rebuild admin action — see the UI design doc

Enablement is deliberately two independent switches: `AssistantOptions:Enabled` (default **false**) and
`Permissions.Assistant.Use` (declared **`IsBasic: false`** — opt-in per role). Semantic search toggles
separately via `ExpendableOptions:SemanticSearch:Enabled` (default true).

### Client: AMIS.Maui

- [ ] On-device validation pass on a physical low-end Android device (per `.claude/rules/maui.md`
      performance rule — needs hardware, cannot be automated)
- [ ] Set the real production API host in `AMIS.Maui/appsettings.Production.json` before first release

### Client: AMIS.Blazor

- [ ] MudBlazor sizing standardization **Phase 3** — see `MUDBLAZOR-SIZING-STANDARDIZATION-PLAN.md`.
      ⚠️ Its worklist ("~40 pages below 50% Dense coverage") was last reconciled 2026-06-10 and has **not**
      been re-verified since; the AMISDataTable/AMISButton sweeps in the UI improvement plan likely closed
      part of it. Re-reconcile before working it.

---

## Housekeeping

- [x] ~~`src/Tests/AssetManagement.Tests/` is orphaned~~ — **deleted 2026-07-14.** The module was gone and the
      test project was not in `src/AMIS.Framework.slnx`, so it never built or ran. No project referenced it.
- [x] ~~JO "Link Purchase Request" picker shows PRs that already have an IAR~~ — **fixed 2026-07-14.** Added
      `SearchPurchaseRequestsQuery.ExcludeWithIar`, mirroring `SearchPurchaseOrdersQuery.ExcludeWithIar`. An IAR
      hangs off a **PO** (never off a PR or JO — a JO records its inspection inline), so the filter walks
      PR → PO → IAR. Opt-in flag; the PR list page and canvass picker are unaffected. Tests:
      `SearchPurchaseRequestsExcludeWithIarTests`.
- [ ] Commit the two revised RAG design docs + this reconciliation + the JO/PR fix (all in the working tree).

---

## Recently Completed

### AssetRegister — the canonical asset bounded context

`AssetManagement` was **removed from HEAD**; AssetRegister is the single unified SE + PPE context, and every
client now targets it (`AMIS.Maui/Services/ApiClient.cs` comments mark AssetManagement deprecated).

- [x] ~50 vertical slices: Catalog, Assets, Accountability, Issuance, Receiving, Counting (incl. freeze),
      Incidents, Unserviceable, ReturnedProperty, Repair, Reports
- [x] PPE depreciation engine + Current Replacement Cost calculator (COA 2022-004 §4.19)
- [x] `AssetIARAcceptedEventConsumer` materializes assets (catalog-id–based, idempotent)
- [x] Reports: RSPI/RegSPI, RPI, PTR + signed-copy upload flow across modules
- [x] 53 Blazor pages under `Components/Pages/AssetRegister/`

### "Balance = found" physical count true-up — **DONE** (was the long-running in-progress item)

- [x] `MarkMissingFromCount`, idempotent `MarkUnderInvestigation`
- [x] Found-at-station `ProposedPropertyNo` / `ProposedCatalogItemId` / `ProposedUnitCost` through
      entry → session → handler → contracts → mapper, with EF config + migration
- [x] `ClosePhysicalCount` **auto-materializes** found-at-station entries into the registry as new Available
      assets and **flags not-found** assets (`FlagNotFoundAsync`)
- [x] `AddFoundAtStationEntryCommandValidator`; `PhysicalCountCloseTests` domain coverage
- [x] Blazor + MAUI capture UI

### Print parity — **DONE**

- [x] RPCPPE via QuestPdfReporting `PrintPhysicalCountReport` (no longer on the dead AssetManagement path)
- [x] Annex B (Found at Station) / Annex C (Non-Existing/Missing) PDFs + Blazor download buttons

### Blazor UI improvement plan — all 5 phases complete/actioned

See `BLAZOR-UI-IMPROVEMENT-PLAN.md`. `AMISDataTable` everywhere (+ opt-in multi-select), by-ids batch
endpoints, design tokens and dark-mode border fixes in `AMIS-theme.css`.

**Deferred by explicit decision** (do not re-raise as bugs): NSwag client regeneration (raw `HttpClient` used
instead), an aggregate `/session/bootstrap` endpoint, and a full inline-style sweep with live dark-mode visual QA.

---

## Backlog (out of current scope)

COA-circular refinements for physical count — 7 items in
`ASSET-REGISTER-PHYSICAL-COUNT-IMPLEMENTATION.md` §5: receivable at *depreciated* replacement cost,
demand-letter step before loss recognition, found-at-station appraisal flag, Registry of Derecognized PPEs
(RDPPE / Annex D), post-close PAR-renewal + IIRUP prompts, derecognition preconditions.

---

## Architecture Decisions

| Decision | What | Why |
| -------- | ---- | --- |
| Assistant placement | Own `Modules.Assistant` (+ `.Contracts`), **not** inside Expendable | For AssetRegister to contribute tools it must implement an interface; if that interface lived in Expendable.Contracts, AssetRegister would depend on Expendable. A neutral module is the only legal home. |
| Assistant tool contribution | Modules **push** tools via `IAssistantToolProvider` | Dependency arrow points modules → Assistant.Contracts. Adding a module's tools requires zero edits to the assistant. |
| Assistant authorization | Per-tool `RequiredPermission`, filtered per caller | Permissions are enforced at endpoints, never in handlers — an unfiltered tool loop is a permission-bypass bus. |
| Vector storage | pgvector, `vector(384)`, similarity ranked in-database | Native store on the Postgres already running. |
| MAUI as second client | Separate `AMIS.Maui`, no BFF | Blazor uses BFF; MAUI calls the API directly with a bearer token. |
| Token storage (MAUI) | `SecureStorage` (Android/iOS) + `PasswordVault` (Windows) | `Preferences` is unencrypted — not safe for tokens. |
| Employee ID resolution | `/employees/me` endpoint (MasterData) | JWT carries only the Identity UserId, not the MasterData EmployeeId. |
| QR scan target | `PropertyNo` from existing stickers | No new QR generation needed. |
| Offline caching (MAUI) | SQLite — ICS + PAR lists only, stale-while-revalidate | Field staff need offline read; detail pages need real-time accuracy. |
| Navigation (MAUI) | Shell-only (`Routing.RegisterRoute`) | Consistent back-stack and deep-link support. |

---

## Session Notes

- Trust **this file and the code**. `_ARCHIVED_DOCUMENTATION/` is stale by design; the archived
  AssetRegister/AssetManagement plan docs are self-contradictory.
- Module renames to keep in mind when reading older docs: `AssetProcurement` → **ProcurementAcquisition**,
  `Finance` → **BudgetDisbursement**, `AssetManagement` → **deleted** (folded into AssetRegister).
- A module only activates via `[assembly: AmisModule(...)]` in its `AssemblyInfo.cs`. Missing it = a dormant
  module with a green build and green tests.
- Use `/maui-feature` when adding a MAUI screen; run the `maui-reviewer` agent after MAUI changes.
