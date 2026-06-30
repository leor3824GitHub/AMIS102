# RPRI Repair Workflow — Split Inspection From Finance/Acceptance

> Status: **Planned (not started)** · Module: `AssetRegister` · Form: NFA Exhibit 6 (RPRI)
> Author note: captured from design discussion 2026-06-30. No code changed yet.

## 1. Goal

Stop forcing the **Property Inspector** to type procurement/finance data they don't own. Split the
post-repair workflow so:

- **Inspector** records only the technical post-repair inspection (Findings, Post-Inspected By, Date).
- **Property Custodian** links the source procurement document **once** (PO **or** JO) at acceptance time,
  which **auto-fills** Repair Shop, Amount, PR/PO-JO/BUR/DV — then accepts.

This collapses ~9 manual fields into **1 document selection + an editable Amount + 2 invoice fields**, and
aligns the UI with both the official Exhibit 6 block layout and the existing permission split.

## 2. Why this is the right cut

The paper Exhibit 6 already separates the data into three blocks, and the endpoints already gate them by
two different permissions:

| Exhibit 6 block | Owner | Endpoint / permission |
| --- | --- | --- |
| **Post-Repair Inspection** → Findings, Post-Inspected By, Date | Inspector | `POST /{id}/post-inspection` · `Repair.Inspect` |
| **Procurement/Finance References** + Repair Shop, JO No., Invoice, Amount | Property Custodian | `POST /{id}/accept` · `Repair.Accept` |
| **Property Custodian Acceptance** → Accepted By, Date | Property Custodian | `POST /{id}/accept` · `Repair.Accept` |

Today the commercial + finance fields are written by `post-inspection` (Inspect permission). The fix moves
them to `accept` (Accept permission) — no new permission, no new workflow state.

## 3. Field ownership (target)

