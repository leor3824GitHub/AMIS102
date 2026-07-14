# AssetRegister — Physical Count "Balance = Found" Implementation Guide

> **Status: COMPLETE** — backend, Blazor, MAUI, and all print formats (RPCPPE + Annex B/C). Nothing outstanding
> except the COA-circular refinements in §5, which are backlog by design.
> Migration `20260611120000_PhysicalCountFoundAtStationIdentity` adds `ProposedPropertyNo`/`ProposedCatalogItemId` columns (apply before running).
> **Last updated:** 2026-07-14 (reconciled against HEAD)
> **Module:** `src/Modules/AssetRegister`
> **Legal basis:** COA Circular No. 2020-006 (Jan 31, 2020) — physical count of PPE, recognition of
> items found at station, disposition of non-existing/missing PPE.
> COA Circular No. 2022-004 (May 31, 2022) §4.19 — current replacement cost definition.

---

## 1. Goal (user requirement)

> "After inventory taking, the assets **found** will be the inventory. All found assets = the balance."

Refined against COA 2020-006, this means **two balances**, not one:

| Balance | Contents | Where it lives |
|---|---|---|
| **Verified/existing balance** | Found-on-books items + found-at-station items (newly recognized) | `AssetRegistry` rows in `Available` / `Assigned` |
| **Gross book balance** | Verified balance **+ missing items still in disposition** | Verified rows **+** rows in `UnderInvestigation` |

⚠️ Missing items can **NOT** be simply dropped at count close. Per §7.11.d / §8.1 they
*"shall remain in the books of accounts"* until produced, converted to a receivable, or
derecognized **with specific COA authority**. The system models this with
`LifecycleState.UnderInvestigation` — out of the *active/verified* balance, still on the books.

---

## 2. What the count does to each asset (COA-mapped)

| Count result | COA 2020-006 | System behavior at session **Close** | LifecycleState after |
|---|---|---|---|
| **Found** (good / repair / unserviceable) | §6.2.6 | Condition mirrored onto `AssetRegistry` (already implemented) | unchanged (`Available`/`Assigned`) |
| **Found at station** (present, not on books) | §6.2.7, §6.3.2.a — *"recognize PPEs found at station"* | **Auto-materialize** into registry using operator-supplied PropertyNo + catalog item | `Available` (new row) |
| **Missing** (recorded not found) | §6.2.8 | **Auto-flag** `MarkMissingFromCount` → relief track | `UnderInvestigation` |
| **Uncounted** (on books, in scope, never recorded) | §6.3.1.d — *"PCs and PARs on file but not included in the RPCPPE"* are non-existing/missing | **Auto-flag** same as Missing | `UnderInvestigation` |

The disposition track after `UnderInvestigation` (already modeled by Incidents feature):

```
UnderInvestigation
 ├─ produced upon demand (§7.7)        → MarkRecovered            → Available
 ├─ can't produce → receivable (§7.10) → IncidentItemResolution.Paid
 ├─ relief granted (§7.11)             → IncidentItemResolution.ReliefGranted → Dispose
 └─ COA derecognition authority (§8)   → IncidentItemResolution.Derecognized  → Dispose
```

---

## 3. Implemented ✅

### 3.1 Earlier hardening (second pass, 2026-06-10/11)

| Change | File |
|---|---|
| Current Replacement Cost per 2022-004 §4.19 — latest similar acquisition price (`ReplacementCostPolicy` pure function + 6 unit tests) | `Domain/Services/ReplacementCostPolicy.cs`, `Data/Services/CurrentReplacementCostCalculator.cs` |
| CRC calculator takes loaded `AssetRegistry` (no redundant re-fetch); single caller updated | `Domain/Services/NumberGenerators.cs` (interface), `Features/v1/Incidents/FileIncidentReport/FileIncidentReportCommandHandler.cs` |
| Freeze-guard consistency: `FileIncidentReport` now calls `EnsureMovementAllowedAsync` (user-approved: RLSDDSP filing is blocked while a covering count is frozen) | same handler |

### 3.2 This feature (domain layer, landed 2026-06-11)

