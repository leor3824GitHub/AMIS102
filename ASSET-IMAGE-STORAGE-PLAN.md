# Asset Image Storage — Implementation Plan

> Status: **✅ complete — all 3 phases delivered 2026-07-07.** Follow-up from the 2026-07-07 performance pass.
> See also `project_perf_security_deferred` in agent memory. Retained as a record of the design; the
> per-phase checklists below describe shipped code.

## Context / problem

Asset photos are stored as **base64 data URLs inside a database column**
(`AssetRegistry.ImageUrl`, `data:image/jpeg;base64,…`), capped at **10 MB** per
`UpdateAssetImageCommandValidator` (`MaxImageUrlLength = 10_000_000`, ~7.6 MB image). That blob is now
pulled into **paginated list projections**, so lists ship multi-MB base64 strings per row that the browser
cannot cache like a real image file.

Offending list projections (the full image is selected per row):
- `SearchAssetsQueryHandler` → `AssetRegistrySummaryDto.ImageUrl`
  (`src/Modules/AssetRegister/.../Features/v1/Assets/SearchAssets/SearchAssetsQueryHandler.cs`,
  DTO at `.../Contracts/v1/Assets/AssetContracts.cs:54`).
- `GetPhysicalCountChecklistQueryHandler` → checklist DTO `ImageUrl`
  (`.../Features/v1/Counting/GetPhysicalCountChecklist/GetPhysicalCountChecklistQueryHandler.cs:56`,
  DTO field at `.../Contracts/v1/Counting/CountingContracts.cs:131`). **This one loads on the MAUI mobile
  client over field data connections — highest-impact.**

Single-image consumers (fine, leave as-is): `AssetRegistryDto` (detail), `PropertyCardDto` (one asset).

## Target architecture

Store images as **files** via the existing `IStorageService` / `LocalStorageService`
(`src/BuildingBlocks/Storage/`, already used for avatars in `UserProfileService` and tenant logos in
`TenantThemeService`), keep only a **relative URL/key** in `ImageUrl` (its original intent), generate a
small **thumbnail** for lists, and never select the image bytes into a list projection.

---

## Phase 1 — Quick win: keep the image out of list projections (low risk, do first) — ✅ DONE (2026-07-07)

Restores the column-narrowing optimization and stops the multi-MB-per-row payload **without** changing
how images are stored. Contained to two projections + one new endpoint + UI `<img>` bindings.

- [x] Remove `ImageUrl` from `SearchAssetsQueryHandler`'s DB `.Select(...)` projection and drop it from
      the `AssetRegistrySummaryDto` mapping. → Replaced the field with `bool HasImage`, projected as
      `HasImage = a.ImageUrl != null` (`IS NOT NULL` — never transfers the blob). Same for the Blazor
      client's copy of `AssetRegistrySummaryDto`. `GetMyAccountableAssets` reuses `SearchAssets`, so it's
      covered too.
- [x] Remove `a.ImageUrl` from `GetPhysicalCountChecklistQueryHandler`'s projection and the checklist DTO.
      → `PhysicalCountChecklistItemDto.ImageUrl` → `bool HasImage`; MAUI DTO + SQLite `CachedChecklistItem`
      updated to match.
- [x] Add a lazy image endpoint: `GET /api/v1/asset-register/assets/{id}/image` returning the image inline
      (`Results.File`), decoding the stored base64 for now, gated with `AssetRegisterPermissions.Assets.View`.
      → New `GetAssetImage` slice (Query/Handler/Endpoint) next to `UpdateAssetImage`; registered in
      `AssetRegisterModule.MapEndpoints`. Only the `ImageUrl` column is projected in the handler.
- [x] Blazor asset-search page: bind the row `<img src>` lazily; only render when `HasImage`. → Because the
      API is a **separate origin** and the endpoint is permission-gated, a browser `<img>` can't auth
      cross-origin. Added a same-origin, cookie-authenticated proxy `GET /bff/asset-image/{id}`
      (`AssetImageEndpoints`, mirrors `/bff/download`) that streams from the API with the circuit's bearer
      token; the `<img loading="lazy" src="/bff/asset-image/{id}">` is browser/URL-cacheable. `AssetPhotoDialog`
      now takes `HasImage`/`AssetId` and lazy-loads via the same proxy (still shows just-uploaded base64 directly).
- [x] MAUI physical-count checklist: bind the thumbnail to the image endpoint. → MAUI's HttpClient carries
      the bearer token but `ImageSource.FromUri` doesn't, so added `IApiClient.GetAssetImageStreamAsync` +
      `AssetImageSourceConverter` (resolves `IApiClient` from the MAUI DI container, streams bytes gated on
      `HasImage`, degrades to the placeholder tile on failure).
