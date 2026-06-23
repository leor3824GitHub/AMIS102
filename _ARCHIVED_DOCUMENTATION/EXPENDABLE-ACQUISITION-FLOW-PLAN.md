# Expendable Acquisition Flow — Implementation Plan

> Routes expendable (consumable supply) purchase requests through the **same** central ProcurementAcquisition pipeline already used for assets, then materializes accepted goods into the Expendable warehouse (`ProductInventory`) instead of the Asset Registry. Mirrors the existing asset integration exactly — no duplicate procurement module.

---

## 1. Goal

Connect `ProcurementAcquisition` to `Expendable` so that when a Purchase Request is for **expendable supplies**, the accepted goods land in the Expendable warehouse stock, exactly as asset PRs land in the Asset Registry today.

```
ProcurementAcquisition (single shared engine)
PR (SupplyType) ──► Canvass ──► PO ──► IAR ──► Inspect ──► Accept
                                                              │ publishes …
                          ┌───────────────────────────────────┴───────────────────────────────────┐
                          ▼ SupplyType = Asset                                                      ▼ SupplyType = Expendable
            AssetIARAcceptedEvent                                                  ExpendableIARAcceptedEvent
                          │                                                                          │
                          ▼                                                                          ▼
            AssetRegister consumer                                              Expendable consumer (NEW)
            → AssetRegistry (1 row / unit, PropertyNo)                          → ProductInventory.ReceiveFromPurchase
                                                                                  (bulk qty into warehouse stock)
```

---

## 2. Decisions locked (from brainstorm)

| # | Decision | Choice |
|---|----------|--------|
| D1 | Where the asset-vs-expendable split is decided | **Per whole PR** — a `SupplyType` flag on the PR header, propagated PR → PO → IAR. A PR is entirely Asset or entirely Expendable. |
| D2 | Existing Expendable internal Purchase/PO flow | **Retire it.** Central acquisition becomes the only stock-in path. The new consumer absorbs what `RecordPurchaseReceipt` does today. |
| D3 | What an expendable PR line references | The **Expendable `Product`** (`ProductId`), carried in the existing `CatalogItemId` slot. `SupplyType` tells the consumer which catalog the id targets. |
| D4 | Inspection for expendables | **Identical to Semi/PPE** — reuse the acquisition IAR's existing `SubmitForInspection → RecordInspection → Accept` workflow. No separate expendable inspection model. |
| D5 | Line item not yet in the Product catalog | **Must create the Product first.** The PR line editor shows an "Add product" action that redirects to Product creation; the new product is then selectable. No ad-hoc/free-text expendable lines reach acceptance. |
| D6 | Destination warehouse | **Determined at acceptance.** `WarehouseLocationId` + `WarehouseLocationName` are supplied on the `Accept` command (header-level — one acceptance = one warehouse), required when `SupplyType == Expendable`, ignored for assets. Acceptance is when goods physically arrive and the custodian knows the destination supply room. (Name is needed because no Warehouse master exists — Open Q G.) |
| D7 | Rejected quantities | **Exclude.** Rejection is whole-line in the IAR model (`LineInspectionResult.Rejected`); rejected lines simply never enter the event, so no stock and no `RejectedInventory` row. |
| D8 | Fractional quantities | **Reject at PR creation.** PR line `Quantity` is `decimal` but warehouse stock is `int`; the PR validator requires whole numbers for `SupplyType == Expendable`. |

---

## 3. Current state (audit)

