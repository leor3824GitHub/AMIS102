# Asset Acquisition Flow — Implementation Plan

> Aligns the end-to-end asset acquisition pipeline (Procurement → Finance → Asset Register) with the agreed business flow. Closes three specific gaps: the Finance payment-first loop-back, PropertyNo generator at IAR time, and soft-state inter-office transfer.

---

## 1. Goal

Implement the broad asset-acquisition flow exactly as drawn:

```
Procurement                       Finance                   Asset Register
-----------                       -------                   --------------
PR  ──►  RFQ  ──►  AoC  ──►  PO ─┐
                                 │  ┌──[payment-first?]──► BUR + Voucher ──┐
                                 │  │                                       │
                                 └──┴──► Receive Item ──► IAR ──► PropertyNo per Item
                                                                        │
                                                                        ▼
                                                          PPERR (PPE) / SMRR (SE)
                                                                        │
                                                                        ▼
                                                              Store in Asset Registry
                                                                        │
                                                          ┌─────────────┴─────────────┐
                                                          ▼                           ▼
                                                Transfer to other Office?       (no transfer)
                                                          │                           │
                                                          ▼                           ▼
                                              PPEIR (PPE) / SMIR (SE)        PAR (PPE) / ICS (SE)
                                                          │                           │
                                                          ▼                           ▼
                                            Mark asset TransferredOut          Issue to accountable
                                                in local registry                     officer
```

---

## 2. Current State (audit)

| Step | Status | Where |
|------|--------|-------|
| PR / RFQ / AoC / PO (general) | ✅ Exists | `Modules/ProcurementAcquisition/.../Features/v1/{PurchaseRequests,Canvass,PurchaseOrders}` |
| Asset-specific PR / PO / IAR | ✅ Exists | `Modules/AssetProcurement/.../Features/v1/{AssetPurchaseRequests,AssetPurchaseOrders,AssetIARs}` |
| BUR / Disbursement Voucher | ✅ Exists | `Modules/Finance/.../Features/v1/{BudgetUtilizationRecords,DisbursementVouchers}` |
| PPERR / SMRR (manual creation) | ✅ Exists | `Modules/AssetRegister/.../Features/v1/Receiving/CreateReceivingReport` |
| PPERR / SMRR (auto from IAR accept) | ⚠️ Partial — materializes `AssetRegistry` but **does not** create a `ReceivingReport` aggregate | `Modules/AssetRegister/.../Integration/AssetIARAcceptedEventConsumer.cs` |
| PropertyNo value object (COA 2020-006) | ✅ Exists | `Modules.AssetRegister.Contracts/v1/ValueObjects/PropertyNumber.cs` (format `YYYY-AA-BB-NNNN-CC`) |
| PropertyNo UI generator (rich, NFA-style) | ✅ Exists | `Playground.Blazor/Components/Shared/PropertyNoField.razor` (format `YYYY-NFA-OFFICE-CLASS-CATEGORY-SEQ`) |
| TangibleItems page using `PropertyNoField` | ✅ Exists | `Pages/AssetManagement/TangibleItemsPage.razor` |
| AssetIAR Blazor page | ❌ Missing | — |
| PAR / ICS (accountability within office) | ✅ Exists | `Modules/AssetRegister/.../Features/v1/Accountability` |
| PPEIR / SMIR (issuance reports) | ✅ Exists | `Modules/AssetRegister/.../Features/v1/Issuance` (numbers minted; lifecycle effect TBD) |
| Inter-office transfer lifecycle state | ❌ Missing — `LifecycleState` has `Available / Assigned / UnderInvestigation / Unserviceable / Disposed` only | `Modules.AssetRegister.Contracts/v1/Enums.cs:23` |
| Finance ↔ Procurement payment-first gate | ❌ No coupling — Finance creates BUR/DV independently; PO has no "payment-first" flag visible to IAR step | — |

### Format dissonance worth surfacing now

Two PropertyNo formats coexist in the codebase:

| Format | Where | Used by |
|--------|-------|---------|
| `YYYY-AA-BB-NNNN-CC` (COA 2020-006) | `Modules.AssetRegister.Contracts/v1/ValueObjects/PropertyNumber.cs` | `AssetRegistry` (the official registry value object) |
| `YYYY-NFA-OFFICE-CLASS-CATEGORY-SEQ` (NFA local) | `PropertyNoField.razor` preview + `TangibleItem` plain-string storage | `TangibleItems` page only |

