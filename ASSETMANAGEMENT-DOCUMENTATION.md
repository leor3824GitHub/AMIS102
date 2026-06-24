# AssetManagement Module Documentation

> Comprehensive guide to asset acquisition flows, domain overhauls, and report alignment for the AssetManagement module.

---

## Part 1: Asset Acquisition Flow — Implementation Plan

### Goal

Implement the broad asset-acquisition flow aligning end-to-end pipeline (Procurement → Finance → Asset Register) with the agreed business flow. This closes three specific gaps: the Finance payment-first loop-back, PropertyNo generator at IAR time, and soft-state inter-office transfer.

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

### Current State Audit

| Step | Status | Where |
|------|--------|-------|
| PR / RFQ / AoC / PO (general) | ✅ Exists | `Modules/ProcurementAcquisition/.../Features/v1/{PurchaseRequests,Canvass,PurchaseOrders}` |
| Asset-specific PR / PO / IAR | ✅ Exists | `Modules/AssetProcurement/.../Features/v1/{AssetPurchaseRequests,AssetPurchaseOrders,AssetIARs}` |
| BUR / Disbursement Voucher | ✅ Exists | `Modules/Finance/.../Features/v1/{BudgetUtilizationRecords,DisbursementVouchers}` |
| PPERR / SMRR (manual creation) | ✅ Exists | `Modules/AssetRegister/.../Features/v1/Receiving/CreateReceivingReport` |
| PPERR / SMRR (auto from IAR accept) | ⚠️ Partial — materializes `AssetRegistry` but **does not** create a `ReceivingReport` aggregate | `Modules/AssetRegister/.../Integration/AssetIARAcceptedEventConsumer.cs` |
| PropertyNo value object (COA 2020-006) | ✅ Exists | `Modules.AssetRegister.Contracts/v1/ValueObjects/PropertyNumber.cs` (format `YYYY-AA-BB-NNNN-CC`) |
| PropertyNo UI generator (rich, NFA-style) | ✅ Exists | `AMIS.Blazor/Components/Shared/PropertyNoField.razor` (format `YYYY-NFA-OFFICE-CLASS-CATEGORY-SEQ`) |
| TangibleItems page using `PropertyNoField` | ✅ Exists | `Pages/AssetManagement/TangibleItemsPage.razor` |
| AssetIAR Blazor page | ❌ Missing | — |
| PAR / ICS (accountability within office) | ✅ Exists | `Modules/AssetRegister/.../Features/v1/Accountability` |
| PPEIR / SMIR (issuance reports) | ✅ Exists | `Modules/AssetRegister/.../Features/v1/Issuance` (numbers minted; lifecycle effect TBD) |
| Inter-office transfer lifecycle state | ❌ Missing — `LifecycleState` has `Available / Assigned / UnderInvestigation / Unserviceable / Disposed` only | `Modules.AssetRegister.Contracts/v1/Enums.cs:23` |
| Finance ↔ Procurement payment-first gate | ❌ No coupling — Finance creates BUR/DV independently; PO has no "payment-first" flag visible to IAR step | — |

#### Format Dissonance

Two PropertyNo formats coexist in the codebase:

| Format | Where | Used by |
|--------|-------|---------|
| `YYYY-AA-BB-NNNN-CC` (COA 2020-006) | `Modules.AssetRegister.Contracts/v1/ValueObjects/PropertyNumber.cs` | `AssetRegistry` (the official registry value object) |
| `YYYY-NFA-OFFICE-CLASS-CATEGORY-SEQ` (NFA local) | `PropertyNoField.razor` preview + `TangibleItem` plain-string storage | `TangibleItems` page only |

> **Open question A:** Which format is canonical PropertyNo for the Asset Registry?

---

### The Three Gaps

#### Gap 1 — Finance Payment-First Loop-Back

**Today:** Finance creates BUR + DV independently. No signal back to Procurement says "ok, now you may receive goods."

**Target:** Make the gate explicit so the UI guides the user.

