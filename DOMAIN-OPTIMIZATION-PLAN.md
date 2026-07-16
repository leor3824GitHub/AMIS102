# Domain Entity Optimization Plan — Performance & UX

> Prepared: 2026-07-15 · Scope: `src/Modules/**/Domain`, EF configurations, search handlers, Blazor list pages
> Status: **Substantially implemented** — all six phases' core work is done. Re-verified 2026-07-16 against the codebase after the inventory-ledger refactor landed and the PG migrations were re-squashed.

## Implementation Status (re-verified 2026-07-16)

| Phase | Status | Notes |
|-------|--------|-------|
| 1 — Product image pipeline | ✅ Done | Storage keys + thumbnail + `HasImage` projection + lazy image endpoint |
| 2 — Concurrency → xmin | ✅ Done (named scope) | Product, ProductInventory, EmployeeInventory, SupplyRequest, EmployeeShoppingCart, EmployeeProfile → xmin; dead `byte[] Version` dropped. SQLite test shim in Expendable/MasterData DbContexts. **See gap G3.** |
| 3 — Indexes + trigram | ✅ Done | Composite + tenant-prefix + pg_trgm GIN indexes |
| 4 — Batch archival | ✅ Done (reframed) | Superseded by JSON→**relational** `ProductInventoryBatches` table (commit `58466dcd`). Ends the whole-document rewrite entirely; under moving-average batches are an append-only receipt ledger, so the "archive exhausted batches" split is moot. Stock Card reads via join. |
| 5 — Redundant props | ◐ Mostly done | ✅ `ProductInventory.ProductName/Code/WarehouseLocationName` **removed** (readers join `Product` + `ResolveWarehouseName`); ✅ `ReservedValue` derived; ✅ `EmployeeInventory.LastInventoryDate` dropped; ✅ `InventoryBatch.InspectionDate` dropped; ✅ duplicate `InventoryBatch` renamed → `EmployeeStockBatch` + `WarehouseReceiptBatch`; ✅ hand-rolled `Version` tokens removed. **See gaps G1, G2.** |
| 6a — Derive stock availability | ✅ Done | `GetProductCatalogCards` joins `ProductInventory`; `AMISProductCard` shows stock chip + disables Add-to-Cart at zero |
| 6b — Low-stock warehouse | ✅ Done | Warehouse page low-stock view |
| 6c — Expiring PAR/ICS | ✅ Done | `GetExpiringAccountabilitiesQuery` + `/expiring` endpoint + client + renewal-reminder banner on `AccountabilityPage` + 2 unit tests |

### Not (fully) implemented — remaining items

| # | Item | Type | Notes |
|---|------|------|-------|
| G1 | `WarehouseReceiptBatch.ProductId` & `EmployeeStockBatch.ProductId` still present | **Genuine gap** | Plan flagged these as redundant (owner aggregate already carries `ProductId`). Small, low-risk to drop. |
| G2 | `ProductStatus.OutOfStock` manual status + `EmployeeProfile.OfficeCode` still present | **Kept by decision** | OutOfStock kept as an admin override alongside derived availability; OfficeCode is a cross-module read-model denormalization (removal is high-ripple, low-reward). Not defects. |
| G3 | 11 MasterData **reference** entities still on `byte[] Version` + `IsConcurrencyToken()` (Category, Department, FundCluster, FundingSourceCode, ModeOfProcurement, Office, OrganizationProfile, Position, ReportSignatory, Supplier, UnitOfMeasure) | **Consistency follow-up** | Out of Phase 2's original named scope (transactional aggregates + EmployeeProfile). These are low-contention reference tables; converting to xmin is cleanup, not a bug fix. |
| G4 | `PurchaseRequest.PrDate` duplicates `CreatedOnUtc`'s date | **Candidate only** | Plan required deciding the business rule first (real user-set document date vs. derive). Not actioned. |

**Bottom line:** every phase is delivered; only G1 (a minor redundant field) is an outright gap. G2 are deliberate keeps, G3 is an optional consistency sweep, G4 needs a product decision.

This plan comes from a review of the ~102 domain entity files across the 15 modules, their EF Core
configurations, the search/query handlers that serve list pages, and the Blazor pages that render them.
The **AssetRegister module is the reference model** — xmin optimistic concurrency, tenant-first composite
indexes, narrow list projections, image-as-storage-key with a separate thumbnail. Most findings below are
places where other modules (chiefly **Expendable**) have not yet caught up to that standard.