> **Open question A:** Which format is the canonical PropertyNo for the Asset Registry? The IAR-acceptance consumer currently produces COA-format numbers with hardcoded `subMajor=01 / glAccount=01 / location=00` — that's a placeholder, not real provenance. The manual `CreateReceivingReport` handler at least derives them from `PropertyItemCatalog.UacsObjectCode`. See §5.

---

## 3. The three gaps from the agreed flow

### Gap 1 — Finance payment-first loop-back

**Today:** Finance creates BUR + DV independently. There's no signal back to Procurement saying "ok, now you may receive the goods and issue the IAR." The procurement officer just knows out of band.

**Target:** Make the gate explicit so the UI guides the user.

**Minimum-viable change:**
- Add `PaymentTerms` (or `RequiresAdvancePayment` bool) to `AssetPurchaseOrder` — captured at PO creation from supplier policy.
- On the AssetIAR creation page (new — see Gap 2), if `PO.RequiresAdvancePayment == true`, check that a `DisbursementVoucher` referencing this PO exists in **Paid** status before allowing IAR submission. Show a banner with a link to create the BUR/DV if missing.
- No new domain event needed; this is a UI guard backed by a query.

**Why this scope:** the diagram shows orchestration, not data flow back from Finance. A flag on the PO + a precondition check on the IAR page is enough.

### Gap 2 — PropertyNo generator at IAR step (duplicate the existing component)

**User's intent:** the same generator UX already running on `TangibleItemsPage` should appear when the user creates/accepts an IAR.

**Module ownership confirmed:** Inspection and Acceptance lives in `Modules/AssetProcurement` (already so in the repo: `Features/v1/AssetIARs/{Create,Accept,Reject}AssetIAR`). All UI and contract changes below belong to that module.

**Today:** `PropertyNoField.razor` already lives in `Components/Shared/` and is a shared component. There's no AssetIAR Blazor page at all, so there's nowhere to use it yet. Backend-side, the `AssetIARAcceptedEventConsumer` auto-mints PropertyNos with placeholder values — the human-in-the-loop step from the diagram is bypassed.

**Target:** the IAR acceptance step becomes the explicit place where the user assigns one PropertyNo per accepted unit, using the existing generator component. The auto-minting consumer is removed (or downgraded to a safety net) so users — not the integration handler — own the numbers.

#### 2.1 IAR form mapping — SOP GS-PD26 (Exhibit 3)

The paper form establishes the required fields. Mapping to current `AssetInspectionAcceptanceReport` + `AssetIARLineItem`:

| Form field | Current domain | Status |
|---|---|---|
| IAR No. | `IarNumber` | ✅ |
| Date (IAR) | `IarDate` | ✅ |
| Supplier | `SupplierId`, `SupplierName` | ✅ |
| PO No. / Date | `PurchaseOrderId` (PO No. resolved by join) | ✅ |
| **Requisitioning Office / Dept.** | — | ❌ Missing |
| **Responsibility Center Code** | — | ❌ Missing |
| **Invoice No.** | reuses `DeliveryReceiptNo`? | ⚠️ Ambiguous |
| **Invoice Date** | reuses `DeliveryDate`? | ⚠️ Ambiguous |
| Stock / Property No. *(per row)* | — | ❌ Missing on `AssetIARLineItem` |
| Description | `Description` | ✅ |
| Unit | `Unit` | ✅ |
| Quantity | `Quantity` | ✅ |
| **Inspection — Date Inspected** | — | ❌ Missing |
| **Inspection — verified-and-in-order flag** | — | ❌ Missing (currently implicit in `Accept()`) |
| Inspection Officer / Committee | `InspectedById` | ✅ |
| **Acceptance — Date Received** | `DeliveryDate`? | ⚠️ Likely separate |
| **Acceptance — Complete / Partial (with qty)** | — | ❌ Missing (no partial-accept model today) |
| Supply / Property Custodian | `ReceivedById` | ✅ |

**Domain additions needed in `AssetProcurement` (Phase B):**

