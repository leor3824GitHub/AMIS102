# MAUI Implementation Plan

## .NET MAUI Mobile/Desktop Client for AMIS

> Second client UI alongside `Playground.Blazor`, consuming the same existing REST API.
> Targets: **Android · iOS · Windows**

---

## 1. Context & Decisions

| Decision        | Choice                                                                                            | Rationale                                                                    |
| --------------- | ------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Client type     | Additional consumer (not a BFF)                                                                   | API already exists; no new backend architecture needed                       |
| Auth flow       | Direct `POST /api/v1/identity/token/issue`                                                        | Blazor uses BFF; MAUI calls API directly                                     |
| Tenant header   | `tenant: root` (configurable via `appsettings.json`)                                              | Required on every request                                                    |
| Token storage   | `SecureStorage` (Android/iOS) + Windows Credential Locker — same `ITokenStorageService` interface | Platform-safe credential storage                                             |
| Employee ID     | Resolved via new `/employees/me` endpoint                                                         | Avoids adding a cross-module FK claim inside the Identity JWT                |
| QR scan target  | `PropertyNo` (e.g. `SPLV-2026-01-0001`) from existing property stickers                           | No new QR label generation needed                                            |
| Scan library    | `ZXing.Net.MAUI`                                                                                  | Supports Android, iOS, and Windows camera                                    |
| Scan fallback   | Manual `PropertyNo` text entry on all platforms                                                   | Covers Windows (no dedicated camera UX), damaged stickers, and accessibility |
| Offline/caching | SQLite via `sqlite-net-pcl` for ICS/PAR list snapshots                                            | Allows field staff to view assigned inventory without connectivity           |
| MVVM library    | `CommunityToolkit.Mvvm`                                                                           | Source-generated commands/properties, minimal boilerplate                    |

---

## 2. Existing API Endpoints Used

| Purpose               | Method + Route                                                                             |
| --------------------- | ------------------------------------------------------------------------------------------ |
| Login                 | `POST /api/v1/identity/token/issue`                                                        |
| Refresh token         | `POST /api/v1/identity/token/refresh`                                                      |
| Current user profile  | `GET /api/v1/identity/profile`                                                             |
| ICS list for employee | `GET /api/v1/asset-management/inventory-custodian-slips?ReceivedByEmployeeId={id}`         |
| ICS detail            | `GET /api/v1/asset-management/inventory-custodian-slips/{id}`                              |
| PAR list for employee | `GET /api/v1/asset-management/property-acknowledgement-receipts?ReceivedByEmployeeId={id}` |
| PAR detail            | `GET /api/v1/asset-management/property-acknowledgement-receipts/{id}`                      |

---

## 3. New Backend Endpoints Required

### 3.1 `GET /api/v1/master-data/employees/me`

**Module:** `MasterData`
**Location:** `src/Modules/MasterData/Modules.MasterData/Features/v1/Employees/GetMyEmployee/`

| File                       | Purpose                                                                                                          |
| -------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `GetMyEmployeeQuery.cs`    | `IQuery<MyEmployeeDto>` — no parameters; reads `ClaimTypes.NameIdentifier` from current user                     |
| `GetMyEmployeeHandler.cs`  | Looks up `EmployeeProfile` where `ApplicationUserId == currentUserId` (or fallback: `Email == currentUserEmail`) |
| `GetMyEmployeeEndpoint.cs` | `GET /api/v1/master-data/employees/me`, `RequirePermission`                                                      |

```csharp
// Response DTO
public sealed record MyEmployeeDto(
    Guid EmployeeId,
    string FullName,
    string? Department,
    string? Position);
```

**MAUI usage:** Called once after login; result cached in `AuthStateService` and in SQLite. Subsequent app launches read from SQLite cache and validate against API only if online.

---

### 3.2 `GET /api/v1/asset-management/tangible-inventory-items/by-property-no/{propertyNo}`

**Module:** `AssetManagement`
**Location:** `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/TangibleInventoryItems/GetByPropertyNo/`

| File                                              | Purpose                                                                                                  |
| ------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `GetTangibleInventoryItemByPropertyNoQuery.cs`    | `IQuery<TangibleInventoryItemDetailDto>` — takes `string PropertyNo` route param                         |
| `GetTangibleInventoryItemByPropertyNoHandler.cs`  | Queries `TangibleInventoryItem` by `PropertyNo`; joins linked ICS or PAR document                        |
| `GetTangibleInventoryItemByPropertyNoEndpoint.cs` | `GET /api/v1/asset-management/tangible-inventory-items/by-property-no/{propertyNo}`, `RequirePermission` |

