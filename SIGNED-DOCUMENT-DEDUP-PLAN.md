# Plan — De-duplicate the triplicated `SignedDocument`

> Status: **Approved, not yet implemented.** Addresses ENTITY-CATALOG.md refactoring observation #1.
> Mirrors the already-completed `NumberSequence` consolidation (observation #2).

## Context

`SignedDocument` (the uploaded wet-signed PDF scan of a document of record) is copy-pasted
across **three modules** — AssetRegister, ProcurementAcquisition, BudgetDisbursement. Each module
carries its own: the aggregate entity, the EF configuration, a `DbSet`, a `SignedDocumentContracts.cs`
(enum + DTOs + command + queries), a 3-endpoint vertical slice (Upload / Get-metadata / Download),
and a Blazor API client.

The three copies are near-identical. The *only* genuinely per-module concerns are:
1. the `DocumentType` **enum** (AssetRegister 7 values, Procurement 6, Budget 2 — disjoint), and
2. the **"is this document in a signable state?"** check, which reads each module's own domain
   entities (`EnsureDocumentSignedAsync`).

Everything else — SHA-256 hashing, storage upload to `uploads/protected/{tenant}/{docType}`,
orphan-blob rollback, replace-then-cleanup, download + integrity re-verify, the DTO shape, the
unique index `(TenantId, DocumentType, DocumentId)`, the route/permission shape — is duplicated
mechanically. The three copies have also **drifted**: AssetRegister lacks soft-delete and uses
`TenantId(50)`; Procurement/Budget have soft-delete and `TenantId(64)`.

**Goal:** collapse the shared entity, EF config, and mechanical upload/get/download logic into
BuildingBlocks (one copy), leaving each module only its enum, its signable-state check, and its
routes/permissions.

## Decisions (locked with the user)

- **Scope:** share the **entity + EF config + mechanical upload/get/download logic**. Module slices
  shrink to enum↔int mapping + the signable-state delegate + routes/permissions.
- **Soft-delete:** **standardize ON** for all three. The shared entity carries **plain**
  `IsDeleted` / `DeletedOnUtc` / `DeletedBy` props (NOT `ISoftDeletable`) + a domain `SoftDelete(...)`
  method. AssetRegister gains 3 nullable columns (non-destructive). No delete endpoint exists today;
  this is for consistency + legal-hold safety.
- **`DocumentType` stored as `int`** on the shared entity (the DB column is already `integer` in all
  three) — the shared type cannot reference three module enums. Each module keeps its enum in its
  Contracts project and casts `(int)`/`(TEnum)` at the feature boundary.
- **One `SignedDocuments` table per module schema** (asset_register / procurement / budgetdisbursement),
  *not* a single cross-module table — preserves module DB isolation, mirrors `NumberSequence`.

## Critical constraint — query-filter collision (do not get this wrong)

`BaseDbContext.OnModelCreating` calls `AppendGlobalQueryFilter<ISoftDeletable>(s => !s.IsDeleted)`
— an **anonymous** filter applied to any `ISoftDeletable` entity. On a `.IsMultiTenant()` entity that
collides with Finbuckle's **named** tenant filter and throws at model build
(*"Both anonymous and named query filters cannot be applied simultaneously"* — see
`.claude/rules/persistence.md`). This is exactly why today's Procurement/Budget `SignedDocument`
**does not implement `ISoftDeletable`** and instead uses a **named** `HasQueryFilter("SoftDelete", …)`.

➡️ The shared entity **must not implement `ISoftDeletable`**. The shared EF config applies the
**named** `builder.HasQueryFilter("SoftDelete", x => !x.IsDeleted)` alongside `.IsMultiTenant()`.

## Approach

### 1. Shared entity — `BuildingBlocks/Core/Domain/SignedDocument.cs`
New file, sibling of `NumberSequence.cs`.
- `public sealed class SignedDocument : AggregateRoot<Guid>, IHasTenant, IAuditableEntity`
- Props mirror the current entity but `DocumentType` becomes **`int`**; add plain soft-delete props
  (`IsDeleted` default-false, `DeletedOnUtc?`, `DeletedBy?`) with `private set`.
- `Create(...)` / `Replace(...)` (as the current AssetRegister entity, but `int documentType`), plus
  `SoftDelete(string deletedBy)`.

### 2. Shared EF config — `BuildingBlocks/Persistence/SignedDocuments/SignedDocumentConfiguration.cs`
New `ModelBuilder` extension `ConfigureSignedDocuments(this ModelBuilder, string schema)`, modeled on
`NumberSequenceConfiguration.cs`. Sets `ToTable("SignedDocuments", schema).IsMultiTenant()`,
`TenantId(64)`, the column lengths (StorageKey 1024, Sha256 64, FileName 260, ContentType 128,
UploadedByName 200), the unique index `(TenantId, DocumentType, DocumentId)`, `IsDeleted` default
false, and the **named** `HasQueryFilter("SoftDelete", x => !x.IsDeleted)`. Because the entity lives
in the framework assembly it is **not** picked up by `ApplyConfigurationsFromAssembly` — each module
calls this extension explicitly (same mechanism as `ConfigureNumberSequences`).

### 3. Shared mechanics — `BuildingBlocks/Persistence/SignedDocuments/SignedDocumentStore.cs`
New **static** helper (mirrors the static `SequenceAllocator.cs` — **no DI registration**). Callers
pass their own dependencies:
- `UploadAsync(BaseDbContext db, IStorageService storage, ICurrentUser currentUser, int documentType, Guid documentId, byte[] content, string fileName, string contentType, string? uploadedByNameOverride, CancellationToken ct)` → `SignedDocument`.
  Does: hash, tenant resolution (`currentUser.GetTenant() ?? db.TenantInfo?.Identifier ?? ""`),
  `storage.UploadAsync<SignedDocument>(…, StoragePaths.Protected(tenant, documentType-as-string), …)`,
  find-existing-by-`(DocumentType, DocumentId)` → `Create` or `Replace`, `SaveChangesAsync` with
  orphan-rollback on failure + best-effort old-blob cleanup on success — lifted verbatim from
  the AssetRegister `UploadSignedDocumentCommandHandler`.
- `GetAsync(BaseDbContext db, int documentType, Guid documentId, CancellationToken ct)` → `SignedDocument?` (AsNoTracking).
- `DownloadAsync(BaseDbContext db, IStorageService storage, ILogger logger, int documentType, Guid documentId, CancellationToken ct)` → `SignedDocumentFile?` (small record `Content/ContentType/FileName`), with the SHA-256 integrity re-verify from the AssetRegister `DownloadSignedDocumentQueryHandler`.

**Structural change:** add a `ProjectReference` from `Persistence` → `Storage` (currently siblings;
Storage does not reference Persistence, so this stays acyclic). Needed for `IStorageService` /
`StoragePaths` / `FileUploadRequest`. `ICurrentUser` (Core) and `BaseDbContext` (Persistence) are
already visible.

> Note — sanctioned **BuildingBlocks** change (`.claude/rules/buildingblocks-protection.md`): user
> approved this scope; adds mirror the existing `NumberSequence` precedent.

### 4. Per-module rewiring (×3: AssetRegister, ProcurementAcquisition, BudgetDisbursement)
For each module:
- **Delete** `Domain/SignedDocuments/SignedDocument.cs` and `Data/Configurations/SignedDocumentConfiguration.cs`.
- **DbContext**: point the `DbSet<SignedDocument>` `using` at `AMIS.Framework.Core.Domain`; add
  `modelBuilder.ConfigureSignedDocuments(<Module>ModuleConstants.SchemaName);` in `OnModelCreating`.
- **Upload handler**: keep the module-local `EnsureDocumentSignedAsync(...)` switch; replace the
  mechanical body with a single `SignedDocumentStore.UploadAsync(db, storageService, currentUser,
  (int)command.DocumentType, …, uploadedByNameOverride, ct)` call, then map the returned entity to the
  module DTO (`(ModuleEnum)entity.DocumentType`). Procurement passes its `SignatoryResolver` result as
  `uploadedByNameOverride`; the other two pass `null` (defaults to `currentUser.Name` inside the store).
- **Get / Download handlers**: delegate to `SignedDocumentStore.GetAsync` / `.DownloadAsync` and map
  to the module DTO / `SignedDocumentFileDto`.
- **Contracts, endpoints, permissions, routes, Blazor clients: unchanged** — the enum, DTOs,
  commands/queries, `/signed-documents` routes and `View`/`Upload` permissions all stay per-module, so
  the public API and the three Blazor `ApiClient` files are untouched.
- **Read-side consumers (~17 handlers)** that filter `db.SignedDocuments.Where/Any(x => x.DocumentType == SomeEnum.X …)`
  must become `x.DocumentType == (int)SomeEnum.X` (EF translates the cast fine).
  Find all with: `grep -rn "\.DocumentType ==" src/Modules` (covers Search*/Get* handlers across all 3 modules).

### 5. Migrations — one per context (do **not** delete snapshots)
Per the migrations-discovery quirk (each context is found only via its existing snapshot), add a
**delta** migration per context — never squash:
```
dotnet ef migrations add SharedSignedDocument \
  --project src/Host/Migrations.PostgreSQL --context AssetRegisterDbContext --output-dir AssetRegister
```
(repeat for `ProcurementDbContext` → `ProcurementAcquisition`, `BudgetDisbursementDbContext` → `BudgetDisbursement`).
- **AssetRegister**: real delta — alter `TenantId` 50→64, add `IsDeleted`/`DeletedOnUtc`/`DeletedBy`.
- **Procurement / Budget**: SQL should be a no-op (schema already matches); the migration mainly re-syncs
  the snapshot's moved CLR type. **Inspect each generated migration** and confirm empty/annotation-only
  `Up`/`Down` before keeping.

### 6. Tests
- Update namespace in `src/Tests/ProcurementAcquisition.Tests/Domain/SignedDocumentDomainTests.cs`
  to the shared `AMIS.Framework.Core.Domain.SignedDocument` (and adapt `int documentType`). Consider
  moving it to a framework/Generic test project since the type is now shared.
- Validator tests are unaffected (validators stay per-module).

## Files at a glance
- **New:** `Core/Domain/SignedDocument.cs`, `Persistence/SignedDocuments/SignedDocumentConfiguration.cs`,
  `Persistence/SignedDocuments/SignedDocumentStore.cs`.
- **Deleted (×3):** each module's `Domain/SignedDocuments/SignedDocument.cs` + `Data/Configurations/SignedDocumentConfiguration.cs`.
- **Edited:** `Persistence.csproj` (+Storage ref); 3 DbContexts; 3 Upload + 3 Get + 3 Download handlers;
  ~17 read-side query handlers; 3 new migrations; ProcAcq domain test.
- **Untouched:** all `*Contracts` (enums/DTOs/commands/queries), all endpoints/permissions/routes,
  all 3 Blazor `ApiClient` files.

## Verification
1. `dotnet build src/AMIS.Framework.slnx` — must be **0 warnings** (also proves no query-filter-collision
   at model build for all three `.IsMultiTenant()` + named-filter contexts).
2. `dotnet test src/AMIS.Framework.slnx` — all pass (incl. Architecture.Tests for module-boundary rules).
3. Duplicate-endpoint-name guard (api-conventions.md): `grep -rh "\.WithName(" src/Modules …` shows no
   new collisions (endpoint names unchanged, so this should stay clean).
4. Apply migrations (`dotnet ef database update` per context) against a dev DB; confirm AssetRegister
   gains the 3 columns + widened `TenantId`, and Procurement/Budget apply cleanly with no data loss.
5. End-to-end per module (run via Aspire / `/run`): for one document type in each module —
   **upload** a signed PDF (verify it lands under `uploads/protected/{tenant}/{docType}/…` and the row
   persists), **get** metadata, **download** (verify the integrity check passes), then **re-upload**
   (verify replace works and the old blob is cleaned up). Confirm a Search list still shows the
   "has signed copy" flag (proves the `(int)` read-side cast works).
