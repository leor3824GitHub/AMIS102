# Canvass Split-Award Implementation Plan

> **Scenario:** 1 Purchase Request with multiple line items → 1 Canvass (RIV) covering those lines → 3 supplier quotations → award **per line item** (lowest bidder wins each line) → **one Purchase Order per winning supplier** (up to 3 POs).

Module: `ProcurementAcquisition` · Pattern: Modular Monolith · CQRS (Mediator) · DDD
Status: **IMPLEMENTED 2026-06-04.** All steps complete; full solution builds with 0 errors and the entire test suite passes (incl. Architecture.Tests). Migration `CanvassSplitAward` generated and verified column-DDL-free (JSON-only), as predicted in §2.2.

---

## 1. Problem statement

Today a canvass is awarded **whole** to a single supplier, and exactly **one** Purchase Order can be created from it. The business needs the committee to award **different line items to different suppliers** within the same canvass, then generate a separate PO for each winning supplier.

The PR → canvass partition rule (each PR line belongs to at most one non-cancelled canvass) is **unchanged**. This work changes only the *award* and *PO generation* granularity **within** a single canvass.

### Current model vs. target

| Concern | Today | Target |
|---|---|---|
| Award granularity | One `AwardedSupplierId` for the whole canvass — [CanvassRequest.cs:140](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Domain/Canvass/CanvassRequest.cs#L140) | One winning supplier **per line item** |
| `AwardCanvassCommand` | `(CanvassRequestId, AwardedQuotationId)` — single quotation | `(CanvassRequestId, LineAwards[])` — PrItemNo → QuotationId |
| PO per canvass | Exactly **one** non-cancelled PO — [CreatePurchaseOrderCommandHandler.cs:102](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/PurchaseOrders/CreatePurchaseOrder/CreatePurchaseOrderCommandHandler.cs#L102) | **One PO per winning supplier** (up to 3) |
| Quotation ↔ PR line | Joined by **description** only; quotation lines carry no PR `ItemNo` | Quotation lines stamped with `PrItemNo` |
| Abstract of Canvass | Lists all supplier prices, no winner marked | Winning price flagged per line |

---

## 2. Second-pass findings (corrections to the first draft)

These are the gaps the first pass missed or got wrong. They reshape Steps 1–5.

1. **Quotation lines have no PR-line key.** `CanvassQuotationLineItem.ItemNo` is a sequential 1..N index *within the quotation*, unrelated to the PR `ItemNo`. All current joins are by **normalized description** — the scope check at [AddQuotationCommandHandler.cs:34-44](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/Canvass/AddQuotation/AddQuotationCommandHandler.cs#L34-L44), the Abstract's price matrix ([PrintAbstractOfCanvassFastQueryHandler.cs:162-184](src/Modules/FastReporting/Modules.FastReporting/Features/v1/Canvass/PrintAbstractOfCanvassFast/PrintAbstractOfCanvassFastQueryHandler.cs#L162-L184)), and the PO line resolver ([PurchaseOrderLineResolver.cs:63-66](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/PurchaseOrders/PurchaseOrderLineResolver.cs#L63-L66)). **Decision:** stamp `PrItemNo` onto `CanvassQuotationLineItem` at quotation-entry time (the covered-line lookup already exists there), so award/abstract/PO can key off `PrItemNo` unambiguously. Description matching stays as the *fallback* used to derive the `PrItemNo` at entry.

2. **Line items are stored as JSON, not relational columns.** `CanvassRequest.LineItems` and `CanvassQuotation.LineItems` are both `OwnsMany(...).ToJson()` ([CanvassRequestConfiguration.cs](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Data/Configurations/CanvassRequestConfiguration.cs), [CanvassQuotationConfiguration.cs](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Data/Configurations/CanvassQuotationConfiguration.cs)). Adding award fields / `PrItemNo` changes the **JSON shape only** — no `ALTER TABLE ADD COLUMN`. Existing rows simply lack the keys and deserialize as `null`. A migration may still be emitted from the model snapshot but should produce no real column DDL; **verify by generating it and inspecting before applying.**

3. **Re-award / award-after-PO guard is missing.** `AwardLines` must reject a re-award once non-cancelled POs exist for the canvass (today's `Award` only checks `Open`/`Evaluated` status). Add the guard in the domain.

4. **`AwardedSupplierId` aggregate property is safe to keep but barely consumed.** Only [CreateCanvassRequestCommandHandler.cs:125](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/Canvass/CreateCanvassRequest/CreateCanvassRequestCommandHandler.cs#L125) (maps null at create) and [GetCanvassRequestQueryHandler.cs:41](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/Canvass/GetCanvassRequest/GetCanvassRequestQueryHandler.cs#L41) read it, and `AwardedSupplierName` is already always passed `null`. No IAR/PO logic depends on it → keep it as a derived/nullable convenience (null when >1 distinct winner). Low risk.

5. **DTO ripple is wider than noted.** Both `CanvassRequestDto` **and** `CanvassRequestSummaryDto` carry singular `HasPurchaseOrder` / `PurchaseOrderNumber`, and [GetCanvassRequestQueryHandler.cs:27-32](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/Canvass/GetCanvassRequest/GetCanvassRequestQueryHandler.cs#L27-L32) takes only the **first** PO. `SearchCanvassRequestsQueryHandler` must also change. All three need a list / count of POs.

6. **PO line price = awarded unit price.** The generated PO's `UnitCost` must come from the **winning quotation line**, not the PR's `EstimatedUnitCost`. This is the reason `AwardedUnitPrice` must be carried through the award.

7. **PO duplicate guard has two parts.** The canvass-level "already a PO from this canvass" check ([CreatePurchaseOrderCommandHandler.cs:102-118](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/PurchaseOrders/CreatePurchaseOrder/CreatePurchaseOrderCommandHandler.cs#L102-L118)) must relax to per-**(canvass, supplier)**. The description-overlap check (lines 120-163) can stay — each supplier's PO carries only its won lines, so no same-supplier overlap arises.

8. **PO line resolver works unchanged.** The bulk PO command can reuse `PurchaseOrderLineResolver.ResolveAndValidate` (description-based backfill of `StockNumber`/`CatalogItemId` + Supply/Asset guard) as-is.

---

## 3. Design decisions (CONFIRMED 2026-06-04)

1. **Winner default:** award dialog auto-selects the lowest unit price per line; committee may override per line. ✅
2. **Partial award:** *all* covered lines must be awarded before status → `Awarded` (all-at-once, no incremental award). ✅
3. **Award vs. PO are two stages:** award first (lets the committee print the Abstract), then a one-click "Generate POs" bulk command. Re-running the bulk command is idempotent via the per-(canvass, supplier) guard. ✅
4. **Description-collision handling:** quotation entry resolves `PrItemNo` server-side from the covered line's description; AddQuotation/UpdateQuotation reject when a canvass covers two lines with the same normalized description (ambiguous), rather than enforcing distinctness globally at PR creation. ✅

---

## 4. Step-by-step plan

### Step 1 — Stamp `PrItemNo` onto quotation lines

Files: [CanvassQuotation.cs](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Domain/Canvass/CanvassQuotation.cs), [AddQuotationCommandHandler.cs](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/Canvass/AddQuotation/AddQuotationCommandHandler.cs), [UpdateQuotationCommandHandler.cs](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/Canvass/UpdateQuotation/UpdateQuotationCommandHandler.cs)

- Add `int PrItemNo` to `CanvassQuotationLineItem` (+ factory/`Create` param).
- In AddQuotation/UpdateQuotation, resolve each quoted line's `PrItemNo` from the canvass's covered lines by normalized description (the lookup is already built for the scope check). Reject a quote line whose description matches no covered line (already done) — now also capture the resolved `PrItemNo`.
- JSON storage → no DDL; just extend the `OwnsMany(...).ToJson()` block with `b.Property(li => li.PrItemNo)`.

### Step 2 — Domain: per-line award on `CanvassRequest`

File: [CanvassRequest.cs](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Domain/Canvass/CanvassRequest.cs)

- Add to `CanvassRequestLineItem`: `Guid? AwardedQuotationId`, `Guid? AwardedSupplierId`, `decimal? AwardedUnitPrice`, plus an `AwardTo(quotationId, supplierId, unitPrice)` mutator.
- Replace `Award(Guid awardedSupplierId, …)` with:
  ```csharp
  public void AwardLines(
      IReadOnlyDictionary<int, (Guid QuotationId, Guid SupplierId, decimal UnitPrice)> awardsByPrItemNo,
      IEnumerable<CanvassAwardSignatory>? signatories = null)
  ```
  - **Guards:** status must be `Open`/`Evaluated`; **no non-cancelled PO may exist** (re-award guard — caller passes this fact in or it's checked in the handler); **every** covered `PrItemNo` has exactly one award.
  - Sets each line's winner; marks each winning quotation `IsAwarded`; sets `Status = Awarded`; freezes ROPC committee signatories.
- Make aggregate `AwardedSupplierId` derived/nullable (null when >1 distinct winner).
- JSON storage → award fields ride inside the existing `LineItems` JSON column.

### Step 3 — Persistence + migration (verify no-op)

- Extend the two `ToJson()` blocks with the new properties (Step 1 + Step 2).
- Generate the migration and **inspect it** — expect no `ADD COLUMN`. If EF emits an empty migration, keep it for snapshot consistency; if it emits column DDL, stop and reconcile.
  ```powershell
  dotnet ef migrations add CanvassSplitAward --project src/Playground/Migrations.PostgreSQL --context ProcurementDbContext --output-dir ProcurementAcquisition
  ```

### Step 4 — Contracts

File: [CanvassContracts.cs](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition.Contracts/v1/Canvass/CanvassContracts.cs)

- New `record CanvassLineAwardRequest(int PrItemNo, Guid QuotationId)`.
- Change `AwardCanvassCommand` → `(Guid CanvassRequestId, IReadOnlyList<CanvassLineAwardRequest> LineAwards)`.
- Add `int PrItemNo` to `CanvassQuotationLineItemDto`.
- Extend `CanvassLineItemDto` with `AwardedQuotationId`, `AwardedSupplierId`, `AwardedSupplierName`, `AwardedUnitPrice`.
- Replace singular `HasPurchaseOrder` / `PurchaseOrderNumber` on `CanvassRequestDto` **and** `CanvassRequestSummaryDto` with a PO list `IReadOnlyList<CanvassPurchaseOrderRefDto>(SupplierId, SupplierName, PoNumber, Status)` (and a count for the summary).
- New PO contract: `CreatePurchaseOrdersFromCanvassCommand(Guid CanvassRequestId, … shared PO terms …) : ICommand<IReadOnlyList<PurchaseOrderDto>>`.

### Step 5 — Award handler + validator

- [AwardCanvassCommandHandler.cs](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/Canvass/AwardCanvass/AwardCanvassCommandHandler.cs): load canvass + quotations + line items; check no non-cancelled PO exists; build the award map by looking up each `LineAward.QuotationId`'s line **by `PrItemNo`** to get the `AwardedUnitPrice` + `SupplierId`; call `canvass.AwardLines(...)` with the frozen committee (committee resolution unchanged).
- [AwardCanvassCommandValidator.cs](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/Canvass/AwardCanvass/AwardCanvassCommandValidator.cs): non-empty `LineAwards`; every covered `PrItemNo` appears exactly once; each `QuotationId` belongs to this canvass and actually quoted that `PrItemNo`; no duplicate `PrItemNo`.

### Step 6 — PO generation: one per winning supplier

- **Relax the canvass-level duplicate guard** ([CreatePurchaseOrderCommandHandler.cs:102-118](src/Modules/ProcurementAcquisition/Modules.ProcurementAcquisition/Features/v1/PurchaseOrders/CreatePurchaseOrder/CreatePurchaseOrderCommandHandler.cs#L102-L118)): "one non-cancelled PO per canvass" → "one non-cancelled PO per **(canvass, supplier)**".
- **New `CreatePurchaseOrdersFromCanvassCommand` handler** that:
  - Loads the canvass; rejects if not `Awarded`.
  - Groups awarded lines by `AwardedSupplierId`; pulls supplier header (name/address/TIN) from each winning quotation.
  - For each group: build PO line requests using **`AwardedUnitPrice`** for `UnitCost`; run them through `PurchaseOrderLineResolver.ResolveAndValidate` (PR backfill + Supply/Asset guard).
  - Allocate a PO number per group via the existing `PoNumberSequence` xmin-retry loop.
  - Skip suppliers that already have a non-cancelled PO for this canvass (idempotent re-run).
  - Save all in one transaction; return `IReadOnlyList<PurchaseOrderDto>`.

### Step 7 — Abstract of Canvass report

File: [PrintAbstractOfCanvassFastQueryHandler.cs](src/Modules/FastReporting/Modules.FastReporting/Features/v1/Canvass/PrintAbstractOfCanvassFast/PrintAbstractOfCanvassFastQueryHandler.cs) + the `AbstractOfCanvassFast.frx` template.

- Switch the price-matrix join from description to `PrItemNo` (now available on both sides).
- Mark the winning supplier's price per line (bold / "✓ Awarded" indicator) using `CanvassLineItemDto.AwardedSupplierId`. Falls back gracefully for un-awarded canvasses (no winner shown).

### Step 8 — Blazor UI

- **Award dialog:** replace the single-quotation radio with a per-line winner grid (PR line rows × supplier columns); default-select the lowest unit price, allow per-line override; submit `LineAwards[]`.
- **PO flow:** add a "Generate POs" action (collects shared PO terms once in a dialog) calling `CreatePurchaseOrdersFromCanvassCommand`; show the resulting N POs. Update [PurchaseOrderFormDialog.razor](src/Playground/Playground.Blazor/Components/Pages/Procurement/PurchaseOrderFormDialog.razor) / [PurchaseOrdersPage.razor](src/Playground/Playground.Blazor/Components/Pages/Procurement/PurchaseOrdersPage.razor) and [CanvassRequestsPage.razor](src/Playground/Playground.Blazor/Components/Pages/Procurement/CanvassRequestsPage.razor) for the new list-of-POs DTO.
- Update `ProcurementClient` for the new command/DTO shapes; gate buttons with `UserProfileState.Permissions.Contains(...)`.

### Step 9 — Tests

- **Domain:** `AwardLines` rejects partial / duplicate / unknown lines, rejects re-award when a PO exists, sets per-line winners + prices, leaves aggregate `AwardedSupplierId` null for multi-supplier.
- **Quotation:** AddQuotation stamps the correct `PrItemNo`; out-of-scope description still rejected.
- **Handler:** 6 lines, 3 suppliers winning 2 each → exactly 3 POs with correct line partition + **awarded** unit prices; per-(canvass, supplier) guard; bulk re-run is idempotent.
- **Invariant:** PR → canvass partition rule unchanged.

---

## 5. Risks & fragilities

- **Description-based matching is the load-bearing weakness.** Two PR lines with identical descriptions would collide in every existing join. Stamping `PrItemNo` (Step 1) removes this for award/PO/abstract, but AddQuotation still *derives* `PrItemNo` from description at entry — duplicate descriptions on the PR remain ambiguous there. Consider validating PR line descriptions are distinct, or have the quotation-entry UI bind directly to `PrItemNo` instead of free-text description.
- **JSON migration uncertainty:** confirm the generated migration is column-DDL-free before applying (Step 3).
- **Two-stage award→PO** means a canvass can sit `Awarded` with 0 POs. The Abstract reads from award data, so it prints correctly in that window; PO list is simply empty until "Generate POs" runs.

---

## 6. Affected files (summary)

| Layer | File |
|---|---|
| Domain | `Domain/Canvass/CanvassRequest.cs`, `Domain/Canvass/CanvassQuotation.cs` |
| Persistence | `Data/Configurations/CanvassRequestConfiguration.cs`, `CanvassQuotationConfiguration.cs`; verify-only migration in `Migrations.PostgreSQL` |
| Contracts | `Contracts/v1/Canvass/CanvassContracts.cs`; new PO command in `Contracts/v1/PurchaseOrders` |
| Features | `Features/v1/Canvass/AddQuotation/*`, `UpdateQuotation/*`, `AwardCanvass/*`, `GetCanvassRequest/*`, `SearchCanvassRequests/*`; `Features/v1/PurchaseOrders/CreatePurchaseOrder/*` + new `CreatePurchaseOrdersFromCanvass/*` |
| Reporting | `Modules.FastReporting/.../PrintAbstractOfCanvassFast/*` + `AbstractOfCanvassFast.frx` |
| Blazor | Award dialog, `PurchaseOrderFormDialog.razor`, `PurchaseOrdersPage.razor`, `CanvassRequestsPage.razor`, `ProcurementClient` |
| Tests | `ProcurementAcquisition.Tests` (domain + handlers) |

---

## 7. Build order & verification

Order: Step 1 (quotation `PrItemNo`) → 2 (domain award) → 3 (migration verify) → 4 (contracts) → 5 (award handler) → 6 (bulk PO) → 7 (report) → 8 (UI) → 9 (tests throughout).

```powershell
dotnet build src/AMIS.Framework.slnx   # 0 warnings required
dotnet test  src/AMIS.Framework.slnx   # all tests pass
```

---

## 8. Open questions — RESOLVED

All four resolved on 2026-06-04 (see §3):
1. ✅ Auto-select lowest unit price per line, with manual override.
2. ✅ All covered lines must be awarded before `Awarded` (no partial award).
3. ✅ One-click bulk PO generation.
4. ✅ Resolve `PrItemNo` server-side from description; reject ambiguous duplicate descriptions within a canvass (no global PR-level distinctness rule).