```csharp
// Response DTO
public sealed record TangibleInventoryItemDetailDto(
    Guid Id,
    string PropertyNo,
    string ItemName,
    string? Description,
    decimal UnitCost,
    string AssetType,          // "SE" or "PPE"
    bool IsIssued,
    string? LinkedDocumentType,  // "ICS" or "PAR"
    string? LinkedDocumentNo,
    Guid? LinkedDocumentId);
```

**Returns 404** when `PropertyNo` not found (handler throws `NotFoundException`).

---

## 4. MAUI Project Structure

```
src/Playground/Playground.Maui/
├── Playground.Maui.csproj          # Targets net10.0-android, net10.0-ios, net10.0-windows10.0.19041.0
├── MauiProgram.cs                  # DI, HTTP, SQLite, Shell registration
├── AppShell.xaml / .cs             # Tab bar: Inventory | Scan | Profile
├── appsettings.json                # Api:BaseUrl, Api:TenantId
│
├── Services/
│   ├── ApiClientOptions.cs         # Bound from appsettings
│   ├── ITokenStorageService.cs
│   ├── TokenStorageService.cs      # SecureStorage (Android/iOS) + PasswordVault (Windows)
│   ├── AuthStateService.cs         # In-memory: current user profile + EmployeeId
│   ├── AuthenticatedHttpHandler.cs # DelegatingHandler: Bearer + tenant + 401 retry/refresh
│   └── CacheService.cs             # SQLite read/write helpers (see §6 Caching)
│
├── Features/
│   ├── Auth/
│   │   ├── LoginPage.xaml / .cs
│   │   └── LoginViewModel.cs
│   │
│   ├── Profile/
│   │   ├── ProfilePage.xaml / .cs
│   │   └── ProfileViewModel.cs
│   │
│   ├── Inventory/
│   │   ├── InventoryPage.xaml / .cs      # Tabbed: ICS / PAR
│   │   ├── InventoryViewModel.cs
│   │   ├── ICSDetailPage.xaml / .cs
│   │   ├── ICSDetailViewModel.cs
│   │   ├── PARDetailPage.xaml / .cs
│   │   └── PARDetailViewModel.cs
│   │
│   ├── Scan/
│   │   ├── ScanPage.xaml / .cs           # Camera + manual entry (see §7)
│   │   └── ScanViewModel.cs
│   │
│   └── Asset/
│       ├── AssetDetailPage.xaml / .cs
│       └── AssetDetailViewModel.cs
│
└── Data/
    ├── LocalDb.cs                  # SQLite connection factory
    └── Models/
        ├── CachedEmployeeProfile.cs
        ├── CachedICS.cs
        └── CachedPAR.cs
```

---

## 5. Implementation Phases

### Phase 1 — Backend: `/employees/me` _(MasterData)_

1. Add `GetMyEmployee` vertical slice (Query + Handler + Endpoint) in `MasterData` module.
2. Handler reads `ClaimTypes.NameIdentifier` from `ICurrentUser`, queries `EmployeeProfile` table.
3. Follow existing employee feature patterns; add permission to `MasterDataPermissions.Employees`.
4. Build and verify: `dotnet build src/AMIS.Framework.slnx` — 0 warnings.

### Phase 2 — Backend: `/tangible-inventory-items/by-property-no/{propertyNo}` _(AssetManagement)_

5. Add `GetTangibleInventoryItemByPropertyNo` vertical slice in `AssetManagement` module.
6. Query joins `TangibleInventoryItem` → `ICSItem` → `InventoryCustodianSlip` (SE path) OR `PARItem` → `PropertyAcknowledgementReceipt` (PPE path) to resolve `LinkedDocumentNo`.
7. Return 404 via `NotFoundException` when property not found.
8. Build and verify.

### Phase 3 — MAUI Project Setup _(parallel with Phases 1–2)_

9. Create `Playground.Maui.csproj` with multi-platform targets.
10. Add to `src/AMIS.Framework.slnx`.
11. Install NuGet packages:
    - `CommunityToolkit.Maui` — UI controls, toasts
    - `CommunityToolkit.Mvvm` — source-generated MVVM
    - `ZXing.Net.MAUI` — camera barcode reader
    - `sqlite-net-pcl` — local SQLite cache
    - `SQLitePCLRaw.bundle_green` — SQLite native bindings
    - `Microsoft.Extensions.Http` — typed HTTP clients