| Change | File |
|---|---|
| `AssetRegistry.MarkMissingFromCount(sessionId)` — not-found asset drops out of active balance → `UnderInvestigation`; **no-op** for rows already outside the active balance (idempotent close) | `Domain/Assets/AssetRegistry.cs` |
| `MarkUnderInvestigation` now also allowed **from** `UnderInvestigation` — so a formal RLSDDSP can attach to an asset the count already flagged | `Domain/Assets/AssetRegistry.cs` |
| `PhysicalCountEntry.ProposedPropertyNo` + `ProposedCatalogItemId` — operator-assigned identity that enables auto-materialization (catalog supplies UACS code, useful life, class/unit, per `AssetRegistry.Register` invariants) | `Domain/Counting/PhysicalCountEntry.cs` |
| Threaded through `PhysicalCountSession.AddFoundAtStationEntry` and its command handler | `Domain/Counting/PhysicalCountSession.cs`, `Features/v1/Counting/AddFoundAtStationEntry/…Handler.cs` |
| Contracts: `AddFoundAtStationEntryCommand` + `PhysicalCountEntryDto` carry the two new optional fields (defaulted — non-breaking); mapper updated | `Modules.AssetRegister.Contracts/v1/Counting/CountingContracts.cs`, `Features/v1/Counting/CountingMapper.cs` |

---

## 3.3 Close-handler true-up + reconciliation (landed 2026-06-11)

| Change | File |
|---|---|
| Close handler materializes found-at-station entries (PropertyNo + catalog → `AssetRegistry.Register` → `AttachReconciledAssetToEntry`), then flags Missing + Uncounted → `MarkMissingFromCount`, in one transaction; 409 lists entries missing PropertyNo/catalog/value | `Features/v1/Counting/ClosePhysicalCount/ClosePhysicalCountCommandHandler.cs` |
| Shared PPE/SE classifier extracted to a single source of truth; IAR consumer refactored to use it | `Data/Services/AssetClassificationPolicy.cs`, `Integration/AssetIARAcceptedEventConsumer.cs` |
| Shared in-scope query (`InCountScope`) used by both reconciliation report and close-handler uncounted flagging, so they never diverge | `Features/v1/Counting/CountScopeQuery.cs`, `Features/v1/Counting/GetReconciliationReport/GetReconciliationReportQueryHandler.cs` |
| `GetReconciliationReport` (variance: Matched/Shortage/Overage/Uncounted) + `RequestPhysicalCountRecount` slices mapped | `Features/v1/Counting/GetReconciliationReport/*`, `Features/v1/Counting/RequestPhysicalCountRecount/*` |
| Validator: `ProposedPropertyNo` ≤64, `ProposedUnitCost` > 0 when present | `Features/v1/Counting/AddFoundAtStationEntry/AddFoundAtStationEntryCommandValidator.cs` |
| 8 unit tests (`MarkMissingFromCount` transitions/no-ops, `MarkUnderInvestigation` from UnderInvestigation, Close FoundAtStation invariant, AddFoundAtStation round-trip) — 38 passing total | `Tests/AssetRegister.Tests/Domain/PhysicalCountCloseTests.cs` |

> Note: §4.2 migration was **not** newly created — `20260610113721_PhysicalCountFreeze` already adds
> `OfficeOrderNo`, `FrozenOnUtc`, recount fields, `ProposedPropertyNo`, `ProposedCatalogItemId`.

---

## 4. To be implemented 🔲

### 4.1 Close-handler true-up (core of "balance = found") — ✅ DONE (see §3.3)

`Features/v1/Counting/ClosePhysicalCount/ClosePhysicalCountCommandHandler.cs` — extend to, **in order**:

1. **Materialize found-at-station entries** (those with `AssetRegistryId == null`):
   - Require `ProposedPropertyNo` + `ProposedCatalogItemId` (else throw 409 listing offending entries — replaces the current blanket close-guard in `PhysicalCountSession.Close`).
   - Load catalog item; call `AssetRegistry.Register(...)` with `ProposedUnitCost ?? SnapshotUnitCost`, `ProposedAcquisitionDate ?? session.AsAt`, fund cluster = `session.FundCluster`, asset type derived from session scope / catalog.
   - Call `session.AttachReconciledAssetToEntry(entryId, asset.Id)` (already exists, `internal`).
2. **Flag not-found**:
   - Missing entries: `asset.MarkMissingFromCount(session.Id)`.
   - Uncounted: reuse the **same in-scope query** as `GetReconciliationReportQueryHandler` (fund cluster + scope + `Available`/`Assigned`, excluding counted ids) — extract it to a shared private/local helper or keep the two literally in sync.
3. Mirror conditions for found entries (already there) and `SaveChangesAsync` once — single transaction.

Also update `PhysicalCountSession.Close` guard: the "FoundAtStation entry has not been materialized" exception stays as a final invariant, but the handler now satisfies it instead of the operator.

### 4.2 Persistence

- EF config: add `ProposedPropertyNo` (maxLength 64) + `ProposedCatalogItemId` to `PhysicalCountEntryConfiguration` (`Data/Configurations/PhysicalCountSessionConfiguration.cs`).
- Migration in `src/Host/Migrations.PostgreSQL/AssetRegister` (context `AssetRegisterDbContext`), e.g. `PhysicalCountFoundAtStationIdentity`. Dev phase — data is disposable; no backfill needed.