Dev-phase note: per current project convention, data is disposable — schema changes can be delivered as
clean migrations without data-preservation gymnastics, except where a backfill is explicitly called out.

---

## Executive Summary

| # | Finding | Impact | Phase |
|---|---------|--------|-------|
| 1 | `Product.ImageUrl` stores **base64 blobs up to ~7.6 MB in-table** and ships them on every search row | Severe: list latency, SignalR circuit bloat, DB I/O | 1 |
| 2 | `Product.Version` uses `IsRowVersion()` — the known Npgsql pitfall other modules explicitly avoid | Correctness risk + inconsistency | 2 |
| 3 | Keyword search is non-sargable everywhere (`%kw%` ILIKE / `ToLower().Contains`) with no trigram indexes | Full scans as data grows | 3 |
| 4 | `ProductInventory.Batches` JSON column grows unbounded; whole document rewritten on every stock movement | Write amplification, row bloat | 4 |
| 5 | Multiple redundant/derived properties duplicated inside the same module (see Phase 5 tables) | Drift bugs, wasted columns | 5 |
| 6 | Domain already carries the data for high-value UX (low-stock badges, auto stock status, expiring PARs) but the UI doesn't surface it | UX opportunity | 6 |

---

## Phase 1 — Product Image Pipeline (highest impact, perf + UX)

### Problem

- `ProductConfiguration.cs` maps `ImageUrl` with `HasMaxLength(10_000_000)` — "*Support base64-encoded
  images (up to ~7.6MB images)*" ([ProductConfiguration.cs:46-47](src/Modules/Expendable/Modules.Expendable/Data/Configurations/ProductConfiguration.cs#L46-L47)).
