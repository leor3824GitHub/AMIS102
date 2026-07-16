# Inventory-Ledger Restructure — Detailed Plan

> Prepared: 2026-07-16 · Scope: the deferred tail of `DOMAIN-OPTIMIZATION-PLAN.md` —
> **Phase 4 (batch data-growth)** + **the structural half of Phase 5** (ProductInventory snapshot
> removal, `InventoryBatch` field cleanup, dual-class rename).
> Status: **PLAN ONLY — not yet implemented.** The money-path test net is already in place (see below).

## Why this is staged separately

Everything else in the optimization plan was mechanical/additive. This tail touches the **moving-average
valuation + Stock Card** on the `ProductInventory` aggregate, which until today had **zero** test coverage.
It also cannot be *runtime*-verified from the coding environment (no way to drive the warehouse/Stock-Card
UI against a live Postgres). So the sequence is: **(1) build the net [done], (2) agree this plan, (3)
implement behind the net, (4) you runtime-verify against Postgres.**

### Safety net already in place (2026-07-16)

- `Expendable.Tests/Domain/ProductInventoryValuationTests.cs` — 17 pure-domain cases: moving-average across
  multi-price receipts, reserve/cancel/issue lifecycle, computed `AverageUnitPrice`/`ReservedValue`, batch
  ledger, TotalValue-never-negative, discontinue + argument guards.
- `Expendable.Tests/Integration/ProductInventoryStockCardTests.cs` — 2 SQLite cases: every batch surfaces as
  a "Receipt" line with the correct moving-average running balance; unknown product → null.
- Full `Expendable.Tests` = 34 green. These assertions are the contract the restructure must preserve.

---

## Discovered facts that change the original one-liners

1. **`ProductInventory` batches are append-only receipt records — they are never drawn down.**
   `ReceiveFromPurchase` does `Batches.Add(...)`; `IssueReservedStock` adjusts only the aggregate
   `QuantityReserved/Issued/TotalValue`. `InventoryBatch.MarkIssued()` exists but **is called nowhere**, so
   `QuantityRemaining` always equals `QuantityAvailable`. **Consequence: Phase 4's literal rule "move
   *exhausted* batches (QuantityRemaining == 0) to an archive" never fires for ProductInventory.** The batch
   list grows by exactly one entry per accepted IAR and is rewritten (whole JSON doc) on every
   reserve/issue/cancel. This is the real growth/write-amplification concern — not per-batch exhaustion.

2. **Single-storeroom system.** There is no managed warehouse table — `ExpendableModuleConstants.DefaultSupplyLocation`
   (`Id = 1111…1111`, `Name = "Central Supply Room"`) is the only location. So `ProductInventory.WarehouseLocationName`
   can't be replaced by a DB join; it resolves from a constant lookup by `WarehouseLocationId`.

3. **`ProductInventoryDto` is baked into `Generated.cs`** (`WarehouseAsync`, `SearchAsync` →
   `PagedResponseOfProductInventoryDto`). **If we keep the DTO shape identical and repopulate the three
   fields via join/lookup, no NSwag regen is needed** — this is the key to a small blast radius.

4. **The reports do NOT read the ProductInventory snapshot fields.**
   - `GetStockCardQueryHandler` already joins `Product` for `StockNo`/`Name` (its `ProductCode`/`ProductName`
     come from `Product`, not `ProductInventory`).
   - `GetDepartmentIssuanceReport*` order by their **own** projected `ProductName` (sourced from
     SupplyRequest/Product), not `ProductInventory`.
   - So the feared "StockCard/DepartmentIssuance/EmployeeIssuance PDF cascade" does **not** materialize for
     the snapshot removal. (EmployeeIssuance to be re-confirmed at implementation time, but it reads employee
     inventory + product, not warehouse snapshot.)

**Net effect:** the snapshot removal is ~7–8 files with **no Generated.cs and no PDF changes** — far smaller
than the earlier "21 files" estimate. Phase 4, conversely, is *more* subtle than its one-liner.

---

## Part A — Phase 5 structural: remove ProductInventory snapshot columns

**Goal:** drop `ProductInventory.ProductCode`, `.ProductName`, `.WarehouseLocationName` (same-DbContext copies
that drift on product rename), keeping `ProductInventoryDto` byte-identical by joining/looking up at read time.

**Approach: keep-DTO-shape-join (recommended).** DTO unchanged ⇒ `Generated.cs` untouched, Blazor untouched.

### Files & changes