### 4.3 Validators

- `AddFoundAtStationEntryCommandValidator`: when `ProposedPropertyNo` present → `MaximumLength(64)`, normalized `.Trim().ToUpperInvariant()` expectations; `ProposedUnitCost > 0` when present.
- `ClosePhysicalCountCommandValidator`: unchanged (handler enforces materialization preconditions).

### 4.4 Tests (pure unit, per project convention — no EF in-memory)

- `MarkMissingFromCount`: from `Available` → `UnderInvestigation` + event; from `Assigned` → same; from `Disposed`/`TransferredOut`/`UnderInvestigation` → no-op.
- `MarkUnderInvestigation` from `UnderInvestigation` → allowed, raises `AssetLostEvent`.
- `PhysicalCountSession.Close` invariant still throws when a FoundAtStation entry is unmaterialized.
- `AddFoundAtStationEntry` round-trips the two new fields.

### 4.5 Clients (after backend lands)

**Blazor — landed 2026-06-11:**
- `AssetRegisterClient.cs` (`IArPhysicalCountClient`): added `FreezeAsync`, `AddFoundAtStationAsync`, `MarkMissingAsync`,
  `ReconcileAsync`, `RequestRecountAsync`, `GetReconciliationAsync`; new DTO fields (OfficeOrderNo/FrozenOnUtc,
  recount + proposed-identity); reconciliation DTOs; `Station` on close; a ProblemDetails-aware error reader so the
  close-handler 409s surface their message.
- `PhysicalCountPage.razor`: full lifecycle Draft→Freeze→Ongoing→Reconcile→Reconciling→Close (fixed prior bug where
  Close showed in Ongoing); reconciliation variance panel (Matched/Shortage/Overage/Uncounted + per-row table) with
  Trigger Recount and Mark Missing; Office Order / Frozen display; Station on close; Draft-aware labels.

- **Found-at-station capture form (Blazor) — landed 2026-06-11:** new `LocationLookupClient` (`ILocationLookupClient`,
  read-only over `api/v1/asset-management/locations`) registered in `ApiClientRegistration`; `PhysicalCountPage`
  Ongoing block gained an "Add Found-at-Station Item" panel with catalog autocomplete + location autocomplete +
  PropertyNo (`.Trim().ToUpperInvariant()`) + article/unit/appraised cost, posting via `AddFoundAtStationAsync`.
  Requires the user to hold AssetManagement `Locations.View`.

**MAUI — landed 2026-06-11:** `PhysicalCountFoundAtStationPage` PropertyNo changed from a read-only label to an
editable `Entry` (manual-entry fallback for damaged/missing stickers, per MAUI rules); VM validates PropertyNo is
non-empty before save (already normalized `.Trim().ToUpperInvariant()`).

> **Superseded 2026-07-14:** the note that once stood here — "MAUI counting targets the AssetManagement
> physical-count API, rewiring out of scope" — is obsolete. The `AssetManagement` module has been **deleted**
> from HEAD and MAUI now calls AssetRegister's record-as-you-go counting endpoints
> (`AMIS.Maui/Services/ApiClient.cs`). Blazor's location autocomplete likewise no longer depends on an
> `asset-management` route or an AssetManagement `Locations.View` permission — read the code above as historical.

**Annex B / Annex C print-parity — landed 2026-06-11 (QuestPDF):**
- `Modules.QuestPdfReporting` now references `Modules.AssetRegister.Contracts`; new slice
  `Features/v1/AssetRegister/PrintCountAnnexes/` (`PrintCountAnnexesQuery` + handler + `CountAnnexPdfDocument`)
  consumes `GetReconciliationReportQuery`: Annex B = Overage rows (Found at Station), Annex C = Shortage + Uncounted
  rows (Non-Existing/Missing). Endpoints `…/asset-register/physical-count/{id}/annex-b|c/pdf`
  (`QuestPdfReporting_PrintCountAnnexB`/`C`, perm `AssetRegister.Count.View`), mapped via new
  `Endpoints/AssetRegisterEndpoints.cs`. Blazor reconciliation panel gained **Annex B / Annex C** download buttons
  (`GetCountAnnexPdfAsync`, data-URL open).

**Still pending: nothing.** (Reconciled against HEAD 2026-07-14.)

- ~~Full RPCPPE (Appendix 73) print-parity~~ — **done.** The main count report now has its own QuestPDF slice,
  `Features/v1/AssetRegister/PrintPhysicalCountReport/` (`PrintPhysicalCountReportQuery` + `PhysicalCountReportPdfDocument`
  + endpoint). It no longer routes through the (now deleted) AssetManagement reports path.