- [x] Build + verify: module, Blazor, MAUI (Windows TFM), and API host all build 0-errors;
      `AssetRegister.Tests` = 81 passed. (Note: resolved committed git-merge-conflict markers left in
      `AMIS.Blazor/build-number.txt` and `AMIS.Maui/build-number.txt` from merge `d6cef2d8`, which were
      breaking `[MSBuild]::Add` in `BuildVersioning.targets` — took the higher counter.)

**Acceptance:** ✅ asset search and the count checklist no longer transfer image bytes in the list payload;
images load per visible row (Blazor: browser-cached by URL via the proxy; MAUI: per-row authenticated fetch).

---

## Phase 2 — Proper fix: file storage + thumbnail (medium effort) — ✅ DONE (2026-07-07)

- [x] Add `ThumbnailUrl` (nullable) to `AssetRegistry` + config; keep `ImageUrl` as the **key** of the full
      image. → `ImageUrl`/`ThumbnailUrl` both `HasMaxLength(1024)` (were 10 MB).
- [x] Change the upload flow. → New `AssetImageStorage` service (`Data/Services`) normalizes the incoming
      data URL to JPEG, downscales the full image (≤1600px) + generates a ~200px thumbnail with
      **SixLabors.ImageSharp 2.1.11 (Apache-2.0, pure-managed/Linux-safe)**, writes both via
      `IStorageService.UploadAsync` under `uploads/protected/{tenant}/asset-images`, and stores the keys.
      `UpdateAssetImageCommandHandler` decodes → `SaveAsync` → `AssetRegistry.SetImage(imageKey, thumbnailKey)`
      (or `ClearImage()`), with orphan-blob cleanup on failure/replace. `AssetRegistry.SetImage` now takes the
      two keys; `ClearImage()` added.
- [x] Update `UpdateAssetImageCommandValidator`: replaced the 10 MB base64 cap with content-type
      (jpeg/png/webp/gif) + decoded-size (≤8 MB) checks.
- [x] Lists/checklist use `ThumbnailUrl`; detail / property card use the full image. → The image endpoint
      takes `?variant=thumb|full` and serves the stored file via `AssetImageStorage.LoadAsync` (thumb falls
      back to full for legacy rows). `AssetRegistryDto.ImageUrl` → `HasImage` (Blazor detail dialog now loads
      from the proxy); the two single-image consumers that embed bytes — the **Property Card PDF** and the
      **MAUI scan-detail** — have their `ImageUrl` resolved to a base64 data URL in their one query handler,
      so QuestPDF and MAUI are untouched. **Legacy base64 rows still render** (transparent decode path kept).
- [x] Asset images live under the **protected** prefix → reachable only via the permission-gated endpoint,
      never anonymous static content.

**Acceptance:** ✅ `AssetRegistry.ImageUrl` holds a short key (col shrunk to 1024); full image + thumbnail are
files under `uploads/protected/{tenant}/asset-images`; list/detail queries are lightweight.

---

## Phase 3 — Migrate existing data + guardrails — ✅ DONE (trimmed; 2026-07-07)

- [x] ~~One-time Hangfire backfill job~~ **Skipped by decision** — dev data is disposable
      (`development-phase-priorities` memory), so instead of a resumable base64→file job, the read paths keep a
      transparent base64-decode fallback (`AssetImageStorage.LoadAsync`/`ToDataUrlAsync`) so any pre-migration
      row still renders; re-uploading a photo migrates it to files. Revisit only if a prod dataset needs it.
- [x] EF migration — originally `20260707122931_AssetImageFileStorage`: `AlterColumn` `ImageUrl` 10 MB→1024 +
      `AddColumn ThumbnailUrl` (hand-corrected from EF's `AddColumn`, since `ImageUrl` predated it).
      **That file no longer exists** — the later migration squash folded it into
      `Migrations.PostgreSQL/AssetRegister/20260716120453_InitialCreate.cs`, where both `ImageUrl` and
      `ThumbnailUrl` are created directly as `character varying(1024)`.
- [x] Guardrail: Blazor capture path resizes client-side via `IBrowserFile.RequestImageFileAsync("image/jpeg",
      1280, 1280)` before upload (multi-MB phone photo → display-quality JPEG). **MAUI has no asset-photo upload
      path** (it only displays), so nothing to guard there.
- [~] Because the backfill was skipped, the base64-decode fallback is intentionally **kept** (removing it would
      break any legacy row). It's a cheap, self-limiting branch.

**Acceptance:** ✅ new uploads are files + size-limited; the column is a short key; legacy base64 rows still
render (fallback retained by design since backfill was skipped).

---

## Notes / risks
- Do Phase 1 alone if time-boxed — it captures most of the performance benefit at low risk.
- `AssetRegistry` is `.IsMultiTenant()`; keep image storage keys tenant-scoped (`uploads/{tenant}/…`).
- Keep report PDFs (which embed the image) working — they read `ImageUrl`; after Phase 2 they must fetch
  the file/thumbnail rather than inline base64. Check `PropertyCardDto` and the QuestPDF/FastReport asset
  reports that render the photo.