12. Add `appsettings.json`:
    ```json
    {
      "Api": {
        "BaseUrl": "https://api.amis.example.com",
        "TenantId": "root"
      }
    }
    ```
13. Configure `MauiProgram.cs`: register services, typed `HttpClient`, SQLite, Shell.

### Phase 4 — Auth Infrastructure

14. Implement `ITokenStorageService`:
    - Android/iOS: `SecureStorage.SetAsync / GetAsync`
    - Windows: `PasswordVault` (WinRT) via `#if WINDOWS` conditional compilation
15. Implement `AuthenticatedHttpHandler` (DelegatingHandler):
    - Inject `Authorization: Bearer {accessToken}` + `tenant: {tenantId}` on every request.
    - On **401**: call `POST /api/v1/identity/token/refresh`, persist new tokens, retry request once.
    - On **second 401**: clear stored tokens, publish `SessionExpired` message (`WeakReferenceMessenger`), navigate to `LoginPage`.
16. Register as named `HttpClient` in `MauiProgram.cs`:
    ```csharp
    builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
        client.BaseAddress = new Uri(options.BaseUrl))
        .AddHttpMessageHandler<AuthenticatedHttpHandler>();
    ```

### Phase 5 — Login Screen

17. `LoginPage`: email field, password field, Login button, error label, loading overlay.
18. `LoginViewModel.LoginCommand`:
    - Validates inputs locally.
    - `POST /api/v1/identity/token/issue` with `{ email, password }` + `tenant` header (set directly here, before the handler intercepts).
    - On success: save tokens via `ITokenStorageService`, fetch profile, navigate to `AppShell`.
    - On failure: display `ProblemDetails.detail` in error label.
19. App startup (`App.xaml.cs`): check `ITokenStorageService.GetAccessTokenAsync()` — if token present and not expired, skip to `AppShell`; otherwise show `LoginPage`.

### Phase 6 — AppShell Navigation

20. Three main tabs: **My Inventory** | **Scan** | **Profile** (Home tab can be added later).
21. Register Shell routes:
    ```csharp
    Routing.RegisterRoute(nameof(ICSDetailPage), typeof(ICSDetailPage));
    Routing.RegisterRoute(nameof(PARDetailPage), typeof(PARDetailPage));
    Routing.RegisterRoute(nameof(AssetDetailPage), typeof(AssetDetailPage));
    ```

### Phase 7 — Profile Screen

22. `ProfileViewModel.LoadAsync`:
    - `GET /api/v1/identity/profile` → bind `FullName`, `Email`, `PhoneNumber`, `ImageUrl`.
    - Display role/permission info from `AuthStateService` (decoded from JWT claims).
23. Logout: `ITokenStorageService.ClearAsync()` → `CacheService.ClearAllAsync()` → navigate to `LoginPage`.

### Phase 8 — Inventory Screen (ICS + PAR) with Caching

24. `InventoryViewModel.LoadAsync` (see §6 Caching for cache strategy):
    - Resolve `EmployeeId` from `AuthStateService` (populated at login from `/employees/me`).
    - **If online:** fetch ICS list and PAR list from API → update UI → persist to SQLite cache.
    - **If offline:** read from SQLite cache → update UI with `[Cached]` badge.
    - Pull-to-refresh triggers API fetch regardless of cache age.
25. ICS list item shows: ICS No., date, status badge (`Active` / `Renewed` / `Expired`), item count, expiry date.
26. PAR list item shows: PAR No., date, PAR type (`New Purchase` / `Transfer`), item count.
27. `ICSDetailViewModel`: `GET /inventory-custodian-slips/{id}` — header fields + scrollable line item list (PropertyNo, ItemName, UnitCost, EUL years). Detail views are **not** cached (always online-only).
28. `PARDetailViewModel`: `GET /property-acknowledgement-receipts/{id}` — same pattern.

### Phase 9 — Scan Screen with Manual Fallback

29. `ScanPage` layout (two modes — see §7 Scan UX):
    - **Camera mode** (Android, iOS, Windows with webcam): `CameraBarcodeReaderView` from ZXing.Net.MAUI fills upper 60% of screen.
    - **Manual mode** (always available): `Entry` field + `Search` button in lower area. Visible at all times; on Windows this is the primary input method.