| File | Change |
|---|---|
| `Domain/Warehouse/ProductInventory.cs` | Remove the 3 properties; drop `productCode`/`productName`/`warehouseLocationName` params from `Create(...)`. Keep `WarehouseLocationId`. |
| `Data/Configurations/ProductInventoryConfiguration.cs` | Remove the 3 `builder.Property(...)` mappings. |
| `Integration/SupplyIARAcceptedEventConsumer.cs` | `ProductInventory.Create(tenantId, product.Id, warehouseId)` — drop the name/code args (they were sourced from `product`/constant anyway). |
| `Features/v1/Warehouse/WarehouseMapper.cs` | `ToProductInventoryDto` gains params `(string productCode, string productName, string warehouseName)` — no longer reads them off the entity. |
| `Features/v1/Warehouse/GetProductInventory/…Handler` | Join `Products` for code/name; resolve warehouse name via a `WarehouseLocationLookup` helper over the constant(s). |
| `Features/v1/Warehouse/SearchProductInventory/…Handler` | Same join + lookup. **Plus:** the `ProductCode`/`ProductName` *filters* now filter on the joined `Product.StockNo`/`Product.Name` instead of the (removed) inventory columns. |
| `Features/v1/Warehouse/GetWarehouseStockLevels/…Handler` | Same join + lookup. |
| `Features/v1/Warehouse/GetProductInventoriesByProducts/…Handler` | Same join + lookup (already loads a bounded product set). |
| `ExpendableModuleConstants` | Add a tiny `ResolveWarehouseName(Guid id)` (returns `DefaultSupplyLocation.Name` for the known id, else `""`). One place to grow when locations become a table. |
| `Migrations.PostgreSQL` | New migration `Expendable_DropInventorySnapshotColumns` (drop `ProductCode`, `ProductName`, `WarehouseLocationName`). |

**Alternative considered — keep + `ProductUpdated`-sync.** Rejected: adds an event + handler to fix drift on a
field the query can trivially join; the join approach removes the drift class entirely. (This is what the
current memory note calls the "deliberate denormalized cache" — this plan overrides it in favour of removal.)

### Tests to add
- Extend the Warehouse handler tests: after a `Product` rename, `SearchProductInventory`/`GetWarehouseStockLevels`
  return the **new** name (proves the join, not a stale snapshot) — the plan's own Phase-5 acceptance check.

---

## Part B — Phase 5 structural: `InventoryBatch` cleanup + dual-class rename

### B1. Field cleanup (Warehouse `InventoryBatch`, the JSON-owned type)
- Remove `InventoryBatch.ProductId` — the owning `ProductInventory` already carries `ProductId`; the Stock Card
  reads it off the parent, not the batch. (JSON column — no migration, but re-check the Stock Card + any batch
  reader.)
- Remove `InventoryBatch.InspectionDate` — always set `= ReceivedDate` at creation, never updated. (JSON column.)
- Also review `InventoryBatch.Version` (int) — a hand-rolled per-batch counter incremented only by the
  never-called `MarkIssued`. If B/Phase-4 keeps batches append-only, this is dead; drop with the field cleanup.

### B2. Dual-class rename (bug-prevention, cosmetic)
Two `InventoryBatch` types coexist in one module:
- `Domain.Warehouse.InventoryBatch` (receipt record, JSON) → rename `WarehouseReceiptBatch`.
- `Domain.Inventory.InventoryBatch` (employee stock, relational, drawn down) → rename `EmployeeStockBatch`.

Rename touches: both domain files, `ProductInventoryConfiguration` (`OwnsMany`), `InventoryConfiguration`
(`OwnsMany` / table name via `nameof`), the Stock Card handler, and the two valuation tests. The employee-batch
table name changes if it derives from `nameof(InventoryBatch)` → **a migration** (or pin the old table name via
`.ToTable("InventoryBatches")` to avoid one). Prefer pinning the table name to keep it migration-free.

---

## Part C — Phase 4: batch data-growth hygiene (re-conceived)