| Concern | Status | Where |
|---|---|---|
| PR / Canvass / PO / IAR engine | ✅ Exists, generic | `Modules/ProcurementAcquisition/.../Features/v1/{PurchaseRequests,Canvass,PurchaseOrders,AssetIARs}` |
| IAR inspection workflow (Submit → Inspect per-line → Accept) | ✅ Exists | `Features/v1/AssetIARs/{SubmitForInspection,RecordInspection,AcceptAssetIAR}` |
| Acceptance integration event | ✅ `AssetIARAcceptedEvent` | `…Contracts/v1/AssetInspectionAcceptanceReports/AssetIARContracts.cs:217` |
| Asset-side consumer | ✅ Exists | `Modules/AssetRegister/.../Integration/AssetIARAcceptedEventConsumer.cs` |
| ProcurementAcquisition → AssetRegister coupling | ✅ Event-only (AssetRegister reaches into Procurement contracts; Procurement knows nothing of AssetRegister) | — |
| PR `SupplyType` discriminator | ❌ Missing — `PrType` is only `Planned/Unplanned` (a different axis) | `…Contracts/v1/PurchaseRequests/PurchaseRequestContracts.cs:23` |
| PR line "must pick a catalog item (+ to create)" enforcement | ✅ Exists — validator requires `CatalogItemId` per line | `…CreatePurchaseRequest/CreatePurchaseRequestCommandValidator.cs:22` (D5 reuses this; just retarget catalog + message) |
| Warehouse master entity (for the acceptance picker) | ❌ None — `WarehouseLocationId`/`Name` are free-form on records | Open question G |
| Expendable warehouse stock-in | ✅ `ProductInventory.ReceiveFromPurchase(...)` | `Modules/Expendable/.../Domain/Warehouse/ProductInventory.cs:104` |
| Expendable `Product` catalog | ✅ Exists | `Modules/Expendable/.../Domain/Products/Product.cs` |
| Expendable internal Purchase/PO flow (to retire) | ✅ Exists | `Modules/Expendable/.../Domain/Purchases/*`, `Features/v1/Purchases/*` |
| Expendable consumer of acquisition events | ❌ Missing — built in this plan | — |
| `ExpendableIARAcceptedEvent` | ❌ Missing — built in this plan | — |

### Naming note (semantic debt to resolve — Decision E)

The IAR entity, statuses, and event are all `Asset`-prefixed (`AssetInspectionAcceptanceReport`, `AssetIARAcceptedEvent`, `AssetIARStatus`) but live in `ProcurementAcquisition` and are about to serve **both** supply types. Two options:

- **E1 (chosen):** Keep the `Asset*` names for this iteration. Reuse the same IAR entity for expendable PRs; name only the *new* artifacts neutrally (`ExpendableIARAcceptedEvent`, `ExpendableIARAcceptedEventConsumer`). Add a doc comment on `AssetInspectionAcceptanceReport` stating it now serves both supply types. Schedule the neutral rename as a dedicated follow-up (Phase E).
- **E2 (rejected for now):** Rename everything to neutral terms in this work.

> **Decision: E1.** The third-pass audit showed the rename's blast radius is large and risky to mix with feature work: the `…v1.AssetInspectionAcceptanceReports` namespace and ~20 contract types (`CreateAssetIARCommand`, `AcceptAssetIARCommand`, `AssetIARDto`, `AssetIARAcceptedEvent`, …), the `AssetIARs` DbSet + EF tables, ~15 feature folders with globally-unique endpoint names, the **auto-generated Blazor API client** (`ApiClient/Generated.cs` — renaming commands ripples into regenerated method names and every Blazor call site), the AssetRegister consumer + module registration, and the architecture tests. Dev-phase makes the *table* rename cheap, but not the cross-package code churn. Do it later as an isolated, behavior-free PR.
>
> When Phase E runs, two cheap tricks shrink it: keep physical table names via `.ToTable("asset_iars")` (no data migration) and preserve existing `WithName(...)` endpoint strings (no client-route churn) — rename only the C# identifiers + namespace.

---

## 4. Design

### 4.1 One PR, one IAR entity, one pipeline — branch only at the edges

The acquisition pipeline stays **single and shared**. Expendable PRs are not a new document type; they're a PR with `SupplyType = Expendable`. The only behavioural differences are:

| Stage | Asset | Expendable |
|---|---|---|
| PR line catalog reference | `CatalogItemId` → AssetRegister `PropertyItemCatalog` | `CatalogItemId` → Expendable `Product` (D3) |
| `AssignPropertyNo` step | Required (1 line = 1 unit, gets PropertyNo) | **Skipped** — bulk qty, no property numbers |
| Inspection | `SubmitForInspection → RecordInspection → Accept` | **Same** (D4) |
| `Accept` command input | `WarehouseLocationId` ignored | `WarehouseLocationId` **required** (D6) |
| On `Accept`, event published | `AssetIARAcceptedEvent` | `ExpendableIARAcceptedEvent` (NEW) |
| Destination | `AssetRegistry` (1 row/unit) | `ProductInventory.ReceiveFromPurchase` (qty into stock) |

`SupplyType` is set at PR creation, **immutable**, and snapshotted forward to PO and IAR exactly like `CatalogItemId`/`UacsObjectCode` already propagate today.

> **Third-pass note — the "+ create catalog item" UX already exists.** `CreatePurchaseRequestCommandValidator` already requires `CatalogItemId` (non-null, non-empty) on *every* PR line, with a message telling the user to "use the + button to create one." So D5 is half-built: for expendable PRs we only need to (a) point that picker/create-button at the Expendable `Product` catalog instead of `PropertyItemCatalog`, and (b) make the validator message `SupplyType`-aware (it currently says "register the asset").

> **Third-pass note — guard the AssetRegister receiving pre-fill.** `SearchAcceptedIARLineItemsQuery` exposes accepted IAR lines so AssetRegister's Receiving Report can pre-fill from them. Once expendable IARs share the table, that query **must filter to `SupplyType == Asset`**, or expendable lines would leak into asset receiving reports. Added to Phase B.

### 4.2 `SupplyType` enum and propagation

```csharp
// ProcurementAcquisition.Contracts/v1/PurchaseRequests
public enum SupplyType { Asset = 0, Expendable = 1 }
```

- Add `SupplyType` to `PurchaseRequest` (header), `PurchaseOrder`, and `AssetInspectionAcceptanceReport`.
- `CreatePurchaseRequestCommand` accepts `SupplyType`; validator requires it (`IsInEnum`).
- **Propagation is server-authoritative, not client-supplied:**
  - `CreatePurchaseOrderCommandHandler` does **not** currently load the PR (it only stores `PurchaseRequestId`). Change it to load the source PR and copy `SupplyType` from it — don't trust a client field. (The duplicate-check already queries by `PurchaseRequestId`, so the PR is in scope.)
  - `CreateAssetIARCommandHandler` **already loads the PO** (`po`), so it copies `SupplyType` from `po` with no extra fetch.
  - The Canvass step needs no change — PO links directly to PR via `PurchaseRequestId`, so `SupplyType` skips over canvass.
- EF migrations add the column on `purchase_requests`, `purchase_orders`, `asset_iars` (default `0`/Asset for existing rows).

### 4.3 Acceptance handler branches the event

The `Accept` command gains an optional warehouse (D6); the validator requires it for expendable. **There is no Warehouse master entity** in the codebase today — `ProductInventory`/`Purchase` just store a free-form `WarehouseLocationId` + `WarehouseLocationName` pair, and the (soon-retired) Expendable PO captured both at creation. Since retiring that flow removes the only capture point, **acceptance must capture both id and name**, and `ProductInventory.Create(...)` requires the name. Hence:

```csharp
public sealed record AcceptAssetIARCommand(
    Guid Id,
    Guid? WarehouseLocationId = null,
    string? WarehouseLocationName = null) : ICommand<AssetIARDto>;
```

