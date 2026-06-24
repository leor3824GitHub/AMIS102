# Identify an asset by photo — on-device image-embedding search

> Implementation plan. Status: approved, not yet started.

## Context

Today the MAUI app finds an asset only via a **readable identifier**: ZXing barcode/QR, live
on-device OCR of the printed PropertyNo
([ScanViewModel.cs](src/Host/AMIS.Maui/Features/Scan/ScanViewModel.cs) +
[PropertyNumberExtractor.cs](src/Host/AMIS.Maui/Services/PropertyNumberExtractor.cs)),
manual entry, or serial search. The goal is to identify an asset from a photo when the sticker is
missing or destroyed and there's nothing to read.

Chosen approach (per discussion): capture **multiple reference photos per property**, and match a
live photo against them using an **on-device image-embedding model** (CLIP/MobileCLIP via ONNX
Runtime). Everything runs on the phone — no photos leave the device (key for government data), works
offline, and accuracy improves as more reference photos are added per asset.

### What this can and can't do — read this first

Image-embedding search returns the **visually-nearest assets**, ranked. It is a candidate-narrowing
assist, not a guaranteed unique hit:

- **Works well:** distinctive assets, or any asset whose reference photos capture something
  identifying (asset tag, serial plate, a partial sticker, wear/damage, a unique configuration).
