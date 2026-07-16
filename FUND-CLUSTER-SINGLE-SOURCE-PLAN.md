# Fund Cluster — Single Source of Truth Plan

> Status: **Phase 0 partially applied** (shared component + 4 dialog swaps on disk). Phases 1–6 pending.
> Approach approved by product owner: **two physical inputs, everything else auto-copied at creation (frozen snapshots). No dropped columns, no EF migrations.**

## Context — the problem

"Fund Cluster" (the COA 2-digit fund code `01`..`07`) is captured **independently at 7+ points** with no single source of truth. The same value is re-typed on the Purchase Order, Job Order, PPERR/SMRR receiving report, SMIR/PPEIR issuance, BUR, and physical-count start — in two clashing conventions (master-data select of numeric codes vs. free text like `CO`/`GAA`/`SEF`). Nothing enforces that these strings agree. Only two hand-offs actually propagate the value today (BUR→DV, and asset→ICS/PAR).

Goal: the user enters fund cluster **exactly twice** — once at **acquisition** (PO/JO) and once at **receiving/transfer** (PPERR/SMRR) — and every downstream document inherits it automatically.

## The two constraints that shape the design

1. **Cross-module SQL joins are illegal.** `BudgetUtilizationRequest` (BudgetDisbursement module) references the PO (ProcurementAcquisition module) by a raw `Guid` — separate DbContext, separate schema, forbidden by the architecture tests. So a BUR query **cannot** join to `PurchaseOrders`. The BUR must **physically store** FundCluster, copied from the PO at creation (exactly how it already snapshots `PurchaseOrderNumber`).

