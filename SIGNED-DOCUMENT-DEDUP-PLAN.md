# Plan — Inline the signed copy onto its parent document aggregate

> Status: **Implemented.** Supersedes the earlier "mirror `NumberSequence` into BuildingBlocks"
> approach (a shared `SignedDocument` *entity* + table per module). Addresses ENTITY-CATALOG.md
> refactoring observation #1.
>
> Build: 0 errors / no new warnings. Tests: all pass (incl. Architecture.Tests). 3 delta migrations added
> (`*_InlineSignedCopy`). **Remaining:** end-to-end runtime check of the `x.SignedCopy != null` Search
> translation on PostgreSQL (module unit tests use the EF InMemory provider, which does not translate to
> SQL). Fallback if EF ever won't translate the bare null check: `x.SignedCopy!.Sha256 != null`.

## Why the approach changed

The earlier plan proposed collapsing the triplicated `SignedDocument` **entity** into BuildingBlocks,
mirroring the completed `NumberSequence` consolidation. That analogy is wrong:

- **`NumberSequence` is an infrastructure primitive** — a bounded set of counter rows, mutated in place
  (`++LastSerial`), zero business meaning. It belongs in the framework kernel.
- **`SignedDocument` is growing *domain* data** — a document of record with legal weight, one row per
  signed document, scaling with business volume. A growing domain aggregate does not belong in the kernel.

The unique index `(TenantId, DocumentType, DocumentId)` + `Replace`-on-re-upload proves the signed copy is
a **strictly 1:1 attribute of its parent document with no independent lifecycle** (confirmed permanent: one
current copy per document; re-upload replaces; no history/versioning). The DDD-correct model is therefore
an **owned value object on the parent aggregate — not a separate entity at all.**

### What inlining removes

| Today (separate `SignedDocument` entity, ×3) | Inlined `SignedCopy` value object |
| --- | --- |
| Polymorphic FK `DocumentType + DocumentId`, no DB constraint (a smell) | Strongly-typed ownership on the parent row |
| `HasSignedCopy` via correlated `EXISTS` subquery (18 handlers) | Plain `x.SignedCopy != null` on the same row |
| 3 near-identical tables + entities + configs + DbSets | **No signed-doc table at all** |
| Query-filter-collision constraint (named vs anonymous soft-delete filter) | **Gone** — owned VOs carry no query filter |
| Soft-delete drift (AssetRegister none; Proc/Budget yes) | **Gone** — the copy rides the parent row |
| **Public API surface** | **Unchanged** — routes, commands, queries, DTOs, permissions, Blazor clients |

Dev data is disposable, so migration is trivial — drop the 3 `SignedDocuments` tables, add owned-VO columns
to the parent tables.

## Decisions (locked with the user)

- **Model:** one `SignedCopy` **value object** (a single grouped property per entity, not flat fields),
  inlined as an EF **owned type** on each parent aggregate. Nullable — sparse until uploaded; re-upload replaces it.
- **VO home:** shared `BuildingBlocks/Core/Domain/SignedCopy.cs` — a value object (immutable, no identity,
  no table) in the shared kernel is textbook (like `Money`), and is *not* the growing-aggregate coupling the
  old plan created. Aggregates that carry one implement `ISignedCopyHolder` (Core).