- ~~Regenerate NSwag client~~ — **not applicable.** NSwag regeneration is deferred by explicit decision across the
  Blazor client; these endpoints are consumed via raw `HttpClient`.

Remaining work on physical count is limited to the COA-circular refinements in §5 below, which are backlog by design.

---

## 5. Refinements from the COA circulars (beyond current scope — backlog)

| # | Refinement | Basis | Note |
|---|---|---|---|
| 1 | **Receivable at *depreciated* replacement cost.** When a missing asset converts to a receivable, amount = CRC **less accumulated depreciation computed on replacement cost** — not raw CRC and not carrying amount. Current `RecordIncidentSettlement` flow should compute `crc − accumulated depreciation(rebased)`. | §7.10 | `CurrentReplacementCostCalculator` gives the CRC; add a small policy for the depreciation rebase. |
| 2 | **Demand-letter step before loss recognition.** Missing → 5-calendar-day demand on the accountable officer; "produced upon demand" → amend count (maps to `MarkRecovered`). Consider `DemandIssuedOn`/`DemandRespondedOn` fields on `PropertyIncidentItem` or a small workflow status. | §7.5–7.8 | Today the gap is procedural; the state machine (UnderInvestigation → Recovered/Paid) already fits. |
| 3 | **Found-at-station valuation hierarchy**: committee-assigned market/fair value → similar item in RPCPPE → formal appraisal. `ProposedUnitCost` covers the first two; flag entries needing appraisal (cf. §647: items "still needing appraisal" are disclosed, not yet recognized). | §6.2.12 | Could add `NeedsAppraisal` flag that blocks materialization. |
| 4 | **Registry of Derecognized PPEs (RDPPE)** — derecognized assets move to a dedicated registry (Annex D) and FS disclosure. Today `Disposed` is terminal but there's no RDPPE report. | §8, Annex D | Report slice: `GetDerecognizedAssetsReport`. |
| 5 | **Report formats**: RPCPPE, List of PPEs Found at Station (Annex B), List of Non-Existing/Missing PPEs (Annex C). The reconciliation report (`GetReconciliationReport`) is the data source; print-parity layouts pending. | §6.2.13, Annexes B–C | Fold into the existing print-parity workstream. |
| 6 | **PAR renewal after count** (§6.3.1.g) and **IIRUP for unserviceable found** (§6.3.1.h) — both flows exist (`RenewAccountability`, Unserviceable feature); consider post-close prompts in the UI linking count results to them. | §6.3.1 | UX wiring only. |
| 7 | **Derecognition preconditions** for no-accountability cases: asset exceeded estimated useful life (carrying value = residual) + investigation done. Validate in `DerecognizeIncidentItem` (compare `EstimatedUsefulLifeYears` vs `AcquisitionDate`). | §8.2 | Cheap guard, high audit value. |
| 8 | 2020-006 is **one-time cleansing**; recurring counts follow the regular RPCPPE cycle — *"in no case shall the herein procedures be used to further derecognize… subsequent discrepancies"* (p. 664). The freeze/close workflow is generic; keep derecognition authority-gated per count. | §9 | Already consistent. |

---

## 6. Key design decisions (user-approved)

| Decision | Choice | Why |
|---|---|---|
| Not-found assets at close | **Flag `UnderInvestigation`** (relief track) — never hard-delete | §7.11.d/§8.1: stay on books until COA authority |
| Uncounted in-scope assets | **Treated as missing** automatically | §6.3.1.d says exactly this |
| Found-at-station entry into balance | **Auto-materialize at close** (operator supplies PropertyNo + catalog item during count) | §6.3.2.a; `AssetRegistry.Register` needs catalog for UACS/useful-life invariants |
| RLSDDSP filing during frozen count | **Blocked** by `ICountFreezeGuard` | Keeps frozen snapshot stable; file after close |

---

## 7. Verification

```powershell
dotnet build src/AMIS.Framework.slnx                                  # 0 errors (≈273 pre-existing style warnings in module)
dotnet test src/Tests/AssetRegister.Tests/AssetRegister.Tests.csproj  # 30 passing now; grows with §4.4 tests
```

End-to-end (Aspire, once §4.1–4.2 land): start count → freeze → record found / found-at-station (with PropertyNo + catalog) / missing → reconcile → close → verify:
1. found-at-station rows exist in `AssetRegistries` as `Available`;
2. missing + uncounted rows are `UnderInvestigation`;
3. registry filtered to `Available`/`Assigned` equals exactly the found set;
4. `FileIncidentReport` against a flagged asset succeeds after close (and 409s while frozen).