- `ProductDto` includes `ImageUrl`, and `SearchProductsQueryHandler` projects it on **every list row**
  ([SearchProductsQueryHandler.cs:57](src/Modules/Expendable/Modules.Expendable/Features/v1/Products/SearchProducts/SearchProductsQueryHandler.cs#L57)).
- `ProductsPage.razor` renders that blob into a **48×48 px** `MudImage`; `ShoppingPage`/`AMISProductCard`
  and `ProductDetailsPage` do the same. On Blazor Server each page of 10–15 products can push tens of MB
  DB → API → circuit → browser, to display thumbnails.

AssetRegister already solved this exact problem: `AssetRegistry.ImageUrl`/`ThumbnailUrl` are **storage keys**
(≤1024 chars), and `SearchAssetsQueryHandler` projects only `HasImage = a.ImageUrl != null` with a comment
"*never the multi-MB base64 blob*" ([SearchAssetsQueryHandler.cs:66-68](src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Assets/SearchAssets/SearchAssetsQueryHandler.cs#L66-L68)).

### Plan

1. **Domain:** replace `Product.ImageUrl` (base64) with `ImageUrl` + `ThumbnailUrl` storage keys, mirroring
   `AssetRegistry.SetImage(imageKey, thumbnailKey)` / `ClearImage()`.
2. **Upload:** the create/update product flow writes the file via BuildingBlocks/Storage, generates a small
   thumbnail (~96 px, same approach as the asset photo pipeline), then records both keys.
3. **Read:** add `GET /api/v1/expendable/products/{id}/image` and `/thumbnail` endpoints (same shape as the
   asset image endpoint). List/summary DTOs carry only `HasImage : bool`.
4. **Contracts:** introduce `ProductSummaryDto` (without image payload) for `SearchProducts`; keep full
   `ProductDto` for the single-product GET. This also stops shipping audit columns the table never renders.
5. **UI:** `ProductsPage`, `ShoppingPage` product cards, `CartPage` render `<img src="api/...thumbnail">`
   lazily (browser-cached, `loading="lazy"`), with the existing `MudAvatar` fallback when `HasImage == false`.
6. **Config:** `ImageUrl`/`ThumbnailUrl` → `HasMaxLength(1024)`.
7. **Migration:** dev-phase — regenerate cleanly; optionally a one-off backfill that decodes existing base64
   rows into storage files (only if current dev data matters).

**Expected result:** product list payload drops from potentially tens of MB to a few KB per page; images
load progressively and cache in the browser instead of re-streaming through the circuit each render.

---

## Phase 2 — Concurrency Token Standardization (Npgsql correctness)

### Problem

Three different concurrency mechanisms coexist:

| Mechanism | Where | Verdict |
|---|---|---|
| `xmin` (`xid`, `ValueGeneratedOnAddOrUpdate`) | AssetRegister aggregates, number sequences | ✅ Reference pattern |
| `byte[] Version` + **`IsRowVersion()`** | `Product` ([ProductConfiguration.cs:53](src/Modules/Expendable/Modules.Expendable/Data/Configurations/ProductConfiguration.cs#L53)) | ❌ Known Npgsql pitfall — `bytea` rowversion is server-generated-expected but never generated, breaking inserts. `BudgetUtilizationRequestConfiguration` and `DisbursementVoucherConfiguration` carry explicit comments avoiding exactly this. |
| `byte[] Version` + `IsConcurrencyToken()` + hand-rolled `RandomNumberGenerator.GetBytes(8)` on every mutation | `ProductInventory`, `EmployeeInventory`, `SupplyRequest`, `EmployeeShoppingCart`, DVs/BURs | ⚠️ Works, but every domain method must remember `Version = NewVersion()` — one forgotten call silently disables conflict detection for that transition. |

### Plan

1. Fix `Product` immediately: drop `IsRowVersion()`, map `xmin` as in `AssetRegistryConfiguration`.
2. Migrate the hand-rolled `Version` aggregates to `xmin` module by module (Expendable first). Delete the
   `Version` property, the `NewVersion()` helpers, and every `Version = NewVersion()` line — the DB then
   guarantees the token with zero domain-code discipline required.
3. Where the client round-trips the token (DV/BUR edit forms send `Version` back), expose `xmin` as a
   `uint RowVersion` on the DTO — one field, no behavior change for the UI.

---

## Phase 3 — Index & Search Optimization

### 3a. Missing composite indexes for actual query shapes

| Table | Query evidence | Add |
|---|---|---|
| `Products` | `SearchProducts` filters `CategoryId`, `SupplierId`; default sort `OrderBy(Name)` | `(TenantId, CategoryId)`, `(TenantId, SupplierId)`, `(TenantId, Name)` |
| `AssetRegistries` | `SearchAssets` default sort `OrderByDescending(AcquisitionDate)` on every page | `(TenantId, AcquisitionDate DESC)` |
| `PurchaseRequests` | Indexes are `(Status)` and `(DepartmentId)` **without tenant prefix** ([PurchaseRequestConfiguration.cs:50-51](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Data/Configurations/PurchaseRequestConfiguration.cs#L50-L51)) — inconsistent with every other module | Replace with `(TenantId, Status)`, `(TenantId, DepartmentId)` |

Audit the remaining ProcurementAcquisition configurations (PO, IAR, JO, Canvass) for the same
tenant-prefix omission while in there.

### 3b. Trigram indexes for keyword search

All keyword searches use leading-wildcard patterns that can never use a b-tree index:

- `SearchProducts`: `EF.Functions.ILike(p.Name, "%kw%")` over Name/StockNo/Article/Description.
- `SearchAssets`: `a.Description.ToLower().Contains(k)` over Description/SerialNo/Brand/Model.

Plan:
1. Enable `pg_trgm` once in the migrations project (`HasPostgresExtension("pg_trgm")`).
2. Add GIN trigram indexes on the hot search columns: `Products (Name, StockNo, Article)`,
   `AssetRegistries (Description, SerialNo)`, and the document-number columns used by list-page keyword
   filters (PrNumber, PoNumber, DocumentNo, DvNumber).
3. Normalize predicates on `EF.Functions.ILike` everywhere (replace the `ToLower().Contains` variants in
   `SearchAssetsQueryHandler` and siblings) — ILIKE is what the trigram index accelerates, and it removes
   the per-row `lower()` call.

### 3c. Projection discipline

`SearchAssetsQueryHandler` is the model: explicit column projection + `HasImage`. Sweep the other 30+
search handlers for full-entity `Select(e => e.ToDto())` mappings that drag unneeded columns (worst
offender is Products via Phase 1; also check handlers that project audit columns list pages never show).

---

## Phase 4 — Aggregate Data-Growth Hygiene

### 4a. `ProductInventory.Batches` (JSON, unbounded)

Every accepted IAR appends a batch to the JSON column; every reserve/issue/cancel **rewrites the whole
document**. Batches are never pruned, so the hottest row in the warehouse grows forever and write
amplification worsens with age.

Plan:
1. Keep only **open** batches (QuantityRemaining > 0) in the JSON document.
2. On exhaustion, move the batch to a small relational `InventoryBatchArchive` table (columns: inventory id,
   purchase id, source reference, qty, unit price, received date). The Stock Card query reads
   `archive ∪ open batches` — and becomes a proper SQL query instead of JSON deserialization.
3. Alternative if Stock Card querying grows further: promote batches to a full child table and drop the JSON
   column. Start with the archive split; it's smaller and preserves aggregate semantics.

### 4b. `EmployeeInventory` batches

Relational (`InventoryBatches` table) but never pruned; fully-consumed batches accumulate per
employee×product forever. Add the same archival rule (or a cleanup job) once volumes matter. Low urgency.

### 4c. Snapshot drift inside one module

`ProductInventory.ProductName/ProductCode/WarehouseLocationName` are copies of rows in the *same*
DbContext — if a product is renamed, the warehouse page shows the stale name forever. Phase 5 removes
them (preferred); if any is deliberately kept, wire `ProductUpdated` → update the copies.

---

## Phase 5 — Redundant Property Removal (per module)

Requested review: properties that are redundant *from the domain-model point of view*. Split into
**Remove** (genuine duplication, no business value) and **Keep — deliberate** (looks redundant but is a
justified snapshot/cache), so the removals don't overreach.

### Expendable — Remove

| Entity.Property | Why redundant | Replacement |
|---|---|---|
| `Product.Version` (byte[]) | Broken `IsRowVersion()` on Npgsql; superseded by xmin | Phase 2 |
| `ProductStatus.OutOfStock` enum value + `MarkOutOfStock()` | Stock state duplicated manually on the catalog entity; truth lives in `ProductInventory` | Derive availability from inventory (Phase 6a) |
| `ProductInventory.ProductCode`, `.ProductName` | Copies of `Product` in the **same module/DbContext**; drift on rename | Join/projection in queries |
| `ProductInventory.WarehouseLocationName` | Copy of the warehouse location entity in the same module | Join/projection |
| `ProductInventory.ReservedValue` | Pure derivation — recomputed as `QuantityReserved × AverageUnitPrice` after every mutation | Computed property (like `AverageUnitPrice` already is) |
| `Warehouse.InventoryBatch.ProductId` | Batch lives inside a `ProductInventory` that already owns `ProductId` | Parent's value |
| `Warehouse.InventoryBatch.InspectionDate` | Always assigned `= ReceivedDate` at creation, never updated anywhere | Drop |
| `Inventory.InventoryBatch.ProductId` (EmployeeInventory child) | Same as above — parent carries `ProductId` | Parent's value |
| `EmployeeInventory.LastInventoryDate` | Set to `UtcNow` in exactly the same places as `LastModifiedOnUtc` | `LastModifiedOnUtc` |
| `SupplyRequest.Version`, `EmployeeInventory.Version`, `ProductInventory.Version` | Hand-rolled tokens | Phase 2 (xmin) |

Also: **two different `InventoryBatch` classes** exist (`Domain.Inventory.InventoryBatch` vs
`Domain.Warehouse.InventoryBatch`) with overlapping-but-different shapes. Rename one (e.g.
`EmployeeStockBatch` vs `WarehouseReceiptBatch`) — same-name concepts in one module invite wrong-import bugs.

### Expendable — Keep (deliberate)

- `Product.Name` on variants (auto-composed `"{Parent} - {Variant}"`) — denormalized but read constantly;
  regenerate on `SetVariantName` instead of removing.
- `EmployeeInventory.TotalQuantityReceived/Consumed` — derivable from batches, but they're the hot path for
  `QuantityOnHand`; keep as maintained counters.

### MasterData — Remove

| Entity.Property | Why redundant | Replacement |
|---|---|---|
| `EmployeeProfile.OfficeCode` | Sits next to `OfficeId` **and** an `Office` navigation in the same module | `Office.Code` via navigation/projection |
| `EmployeeProfile.Version` (byte[]) | Same token cleanup | Phase 2 |

### ProcurementAcquisition — Keep, with one candidate

- All `*ByName/*ByDesignation` signatory snapshots on `PurchaseRequest`/PO/IAR — **keep**; print fidelity
  is a documented requirement (reprints must show who signed with their title at the time).
- **Candidate:** `PurchaseRequest.PrDate` is set from `DateTime.UtcNow` at create and is not user-editable —
  it duplicates `CreatedOnUtc`'s date. Either make it a real user-supplied document date (matches how paper
  PRs work) or drop it and derive. Decide the business rule first; don't just delete.

### BudgetDisbursement — Keep

- `DisbursementVoucher.PurchaseOrderNumber` — cross-module snapshot; justified (no cross-module joins).
- `DisbursementVoucher.BurNumber` — same-module denormalization, but document numbers are immutable after
  issuance, so drift is impossible; keep for query/print cheapness.

### AssetRegister — Keep (reference model)

- `AssetRegistry.CurrentCustodianId/CurrentLocationId/CurrentAccountabilityId` — denormalized "current
  state" cache maintained by the lifecycle methods; this is what makes asset lists and custodian filters
  cheap. Keep.
- `PropertyAccountability.ExpiresOn` stored even though PAR expiry is derivable (issued + 3y) — keep;
  the `(TenantId, Status, ExpiresOn)` index and the ICS (user-set) case need a uniform column.
- Accountability line `Snapshot` owned type — keep; historical fidelity by design.

---

## Phase 6 — Domain-Driven UX Enhancements

These use data the domain already has; each is small once Phases 1–5 land.

### 6a. Real product availability on Shopping/Products pages

Today `ProductStatus.OutOfStock` is a manual toggle, so the Shopping page can happily sell items with zero
stock (or hide items that have stock). After removing the manual state (Phase 5):
- `SearchProducts`/`ShoppingPage` queries join `ProductInventory` to expose `QuantityOnHand` per product
  (a grouped sum — the `(TenantId, ProductId)` index already exists).
- UI: "In stock (123)" / "Out of stock" chip on product cards; disable Add-to-Cart at zero; low-stock
  badge when `QuantityOnHand <= MinimumStockLevel` with the `ReorderQuantity` as the suggested reorder —
  both fields already live on `Product` but are shown today only as raw numbers in an admin column.

### 6b. Low-stock / reorder dashboard (Warehouse)

Single grouped query over `ProductInventory` vs `Product.MinimumStockLevel` → "N products below minimum"
KPI + filtered list on `WarehousePage`. No schema change needed.

### 6c. Expiring accountabilities (PAR renewals / ICS expiry)

`PropertyAccountability` already has `ExpiresOn`, `ParRenewalYears`, a `Renew()` method, and the
`(TenantId, Status, ExpiresOn)` index — but nothing surfaces upcoming expiries. Add an
"expiring within 60 days" query + a card on the AssetRegister landing page and a Notifications-module
digest. Pure read model; zero domain change.

### 6d. Faster perceived list loads

- Phase 1 thumbnails + existing `AMISDataTable` skeletons make Products/Shopping render at
  interactive speed; images fill in lazily.
- Phase 3 indexes keep server paging flat as row counts grow (asset registry sorted by
  `AcquisitionDate DESC` is the page every property officer lives in).

---

## Sequencing & Effort

| Phase | Est. effort | Depends on | Migration? |
|---|---|---|---|
| 1 — Product image pipeline | 2–3 days | — | Yes (column resize; optional backfill) |
| 2 — Concurrency (Product fix first) | 0.5 day + 1 day sweep | — | Yes (drop Version columns) |
| 3 — Indexes + trigram | 1 day | — | Yes (index-only) |
| 4 — Batch archival | 1–2 days | — | Yes (new archive table) |
| 5 — Redundant property removal | 1–2 days | Phase 2 (Version removals overlap) | Yes |
| 6 — UX enhancements | 2–3 days | 1, 3, 5 (for 6a) | No |

Phases 1–3 are independent and can proceed in parallel; Phase 5 folds naturally into the same migration
batch as Phase 2. Combine Expendable schema changes (1, 2, 4, 5) into as few migrations as possible.

## Verification

For every phase:

```powershell
dotnet build src/AMIS.Framework.slnx   # 0 warnings required
dotnet test src/AMIS.Framework.slnx    # includes Architecture.Tests
```

Plus targeted checks:
- Phase 1: product list network payload before/after (browser dev tools on `/expendable/products` and the
  shopping page); image upload → thumbnail render → full image on detail page.
- Phase 2: concurrent-edit test — two edits of the same Product/SupplyRequest, second must get a 409/conflict.
- Phase 3: `EXPLAIN ANALYZE` on the keyword search SQL before/after trigram indexes.
- Phase 4: Stock Card page still shows historical (archived) batches.
- Phase 5: rename a product → warehouse page and stock card show the new name.