> See Open question G — without a warehouse master, the id/name are free-form (mirrors today's behaviour). Recommend a lightweight `WarehouseLocation` master (MasterData) so acceptance picks from a list instead of typing.

In `AcceptAssetIARCommandHandler` (the PO/PR completion logic stays untouched):

```csharp
if (iar.SupplyType == SupplyType.Asset)
    await eventBus.PublishAsync(new AssetIARAcceptedEvent(...), ct);
else // Expendable — only non-rejected lines flow (D7); warehouse from the command (D6)
    await eventBus.PublishAsync(new ExpendableIARAcceptedEvent(
        WarehouseLocationId: command.WarehouseLocationId!.Value, /* … accepted items … */), ct);
```

New contract (parallel to `AssetIARAcceptedEvent`, trimmed to what Expendable needs):

```csharp
public sealed record ExpendableIARAcceptedEventItem(
    Guid ProductId,            // = line.CatalogItemId (the Expendable Product)
    string Description,
    string Unit,
    decimal Quantity,          // accepted (non-rejected) quantity
    decimal UnitCost);

public sealed record ExpendableIARAcceptedEvent(
    Guid IARId,
    Guid PurchaseOrderId,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    Guid WarehouseLocationId,      // destination warehouse, chosen at acceptance (D6)
    string WarehouseLocationName,  // required by ProductInventory.Create — no warehouse master exists (Open Q G)
    IReadOnlyList<ExpendableIARAcceptedEventItem> AcceptedItems,
    string? TenantId,
    string CorrelationId = "") : IIntegrationEvent { /* Id, OccurredOnUtc, Source */ }
```

### 4.4 Expendable consumer (the new piece)

`Modules/Expendable/.../Integration/ExpendableIARAcceptedEventConsumer.cs`, implementing
`IIntegrationEventHandler<ExpendableIARAcceptedEvent>` — structurally a twin of the asset consumer:

**Idempotency guard (first thing the handler does):** the event may redeliver. Reusing `IARId` as the batch's `PurchaseId`, the consumer checks whether this IAR was already processed and returns early if so — preventing double-counting, a real risk because inventory is a running total (the asset consumer would only add duplicate rows). Implement the check as `db.ProductInventories.AnyAsync(pi => pi.Batches.Any(b => b.PurchaseId == @event.IARId))` if `InventoryBatch` is mapped as a queryable owned/related collection; otherwise add a small `ProcessedIntegrationEvents(IARId)` dedup table. Confirm the `InventoryBatch` mapping in `ProductInventoryConfiguration` before choosing.

The event already carries only non-rejected lines (D7), so the consumer does no rejection filtering. For each accepted line:
1. Skip + warn if `Quantity <= 0` or `ProductId` empty (parallels the asset consumer's skip-and-warn).
2. Resolve the `Product` by `ProductId`; skip + warn if not found (D5 guarantees it exists, but defend anyway). Map `Product.SKU → ProductCode` and `Product.Name → ProductName` for inventory creation (note: `Product` has no `ProductCode` field — it uses `SKU`).
3. Find the `ProductInventory` row for `(ProductId, WarehouseLocationId)`; if none, `ProductInventory.Create(tenantId, productId, product.SKU, product.Name, @event.WarehouseLocationId, @event.WarehouseLocationName)`.
4. Call `inventory.ReceiveFromPurchase(purchaseId: @event.IARId, productId, quantityAccepted: (int)qty, unitPrice: line.UnitCost)`.
   - Reuse `IARId` as the batch's `purchaseId` for traceability **and** as the idempotency key above.
5. `SaveChangesAsync` once after the loop; log `materialized / skipped` counts (same telemetry shape as the asset consumer).

> The consumer reaches into `ProcurementAcquisition.Contracts` only (allowed). It must **not** reference ProcurementAcquisition internals — same boundary rule the AssetRegister consumer already follows.

### 4.5 Retire the Expendable internal Purchase flow (D2)

- **Phase-gated removal.** Keep `Purchase`, `PurchaseInspection`, and their features compiling until the new path is verified, then delete:
  - `Features/v1/Purchases/*` (CreatePurchaseOrder, AddPurchaseLineItem, RecordPurchaseReceipt, SubmitPurchaseOrder, ApprovePurchaseOrder, CancelPurchaseOrder, GetPurchase, SearchPurchases, GetPurchasesBySupplier).
  - `Domain/Purchases/{Purchase,PurchaseInspection}.cs` and their EF configurations.
  - The Blazor pages/clients that call them.
- **Keep** `ProductInventory`, `InventoryBatch`, `RejectedInventory`, and the warehouse/issuance features — those remain the system of record for stock; only the *inbound purchase* path changes.
- EF migration drops `purchases` / `purchase_inspections` tables (after confirming no production data, or with a data-migration step — see Open question C).

### 4.6 Blazor / UX

- **PR create/edit:** add a `SupplyType` selector (Asset / Expendable) at the header. When `Expendable`, the line-item picker searches Expendable `Product`s; when `Asset`, it searches `PropertyItemCatalog` (current behaviour).
- **"Add product" redirect (D5):** in the expendable line picker, an "Add new product" action routes to the Product create page and returns the new product as selectable. No free-text expendable lines.
- **IAR accept screen:** for `SupplyType = Expendable`, hide the `PropertyNoField` / AssignPropertyNo column entirely; show the inspection (Passed/Rejected per line) UI that Semi/PPE already use.

---

## 5. Phased implementation

Each phase builds zero-warnings and keeps architecture tests green.

### Phase A — `SupplyType` plumbing (no behavioural change yet)
1. Add `SupplyType` enum + property to PR/PO/IAR domain + contracts.
2. Thread it through Create PR → PO → IAR (default `Asset` so existing flows are unchanged):
   - `CreatePurchaseRequestCommand` carries `SupplyType`; validator `IsInEnum`.
   - `CreatePurchaseOrderCommandHandler` **loads the source PR** and copies its `SupplyType` (server-authoritative — see §4.2).
   - `CreateAssetIARCommandHandler` copies from the already-loaded `po`.
3. Add the whole-number rule for expendable PR lines (D8) to `CreatePurchaseRequestCommandValidator`.
4. EF migrations for the three new columns (default `0`/Asset for existing rows).
5. Unit tests: PR created with each `SupplyType`; value propagates to PO and IAR; fractional expendable line is rejected.

**Deliverable:** every PR/PO/IAR carries an immutable supply type; asset flow behaves exactly as before.

### Phase B — Expendable acceptance event + consumer
1. Add `ExpendableIARAcceptedEvent` / `…Item` to ProcurementAcquisition contracts.
2. Add `WarehouseLocationId` + `WarehouseLocationName` to `AcceptAssetIARCommand` (optional); validator requires both when `SupplyType == Expendable` (D6).
3. Branch `AcceptAssetIARCommandHandler` to publish the right event by `SupplyType` (expendable event carries only non-rejected lines — D7).
4. Gate `AssignPropertyNo` (command + validator) to `SupplyType == Asset`.
5. **Filter `SearchAcceptedIARLineItemsQuery` to `SupplyType == Asset`** so expendable IAR lines never leak into AssetRegister's Receiving Report pre-fill (§4.1 note).
6. Add the `Modules.Expendable` → `Modules.ProcurementAcquisition.Contracts` project reference (mirrors AssetRegister).
7. Build `ExpendableIARAcceptedEventConsumer` in Expendable with the idempotency guard (§4.4); register it in `ExpendableModule` via `services.AddScoped<IIntegrationEventHandler<ExpendableIARAcceptedEvent>, ExpendableIARAcceptedEventConsumer>()` (same shape as `AssetRegisterModule`).
8. Tests: accepting an Expendable IAR increments the correct `ProductInventory`; **re-firing the same event does not double-count** (idempotency); asset IAR still hits AssetRegister; `SearchAcceptedIARLineItems` excludes expendable lines; architecture test confirms Expendable references only `ProcurementAcquisition.Contracts`.

**Deliverable:** an end-to-end expendable PR → accepted IAR → warehouse stock increase.

### Phase C — Blazor wiring
1. `SupplyType` selector on PR; catalog picker switches source by type.
2. "Add product" redirect for expendable lines (D5).
3. IAR accept screen hides PropertyNo for expendable; shows inspection UI.

**Deliverable:** operators can run the whole expendable acquisition in the UI.

### Phase D — Retire internal Expendable Purchase flow (D2)
1. Remove `Features/v1/Purchases/*`, `Domain/Purchases/{Purchase,PurchaseInspection}.cs`, their EF configs, the Blazor pages, and the generated-client call sites.
2. EF migration to **drop** `purchases` / `purchase_inspections` (dev-phase — no data migration; per Decision C).
3. Update `ExpendableModule` registrations + the Mediator assembly type list in `AMIS.Api/Program.cs` if any removed type was referenced there.
4. Remove now-unused Expendable purchase permissions + their UI permission gates.
5. **Keep** `ProductInventory`, `InventoryBatch`, `RejectedInventory`, and all warehouse/issuance features — only the inbound *purchase* path is removed.

**Deliverable:** a single stock-in path; dead code and tables gone.

### Phase E — Naming refactor (deferred; see §3 / Decision E1)
Neutral rename `Asset*` IAR types → `InspectionAcceptanceReport` / `IARAcceptedEvent`; update AssetRegister consumer + architecture tests + regenerate the Blazor client. Isolated, behavior-free PR. Preserve table names (`.ToTable`) and endpoint `WithName` strings to keep it code-only.

---

## 6. Open questions to resolve before kickoff

| # | Question | Resolution |
|---|----------|------------|
| A | ✅ **Resolved (D6).** Destination warehouse is chosen **at acceptance** — `WarehouseLocationId` on the `Accept` command, required for expendable. | Closed. |
| B | ✅ **Resolved (D7).** Rejected lines are excluded; no `RejectedInventory` row. (Note: IAR rejection is whole-line, not partial-quantity — there is no "N of M rejected" at IAR level.) | Closed. |
| C | ✅ **Resolved.** Dev phase — just **drop** `purchases`/`purchase_inspections` in Phase D. No data migration. | Closed. |
| D | ✅ **Resolved (D8).** Expendable PR line quantities must be whole numbers; PR validator rejects fractional. | Closed. |
| E | ✅ **Resolved.** **E1 — defer the rename.** Third-pass audit showed the blast radius (≈20 contract types, DbSet/tables, generated Blazor client, AssetRegister consumer, arch tests) is too large to mix with feature work. Name new artifacts neutrally now; do the full rename as a behavior-free Phase E PR. | Closed. |
| F | **Cost basis for stock value.** Use IAR accepted `UnitCost` (from PO/awarded canvass) as the receipt `unitPrice`. | Yes — `UnitCost` already flows on the accepted line; feeds moving-average in `ReceiveFromPurchase`. |
| G | 🆕 **No Warehouse master exists.** `WarehouseLocationId`/`Name` are free-form strings on records; the retired Expendable PO was the only capture point. | **Open.** Short term: capture free-form id+name at acceptance (mirrors today). Recommended: add a lightweight `WarehouseLocation` master (MasterData) so acceptance picks from a list. Decide before Phase C (it shapes the accept-screen UI). |

---

## 7. Why not duplicate ProcurementAcquisition

A parallel "ExpendableProcurement" module would duplicate PR/Canvass/PO/IAR logic that is **identical** in government practice — the PR, Abstract of Canvass, PO, and IAR are the same COA forms whether the item is a cabinet or bond paper. Duplication guarantees drift and double maintenance. The only real difference is the *destination* of accepted goods, which is already a clean publish/subscribe seam. We add one event + one consumer, mirroring the asset integration we already trust.

---

## 8. Verification gates

```powershell
dotnet build src/AMIS.Framework.slnx   # 0 warnings (CI gate)
dotnet test  src/AMIS.Framework.slnx   # all green, incl. Architecture.Tests
```

Manual smoke:
- **A/B:** create PR `SupplyType=Expendable` with catalog-backed lines → Canvass → PO → IAR → inspect → accept → confirm `ProductInventory.QuantityAvailable` rose by accepted qty and a batch exists referencing the IAR id.
- **Regression:** an asset PR still materializes `AssetRegistry` rows unchanged.
- **D5:** attempt an expendable line with no product → blocked; "Add product" redirect creates one and it becomes selectable.
- **D:** after Phase D, the old Expendable PO pages/endpoints are gone and stock can only be raised via acquisition acceptance.
```