30. `ScanViewModel.OnBarcodeDetected(string rawValue)`:
    - Normalize value (trim whitespace, uppercase).
    - Debounce: ignore duplicate detections within 2 seconds.
    - Navigate: `await Shell.Current.GoToAsync($"{nameof(AssetDetailPage)}?PropertyNo={Uri.EscapeDataString(rawValue)}")`.
31. `ScanViewModel.SearchManualCommand(string propertyNo)`:
    - Trim and validate non-empty.
    - Same navigation call as camera decode path.
32. Camera permission: declared in `AndroidManifest.xml` (`android.permission.CAMERA`) and `Info.plist` (`NSCameraUsageDescription`).
33. On Windows: hide `CameraBarcodeReaderView` if no camera detected (check `DeviceInfo.Current.Platform`); show informational label "Use the search box below to look up a property number."

### Phase 10 — Asset Detail Screen

34. `AssetDetailViewModel` receives `PropertyNo` as a Shell query parameter.
35. `GET /api/v1/asset-management/tangible-inventory-items/by-property-no/{propertyNo}`.
36. Display:
    - **Property No.** (prominent, copyable on tap)
    - Item name and description
    - Unit cost (formatted as currency)
    - Asset type badge (`Semi-Expendable` / `PPE`)
    - Issuance status (`Issued` / `In Stock`)
    - Linked document: tappable link → navigate to `ICSDetailPage` or `PARDetailPage`
37. **Not found**: show friendly error ("Property not found. Check the number and try again.") with a Back button.

### Phase 11 — Polish & Permissions

38. Loading overlays on all async operations.
39. Pull-to-refresh on Inventory and Profile.
40. Toast messages for network errors (`CommunityToolkit.Maui.Alerts.Toast`).
41. 403 handling: show "You don't have permission to view this." — do not redirect to login.
42. Network connectivity check via `IConnectivity` before API calls; if offline and no cache, show "No internet connection. Showing cached data." or "No internet connection and no cached data available."
43. Android internet permission in `AndroidManifest.xml`.

---

## 6. Offline Caching Strategy

**Library:** `sqlite-net-pcl` with `SQLitePCLRaw.bundle_green`
**Location:** `FileSystem.AppDataDirectory/amis-cache.db`

### Cached Tables

```csharp
[Table("CachedEmployeeProfile")]
public class CachedEmployeeProfile
{
    [PrimaryKey] public string UserId { get; set; } = "";
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = "";
    public string? Department { get; set; }
    public string? Position { get; set; }
    public DateTimeOffset CachedAt { get; set; }
}

[Table("CachedICS")]
public class CachedICS
{
    [PrimaryKey] public string Id { get; set; } = "";   // Guid as string
    public string ICSNo { get; set; } = "";
    public string Date { get; set; } = "";              // ISO 8601 DateOnly
    public string Status { get; set; } = "";
    public string? ExpiresOn { get; set; }
    public int ItemCount { get; set; }
    public string EmployeeId { get; set; } = "";        // partition key
    public DateTimeOffset CachedAt { get; set; }
}

[Table("CachedPAR")]
public class CachedPAR
{
    [PrimaryKey] public string Id { get; set; } = "";
    public string PARNo { get; set; } = "";
    public string Date { get; set; } = "";
    public string PARType { get; set; } = "";
    public int ItemCount { get; set; }
    public string EmployeeId { get; set; } = "";
    public DateTimeOffset CachedAt { get; set; }
}
```

### Cache Strategy — Stale-While-Revalidate

```
On InventoryPage open:
  1. Read SQLite cache → show immediately (instant display, no spinner)
  2. If online → fetch from API in background → update UI + overwrite cache
  3. If online fetch fails → keep showing cached data + show "Showing cached data" banner
  4. Cache age displayed as "Last updated X minutes ago"
  5. Pull-to-refresh → force API fetch (show spinner, block UI)
```

### Cache Invalidation

| Trigger                                | Action                                                   |
| -------------------------------------- | -------------------------------------------------------- |
| Logout                                 | `CacheService.ClearAllAsync()` — wipe all tables         |
| Pull-to-refresh                        | API fetch → overwrite cache entries for current employee |
| App foreground (>30 min in background) | Background API refresh on next InventoryPage open        |

### `CacheService` Interface