- On `AssetInspectionAcceptanceReport`:
  - `RequisitioningOffice` (string), `ResponsibilityCenterCode` (string).
  - `InvoiceNo` (string?), `InvoiceDate` (DateOnly?) — distinct from delivery receipt.
  - `DateInspected` (DateOnly?), `InspectionVerified` (bool).
  - `DateReceived` (DateOnly?), `AcceptanceKind` (enum: `Complete | Partial`), and per-line `AcceptedQuantity` when partial.
- On `AssetIARLineItem`:
  - `StockPropertyNo` (string) — the value typed/generated via `PropertyNoField`. Validation deferred to the registry.

> **Open question F:** Is "Invoice No." (form) the same as `DeliveryReceiptNo` (current domain)? Government practice usually treats them separately (DR is from courier/warehouse; invoice is the billing doc). Recommendation: add `InvoiceNo` as a distinct field and keep `DeliveryReceiptNo`.

#### 2.2 PropertyNo cardinality — per row vs. per unit

The paper form shows **one Stock/Property No. per row**. The codebase today treats **one PropertyNo per physical unit** (`AssetIARAcceptedEventConsumer` loops `for unit in 0..Quantity`).

Reconciliation: rows on the printed IAR are typically `Quantity = 1` per row when assets are individually serialized — the form supports either. Two clean options:

- **Option G1 (recommended):** keep `Quantity` on the line, but require operators to add **one IAR line per physical unit** when items will be individually tracked (Quantity always = 1 for trackable PPE/SE). Bulk consumables can remain Quantity > 1 with a single Stock/Property No.
- **Option G2:** allow Quantity > 1 on a tracked line and render N `PropertyNoField` instances on the accept screen, persisting N child rows (current behaviour, but with manual numbers).

> **Open question G:** G1 or G2? G1 matches the form literally and simplifies the UI; G2 matches the current consumer and keeps fewer rows in storage. Recommendation: G1 for PPE/SE, G2 only for fungible inventory (none in this scope yet).

#### 2.3 Changes (Phase B)

1. **Domain — `AssetProcurement`:** add the fields from §2.1 to `AssetInspectionAcceptanceReport` and `AssetIARLineItem`. New value objects only where they earn their keep (no premature wrappers).
2. **Contracts — `AssetProcurement.Contracts`:** extend `CreateAssetIARCommand`, `AcceptAssetIARCommand`, `AssetIARDto`, `AssetIARLineItemDto`, and `AssetIARAcceptedEvent` with the new fields (including `StockPropertyNo` per line).
3. **Handlers:** update `Create`/`Accept` handlers + validators; validator enforces `StockPropertyNo` present on every line at acceptance time.
4. **Blazor — new page** `Pages/AssetProcurement/AssetIARsPage.razor`:
   - List + search of IARs (states: Draft, Submitted, Accepted, Rejected).
   - "Create from PO" wizard pre-filling line items from `AssetPurchaseOrder`, then captures the SOP GS-PD26 header fields (requisitioning office, responsibility center, invoice no/date) plus the inspection/acceptance signoff blocks.
   - Per-line: `<PropertyNoField>` for Stock/Property No (resolves to G1 above). Show line items grouped by description if G2 is chosen.
   - Use the existing `PropertyNoField` from `Components/Shared/` — no duplication of the Razor component itself; just instantiated in a new page. (This is what "copy/duplicate the implementation we have on Tangible Items" means in practice — same component, new host page.)
5. **AssetRegister consumer:** update `AssetIARAcceptedEventConsumer` to use the supplied PropertyNos rather than minting placeholders; keep auto-mint as a per-tenant fallback (Open question B).
6. **Master Data prerequisites:** the generator depends on `IPropertyClassClient` and `IOrganizationProfileClient`. Both already serve `TangibleItemsPage` — no new master data work.
7. **Migration:** EF migration in `Migrations.PostgreSQL` for the new columns on `asset_iars` and `asset_iar_line_items`.

> **Open question B:** Does every tenant want manual PropertyNo assignment at IAR time, or do some want the current auto-mint-on-accept behaviour? Recommendation: make manual the default (matches the diagram), keep auto-mint as a fallback configurable per tenant.

### Gap 3 — Soft-state inter-office transfer

**Today:** `LifecycleState` has no value for "transferred out of this office." When PPEIR/SMIR is posted today, assets stay `Assigned` (or `Available`) in the registry, which means they keep appearing in local reports indefinitely. The diagram explicitly calls this out as "exclude from Reports from now on."