2. **The asset has no join-back to its fund-cluster source.** `AssetRegistry` ([Domain/Assets/AssetRegistry.cs:59-61](src/Modules/AssetRegister/Modules.AssetRegister/Domain/Assets/AssetRegistry.cs#L59-L61)) links only to `SourceIARId` / `SourcePurchaseOrderId` (both nullable) — **no FK to the receiving report**, and transfer/donation assets have **no PO at all**. Fund cluster is typed once on the SMRR/PPERR and stamped onto the asset at registration. The asset is therefore the **durable anchor** for the entire asset side (same snapshot pattern it already uses for `UnitCost`, `UacsObjectCode`, `AcquisitionDate`).

Because BUR and the asset are forced to be physical anchors regardless, and because signed COA documents (ICS/PAR/DV) should stay **frozen** at their issued fund cluster (a later correction must not retroactively rewrite a signed form), the same-module documents also use **auto-copy at creation** rather than a live join. This delivers the identical user outcome with zero migrations and zero query/PDF-handler rewrites.

## Final model

| Record | Fund cluster source | Kind |
|---|---|---|
| **PurchaseOrder / JobOrder** | **User input #1** (acquisition) | physical, `FundClusterSelect` |
| **ReceivingReport (PPERR/SMRR)** | **User input #2** (receiving/transfer); pre-filled from source PO on purchases | physical, `FundClusterSelect` |
| **AssetRegistry** | stamped from the receiving report at registration | physical anchor (no join-back) |
| **BudgetUtilizationRequest** | auto-copied from PO at creation | physical (cross-module — join forbidden) |
| **DisbursementVoucher** | auto-copied from BUR at creation | physical, same-module (kept frozen) |
| **PropertyAccountability (ICS/PAR)** | derived from the assets on its lines at creation | physical, same-module (kept frozen) |
| **PropertyIssuanceReport (SMIR/PPEIR)** | derived from the assets on its lines at creation | physical, same-module (kept frozen) |
| PhysicalCountSession | independent user select (scope of the count, not a hand-off) | unchanged |

**No FundCluster columns are dropped. No EF migrations.** Every value is stored where it lands; downstream records copy/derive it once at creation and never re-ask the user.

---

## Phase 0 — Shared `FundClusterSelect` component  ✅ APPLIED

New: [src/Host/AMIS.Blazor/Components/Shared/FundClusterSelect.razor](src/Host/AMIS.Blazor/Components/Shared/FundClusterSelect.razor)
- `@bind-Value` over the cluster code; params `Label`, `Required`, `Clearable`, `Disabled`, `ReadOnly`, `HelperText`.
- Loads active clusters via `IFundClusterClient.SearchAsync(pageSize:200)`, ordered by `Code`.
- `ReadOnly` renders a `"Code — Name"` read-only field (for inherited/derived display).
- Public `ContainsCode(string?)` — callers adopt an upstream code only if it maps to a loaded cluster (used by BUR via `@ref`).

Refactored to consume it (deleted each file's copy-pasted `LoadFundClustersAsync`/`_fundClusters`):
- ✅ [BudgetUtilizationRequestFormDialog.razor](src/Host/AMIS.Blazor/Components/Pages/BudgetDisbursement/BudgetUtilizationRequestFormDialog.razor) — select + `@ref` pre-fill guard. *(Note: this input is removed entirely in Phase 3.)*
- ✅ [StartPhysicalCountDialog.razor](src/Host/AMIS.Blazor/Components/Pages/AssetRegister/StartPhysicalCountDialog.razor)
- ✅ [FundingSourceCodeFormDialog.razor](src/Host/AMIS.Blazor/Components/Pages/MasterData/FundingSourceCodeFormDialog.razor) + dropped the `Clusters` param from [FundingSourceCodesPage.razor](src/Host/AMIS.Blazor/Components/Pages/MasterData/FundingSourceCodesPage.razor).
- ⬜ **TODO** [IssueAccountabilityDialog.razor](src/Host/AMIS.Blazor/Components/Pages/AssetRegister/IssueAccountabilityDialog.razor) — not yet swapped (still has its own `_fundClusters`). In Phase 4 this dialog's input becomes a read-only derived display, so fold the swap into that phase.

## Phase 1 — Acquisition input #1 (PO / JO)

Swap free-text → `<FundClusterSelect>` (optional; helper "COA fund cluster (01–07)"):
- [PurchaseOrderFormDialog.razor:127](src/Host/AMIS.Blazor/Components/Pages/Procurement/PurchaseOrderFormDialog.razor#L127) — binds `_form.FundCluster` (`string?`, create + edit paths already round-trip).
- [JobOrderFormDialog.razor:115](src/Host/AMIS.Blazor/Components/Pages/Procurement/JobOrderFormDialog.razor#L115).
- [GeneratePurchaseOrdersDialog.razor](src/Host/AMIS.Blazor/Components/Pages/Procurement/GeneratePurchaseOrdersDialog.razor) — change `_fundCluster` to `string?`.
- [CertifyPurchaseOrderFundsAvailableDialog.razor](src/Host/AMIS.Blazor/Components/Pages/Procurement/CertifyPurchaseOrderFundsAvailableDialog.razor) + [CertifyJobOrderFundsAvailableDialog.razor](src/Host/AMIS.Blazor/Components/Pages/Procurement/CertifyJobOrderFundsAvailableDialog.razor) — pre-filled from the PO/JO; keep as `FundClusterSelect`.

Domain unchanged (`PurchaseOrder.FundCluster`, `JobOrder.FundCluster` already `string?`).

## Phase 2 — Receiving input #2 (PPERR / SMRR) + pre-fill from source PO

**Data path** — surface the source PO's cluster on the accepted-IAR line query (the handler already dictionaries POs for supplier name/address; add one field):
1. `AcceptedIARLineItemDto` (ProcurementAcquisition.Contracts, `v1/InspectionAcceptanceReports/InspectionAcceptanceReportContracts.cs`) — append `string? FundCluster = null`.
2. `SearchAcceptedIARLineItemsQueryHandler.cs` — add `po?.FundCluster` to the projection.
3. Blazor mirror record in [AssetRegisterClient.cs](src/Host/AMIS.Blazor/ApiClient/AssetRegisterClient.cs) — append `string? FundCluster = null`.
4. [ReceivingReportForm.razor](src/Host/AMIS.Blazor/Components/Pages/AssetRegister/ReceivingReportForm.razor) — replace the free-text field (~line 80) with `<FundClusterSelect>`; add `RecomputeFundCluster()` (model on `IssueAccountabilityDialog.RecomputeFundCluster`): when receipt type = Purchase, auto-fill read-only from the distinct source-PO cluster of the selected items; editable for Donation/Transfer/Other. Save path already flows `_fundCluster` into `CreateReceivingReportRequest`.

Unchanged: `CreateReceivingReportCommandHandler.cs` already stamps `AssetRegistry.Register(... cmd.FundCluster ...)`. This is the asset anchor.

## Phase 3 — BUR auto-copies from PO (remove UI input)

- **Handler** `CreateBudgetUtilizationRequestCommandHandler.cs` — it already loads the PO to validate existence; read the PO's `FundCluster` from that same cross-module read and pass it into `BudgetUtilizationRequest.Create(...)`. (Confirm the PO read model/contract carries `FundCluster`; if not, add it to the query the handler uses.)
- **Command** `CreateBudgetUtilizationRequestCommand` — remove the `FundCluster` parameter.
- **Validator** — drop the FundCluster rule.
- **UI** [BudgetUtilizationRequestFormDialog.razor](src/Host/AMIS.Blazor/Components/Pages/BudgetDisbursement/BudgetUtilizationRequestFormDialog.razor) — remove the `FundClusterSelect` added in Phase 0, the `_form.FundCluster`, the `_clusterSelect` ref, and the pre-fill block. (Revisits the Phase 0 edit.)

## Phase 4 — DV auto-copies from BUR (remove UI input) + ICS/PAR derives from assets

**DV:**
- `CreateDisbursementVoucherCommandHandler.cs` already reads the BUR — always set `dv.FundCluster` from `bur.FundCluster`; stop trusting the client value.
- `CreateDisbursementVoucherCommand` / `UpdateDisbursementVoucherCommand` — remove `FundCluster` param.
- [DisbursementVoucherFormDialog.razor](src/Host/AMIS.Blazor/Components/Pages/BudgetDisbursement/DisbursementVoucherFormDialog.razor) — remove the inherited/fallback fund-cluster block and `_form.FundCluster` (keep the rest of the inherit-from-BUR wiring). Optionally keep a read-only `<FundClusterSelect ReadOnly>` for display only.

**ICS/PAR:**
- `IssueAccountabilityCommandHandler.cs` — it already loads the `AssetRegistry` entities for the lines. Derive fund cluster = the **distinct** `asset.FundCluster` across the lines; **throw** a validation error if the assets span multiple clusters (COA: one accountability = one fund cluster). Pass the derived value into `PropertyAccountability.Issue(...)`.
- `IssueAccountabilityRequest` / command — remove the `FundCluster` field.
- [IssueAccountabilityDialog.razor](src/Host/AMIS.Blazor/Components/Pages/AssetRegister/IssueAccountabilityDialog.razor) — keep `RecomputeFundCluster()` as a **read-only derived display** + client-side mixed-cluster block (good UX), but it is no longer an input; also complete the Phase 0 `FundClusterSelect` swap here (read-only variant).

## Phase 5 — SMIR/PPEIR derives from assets (remove free-text input)

- Add `FundCluster` to the asset summary used by the pickers: `AssetRegistrySummaryDto` (AssetRegister.Contracts `v1/Assets/AssetContracts.cs`) — append `string? FundCluster = null`; project `a.FundCluster` in `SearchAssetsQueryHandler.cs`; mirror in [AssetRegisterClient.cs](src/Host/AMIS.Blazor/ApiClient/AssetRegisterClient.cs). *(Needed so the dialogs can show the derived value; the server still derives authoritatively from the loaded assets.)*
- `CreateIssuanceReportCommandHandler.cs` — derive fund cluster from the loaded assets (distinct; throw on mixed); stop trusting client value.
- `CreateIssuanceReportCommand` — remove `FundCluster` param.
- [CreateSmirDialog.razor](src/Host/AMIS.Blazor/Components/Pages/AssetRegister/CreateSmirDialog.razor) + [CreatePpeirDialog.razor](src/Host/AMIS.Blazor/Components/Pages/AssetRegister/CreatePpeirDialog.razor) — replace the free-text field with a read-only derived display (mirror `RecomputeFundCluster`) + mixed-cluster block.

## Phase 6 — Validators (the 2 real inputs only)

On PO/JO create/update/certify-funds and receiving-report create validators, keep MaxLength and add, for non-empty values:
`.Matches(@"^\d{2}$").WithMessage("Fund cluster must be a 2-digit COA code (e.g. 01).").When(x => !string.IsNullOrEmpty(x.FundCluster))`.
The inherited-doc validators lose their FundCluster rule (param removed).

## What NOT to change

- `AssetRegistry.Register` fund-cluster stamping (the asset anchor).
- Register reports (RegSPI/RegPPEI) that group by `AssetRegistry.FundCluster` — they read the anchor and are correct.
- `PhysicalCountSession` fund cluster (independent count scope, not a hand-off).
- All EF configurations, all FundCluster columns — **nothing is dropped, no migrations**.
- QuestPDF/FastReport handlers — every one reads `.FundCluster` off its own record and keeps working.

## Verification

- `dotnet build src/AMIS.Framework.slnx` — zero warnings.
- `dotnet test src/AMIS.Framework.slnx` — architecture + unit tests.
- Encoding: grep changed `.razor` for `�`, `?@`, `? @` (em-dash `—` present in the component) before committing.
- Manual flows:
  1. PO with cluster `01` → create BUR from it → BUR shows no cluster field, saved BUR carries `01` → obligate → create DV → DV carries `01`; PDFs print `01`.
  2. Accept an asset IAR → new PPERR → pick supplier + IAR items → cluster auto-fills read-only from the source PO; switch receipt type to Transfer → editable; save → asset detail shows the cluster.
  3. Issue ICS from assets sharing `01` → no input, document saves with `01`; mix two clusters → blocked with a clear message.
  4. SMIR/PPEIR from assets → same derive/blocking behavior.