**Minimum-viable change:**
- Add `PaymentTerms` (or `RequiresAdvancePayment` bool) to `AssetPurchaseOrder` — captured at PO creation from supplier policy.
- On the AssetIAR creation page, if `PO.RequiresAdvancePayment == true`, check that a `DisbursementVoucher` referencing this PO exists in **Paid** status before allowing IAR submission. Show a banner with link to create BUR/DV if missing.
- No new domain event needed; this is a UI guard backed by a query.

#### Gap 2 — PropertyNo Generator at IAR Step

**User's intent:** The same generator UX on `TangibleItemsPage` should appear when creating/accepting an IAR.

**Today:** `PropertyNoField.razor` is in `Components/Shared/` but there is no AssetIAR Blazor page. Backend-side, `AssetIARAcceptedEventConsumer` auto-mints PropertyNos with placeholder values.

**Target:** The IAR acceptance step becomes where the user assigns one PropertyNo per accepted unit using the existing generator component.

##### IAR Form Mapping — SOP GS-PD26

| Form field | Current domain | Status |
|---|---|---|
| IAR No. | `IarNumber` | ✅ |
| Date (IAR) | `IarDate` | ✅ |
| Supplier | `SupplierId`, `SupplierName` | ✅ |
| PO No. / Date | `PurchaseOrderId` | ✅ |
| **Requisitioning Office / Dept.** | — | ❌ Missing |
| **Responsibility Center Code** | — | ❌ Missing |
| **Invoice No.** | reuses `DeliveryReceiptNo`? | ⚠️ Ambiguous |
| Stock / Property No. *(per row)* | — | ❌ Missing on `AssetIARLineItem` |
| Description | `Description` | ✅ |
| Unit | `Unit` | ✅ |
| Quantity | `Quantity` | ✅ |
| **Inspection — Date Inspected** | — | ❌ Missing |
| Inspection Officer / Committee | `InspectedById` | ✅ |
| **Acceptance — Date Received** | `DeliveryDate`? | ⚠️ Likely separate |

**Domain additions needed in `AssetProcurement`:**

- On `AssetInspectionAcceptanceReport`:
  - `RequisitioningOffice` (string), `ResponsibilityCenterCode` (string).
  - `InvoiceNo` (string?), `InvoiceDate` (DateOnly?) — distinct from delivery receipt.
  - `DateInspected` (DateOnly?), `InspectionVerified` (bool).
  - `DateReceived` (DateOnly?), `AcceptanceKind` (enum: `Complete | Partial`), and per-line `AcceptedQuantity` when partial.
- On `AssetIARLineItem`:
  - `StockPropertyNo` (string) — the value typed/generated via `PropertyNoField`.

#### Gap 3 — Soft-State Inter-Office Transfer

**Today:** `LifecycleState` has no value for "transferred out of this office." When PPEIR/SMIR is posted, assets stay in registry, appearing in local reports indefinitely.

**Target:** New lifecycle state `TransferredOut`, applied to each asset on a posted PPEIR/SMIR.

**Changes:**

1. Add `TransferredOut = 5` to `LifecycleState` enum.
2. Add `AssetRegistry.MarkTransferredOut(Guid issuanceReportId)` — allowed from `Available` or `Assigned`.
3. In `PostIssuanceReportCommandHandler`, when `IssuanceReportType` is PPEIR or SMIR, call `MarkTransferredOut` on each asset.
4. Update default predicates in `SearchAssets`, `GetAssetRegistry` to exclude `TransferredOut` unless `includeTransferred=true`.

---

### Phased Implementation

Each phase is independently shippable. Build runs zero-warnings and architecture tests pass.

#### Phase A — Soft-State Transfer (smallest, highest-value)
1. Add `LifecycleState.TransferredOut` and `AssetTransferredOutEvent`.
2. Add `AssetRegistry.MarkTransferredOut(...)`.
3. Wire `PostIssuanceReportCommandHandler` to call it for PPEIR/SMIR.
4. Update default filters in registry search/report handlers.

**Deliverable:** PPEIR/SMIR postings remove assets from local registry views without losing history.