**Target:** new lifecycle state `TransferredOut`, applied to each asset listed on a posted PPEIR/SMIR. Local reports filter it out by default.

**Changes:**

1. **Domain:** add `TransferredOut = 5` to `LifecycleState` enum (`Modules.AssetRegister.Contracts/v1/Enums.cs`).
2. **Domain:** add `AssetRegistry.MarkTransferredOut(Guid issuanceReportId)` — allowed only from `Available` or `Assigned`. Records `AssetTransferredOutEvent`. EnsureNotDisposed guard.
3. **Issuance feature:** in `PostIssuanceReportCommandHandler`, when `IssuanceReportType` is PPEIR or SMIR (inter-office transfer), call `MarkTransferredOut` on each asset on the report.
4. **Reports / searches:** update default predicates in `SearchAssets`, `GetAssetRegistry`, and the report query handlers to exclude `TransferredOut` unless an explicit `includeTransferred=true` parameter is passed. Audit/history views may keep them visible.
5. **EF Core migration:** no schema change needed if `LifecycleState` is persisted as `int` (verify in `AssetRegistryConfiguration`). If persisted as string, add migration.
6. **Architecture test:** add a test asserting that an asset which transitions through PPEIR posting is filtered out of the default registry search.

> **Open question C:** Do PAR/ICS within-office accountabilities also need a different status on the asset (e.g., `Assigned` already covers it — confirm). Today an asset under PAR shows `LifecycleState=Assigned, CurrentAccountabilityId=X` — that looks correct.

---

## 4. Phased implementation

Each phase is independently shippable. Build runs zero-warnings (per CLAUDE.md) and architecture tests pass at every phase boundary.

### Phase A — Soft-state transfer (smallest, highest-value, no UI work)
1. Add `LifecycleState.TransferredOut` and `AssetTransferredOutEvent`.
2. Add `AssetRegistry.MarkTransferredOut(...)`.
3. Wire `PostIssuanceReportCommandHandler` to call it for PPEIR/SMIR.
4. Update default filters in registry search/report handlers.
5. Architecture + unit tests.

**Deliverable:** PPEIR/SMIR postings remove assets from local registry views without losing history.

### Phase B — IAR page + manual PropertyNo assignment (per SOP GS-PD26)
1. Extend `AssetInspectionAcceptanceReport` + `AssetIARLineItem` domain with the SOP GS-PD26 fields (§2.1).
2. Extend contracts (`CreateAssetIARCommand`, `AcceptAssetIARCommand`, `AssetIARDto`, `AssetIARLineItemDto`, `AssetIARAcceptedEvent`) with the same fields plus per-line `StockPropertyNo`.
3. Validators: require `StockPropertyNo` on every line at acceptance time; require `AcceptanceKind` (Complete / Partial) + per-line `AcceptedQuantity` when Partial.
4. EF migration for `asset_iars` and `asset_iar_line_items` columns.
5. Build `Pages/AssetProcurement/AssetIARsPage.razor` (list, create-from-PO wizard, SOP-shaped accept screen with `<PropertyNoField>` per line).
6. Update `AssetIARAcceptedEventConsumer` to use supplied PropertyNos; fall back to auto-mint only when absent (per-tenant fallback — see Open question B).
7. Handler + validator unit tests; Architecture test that AssetProcurement still does not reference AssetRegister internals.

**Deliverable:** Operator-assigned PropertyNos flow IAR → AssetRegister with full provenance, with the IAR form-of-record matching SOP GS-PD26.

### Phase C — PPERR / SMRR explicit creation in the registry UI
The diagram treats PPERR/SMRR as an explicit asset-register step after IAR acceptance. Today it's implicit (consumer mints assets directly without a `ReceivingReport` aggregate from the IAR path). Decide:
- **Option C1 (recommended):** make the consumer also create a `ReceivingReport` aggregate (PPERR or SMRR per asset type) at acceptance time so the paperwork exists automatically. No new UI.
- **Option C2:** add a Blazor wizard "Create PPERR/SMRR from accepted IAR" — fully manual.

**Recommendation:** C1. Saves the user a click and the document is required regardless.

**Deliverable:** every accepted IAR produces both the registry rows AND the PPERR/SMRR document.