| Field | Step (today) | Step (target) | Source at acceptance |
| --- | --- | --- | --- |
| Findings | post-inspection | **post-inspection** (unchanged) | manual |
| Post-Inspected By | post-inspection | **post-inspection** (unchanged) | manual |
| (Post-)Inspection Date | post-inspection | **post-inspection** (unchanged) | manual |
| Repair Shop / Contractor | post-inspection | **accept** | linked PO/JO `SupplierName` |
| Job Order / Contract No. (`JobOrderNo`) | post-inspection | **accept** | linked PO/JO number |
| Amount per JO / Payable (`AmountPerJO`) | post-inspection | **accept** | linked PO/JO `TotalAmount` (editable) |
| Invoice No. / Invoice Date | post-inspection | **accept** | manual (contractor's external doc) |
| PR No. (`PrNo`) | post-inspection | **accept** | linked PO/JO `PrNumber` |
| PO/JO No. (`PoJoNo`) | post-inspection | **accept** | linked PO/JO number |
| BUR No. (`BurNo`) | post-inspection | **accept** | linked PO/JO `OursBursNumber` |
| DV No. (`DvNo`) | post-inspection | **accept** | auto-resolved via DV search |
| Accepted By / Accepted On | accept | **accept** (unchanged) | manual |

All finance/commercial fields stay **optional** ("outsourced repairs only"). In-house repairs leave the
whole block blank.

## 4. Target UX

**Inspector — Post-Inspection** (shrinks to 3 fields)
```
Findings ............... [required]
Post-Inspected By ...... [required]
Date ................... [____]
```

**Property Custodian — Acceptance** (the "another UI")
```
Linked source document . [ 🔍 unified PO/JO autocomplete ]      ← outsourced only, optional
   ↳ inherited (read-only summary line):
     ACME Repair Shop · PR-2026-04-012 · PO/JO-… · BUR-… · DV-… (if paid)
Amount Payable ......... [ ₱ pre-filled, editable ]
Invoice No. / Date ..... [____] [____]
─────────────────────────────────
Accepted By (Custodian)  [required]
Date ................... [____]
```

### Unified PO + JO selector

A single autocomplete queries **both** `IPurchaseOrderClient.SearchAsync` and `IJobOrderClient.SearchAsync`,
merges results, tags each with its kind, and projects to one shape:

```csharp
private sealed record LinkedDoc(
    string Kind,        // "PO" | "JO"
    Guid   Id,
    string Number,      // PoNumber / JoNumber
    string Contractor,  // SupplierName
    decimal Amount);    // TotalAmount
// PR + BUR come from the full PO/JO DTO (GetByIdAsync) on select, since the *summary* DTOs
// don't carry PrNumber / OursBursNumber.
```

On select:
1. Fetch the full DTO (`GetPurchaseOrderQuery` / `GetJobOrderQuery`) to read `PrNumber` + `OursBursNumber`.
2. Fill `_repairShop`, `_amount` (editable), `_jobOrderNo`, `_poJoNo`, `_prNo`, `_burNo`.
3. Auto-resolve DV: `IDisbursementVoucherClient.SearchAsync(purchaseOrderId: doc.Id)` → if a non-cancelled
   DV exists, set `_dvNo`; otherwise leave blank (payment usually follows acceptance).

> The PO/JO *summary* DTOs lack `PrNumber`/`OursBursNumber`, so a `GetByIdAsync` round-trip on select is
> required to inherit PR + BUR. Acceptable — one call on an explicit user action.

## 5. Implementation steps (ordered)

### 5.1 Contracts — `src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/Repairs/RepairContracts.cs`
- **`RecordPostRepairInspectionCommand`** → reduce to:
  `(Guid RepairId, string Findings, string PostInspectedBy, DateOnly PostInspectedOn)`.
- **`AcceptRepairCommand`** → add the moved fields (all optional):
  `(Guid RepairId, string AcceptedBy, DateOnly AcceptedOn, string? RepairShop = null, string? JobOrderNo = null,
   string? InvoiceNo = null, DateOnly? InvoiceDate = null, decimal? AmountPerJO = null, string? PrNo = null,
   string? PoJoNo = null, string? BurNo = null, string? DvNo = null)`.
- `PropertyRepairDto` is unchanged (it already carries every field).

### 5.2 Domain — `src/Modules/AssetRegister/Modules.AssetRegister/Domain/Repairs/PropertyRepair.cs`
- **`RecordPostRepairInspection`** → signature `(string findings, string postInspectedBy, DateOnly inspectedOn)`;
  drop assignment of RepairShop/JobOrderNo/Invoice*/AmountPerJO/Pr/PoJo/Bur/Dv. Keep the `PreInspected|Repaired`
  guard and the findings-required check.
- **`Accept`** → add the optional finance/commercial params and assign them before flipping to `Accepted`.
  Keep the `PostInspected`-only guard.
- Update the class XML summary comment ("captured ... on the post-repair section" → "captured at acceptance").

### 5.3 Handlers — `src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Repairs/`
- `RecordPostRepairInspectionCommandHandler.cs` → call the slimmed domain method.
- `AcceptRepairCommandHandler.cs` → pass the new fields into `repair.Accept(...)` (currently line ~20:
  `repair.Accept(cmd.AcceptedBy, cmd.AcceptedOn)`).

### 5.4 Validators — `src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Repairs/RepairValidators.cs`
- Move the `AmountPerJO >= 0` and `RepairShop` length rules **out of**
  `RecordPostRepairInspectionCommandValidator` **into** `AcceptRepairCommandValidator` (keep them conditional).
- `RecordPostRepairInspectionCommandValidator` keeps only RepairId/Findings/PostInspectedBy/PostInspectedOn.

### 5.5 Blazor API client — `src/Host/AMIS.Blazor/ApiClient/AssetRegisterClient.cs`
- `ArPostRepairInspectionRequest` (≈ line 1797) → reduce to `(string Findings, string PostInspectedBy, DateOnly PostInspectedOn)`.
- `ArAcceptRepairRequest` (≈ line 1802) → add the optional finance/commercial fields, mirroring `AcceptRepairCommand`.

### 5.6 Blazor dialog — `src/Host/AMIS.Blazor/Components/Pages/AssetRegister/RepairWorkflowDialog.razor`
- **`PreInspected | Repaired` branch** → only Findings + Post-Inspected By + Date.
- **`PostInspected` branch (acceptance)** → add the unified PO/JO autocomplete, read-only inherited refs line,
  editable Amount, Invoice No./Date, plus the existing Accepted By + Date.
- Inject `IPurchaseOrderClient`, `IJobOrderClient`, `IDisbursementVoucherClient`.
- `Submit()` → `AcceptAsync` now sends the full `ArAcceptRepairRequest`; `PostInspectAsync` sends only the 3 inspection fields.
- Save as **UTF-8** (peso glyph `₱`).

### 5.7 Build + verify
- `dotnet build src/AMIS.Framework.slnx` (0 warnings).
- `dotnet test src/AMIS.Framework.slnx`.

## 6. No migration required
The columns (`RepairShop`, `JobOrderNo`, `InvoiceNo`, `InvoiceDate`, `AmountPerJO`, `PrNo`, `PoJoNo`, `BurNo`,
`DvNo`) already exist on `PropertyRepair`. Only **which command writes them** changes — the table is untouched.
(Consistent with dev-phase "structure over data" stance.)

## 7. PDF / print
`RepairPdfDocument` and the mapper render all three Exhibit 6 blocks from `PropertyRepairDto`, which is
unchanged. No print changes — the data simply arrives via the correct step. Verify the PDF still populates
the finance block after the move (data now lands at acceptance instead of post-inspection).

## 8. Open questions / decisions to confirm at build time
1. **DV ↔ JO linkage.** DV search keys on `purchaseOrderId`. Confirm whether a DV against a **Job Order** is
   discoverable the same way; if JOs are paid through a different reference, the JO-anchored DV auto-resolve
   may need a keyword fallback (search DV by JO number) or be left manual.
2. **`JobOrderNo` vs `PoJoNo` duplication.** Two near-identical fields exist (Block 1 "Job Order / Contract No."
   vs Block 2 "PO / JO No."). Decide: set both from the linked document number, or keep `JobOrderNo` as a
   free-text contract reference for in-house/standalone cases. Default plan: set both from the linked doc.
3. **Combined vs separate.** Acceptance UI both captures finance refs **and** accepts in one submit. If
   custodians need to record refs without accepting yet, add a separate "save refs" command later (out of scope now).
4. **Edit-after-accept.** Accepted RPRIs are locked. If a DV number arrives *after* acceptance (payment later),
   decide whether a narrow "attach DV" action is needed, or it's acceptable to leave DV blank on history.

## 9. Out of scope
- Workflow state changes (still Requested → PreInspected → [Repaired] → PostInspected → Accepted).
- Any backend schema/migration.
- Changes to the request or pre-inspection steps.

## 10. Touch list (quick reference)
```
src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/Repairs/RepairContracts.cs
src/Modules/AssetRegister/Modules.AssetRegister/Domain/Repairs/PropertyRepair.cs
src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Repairs/RecordPostRepairInspectionCommandHandler.cs
src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Repairs/AcceptRepairCommandHandler.cs
src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Repairs/RepairValidators.cs
src/Host/AMIS.Blazor/ApiClient/AssetRegisterClient.cs              (request DTOs)
src/Host/AMIS.Blazor/Components/Pages/AssetRegister/RepairWorkflowDialog.razor
— reuse only (no change): IPurchaseOrderClient, IJobOrderClient, IDisbursementVoucherClient (ProcurementClient.cs / BudgetDisbursementClient.cs)
```