Because ProductInventory batches are **append-only and never exhausted** (fact #1), the original "archive
exhausted batches" rule does not apply. Three real options — **decide the business rule first**:

| Option | What | Pros | Cons |
|---|---|---|---|
| **C1. Promote batches to a relational child table** (recommended) | Replace the `ToJson("Batches")` owned collection with a real `WarehouseReceiptBatch` table (FK → ProductInventory). Stock Card becomes a SQL join instead of JSON load-and-flatten. | Kills whole-doc rewrite on every reserve/issue; batches queryable/indexable; Stock Card becomes set-based; natural home for future FIFO. | Migration moves JSON → table (dev data disposable, so clean migration is fine); Stock Card handler rewritten to query the table. |
| **C2. Age/count rollover to `WarehouseReceiptBatchArchive`** | Keep JSON for "recent" batches; a job moves batches older than N months (or beyond the newest K) to a relational archive. Stock Card reads `archive ∪ open`. | Smallest hot JSON doc; matches the plan's archive-table idea. | Two storage shapes to union; rollover job + its own idempotency; still rewrites the JSON doc on movement. |
| **C3. Do nothing structural; add `EmployeeInventory` pruning only** | Leave ProductInventory as-is; add exhaustion-based pruning/cleanup to `EmployeeInventory` batches (those *are* drawn down). | Minimal risk; addresses the batch type that genuinely grows-then-dies. | Doesn't fix ProductInventory write-amplification. |

**Recommendation:** **C1** (promote to a child table). It's the plan's own stated end-state ("promote batches
to a full child table and drop the JSON column"), it removes write-amplification outright, and it makes the
Stock Card a proper query — which the net already pins the expected output for. C2's union is more moving parts
for a single-storeroom system that isn't yet at the volume that needs tiering.

### C1 files & changes
- `Domain/Warehouse/ProductInventory.cs` — `Batches` stays a domain collection; only persistence mapping changes.
- `Data/Configurations/ProductInventoryConfiguration.cs` — replace `OwnsMany(...ToJson("Batches"))` with
  `OwnsMany(... ob.ToTable("WarehouseReceiptBatches"); ob.WithOwner()...)` (owned relational) **or** a full
  entity + explicit FK. Owned-relational keeps the aggregate boundary and is the smaller change.
- `Features/v1/Reports/GetStockCard/GetStockCardQueryHandler.cs` — read batches via the table (can stay
  in-memory flatten initially; the net's assertions are storage-agnostic, so this is safe to refactor).
- `Migrations.PostgreSQL` — `Expendable_WarehouseBatchesToTable` (create table, drop JSON column).
- Tests: `ProductInventoryStockCardTests` already asserts the receipt lines + running balance; extend the
  valuation tests only if the batch mapping change alters any domain method (it should not — mapping-only).

---

## Migration strategy

Dev data is disposable (project convention), so these can be **clean** migrations — no backfill gymnastics.
Batch them to minimize snapshot churn, in dependency order:

1. `Expendable_DropInventorySnapshotColumns` (Part A) — independent, lowest risk; ship first.
2. `Expendable_WarehouseBatchesToTable` (Part C1) — the structural one; ship after A is verified.
3. Field cleanup / rename (Part B) folds into whichever of the above touches the batch shape (B1 into C1; B2
   table-name pinned to stay migration-free).

Each: `dotnet ef migrations add <Name> --project src/Host/Migrations.PostgreSQL --startup-project
src/Host/AMIS.Api --context ExpendableDbContext --output-dir Migrations/Expendable`, then review the scaffold
(watch for the `xmin` system-column `AddColumn`/`DropColumn` artifact — hand-remove as before).

---

## Verification

Automated (I can run):
- `dotnet build src/AMIS.Framework.slnx` (0 new warnings) + full `Expendable.Tests` green after each part.
- New rename-drift test (Part A) proves the join replaced the snapshot.

Runtime (you, against Postgres) — the plan's required pass:
- Warehouse page + `SearchProductInventory`: rows still show code/name/warehouse; rename a product → the list
  reflects the new name immediately (no stale snapshot).
- Stock Card page for a product with ≥2 receipts + ≥1 fulfilled request: receipt + issue lines and the running
  moving-average balance match pre-change output.
- Fulfill a supply request end-to-end (reserve → issue): `AverageUnitPrice`/`TotalIssuedValue` unchanged;
  concurrent double-fulfill still conflicts (xmin).
- Accept a Supply IAR: new batch lands; `EXPLAIN` the Stock Card query (C1) shows an index scan, not JSON.

---

## Recommended sequencing

1. **Part A (snapshot removal)** — smallest, no Generated.cs/PDF impact, immediate drift fix. Do first.
2. **Part C1 (batches → table)** — the real perf win; do behind the net after A is runtime-verified.
3. **Part B (field cleanup + rename)** — fold into C1's migration; pin the employee-batch table name to avoid
   a second migration.

Parts A, B, C1 can each be a separate PR/commit so runtime verification is incremental and each is easy to
revert. **Nothing here should be committed without the Postgres runtime pass above.**
