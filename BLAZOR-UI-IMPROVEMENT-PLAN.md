# Blazor UI/UX, Performance & Enterprise-Grade Design Improvement Plan

> **First execution step:** save this plan into the repo as `e:\AMIS102\BLAZOR-UI-IMPROVEMENT-PLAN.md` (user request).

## Context

`AMIS.Blazor` is a Blazor Server (.NET 10, InteractiveServer) app talking to `AMIS.Api` over HTTP via NSwag-generated + hand-written clients. Exploration found:

- **Silent data truncation bug**: ~24 call sites across 16 files request `pageSize: 1000`, but `PaginationExtensions.cs` clamps to `MaxPageSize = 100` → dropdowns silently incomplete for tenants with >100 rows. This is a correctness bug, not just perf.
- **No reference-data caching**: departments/offices/positions/employees/suppliers/categories re-fetched from the API on every page/dialog open. No caching server-side either.
- **5 sequential API calls at startup** in `AMISLayout` (profile → permissions → employee → org profile → threshold); permissions arriving late forces pages to subscribe `OnProfileChanged` + `StateHasChanged`.
- **UI-side N+1s**: per-row product-name fetches (`SupplyRequestsPage.razor:442-450`), per-line-item hydration (`PurchaseRequestFormDialog.razor` ~470-492), per-employee identity fetch (`EmployeesPage.razor:204-228`). A batch query `GetEmployeeReferencesByIdsQuery` already exists (`EmployeeReferenceContracts.cs:90`, handled in `MasterDataLookupHandlers.cs:47`) but has **no HTTP endpoint**.
- **UI inconsistency**: table primitives vary (MudTable / MudSimpleTable / MudDataGrid), pagination varies, `AMISBreadcrumbs` unused, no skeleton loaders, minimal empty states, wrapper drift (raw Mud controls where `AMIS*` wrappers are mandated), encoding bug (`AMISLayout.razor:86` renders `??` instead of `×`), dark mode not persisted.
- **Already solid** (don't touch): permission gating, snackbar error handling, `AMISDialogService` confirms, PDF off-circuit download cache, asset-thumbnail proxy, response compression, per-tenant theming.

## User decisions (confirmed)

1. ✅ **Blazor.UI (BuildingBlocks) additions approved** — AMISDataTable, AMISEmptyState, AMISPageHeader breadcrumbs slot, dark-mode setter.
2. ✅ **Bootstrap: `Task.WhenAll` client-side only** — no aggregate endpoint for now.
3. ✅ **Keep `MaxPageSize=100` clamp** — fix via unpaged lookup endpoints (bounded sets) + server-driven autocomplete (unbounded sets).

---

## Phase 1 — Zero-contract quick wins (S)

No API surface changes; each item independently shippable.

1. **Parallelize bootstrap** — `src/Host/AMIS.Blazor/Components/Layout/AMISLayout.razor`:
   - `LoadUserProfileAsync()` (lines ~351-402): run `ProfileGetAsync`, `PermissionsGetAsync`, `EmployeesClient.ByIdentityAsync` concurrently with `Task.WhenAll` (no data dependency between them).
   - `OnAfterRenderAsync(firstRender)` (~237-246): run `LoadUserProfileAsync()`, `CheckOrganizationProfileAsync()`, `ThresholdState.EnsureLoadedAsync()` concurrently; keep org-setup dialog awaiting its own task result. Single `StateHasChanged` at end.
2. **Fix encoding bug** — `AMISLayout.razor:86`: `??` → `&times;` (HTML entity, immune to codepage corruption). Grep `Components/` for other `�`/`??` corruption while there.
3. **Persist dark mode** — inject `ProtectedLocalStorage` in `AMISLayout`; read `AMIS_darkMode` on first interactive render, write on toggle. Add a `SetDarkMode(bool)` to theme state in Blazor.UI if only a toggle exists (approved BB touch).
4. **SignalR message size** — `Program.cs` (~line 236): `.AddInteractiveServerComponents()` → chain `.AddHubOptions(o => o.MaximumReceiveMessageSize = 512 * 1024)`.
5. **Lazy tabs on SupplyRequestsPage** — `Components/Pages/Expendable/SupplyRequestsPage.razor`: 4 tabs each with own `ServerData` currently all load on open; bind `ActivePanelIndex`, load only active tab, per-tab `_loaded` flags.

**Verify:** `dotnet build src/AMIS.Framework.slnx` zero warnings; Aspire dashboard traces show startup collapsing from 5 sequential calls to ~2 parallel waves; dark mode survives hard refresh; `×` renders; opening Supply Requests fires 1 list load, not 4.

---

## Phase 2 — Lookup pipeline: kill clamp mismatch, cache reference data, fix N+1s (M)

### Server side (extends existing Lookups vertical slice — `src/Modules/MasterData/Modules.MasterData/Features/v1/Lookups/`)

1. **Slim `LookupItemDto`** (`Id`, `Name`, optional `Code`) in `Modules.MasterData.Contracts/v1/References/` + **unpaged "all active" endpoints** on `MasterDataLookupEndpoint.cs`: `/lookups/departments/all`, `/offices/all`, `/positions/all`, `/unit-of-measures/all`. Categories: add equivalent in the owning module (verify owner — likely MasterData or Expendable). Reuse existing view permissions; **confirm each is in the module's `PermissionConstants` catalog** (memory: `RequirePermission` with uncataloged permission → 403 for everyone). Module-prefixed `WithName` per api-conventions.
2. **Handler-level caching**: `IMemoryCache` in the all-active handlers, key `lookup:{tenant}:{set}`, TTL ~120s. (Not ASP.NET OutputCache — endpoints are authenticated.)
3. **Expose existing batch query**: endpoint `POST /lookups/employees/by-ids` → existing `GetEmployeeReferencesByIdsQuery`. Add `GetUsersByIdsQuery` + endpoint in Identity module (for EmployeesPage linked accounts).
4. **Read-model enrichment for dialog N+1s**: add `ProductName` (+ needed catalog fields) to supply-request and purchase-request line-item DTOs; enrich in the query handlers (Expendable/Procurement can mediate to product reference queries, same pattern Procurement already uses for employee references). Watch for server-side N+1 — project/join, `AsSplitQuery` where needed.
5. **Regenerate NSwag client** (`ApiClient/Generated.cs`) — commit separately (22k-line churn).

### Client side

6. **New `ILookupDataService`** — `src/Host/AMIS.Blazor/Services/LookupDataService.cs`: scoped per-circuit; per-set idempotent `Task? _loadTask` (copy the `ICapitalizationThresholdState.EnsureLoadedAsync` pattern in `Services/CapitalizationThresholdState.cs`); 5-min TTL; `Invalidate(set)` called by admin CRUD pages after mutations. Register in `Program.cs` beside the other scoped states.
7. **Convert the ~24 `pageSize:1000` call sites** (16 files, incl. `PurchaseRequestFormDialog.razor:328`, `EmployeesPage.razor:237/247/257`, `SupplyRequestsPage.razor:362/374/417`, `ProductsPage.razor:291/305`, `StockCardPage.razor:156`, `WarehousePage.razor:131`, `CartPage.razor:400`):
   - Bounded sets (departments/offices/positions/UoM/categories) → `ILookupDataService`.
   - Unbounded sets (employees/products/suppliers) → `AMISAutocomplete` with `SearchFunc` hitting the existing keyword-paginated lookup endpoints (pageSize 20, min 2 chars). Reuse `Components/Shared/EntityPickerDialog.razor` where the UX is really a picker over a large set.
8. **Replace the N+1 loops** in `SupplyRequestsPage.razor`, `PurchaseRequestFormDialog.razor`, `EmployeesPage.razor` with enriched DTOs / by-ids calls.

**Verify:** dropdowns show complete bounded lists (correctness fix); second open of the same dialog makes zero lookup HTTP calls (Aspire traces); EmployeesPage = 2 requests instead of 1+N; `dotnet test src/AMIS.Framework.slnx` passes.

**Risks:** stale lookups after admin edits (short TTL + `Invalidate`; acceptable in dev phase); NSwag regen churn (separate commit).

---

## Phase 3 — Canonical list-page pattern (M) — Blazor.UI additions (approved)

1. **`AMISDataTable<TItem>`** — new file in `src/BuildingBlocks/Blazor.UI/Components/` alongside the existing thin `AMISTable.razor` (which stays for simple in-memory lists). Wraps: MudTable with `ServerData` delegate, `ToolbarContent`, `FilterContent` (hosts `AMISFilterBar`), `HeaderContent`/`RowTemplate`, `RowsPerPage=15`, built-in **skeleton rows** (`MudSkeleton`) on first load, built-in **`AMISEmptyState`** (new component: icon + message + optional CTA slot), footer `AMISPagination`. Enforce `Dense`, hover, 40px toolbar baseline per `.claude/rules/blazor.md`.
   - Chosen primitive: **MudTable ServerData** (matches existing majority; denser than MudDataGrid; MudDataGrid's client-side filter model fights server paging). No mega `AMISListPage` component — a documented template suffices.
2. **Breadcrumbs adoption** — add optional `Breadcrumbs` parameter to `AMISPageHeader.razor` rendering the currently-unused `AMISBreadcrumbs`, so adoption = one attribute per page.
3. **Pilot on MasterData module** (`Components/Pages/MasterData/`: EmployeesPage, OfficesPage, DepartmentsPage, PositionsPage, …): convert to `AMISDataTable` + ServerData, skeletons, empty states, breadcrumbs; sweep wrapper drift (raw `MudTextField`/`MudButton`/`MudSimpleTable` → `AMIS*`) in the same touch.
4. **Document the canonical page template** in `.claude/rules/blazor.md`: `AMISPageTitle` → `AMISPageHeader` (breadcrumbs + permission-gated CTA) → `AMISDataTable`.

**Verify:** pilot pages visually consistent (same pager, empty state, skeleton-not-spinner); zero warnings; permission-gated buttons re-tested per role; `.razor` files saved UTF-8 (run corruption grep).

**Risk:** MudTable `ServerData` reload semantics (`ReloadServerData()` on filter change) differ from manual-fetch pages — the pilot shakes this out before mass migration.

---

## Phase 4 — Module-by-module migration + report data paths (L)

1. **Migrate remaining modules** to the Phase-3 pattern in leverage order: **Expendable → Procurement → AssetRegister → Vehicle → BudgetDisbursement → rest**. One PR per module; each = table conversion + wrapper-drift sweep + breadcrumbs + empty states for that folder. No behavior changes bundled beyond the pattern.
2. **Report full-dataset endpoints**: replace client-side page-looping (e.g. `DepartmentIssuanceReportPage.razor:296-303`) with dedicated unpaged report queries server-side — reports are the legitimate "give me everything" case; server returns the full projection in one response.
3. Skip `<Virtualize>` — 15-row server paging makes it unnecessary.

**Verify:** per-module smoke pass (list/filter/sort/paginate/CRUD + gated actions); grep raw `MudSimpleTable`/`MudDataGrid` usage trending to zero outside justified exceptions.

---

## Phase 5 — Enterprise polish pass (S/M)

1. **Typography/spacing audit**: single pass over `Blazor.UI/wwwroot/css/AMIS-theme.css` + `Theme/AMISTheme.cs` — codify heading scale, table row height, card padding as CSS custom properties; grep pages for inline `style=`/`font-size` overrides and replace with tokens (e.g. `AMISLayout` inline-styled footer).
2. **Dark-mode contrast QA** across migrated modules; `AMISStatCard`/dashboard density alignment.
3. *(Deferred by user decision)* Aggregate `GET /api/v1/session/bootstrap` endpoint — revisit only if post-Phase-1 startup latency still feels slow.

---

## What to reuse (found in exploration)

| Need | Reuse |
|---|---|
| Idempotent load-once state | `Services/CapitalizationThresholdState.cs` `EnsureLoadedAsync` pattern |
| Batch employee lookup | `GetEmployeeReferencesByIdsQuery` (`EmployeeReferenceContracts.cs:90`, handler exists — just add endpoint) |
| Lookup endpoints | Existing `Features/v1/Lookups/MasterDataLookupEndpoint.cs` slice — extend, don't invent |
| Picker UX for large sets | `Components/Shared/EntityPickerDialog.razor` |
| Design-system components | `AMIS*` inventory in `src/BuildingBlocks/Blazor.UI/Components/` (incl. unused `AMISBreadcrumbs`, seed `AMISTable`) |
| Confirm dialogs | `AMISDialogService.ShowConfirmAsync/ShowDeleteConfirmAsync` |

## Critical files

- `src/Host/AMIS.Blazor/Components/Layout/AMISLayout.razor` — Phase 1 (bootstrap, encoding, dark mode)
- `src/Host/AMIS.Blazor/Program.cs` — hub options, service registrations
- `src/Modules/MasterData/Modules.MasterData/Features/v1/Lookups/MasterDataLookupEndpoint.cs` + `MasterDataLookupHandlers.cs` — Phase 2 server
- `src/Host/AMIS.Blazor/Services/LookupDataService.cs` — new, Phase 2 client
- `src/BuildingBlocks/Blazor.UI/Components/` — Phase 3 (`AMISDataTable`, `AMISEmptyState`, `AMISPageHeader`)
- `src/Host/AMIS.Blazor/Components/Pages/MasterData/EmployeesPage.razor` — pilot page exercising every workstream

## Verification (end-to-end)

1. `dotnet build src/AMIS.Framework.slnx` — zero warnings (CI gate).
2. `dotnet test src/AMIS.Framework.slnx` — all pass (architecture tests guard module boundaries for new endpoints).
3. Run via `dotnet run --project src/Host/AMIS.AppHost`; use Aspire dashboard traces to count HTTP calls: startup ≤2 waves, dialog re-open = 0 lookup calls, list pages = 1 call per load, N+1 pages = 2 calls.
4. Functional smoke per migrated module: search/filter/paginate/CRUD, permission-gated buttons per role, dark-mode toggle + refresh, dropdowns with >100-row datasets show complete lists.
5. Duplicate-endpoint-name grep from api-conventions before committing new endpoints.
