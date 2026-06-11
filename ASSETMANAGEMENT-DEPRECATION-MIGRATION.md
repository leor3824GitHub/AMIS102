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

- **Phase 1b — MAUI physical-count → AssetRegister. 🔲 PENDING (semantic redesign).**
  AssetRegister counting has no pre-generated checklist: you *record* an entry per found asset
  (`POST /count/{id}/entries` with AssetRegistryId + condition + locationId) on a **Frozen** session;
  missing items are derived (uncounted) at reconcile. The MAUI walkthrough (Found/NotFound/Pending
  checklist, mark-by-entryId, offline `PendingCountEntry`) must be reworked to record-as-you-go, plus a
  location source and frozen-session handling. Larger; distinct sub-phase.

- **Phase 2 — Blazor pages (~18) → AssetRegister.** Re-point each `Components/Pages/AssetManagement/*`
  page/client to the AssetRegister equivalent (accountability, assets, issuance, incidents,
  unserviceable, receiving, returned-property, catalog, counting). Some already have AssetRegister
  twins under `Components/Pages/AssetRegister/*`.

- **Phase 3 — Reports.** Move/replace AssetManagement QuestPDF (RegSPI/RSPI) + RPCPPE/RPCSEMEX/ICF onto
  AssetRegister data (Annex B/C already done).

- **Phase 4 — Deprecate.** Mark `AssetManagementModule` + contracts `[Obsolete]`; keep registered/served
  as reference. Later: remove from `Program.cs` module pipeline once nothing consumes it.

## Verification per phase

`dotnet build` the affected client project (0 errors) + smoke-test the re-pointed screens against a
running API (`dotnet run --project src/Playground/AMIS.Playground.AppHost`).