#### Phase B — IAR Page + Manual PropertyNo Assignment
1. Extend `AssetInspectionAcceptanceReport` + `AssetIARLineItem` domain with SOP GS-PD26 fields.
2. Extend contracts with same fields plus per-line `StockPropertyNo`.
3. Build `Pages/AssetProcurement/AssetIARsPage.razor`.
4. Update `AssetIARAcceptedEventConsumer` to use supplied PropertyNos.

**Deliverable:** Operator-assigned PropertyNos flow IAR → AssetRegister with full provenance.

#### Phase C — PPERR / SMRR Explicit Creation
- **Option C1 (recommended):** Consumer creates `ReceivingReport` at acceptance time automatically.
- **Option C2:** Add "Create PPERR/SMRR from accepted IAR" wizard.

**Recommendation:** C1.

#### Phase D — Finance Payment-First Gate
1. Add `RequiresAdvancePayment` (bool) to `AssetPurchaseOrder`.
2. On IAR create page, query Finance for Paid DV; block if missing.

#### Phase E — Polish & Documentation

---

### Cross-Cutting Decisions

| # | Decision | Proposal |
|---|----------|----------|
| A | Canonical PropertyNo format — COA 2020-006 vs. NFA local? | Keep both; store NFA as `PropertyNo`, derive COA components separately. |
| B | Manual PropertyNo at IAR vs. auto-mint on accept? | Manual default; auto-mint as per-tenant fallback. |
| C | Auto-create PPERR/SMRR vs. separate UI step? | Auto-create (C1). |
| D | Where does `RequiresAdvancePayment` come from? | Per-PO, defaulted from `Supplier.PaymentTerms` if present. |
| E | Should `TransferredOut` assets be findable? | Yes — direct PropertyNo lookup ignores filter; only list/report filter. |
| F | Is form's "Invoice No." the same as `DeliveryReceiptNo`? | Add `InvoiceNo`/`InvoiceDate` as distinct; keep `DeliveryReceiptNo`. |

---

### Verification Gates

Before each phase merges:

```powershell
dotnet build src/AMIS.Framework.slnx   # 0 warnings (CI gate)
dotnet test  src/AMIS.Framework.slnx   # all green, including Architecture.Tests
```

Manual smoke per phase:
- **A:** post PPEIR → asset disappears from default `SearchAssets` but is resolvable by PropertyNo lookup.
- **B:** create IAR → accept → verify user-typed PropertyNos appear on `AssetRegistry` rows and PPERR header.
- **C:** accept IAR → verify PPERR/SMRR aggregate exists with correct number + line items.
- **D:** flag PO as advance-payment → try to submit IAR without Paid DV → blocked → create DV, mark Paid → unblocked.

---

## Part 2: AssetManagement Additive Overhaul Plan

### Objective

Implement an additive-only overhaul for AssetManagement that:

1. Keeps agency/legal document entities separate (ICS, PAR, SMIR, PPEIR, RRSP, RRP).
2. Adds a unified current-state layer for all tangible assets.
3. Avoids delete/remove of existing entities, endpoints, and workflows.

### Design Position

Given agency forms are distinct, the system should be:

1. **Unified by asset identity and current state** — `AssetRegistry` is the source of truth.
2. **Separated by legal document and compliance output** — existing document aggregates remain immutable history and printable records.

This ensures:

1. `AssetRegistry` is the source of truth for current state.
2. Existing document aggregates remain immutable history and printable records.
3. `AssetAssignmentHistory` links document events to accountability transitions.

### Implemented (Current)

#### New Domain Types
- `AssetRegistry`
- `AssetAssignmentHistory`
- `Location`
- `AssetLifecycleState`
- `AssetAssignmentEventType`
- `LocationType`

#### Persistence Wiring
- New entity configurations for registry/history/location.
- New DbSets added to `AssetManagementDbContext`.

#### Write-Through Integration

The following handlers now write to the registry/history in addition to existing document tables:

1. `CreateTangibleInventory`
2. `CreateICS`
3. `CreatePAR`
4. `CreateSMIR`
5. `CreatePPEIR`
6. `CreateRRSP`
7. `CreateRRP`
8. `CreatePropertyIncidentReport`
9. `CreateUnserviceablePropertyReport`
10. `ReclassifyProperties`
11. `RenewICS` (status-history sync)
12. `ICSExpiryJob` (status-history sync)

#### Read Integration

1. `GetPropertyHistory` now reads current custodian from `AssetRegistry` first.
2. Existing transaction-based fallback remains for compatibility.

### Completed Phases

#### Phase 1: Schema and Migration ✅
- EF migration for `AssetRegistry`, `AssetAssignmentHistory`, and `Locations`.
- Migration: `20260509113347_AddAssetRegistryAndLocation`.

#### Phase 2: Complete Document Coverage ✅
- Registry/history updates in all current documented flows.

#### Phase 3: Registry Feature Slices ✅
- Get assets by current custodian (`/asset-registry/by-custodian/{custodianId}`).
- Get assets by location (`/asset-registry/by-location/{locationId}`).
- Get assignment history by asset (`/asset-registry/{assetRegistryId}/assignment-history`).
- Location CRUD (`/locations`) with permission wiring.

#### Phase 4: Reporting Alignment 🔄 (see Part 3)
- Legal reports sourced from existing document entities.
- Current-state dashboards and accountability views from `AssetRegistry`.
- Values match official forms for ICS/PAR/SMIR/PPEIR.

### Classification Rules

1. `TangibleInventoryItem.AssetType` is snapshotted at receipt from capitalization threshold.
2. ICS and SMIR are SE-only document flows.
3. PAR and PPEIR are PPE-only document flows.
4. Reclassification updates state/history, not identity.

### Guardrails

1. No deletion/removal of existing entities/endpoints.
2. Preserve existing API contracts and route groups.
3. Preserve separate document numbering and permissions.
4. Keep additive path safe for phased rollout.

### Build Status

- AssetManagement module build succeeds with warnings.
- Full solution build succeeds after fixing Vehicle compile error.
- AssetManagement tests pass: **179/179 passing** ✅
- RSPI/RegSPI report DTO enrichment: validated ✅
- PTR + report totals metadata: validated ✅
- RSPI/RegSPI signatory projection: validated ✅
- RSPI/RegSPI section-group + deterministic ordering: validated ✅

---

## Part 3: AssetManagement Report Alignment Checklist

### Scope

Cross-check current API report outputs against official form expectations for:

1. **ICS** (Inventory Custodian Slip) derived reports
2. **PAR** (Property Acknowledgement Receipt) related reports
3. **SMIR** (Semi-Expendable Issuance Record) related reports
4. **PPEIR/PTR** (PPE Issuance and Transfer) reports

### Current API Surfaces

1. `GetRegSPIQuery` + handler
2. `GetRSPIQuery` + handler
3. `GetSPCQuery` contract
4. `GetPropertyHistoryQuery` contract + handler
5. `GetPTRQuery` contract

### Alignment Matrix

#### RegSPI (Registry of Semi-Expendable Property Issued)

**Status:** ✅ Improved (employee display, totals, signatory projection, deterministic ordering, and ICS sections added)

**Available fields:**
- ICS No, Date, Fund Cluster
- Property No, Item Code/Name, Asset Type, Unit Cost
- EUL, Expires On, ICS Status
- Issued-From employee display fields (name, position, office)
- Requested employee header fields (employee no, name, office, department, position)
- Page and overall amount totals metadata
- Signatory block projection (`SortOrder`, `Label`, `Name`, `Title`)
- Section metadata grouped by ICS
- Deterministic ordering for printable output

**Gaps to verify:**
- Final print-layout ordering/parity against approved template

#### RSPI (Report of Semi-Expendable Property Issued)

**Status:** ✅ Improved (employee display, totals, signatory projection, deterministic ordering)

**Available fields:**
- ICS No, Date, Status, Fund Cluster
- Received By / Issued From employee display fields
- Property No, Item Code/Name, Asset Type, Unit Cost
- Expires On, Page/overall amount totals
- Signatory block projection
- Section metadata grouped by ICS
- Deterministic ordering for printable output