```csharp
public interface ICacheService
{
    Task<List<CachedICS>> GetCachedICSAsync(Guid employeeId);
    Task UpsertICSAsync(IEnumerable<CachedICS> items);
    Task<List<CachedPAR>> GetCachedPARAsync(Guid employeeId);
    Task UpsertPARAsync(IEnumerable<CachedPAR> items);
    Task SaveEmployeeProfileAsync(CachedEmployeeProfile profile);
    Task<CachedEmployeeProfile?> GetEmployeeProfileAsync(string userId);
    Task ClearAllAsync();
}
```

---

## 7. Scan UX — Camera + Manual Entry

### Screen Layout (all platforms)

```
┌─────────────────────────────────────┐
│  [← Back]        Scan Asset         │
├─────────────────────────────────────┤
│                                     │
│   ┌─────────────────────────────┐   │
│   │                             │   │
│   │     Camera Viewfinder       │   │  ← Hidden on Windows if no webcam
│   │     (ZXing.Net.MAUI)        │   │
│   │                             │   │
│   └─────────────────────────────┘   │
│                                     │
│  ── or enter manually ──────────── │
│                                     │
│  ┌────────────────────┐  [Search]  │
│  │  Property No.      │            │
│  └────────────────────┘            │
│                                     │
│  e.g. SPLV-2026-01-0001            │
└─────────────────────────────────────┘
```

### Camera Mode (Android / iOS / Windows with webcam)

- `CameraBarcodeReaderView` targets: `QrCode`, `Code128`, `Code39`, `DataMatrix` (the formats typically used on government property stickers).
- Decodes continuously; first successful decode triggers navigation.
- 2-second debounce to avoid re-triggering on the same sticker.
- Torch/flashlight toggle button for low-light environments.
- Permission denied → hide viewfinder, show "Camera permission required. Please allow camera access in Settings." with a Settings button.

### Manual Entry Mode (all platforms)

- Always visible below camera viewfinder.
- `Entry` with `Keyboard.Default`, `ReturnCommand` wired to `SearchManualCommand`.
- `Search` button fires `SearchManualCommand`.
- Input validation: non-empty after trim.
- On Windows desktop: this is the **primary lookup method**; camera section shows "Scan not available on this device — use the search box below."

### `ScanViewModel` Logic

```csharp
[RelayCommand]
private async Task OnBarcodeDetected(string rawValue)
{
    if (_lastScanTime.HasValue &&
        (DateTimeOffset.UtcNow - _lastScanTime.Value).TotalSeconds < 2) return;
    _lastScanTime = DateTimeOffset.UtcNow;

    var propertyNo = rawValue.Trim().ToUpperInvariant();
    await NavigateToAssetDetail(propertyNo);
}

[RelayCommand]
private async Task SearchManual()
{
    var propertyNo = ManualPropertyNo?.Trim().ToUpperInvariant();
    if (string.IsNullOrEmpty(propertyNo)) return;
    await NavigateToAssetDetail(propertyNo);
}

private async Task NavigateToAssetDetail(string propertyNo) =>
    await Shell.Current.GoToAsync(
        $"{nameof(AssetDetailPage)}?{nameof(AssetDetailViewModel.PropertyNo)}={Uri.EscapeDataString(propertyNo)}");
```

---

## 8. File Checklist

### Backend (add to existing modules)

- [ ] `src/Modules/MasterData/Modules.MasterData/Features/v1/Employees/GetMyEmployee/GetMyEmployeeQuery.cs`
- [ ] `src/Modules/MasterData/Modules.MasterData/Features/v1/Employees/GetMyEmployee/GetMyEmployeeHandler.cs`
- [ ] `src/Modules/MasterData/Modules.MasterData/Features/v1/Employees/GetMyEmployee/GetMyEmployeeEndpoint.cs`
- [ ] `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/TangibleInventoryItems/GetByPropertyNo/GetTangibleInventoryItemByPropertyNoQuery.cs`
- [ ] `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/TangibleInventoryItems/GetByPropertyNo/GetTangibleInventoryItemByPropertyNoHandler.cs`
- [ ] `src/Modules/AssetManagement/Modules.AssetManagement/Features/v1/TangibleInventoryItems/GetByPropertyNo/GetTangibleInventoryItemByPropertyNoEndpoint.cs`

### MAUI Project (all new)