### Phase D — Finance payment-first gate
1. Add `RequiresAdvancePayment` (bool) to `AssetPurchaseOrder` (default false). Migration.
2. Surface the flag on the asset PO Blazor screen (read from supplier or operator toggle).
3. On IAR create page, query Finance for a Paid DV referencing the PO; block submit + show banner if `RequiresAdvancePayment && !paidDvExists`.
4. Cross-module read goes through `Modules.Finance.Contracts` (search DV by referenced PO id) — no internals exposed.

**Deliverable:** the diagram's payment-first branch is enforced in the UI.

### Phase E — Polish & documentation
1. Update `.claude/rules/*` if any module added a new pattern worth documenting.
2. Refresh `ASSET-REGISTER-MODULE-PLAN.md` to reference the new lifecycle state.
3. Add a short operator-facing walkthrough in the Playground (markdown or Razor doc page).

---

## 5. Cross-cutting decisions to confirm

| # | Decision | Default proposal |
|---|----------|------------------|
| A | Canonical PropertyNo format for the registry — COA 2020-006 (`YYYY-AA-BB-NNNN-CC`) vs. NFA local (`YYYY-NFA-OFFICE-CLASS-CATEGORY-SEQ`)? | Keep both. Store the NFA-local string as `PropertyNo` (the human-readable identifier shown on stickers) and derive/store the COA components separately for accounting reports. Requires extending the `PropertyNumber` value object. |
| B | Manual PropertyNo at IAR vs. auto-mint on accept? | Manual default; auto-mint as per-tenant fallback. |
| C | Auto-create PPERR/SMRR document at IAR acceptance vs. separate UI step? | Auto-create (C1 above). |
| D | Where does `RequiresAdvancePayment` come from — supplier master data or per-PO operator decision? | Per-PO, defaulted from a `Supplier.PaymentTerms` field if present. |
| E | Should `TransferredOut` assets still be findable by PropertyNo for audit (e.g., recipient office searching by sticker)? | Yes — direct PropertyNo lookup ignores the lifecycle filter; only list/search/report endpoints filter. |
| F | Is the form's "Invoice No." the same as `DeliveryReceiptNo`? | Add `InvoiceNo`/`InvoiceDate` as distinct fields; keep `DeliveryReceiptNo`. |
| G | PropertyNo cardinality at IAR — per row (Quantity always 1 for tracked items, G1) or per unit (split N rows in UI from Quantity>1, G2)? | G1 for PPE/SE; G2 only if fungible assets later land in scope. |

---

## 6. Risks & non-goals

**Risks**
- Format dissonance (decision A) is the largest risk — both forms are persisted today. Mishandling it will produce duplicate PropertyNos or mismatched accounting reports. **Mitigation:** lock decision A before Phase B starts.
- Adding a `LifecycleState` value is a contract change; any external consumer of `AssetSnapshot` or registry events that switches on lifecycle must be updated. Internal-only today, so low risk.

**Non-goals**
- No changes to RFQ / Canvass / PO core workflow — those already match the diagram.
- No MAUI client changes; the diagram is back-office.
- No depreciation, impairment, or COA report changes — out of scope.
- No new Finance flows beyond the gating query (BUR/DV already exist).

---

## 7. Verification gates

Before each phase merges:

```powershell
dotnet build src/AMIS.Framework.slnx   # 0 warnings (CI gate)
dotnet test  src/AMIS.Framework.slnx   # all green, including Architecture.Tests
```

Manual smoke per phase:
- **A:** post a PPEIR; confirm the asset disappears from `SearchAssets` default view but is still resolvable by PropertyNo lookup.
- **B:** create IAR → accept → check that user-typed PropertyNos appear on `AssetRegistry` rows and PPERR header.
- **C:** accept IAR → verify PPERR/SMRR aggregate exists with correct number + line items.
- **D:** flag a PO as advance-payment; try to submit IAR without a Paid DV → blocked. Create DV, mark Paid → unblocked.

---

## 8. Open questions to resolve before kickoff

1. Confirm decisions A–E in §5.
2. Confirm the new IAR Blazor page belongs under `Pages/AssetProcurement/` (matches the module that owns IAR). Today there is no `AssetProcurement` UI folder — Phase B creates it.
3. Confirm that "Transfer to other Office" in the diagram is exactly today's `PPEIR / SMIR` issuance — i.e., issuance-report transfer, not the (currently unimplemented) `ITR` inventory transfer. If it should be ITR, Phase A wires ITR posting instead.
