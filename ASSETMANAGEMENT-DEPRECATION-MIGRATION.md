# AssetManagement → AssetRegister Migration & Deprecation

> **Decision (2026-06-11):** AssetRegister fully replaces AssetManagement. Migrate clients off
> AssetManagement in phases (MAUI first), then mark AssetManagement `[Obsolete]` and keep it served
> as a **reference only** (existing endpoints still respond; no client consumes them). Unregister it
> from the host later, once parity is proven. **Dev data is disposable — no data migration.**

## Why phased

AssetManagement is consumed by: the entire **MAUI** app (ICS/PAR/asset-lookup/physical-count),
~**18 Blazor pages** (`Components/Pages/AssetManagement/*`), and **reporting** (QuestPDF RegSPI/RSPI,
plus AssetManagement's own RPCPPE/RPCSEMEX/ICF). The DTOs, routes, lifecycle and ID models differ, so
each surface is a real re-point, not a find-replace.

Strategy that keeps blast radius small: **keep each client's existing public DTOs unchanged and rewrite
only the API-client internals** to call AssetRegister endpoints and map the responses into those DTOs.
ViewModels/pages stay untouched where the semantics match.

## Endpoint mapping (AssetManagement → AssetRegister)

| Concern | AssetManagement | AssetRegister |
|---|---|---|
| ICS list/detail | `/asset-management/inventory-custodian-slips` | `/asset-register/accountability/mine?type=SE_ICS` + `/mine/{id}` |
| PAR list/detail | `/asset-management/property-acknowledgement-receipts` | `/asset-register/accountability/mine?type=PPE_PAR` + `/mine/{id}` |
| Asset by PropertyNo | `/asset-management/tangible-inventory-items/by-property-no/{no}` | `/asset-register/assets/by-property-no/{no}` |
| Physical count | `/asset-management/physical-count/*` (pre-generated checklist; mark entries by id) | `/asset-register/count/*` (record-as-you-go; missing derived at reconcile) |

## Phases

- **Phase 1a — MAUI ICS/PAR/asset-lookup → AssetRegister. ✅ DONE (2026-06-11).**
  Rewrote `Playground.Maui/Services/ApiClient.cs` ICS/PAR/lookup methods to call AssetRegister
  `accountability/mine` + `assets/by-property-no`, mapping `ArAccountability*`/`ArAssetDetail` (local
  string-enum mirrors — MAUI can't reference `Modules.*`) into the unchanged MAUI DTOs. `IApiClient`,
  ViewModels and pages unchanged.

- **Phase 1b — MAUI physical-count → AssetRegister. ✅ DONE (2026-06-11) — ⚠️ needs device testing.**
  Reworked the MAUI counting feature from AssetManagement's checklist model to AssetRegister's
  record-as-you-go model:
  - `ApiClient`/`IApiClient`: the 4 physical-count methods now hit `…/asset-register/count/*`; responses
    map into the unchanged MAUI display DTOs (session list + walkthrough list render as-is). Added
    `GetLocationsAsync` (AM locations served as reference). `RecordPhysicalCountEntryAsync` drops `entryId`
    and posts `{AssetRegistryId, Article, Unit, UnitCost, Condition, LocationId, ScannedOnUtc, Remarks}`;
    found-at-station posts `{Article, Unit, UnitCost, LocationId, ProposedPropertyNo, Remarks}`.
  - Walkthrough: added a **"Counting at" location picker** (records store where each item was counted);
    scanning/typing a PropertyNo now resolves it against the asset registry → records the found asset
    (condition on the next screen) or, if unknown, routes to found-at-station. Recorded entries are
    read-only (tap is a no-op; the list reloads after each record).
  - Mark-entry: records by AssetRegistryId + condition (`InGoodCondition`/`NeedingRepair`/`Unserviceable`)
    + location; dropped the Found/NotFound/Pending Result picker and the Quantity field.
  - Found-at-station: carries the selected location; dropped the condition picker (AssetRegister recognizes
    found items as Available at close).
  - Offline: `PendingCountEntry` + `PhysicalCountSyncService` reworked to the AssetRegister record shape.
  - **Limitation:** locations come from the (deprecated) AssetManagement locations endpoint as shared
    reference data; a frozen (Ongoing) session is required to record. Builds clean on Windows; the mobile
    UX (scan → record, offline queue/flush, location picker) must be validated on a device/emulator.

- **Phase 2 — Blazor navigation → AssetRegister. ✅ DONE (2026-06-11).**
  AssetRegister already has full-parity Blazor pages (`Components/Pages/AssetRegister/*`:
  MyAccountability, Accountability=ICS/PAR, AssetRegistry, Catalog, PPE/SMRR Receiving + PPERR series,
  PhysicalCount, Incidents, Issuance, ReturnedProperty, Unserviceable, Reports). Retired the parallel
  "Asset Management" `MudNavGroup` from `Components/Layout/NavMenu.razor` (+ its unused expand-state
  fields) so users consume only AssetRegister. The AssetManagement pages/routes remain reachable by
  direct URL for reference. No page rebuild was needed — parity already existed.

- **Phase 3 — Reports.** Move/replace AssetManagement QuestPDF (RegSPI/RSPI) + RPCPPE/RPCSEMEX/ICF onto
  AssetRegister data (Annex B/C already done).

- **Phase 4 — Deprecate (marker). ✅ DONE (2026-06-11) / hard-obsolete DEFERRED.**
  `AssetManagementModule` carries a `DEPRECATED` XML-doc marker (replaced by AssetRegister; kept served as
  reference; no new features). A hard `[Obsolete]` was intentionally **not** applied yet: the repo requires
  0 warnings and AssetManagement is still consumed by its own pages, the MAUI physical-count flow, and the
  RPCPPE/RegSPI/RSPI reports — so `[Obsolete]` would emit CS0618 storms. Apply `[Obsolete]` + remove from
  `Program.cs` only after Phases 1b and 3 land and the AssetManagement Blazor pages are dropped from build.

## Verification per phase

`dotnet build` the affected client project (0 errors) + smoke-test the re-pointed screens against a
running API (`dotnet run --project src/Playground/AMIS.Playground.AppHost`).