**Gaps to verify:**
- Printed section/signatory layout parity with approved template

#### SPC (Semi-Expendable Property Card)

**Status:** Contract-level aligned, handler verification pending

**Contract includes:**
- Date, Document Type/No
- Quantity In/Out, Unit Cost
- Running Balance, Remarks

**Gaps to verify:**
- Exact movement event mapping priority and tie-break ordering
- Running balance behavior under back-dated transactions

#### Property History

**Status:** ✅ Aligned for lifecycle audit view

**Current output includes:**
- Core item identity and threshold snapshot
- Current custodian
- Event timeline with source document trace

#### PTR (Derived from PPEIR)

**Status:** ✅ Improved (officer display projection added)

**Available fields:**
- PTR No, Date
- From/To accountable officer IDs + display fields
- Transfer Type, Approved/Released/Received By IDs
- Item lines (property no, description, amount, condition, reason)
- Name/position/office for From/To/Approved/Released/Received officers

### Verification Status (2026-05-09)

1. ✅ API/data-level cross-check for RegSPI, RSPI, and PTR fields/order/totals/signatories.
2. ✅ Regression coverage now locks RSPI/RegSPI deterministic ordering, section totals, signatory projection, summary totals, and PTR officer projection/item ordering.
3. 🔄 Pending external artifact: Final visual print-layout parity requires approved report templates/snapshots.

### This Pass (Implemented)

1. ✅ RegSPI now includes form-ready employee display fields.
2. ✅ RSPI now includes fund cluster plus received-by/issued-from employee display fields.
3. ✅ Employee display fields resolved through `GetEmployeeReferenceByIdQuery`.
4. ✅ RSPI and RegSPI include additive totals metadata for report summary rows.
5. ✅ PTR includes additive officer display metadata for signature/name blocks.
6. ✅ RSPI and RegSPI include additive signatory-block projection.
7. ✅ RSPI and RegSPI include additive per-ICS section metadata for grouped rendering.
8. ✅ RSPI and RegSPI line ordering is deterministic for template rendering.
9. ✅ Regression tests for RSPI/RegSPI query handlers validate deterministic ordering, section totals, signatory projection, and summary totals.
10. ✅ Regression test for PTR query handler validates officer display projection and item ordering.

### Evidence References

1. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/RegistryOfSPIssued/GetRegSPIQuery.cs`
2. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/RegistryOfSPIssued/GetRegSPIQueryHandler.cs`
3. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/ReportOfSPIssued/GetRSPIQuery.cs`
4. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/ReportOfSPIssued/GetRSPIQueryHandler.cs`
5. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/SemiExpendablePropertyCard/GetSPCQuery.cs`
6. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/Reports/PropertyHistory/GetPropertyHistoryQuery.cs`
7. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/PPEIssuanceReports/GetPTR/GetPTRQuery.cs`
8. `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/PPEIssuanceReports/GetPTR/GetPTRQueryHandler.cs`
9. `src/Tests/AssetManagement.Tests/Handlers/Reports/ReportQueryHandlerAlignmentTests.cs`

### Recommended Next Steps

1. Add regression tests for report query handlers covering expected ordering, filtering, and totals.
2. Validate each report endpoint against sample official form outputs.
3. Keep current registry/current-state sources for dashboards, while legal document reports remain sourced from immutable document tables.

---

## Quick Navigation

- **Implementation Planning:** See Part 1 for phased rollout of asset acquisition workflows.
- **Architecture & Registry:** See Part 2 for the additive overhaul design and current implementation status.
- **Report Verification:** See Part 3 for alignment checklist and current form-ready status.

**Related Documentation:**
- `ASSET-REGISTER-DOCUMENTATION.md` — AssetRegister bounded context specifics.
- `CLAUDE.md` — Development conventions and patterns.
- Module-specific guides in `Modules/AssetManagement/` and `Modules/AssetProcurement/`.
