# Asset Image Storage — Implementation Plan

> Status: **planned, not started.** Follow-up from the 2026-07-07 performance pass.
> Owner: TBD. See also `project_perf_security_deferred` in agent memory.

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

## Phase 1 — Quick win: keep the image out of list projections (low risk, do first)

Restores the column-narrowing optimization and stops the multi-MB-per-row payload **without** changing
how images are stored. Contained to two projections + one new endpoint + UI `<img>` bindings.

- [ ] Remove `ImageUrl` from `SearchAssetsQueryHandler`'s DB `.Select(...)` projection and drop it from
      the `AssetRegistrySummaryDto` mapping (leave the DTO field default `null`, or remove the field if no
      other consumer needs it — check first).
- [ ] Remove `a.ImageUrl` from `GetPhysicalCountChecklistQueryHandler`'s projection (line 56) and the
      checklist DTO.
- [ ] Add a lazy image endpoint: `GET /api/v1/asset-register/assets/{id}/image` that returns the image
      inline (`Results.File(bytes, contentType)`), decoding the stored base64 for now. Gate with
      `AssetRegisterPermissions.Assets.View`. Natural home: a new `GetAssetImage` feature slice next to
      `UpdateAssetImage` (`.../Features/v1/Assets/`); register in `AssetRegisterModule.MapEndpoints`.
- [ ] Blazor asset-search page: bind the row `<img src>` to `.../assets/{id}/image` with
      `loading="lazy"` so only on-screen rows fetch (and the browser caches by URL). Only render `<img>`
      when the asset actually has an image (add a cheap `bool HasImage` to the summary DTO instead of the
      blob, so the UI knows whether to show the tag).
- [ ] MAUI physical-count checklist: same — bind the thumbnail to the image endpoint, respect the
      client's image caching rules in `.claude/rules/maui.md` (resize to display size, `CachingStrategy`).
- [ ] Build + verify: `dotnet build src/AMIS.Framework.slnx`; run `AssetRegister.Tests`; confirm via EF
      logging that `SearchAssets` no longer selects the `ImageUrl` column; confirm the search page issues
      one image request per visible row (browser network tab), cached on re-render.

**Acceptance:** asset search and the count checklist no longer transfer image bytes in the list payload;
images load per visible row and are browser-cached.

---

## Phase 2 — Proper fix: file storage + thumbnail (medium effort)

- [ ] Add `ThumbnailUrl` (nullable) to `AssetRegistry` + config; keep `ImageUrl` as the **key/URL** of the
      full image (drop `HasMaxLength(10_000_000)` back to a normal URL length, e.g. 500).
- [ ] Change the upload flow (`UpdateAssetImageCommand` / `UpdateAssetImageCommandHandler` /
      `AssetRegistry.SetImage`): accept the raw image bytes (or a data URL), **write the full image via
      `IStorageService.UploadAsync`** under `uploads/asset-images/{tenant}/…`, generate a ~100×100
      thumbnail (server-side resize), store both, and set `ImageUrl`/`ThumbnailUrl` to the returned keys.
      Reuse the avatar pattern in `UserProfileService`.
- [ ] Update `UpdateAssetImageCommandValidator`: replace the 10 MB base64 cap with a real image-size limit
      + allowed content types (jpeg/png/webp).
- [ ] Lists/checklist use `ThumbnailUrl`; detail / property card use `ImageUrl` (full). The Phase-1 image
      endpoint now redirects to / serves the stored file instead of decoding base64.
- [ ] Confirm asset images are served from the protected/appropriate `uploads` prefix and that static
      serving isn't blanket-blocked (see `project_signed_doc_storage` note — Storage↔Web prefix sync).

**Acceptance:** `AssetRegistry.ImageUrl` column holds a short key, not base64; images and thumbnails are
static files; the DB table and every query touching it are lightweight.

---

## Phase 3 — Migrate existing data + guardrails

- [ ] One-time background job (Hangfire): for every asset whose `ImageUrl` is a `data:*;base64,` value,
      decode → `UploadAsync` full + thumbnail → replace `ImageUrl`/`ThumbnailUrl` with the keys. Idempotent,
      batched, resumable.
- [ ] EF migration in `src/Host/Migrations.PostgreSQL/AssetRegister/` for the `ThumbnailUrl` column and the
      `ImageUrl` length change. (Data backfill runs via the job above, not in the migration.)
- [ ] Guardrail: resize client-side to ≤ ~200 KB before upload (Blazor + MAUI capture paths); the current
      10 MB cap is far larger than a property photo needs.
- [ ] Remove the temporary base64-decoding branch from the image endpoint once migration completes.

**Acceptance:** no base64 remains in `AssetRegistry.ImageUrl`; uploads are size-limited; the image endpoint
serves files only.

---

## Notes / risks
- Do Phase 1 alone if time-boxed — it captures most of the performance benefit at low risk.
- `AssetRegistry` is `.IsMultiTenant()`; keep image storage keys tenant-scoped (`uploads/{tenant}/…`).
- Keep report PDFs (which embed the image) working — they read `ImageUrl`; after Phase 2 they must fetch
  the file/thumbnail rather than inline base64. Check `PropertyCardDto` and the QuestPDF/FastReport asset
  reports that render the photo.
