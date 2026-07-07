# Plan: Add Asset Photo Thumbnails to ICS/PAR Detail Item Cards (MAUI)

> Status: **planned, not started**
> Scope: AssetRegister backend (HasImage flag on accountability lines) + AMIS.Maui (ICS/PAR detail item cards)

## Context

The MAUI ICS Detail page (and its twin, PAR Detail) lists accountability line items as cards showing property no, description, cost, SE/PPE badge, EUL, and acquisition date — but no photo. Goal: each item card shows the asset's image so field staff can eyeball-match items, just like the Physical Count checklist already does.

The app already has the complete infrastructure for this, proven on `PhysicalCountChecklistPage`:

- **Authenticated thumbnail streaming:** `IApiClient.GetAssetImageStreamAsync(assetRegistryId, "thumb", ct)` → `GET /api/v1/asset-register/assets/{id}/image?variant=thumb` (bearer token attached by `AuthenticatedHttpHandler`).
- **Lazy per-row loading:** `AssetImageSourceConverter` in `src/Host/AMIS.Maui/Converters/ValueConverters.cs` (currently hard-typed to `PhysicalCountChecklistItemDto`).
- **Thumbnail tile UI:** 52×52 rounded tile with a 📦 glyph placeholder tinted by `KindToColorConverter` (PPE→blue, SE→teal), in `PhysicalCountChecklistPage.xaml` lines ~218–234.

**Permission check (verified — no blocker):** the image endpoint requires `AssetRegisterPermissions.Assets.View`, which is registered `IsBasic: true` in `AssetRegisterModule.cs:29`. `PermissionConstants.Basic` seeds it into the Basic role assigned to all users, so employees viewing their own ICS can already fetch images (this is why Scan and Physical Count thumbnails work today).

**Two gaps to close:**

1. Backend: `PropertyAccountabilityLineDto` carries `AssetRegistryId` but no `HasImage` flag (needed so the client doesn't fire image requests for photo-less assets).
2. MAUI: `ICSItemDto`/`PARItemDto` drop `AssetRegistryId` during mapping and have no `HasImage`.

## Backend changes (3 files)

### 1. `src/Modules/AssetRegister/Modules.AssetRegister.Contracts/v1/Accountability/AccountabilityContracts.cs`

Add `bool HasImage = false` as the last positional parameter of `PropertyAccountabilityLineDto`. The default keeps all other mapper callers (Issue/Accept/Cancel/Update/Renew/Return command handlers) compiling unchanged — they return `false`, which is fine for write-op responses.

### 2. `src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Accountability/AccountabilityMapper.cs`

- `ToDto(PropertyAccountabilityLine l)` → add optional `bool hasImage = false`, pass through.
- `ToDto(PropertyAccountability a)` → add optional `IReadOnlySet<Guid>? assetsWithImage = null`; map each line with `assetsWithImage?.Contains(l.AssetRegistryId) == true`.

### 3. `src/Modules/AssetRegister/Modules.AssetRegister/Features/v1/Accountability/GetAccountability/GetAccountabilityQueryHandler.cs`

After loading the accountability, one set-based lookup (mirrors the presence-flag-only pattern in `GetPhysicalCountChecklistQueryHandler.cs:57` — no image bytes in the payload):

```csharp
var assetIds = entity.Lines.Select(l => l.AssetRegistryId).Distinct().ToList();
var withImage = (await db.AssetRegistries.AsNoTracking()
    .Where(a => assetIds.Contains(a.Id) && a.ImageUrl != null)
    .Select(a => a.Id)
    .ToListAsync(cancellationToken)).ToHashSet();
return AccountabilityMapper.ToDto(entity, withImage);
```

This single change covers both the officer-facing `GetAccountability` and the mobile self-service `GetMyAccountabilityDetail` (which delegates to this query). Blazor is unaffected (extra JSON field; its client DTOs deserialize by name).

## MAUI changes (5 files)

### 4. `src/Host/AMIS.Maui/Services/IApiClient.cs`

- Add a tiny interface next to the DTOs so one converter serves all image-bearing rows:

  ```csharp
  public interface IAssetImageRow { Guid AssetRegistryId { get; } bool HasImage { get; } }
  ```

- Append `Guid AssetRegistryId` + `bool HasImage = false` to `ICSItemDto` and `PARItemDto`; declare both (and the existing `PhysicalCountChecklistItemDto`) as `: IAssetImageRow` (positional record properties satisfy it automatically).

### 5. `src/Host/AMIS.Maui/Services/ApiClient.cs`

- `ArAccountabilityLine` (~line 344): add `bool HasImage = false` (tolerates older servers; `GetFromJsonAsync` uses Web defaults, case-insensitive).
- `GetICSByIdAsync` / `GetPARByIdAsync` (~lines 43–76): pass `l.AssetRegistryId` and `l.HasImage` into the item DTOs.

### 6. `src/Host/AMIS.Maui/Converters/ValueConverters.cs`

In `AssetImageSourceConverter.Convert`, replace the hard type check `value is not PhysicalCountChecklistItemDto item` with `value is not IAssetImageRow item`. No other logic changes; Physical Count checklist keeps working as-is.

### 7 & 8. `ICSDetailPage.xaml` / `PARDetailPage.xaml` (`src/Host/AMIS.Maui/Features/Inventory/`)

In each item `DataTemplate`, wrap the existing card content in `Grid ColumnDefinitions="Auto,*" ColumnSpacing="12"`:

- **Column 0:** the same 52×52 thumbnail tile as the checklist — `Border` with `StrokeShape="RoundRectangle 12"`, `KindToColorConverter` background keyed on `AssetType`, 📦 glyph (`&#x1F4E6;`) when `!HasImage`, and `Image Aspect="AspectFill"` with `Source="{Binding ., Converter={StaticResource AssetImageSourceConverter}}"` when `HasImage`.
- **Column 1:** the existing `VerticalStackLayout` card content, unchanged.

Tile always visible (glyph placeholder when no photo) — consistent with the checklist design. All converters are already app-wide `StaticResource`s in `Styles.xaml`, no registration needed. Keep files UTF-8 (₱ glyph present).

## Verification

1. `dotnet build src/AMIS.Framework.slnx` — must pass with 0 warnings; `dotnet test src/AMIS.Framework.slnx`.
2. Run the API (Aspire) + MAUI Windows app. Ensure at least one asset on the test ICS has a photo (upload via Blazor Asset Register → photo dialog if needed).
3. Open My Inventory → an ICS detail → items with photos show thumbnails, items without show the tinted 📦 tile (SE=teal, PPE=blue). Repeat for a PAR detail.
4. Regression: open a Physical Count checklist — thumbnails still load (converter change).
5. Offline/failure path: thumbnails degrade to the placeholder tile (handled inside the converter), no crash.