- **Inherent limit:** identical fungible items (twenty identical chairs) have near-identical
  embeddings, so they come back as a *cluster*. The user taps the right one from a short ranked list
  — exactly like the existing serial-search disambiguation
  (`PickAssetBySerialAsync` action sheet, [ScanViewModel.cs](src/Host/AMIS.Maui/Features/Scan/ScanViewModel.cs#L96)).
  Scoping candidates by location/custodian shrinks the cluster.
- **Consistency rule:** reference and query embeddings must come from the **same encoder + version**,
  or cosine similarity is meaningless. Pin a model version and store it with every embedding.

There is no ML/CV code and no asset photos in the repo today — this is greenfield.

> Note on "local SLM": a full on-device multimodal LLM (Phi-vision / Qwen-VL class) is multi-GB and
> slow on low-end field devices. The **embedding encoder** chosen here is the right-sized local model
> for image search (~hundreds of MB) and is the recommended engine.

## Architecture

```
Capture reference photos (on asset detail)        Identify (new scan mode)
  → embed each on-device (ONNX)                      → embed live photo on-device (same model)
  → upload {photo, embedding, modelVersion}          → cosine-similarity vs cached reference
  → backend persists AssetPhoto                         embeddings (scoped to location/custodian)
  → other devices sync embeddings into SQLite        → top-N ranked shortlist → user confirms
```

The backend only **stores and serves** photos + embedding vectors — it runs no ML model. The device
owns both embedding (reference at capture, query at identify) and matching. Reference embeddings are
synced into the existing SQLite cache so identify works offline against a bounded candidate set.

## Build phases

### Phase 0 — Reference photos + embeddings (backend)
- New child entity `AssetPhoto` under `Modules.AssetRegister` (`Domain/Assets/AssetPhoto.cs`): `Id`,
  `AssetRegistryId` (FK to `AssetRegistry` in `Domain/Assets/AssetRegistry.cs`), `StorageKey`,
  `EmbeddingVector` (serialized `float[]`, e.g. `byte[]`/jsonb), `EmbeddingModelVersion`, audit fields.
  EF config under `Data/Configurations/`; delta migration in `Migrations.PostgreSQL` (**add** a
  migration — do not delete snapshots; see migrations-discovery note).
- Contracts ([AssetContracts.cs](src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/Assets/AssetContracts.cs)):
  `AssetPhotoDto`, `UploadAssetPhotoRequest` (photo + embedding + modelVersion), and a lightweight
  `AssetEmbeddingDto` (assetId, photoId, embedding, modelVersion) for sync.
- Upload feature slice `Features/v1/Assets/UploadAssetPhoto/` (command/handler/validator/endpoint)
  following the `SignedDocuments` upload pattern:
  `IStorageService.UploadAsync<AssetPhoto>(req, FileType.Image, folderPath, ct)`
  ([IStorageService.cs](src/BuildingBlocks/Storage/Services/IStorageService.cs)); persist the
  `AssetPhoto` row with the embedding. Endpoint `POST /api/v1/asset-register/assets/{id}/photos`,
  prefixed `WithName` per the endpoint-uniqueness rule, `.RequirePermission(...Assets.Update)`.
- Sync endpoint `Features/v1/Assets/GetAssetEmbeddings/`:
  `GET /api/v1/asset-register/assets/embeddings` scoped by `currentCustodianId`/`currentLocationId`,
  returns `AssetEmbeddingDto[]` for the device cache. `.RequirePermission(...Assets.View)`.

### Phase 1 — On-device embedding + match (MAUI, the core deliverable)
- Bundle a CLIP/MobileCLIP image encoder as ONNX (e.g. MobileCLIP-S0). Add `Microsoft.ML.OnnxRuntime`;
  ship the `.onnx` as a `MauiAsset` (flag app-size growth) or first-run download with version pinning.
- `IImageEmbeddingService` (mirrors the `IOcrService` service pattern): `float[] Embed(Stream image)`
  + `ModelVersion`. Cross-platform ONNX Runtime impl; graceful unavailable-state on Windows w/o
  support. Register in `MauiProgram.cs`.
- Extend the SQLite cache (`ICacheService`) with a reference-embeddings table; add a sync method that
  pulls `GetAssetEmbeddings` for the current employee's accountable scope (consistent with the
  existing per-employee ICS/PAR cache strategy in `.claude/rules/maui.md`).
- New "Identify by photo" scan mode in the scan flow: capture (camera frame / `MediaPicker`) → embed
  on-device → cosine-similarity vs cached embeddings → top-N ranked candidates → render in the
  existing action-sheet shortlist pattern → `AssetDetailPage` on selection. Keep manual entry visible
  as the always-available fallback (MAUI rule). Marshal UI updates with `MainThread`; embed off the UI
  thread but per MAUI rules avoid `Task.Run` for I/O.
- Reference-photo capture UX on asset detail: take several photos per asset → embed each → upload
  `{photo, embedding, modelVersion}` via a new `IApiClient` method.

### Phase 1b — Optional candidate pre-filter (nice-to-have)
- Add a `CurrentLocationId` filter to `SearchAssetsQuery`/handler/endpoint (today it filters
  `AssetType`, `LifecycleState`, `CurrentCustodianId` only —
  [SearchAssetsQueryHandler.cs](src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Assets/SearchAssets/SearchAssetsQueryHandler.cs#L35-L38),
  [SearchAssetsEndpoint.cs](src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Assets/SearchAssets/SearchAssetsEndpoint.cs))
  so the embedding candidate set can be scoped by location, tightening identical-item clusters.

## Out of scope (note only)
A cloud Claude-vision fallback for maximum accuracy on hard cases is possible later, but it ships
photos to an external service and needs org egress approval — explicitly **not** part of this
local-first plan.

## Verification

- Backend: `dotnet build src/AMIS.Framework.slnx` (0 warnings) + `dotnet test`; handler/validator
  tests for `UploadAssetPhoto`, the embeddings sync, and the `CurrentLocationId` filter; check
  `WithName` uniqueness for the new endpoints (duplicate names 500 every request).
- Phase 0: upload several photos to one asset; confirm photos persist via `IStorageService` and the
  `AssetPhoto` rows carry embedding + model version; confirm the embeddings endpoint returns them.
- Phase 1 end-to-end on a **real low-end Android device** (HTTPS:7030 per project config, not the
  emulator — MAUI perf rule): capture reference photos for a few distinct assets, then identify by
  photo offline and confirm the correct asset ranks in the top results and selection opens the right
  detail. Measure embed latency + app-size impact on the device.
- **Honesty acceptance check:** for identical unmarked items, confirm the result is a short ranked
  cluster the user disambiguates — *not* a single confident wrong answer. Confirm match quality
  improves as more reference photos are added per property.