- **VO fields (6):** `StorageKey`, `Sha256`, `FileName`, `FileSizeBytes`, `UploadedByName?`, `UploadedOnUtc`.
  - Dropped **`UploadedById`** (dead — never read/shown/queried; acting user is on the parent's audit).
  - Dropped **`ContentType`** (provably constant — validators enforce PDF-only via `.pdf` + `%PDF` magic
    bytes; not shown in any dialog; download serves the literal `"application/pdf"`). The `SignedDocumentDto`
    / `SignedDocumentFileDto` keep their `ContentType` field (API unchanged), populated with the constant.
- **Mechanics de-duplicated once** via a stateless static `SignedCopyStore` in `BuildingBlocks/Storage`
  (mirrors `SequenceAllocator`; no DI). No `Persistence → Storage` reference needed.
- **IAR gap preserved:** `ProcurementDocumentType.InspectionAcceptanceReport` has no upload path today (the
  upload `switch` has no arm for it → throws); its read-side flag is effectively always-false. The refactor
  keeps this — IAR's aggregate gets a `SignedCopy` property that simply stays null. Closing the gap is an
  optional separate follow-up.
- **Public API unchanged:** per-module enum, `UploadSignedDocumentCommand`, `GetSignedDocumentQuery`,
  `DownloadSignedDocumentQuery`, DTOs, `/signed-documents` routes, permissions, and all 3 Blazor `ApiClient`
  files stay. The enum now only *dispatches to the right aggregate* inside the handler; it is never stored.

## Parent-aggregate mapping (15 document types)

Each match is `x.Id == DocumentId` on the listed DbSet, **except RFQ** whose `DocumentId` is a
`CanvassQuotation.Id` (its own aggregate/table) — so the RFQ `SignedCopy` lives on `CanvassQuotation`, not
`CanvassRequest`.

- **AssetRegister:** ReturnedPropertyReceipt→`ReturnedPropertyReceipt`, PropertyAccountability→`PropertyAccountability`,
  IssuanceReport→`PropertyIssuanceReport`, ReceivingReport→`ReceivingReport`,
  UnserviceableReport→`UnserviceablePropertyReport`, IncidentReport→`PropertyIncidentReport`,
  PhysicalCountReport→`PhysicalCountSession`. *(No soft-delete on these.)*
- **Procurement:** PurchaseRequest→`PurchaseRequest`, PurchaseOrder→`PurchaseOrder`,
  AbstractOfCanvass→`CanvassRequest`, RequestForQuotation→**`CanvassQuotation`**, JobOrder→`JobOrder`,
  InspectionAcceptanceReport→`InspectionAcceptanceReport` *(stays null — IAR gap)*.
- **BudgetDisbursement:** DisbursementVoucher→`DisbursementVoucher`, BudgetUtilizationRequest→`BudgetUtilizationRequest`.

## Implementation

1. **`Core/Domain/SignedCopy.cs`** (record, 6 fields) + **`Core/Domain/ISignedCopyHolder.cs`**
   (`SignedCopy? SignedCopy { get; }` + `void SetSignedCopy(SignedCopy)`).
2. **`Storage/SignedDocuments/SignedCopyStore.cs`** (static): `BuildAsync` (hash + upload → `SignedCopy`) and
   `DownloadAsync` (fetch + SHA-256 re-verify → `SignedCopyFile`).
3. **`Persistence/SignedDocuments/SignedCopyConfigurationExtensions.cs`**: `ConfigureSignedCopy(x => x.SignedCopy)`
   optional-owned-type mapping (columns `SignedCopy_*`; StorageKey 1024 / Sha256 64 / FileName 260 /
   UploadedByName 200), reused by all 15 configs.
4. **15 parent aggregates:** add `public SignedCopy? SignedCopy { get; private set; }` +
   `SetSignedCopy(...)`, implement `ISignedCopyHolder`; each EF config calls `ConfigureSignedCopy`.
5. **3 upload handlers:** load the parent **tracked** (the existing signable-state `switch`), `SetSignedCopy`
   with `SignedCopyStore.BuildAsync`, save with orphan-blob rollback + old-blob cleanup.
6. **3 get + 3 download handlers:** resolve the parent's `SignedCopy` by (type, id); Get → DTO (404 if null),
   Download → `SignedCopyStore.DownloadAsync`.
7. **18 read-side handlers:** `db.SignedDocuments.Any(sd => sd.DocumentType == E.X && sd.DocumentId == x.Id)`
   → `x.SignedCopy != null` (fallback `x.SignedCopy!.Sha256 != null` if EF won't translate the bare null check).
8. **Delete** the 3 `SignedDocument` entities + configs + DbSet lines.
9. **3 delta migrations** (one per context; do not squash; `--startup-project src/Host/AMIS.Api`): drop the
   `SignedDocuments` table, add `SignedCopy_*` columns to the parent tables.
10. **Tests:** retarget `SignedDocumentDomainTests` to the `SignedCopy` VO; drop the `UploadedById` assertion.

## Verification

1. `dotnet build src/AMIS.Framework.slnx` — 0 warnings.
2. `dotnet test src/AMIS.Framework.slnx` — all pass (incl. Architecture.Tests; a shared VO in Core is allowed).
3. Apply the 3 migrations against a dev DB; confirm the 3 `SignedDocuments` tables are gone and the parent
   tables gained `SignedCopy_*` columns.
4. End-to-end per module (Aspire / `/run`): for one document type each — upload a signed PDF (lands under
   `uploads/protected/{tenant}/{docType}/…`, `SignedCopy` persists on the parent row), get metadata,
   download (integrity check passes), re-upload (replaces the VO, old blob cleaned up). Confirm a Search list
   still shows the "has signed copy" flag (proves `x.SignedCopy != null` translates).