- [ ] `src/Playground/Playground.Maui/Playground.Maui.csproj`
- [ ] `src/Playground/Playground.Maui/MauiProgram.cs`
- [ ] `src/Playground/Playground.Maui/AppShell.xaml` + `.cs`
- [ ] `src/Playground/Playground.Maui/appsettings.json`
- [ ] `src/Playground/Playground.Maui/Services/ApiClientOptions.cs`
- [ ] `src/Playground/Playground.Maui/Services/ITokenStorageService.cs`
- [ ] `src/Playground/Playground.Maui/Services/TokenStorageService.cs`
- [ ] `src/Playground/Playground.Maui/Services/AuthStateService.cs`
- [ ] `src/Playground/Playground.Maui/Services/AuthenticatedHttpHandler.cs`
- [ ] `src/Playground/Playground.Maui/Services/ICacheService.cs`
- [ ] `src/Playground/Playground.Maui/Services/CacheService.cs`
- [ ] `src/Playground/Playground.Maui/Data/LocalDb.cs`
- [ ] `src/Playground/Playground.Maui/Data/Models/CachedEmployeeProfile.cs`
- [ ] `src/Playground/Playground.Maui/Data/Models/CachedICS.cs`
- [ ] `src/Playground/Playground.Maui/Data/Models/CachedPAR.cs`
- [ ] `src/Playground/Playground.Maui/Features/Auth/LoginPage.xaml` + `.cs`
- [ ] `src/Playground/Playground.Maui/Features/Auth/LoginViewModel.cs`
- [ ] `src/Playground/Playground.Maui/Features/Profile/ProfilePage.xaml` + `.cs`
- [ ] `src/Playground/Playground.Maui/Features/Profile/ProfileViewModel.cs`
- [ ] `src/Playground/Playground.Maui/Features/Inventory/InventoryPage.xaml` + `.cs`
- [ ] `src/Playground/Playground.Maui/Features/Inventory/InventoryViewModel.cs`
- [ ] `src/Playground/Playground.Maui/Features/Inventory/ICSDetailPage.xaml` + `.cs`
- [ ] `src/Playground/Playground.Maui/Features/Inventory/ICSDetailViewModel.cs`
- [ ] `src/Playground/Playground.Maui/Features/Inventory/PARDetailPage.xaml` + `.cs`
- [ ] `src/Playground/Playground.Maui/Features/Inventory/PARDetailViewModel.cs`
- [ ] `src/Playground/Playground.Maui/Features/Scan/ScanPage.xaml` + `.cs`
- [ ] `src/Playground/Playground.Maui/Features/Scan/ScanViewModel.cs`
- [ ] `src/Playground/Playground.Maui/Features/Asset/AssetDetailPage.xaml` + `.cs`
- [ ] `src/Playground/Playground.Maui/Features/Asset/AssetDetailViewModel.cs`

---

## 9. Verification Checklist

| #   | Test                           | Pass criteria                                               |
| --- | ------------------------------ | ----------------------------------------------------------- |
| 1   | Build                          | `dotnet build src/AMIS.Framework.slnx` — 0 warnings          |
| 2   | `/employees/me`                | Returns correct `EmployeeId` for logged-in user             |
| 3   | `/by-property-no/{propertyNo}` | Returns item data; 404 for unknown PropertyNo               |
| 4   | Android — Login                | Credentials → tokens stored in SecureStorage → Shell shown  |
| 5   | Android — Token refresh        | Force 401 → auto-refresh → request retried transparently    |
| 6   | Android — Profile              | Name, email, photo load correctly                           |
| 7   | Android — Inventory (online)   | ICS + PAR lists show items for logged-in employee           |
| 8   | Android — Inventory (offline)  | Airplane mode → cached ICS/PAR still visible with banner    |
| 9   | Android — ICS Detail           | All line items show (PropertyNo, ItemName, UnitCost)        |
| 10  | Android — PAR Detail           | All line items show with correct totals                     |
| 11  | Android — Camera scan          | Hold PropertyNo barcode to camera → Asset Detail page loads |
| 12  | Android — Manual entry         | Type PropertyNo → tap Search → Asset Detail page loads      |
| 13  | Windows — Manual entry         | Camera section shows fallback message; Search box works     |
| 14  | Windows — Login                | Credentials stored in PasswordVault (not SecureStorage)     |
| 15  | Logout                         | Tokens cleared, SQLite cache wiped, Login page shown        |

