---
paths:
  - "src/Playground/Playground.Maui/**"
---

# MAUI Client Rules

`Playground.Maui` is a **client-only** project. It consumes the existing REST API and has no knowledge of backend module internals. It never references any `Modules.*` project directly.

## Project Location

```
src/Playground/Playground.Maui/
```

Added to `src/AMIS.Framework.slnx`. Targets: `net10.0-android`, `net10.0-ios`, `net10.0-windows10.0.19041.0`.

---

## MVVM Pattern

All screens follow **CommunityToolkit.Mvvm** source-generated MVVM. Never use code-behind logic or raw event handlers for business logic.

### ViewModel Rules

```csharp
// ✅ Correct
[ObservableProperty] private string _email = "";
[ObservableProperty] private bool _isLoading;

[RelayCommand]
private async Task LoginAsync(CancellationToken ct)
{
    IsLoading = true;
    try { /* call API */ }
    finally { IsLoading = false; }
}

// ❌ Wrong — no source generation, manual INotifyPropertyChanged
public string Email
{
    get => _email;
    set { _email = value; OnPropertyChanged(); }
}
```

| Rule                                  | Detail                                                        |
| ------------------------------------- | ------------------------------------------------------------- |
| ViewModels are `sealed partial class` | Required for source generator                                 |
| Properties use `[ObservableProperty]` | Generates `Email`, `IsLoading`, etc.                          |
| Commands use `[RelayCommand]`         | Generates `LoginCommand`, `LoginCommand.CanExecute`           |
| CancellationToken on async commands   | Pass `CancellationToken ct` to async relay commands           |
| ViewModels injected via DI            | Registered in `MauiProgram.cs`; constructor-injected in pages |
| No business logic in page code-behind | Pages only wire ViewModel to `BindingContext`                 |

### Page Code-Behind

```csharp
// ✅ Correct — pages delegate everything to ViewModel
public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
```

---

## Services Pattern

All services are interfaces registered in `MauiProgram.cs`. Never instantiate services with `new` in ViewModels.

### Core Services

| Service                    | Interface              | Responsibility                                             |
| -------------------------- | ---------------------- | ---------------------------------------------------------- |
| `TokenStorageService`      | `ITokenStorageService` | SecureStorage (Android/iOS), PasswordVault (Windows)       |
| `AuthStateService`         | `IAuthStateService`    | In-memory current user + EmployeeId after login            |
| `AuthenticatedHttpHandler` | DelegatingHandler      | Bearer token injection, tenant header, 401 refresh + retry |
| `CacheService`             | `ICacheService`        | SQLite read/write for ICS/PAR/employee profile snapshots   |

### `ITokenStorageService` Contract

```csharp
public interface ITokenStorageService
{
    Task SaveTokensAsync(string accessToken, string refreshToken);
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task ClearAsync();
}
```

### Platform-Conditional Token Storage

```csharp
// ✅ Correct — conditional compilation for platform differences
public async Task SaveTokensAsync(string accessToken, string refreshToken)
{
#if WINDOWS
    // PasswordVault (WinRT)
    var vault = new PasswordVault();
    vault.Add(new PasswordCredential("amis", "access_token", accessToken));
#else
    await SecureStorage.SetAsync("access_token", accessToken);
#endif
}
```

---

## HTTP Client Rules

### Registration in `MauiProgram.cs`

```csharp
builder.Services.AddTransient<AuthenticatedHttpHandler>();
builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
    client.BaseAddress = new Uri(options.BaseUrl))
    .AddHttpMessageHandler<AuthenticatedHttpHandler>();
```

### `AuthenticatedHttpHandler` Behavior

1. Inject `Authorization: Bearer {accessToken}` on every request.
2. Inject `tenant: {tenantId}` header on every request.
3. On **401**: call `POST /api/v1/identity/token/refresh`, persist new tokens, retry original request **once**.
4. On **second 401**: clear tokens → publish `SessionExpired` via `WeakReferenceMessenger` → navigate to `LoginPage`.
5. On **403**: do NOT redirect to login. Let ViewModel display "You don't have permission."
6. Handler must be `Transient` (not Singleton) — `HttpClient` lifetime controls re-use.

### Required Headers on All Requests

```
Authorization: Bearer {accessToken}
tenant: {tenantId}          ← from ApiClientOptions.TenantId (default: "root")
```

---

## Offline / Caching Rules

**Library:** `sqlite-net-pcl` with `SQLitePCLRaw.bundle_green`
**Location:** `FileSystem.AppDataDirectory/amis-cache.db`

### Cache Strategy — Stale-While-Revalidate

```
1. Read SQLite → render immediately (no spinner)
2. If online → fetch API in background → update UI → overwrite cache
3. If offline → show cached data + "[Cached · Last updated X min ago]" banner
4. Pull-to-refresh → always force API fetch, show progress indicator
```

### What Is and Is Not Cached

| Data                             | Cached | Reason                                            |
| -------------------------------- | ------ | ------------------------------------------------- |
| ICS list for current employee    | ✅ Yes | Field staff need offline access                   |
| PAR list for current employee    | ✅ Yes | Field staff need offline access                   |
| Employee profile (my EmployeeId) | ✅ Yes | Needed to filter ICS/PAR; stable data             |
| ICS detail (line items)          | ❌ No  | Online-only; too granular, changes infrequently   |
| PAR detail (line items)          | ❌ No  | Online-only                                       |
| Asset detail (by PropertyNo)     | ❌ No  | Online-only; real-time accuracy required          |
| User profile (identity)          | ❌ No  | Online-only; changes rarely, not critical offline |

### Cache Invalidation

| Trigger                                   | Action                                            |
| ----------------------------------------- | ------------------------------------------------- |
| Logout                                    | `ICacheService.ClearAllAsync()` — wipe all tables |
| Pull-to-refresh                           | Employee-scoped overwrite of ICS + PAR tables     |
| App foregrounded after >30 min background | Background API refresh on next InventoryPage open |

---

## Navigation Rules

Use **Shell navigation** exclusively. Never use `Navigation.PushAsync()` directly.

```csharp
// ✅ Correct — Shell navigation with query parameters
await Shell.Current.GoToAsync($"{nameof(ICSDetailPage)}?Id={id}");
await Shell.Current.GoToAsync($"{nameof(AssetDetailPage)}?PropertyNo={Uri.EscapeDataString(propertyNo)}");

// ✅ Receive in ViewModel
[QueryProperty(nameof(Id), "Id")]
public sealed partial class ICSDetailViewModel : ObservableObject
{
    [ObservableProperty] private string _id = "";
    partial void OnIdChanged(string value) => _ = LoadAsync();
}

// ❌ Wrong
await Navigation.PushAsync(new ICSDetailPage());
```

### Route Registration

All detail pages registered in `MauiProgram.cs` or `AppShell.xaml.cs`:

```csharp
Routing.RegisterRoute(nameof(ICSDetailPage), typeof(ICSDetailPage));
Routing.RegisterRoute(nameof(PARDetailPage), typeof(PARDetailPage));
Routing.RegisterRoute(nameof(AssetDetailPage), typeof(AssetDetailPage));
```

---

## QR / Barcode Scan Rules

**Library:** `ZXing.Net.MAUI`

### Supported Formats

Always configure all four formats that appear on government property stickers:

```csharp
BarcodeFormats = BarcodeFormat.QrCode |
                 BarcodeFormat.Code128 |
                 BarcodeFormat.Code39 |
                 BarcodeFormat.DataMatrix
```

### Debounce Rule

The camera reader fires continuously. Always debounce successful decodes:

```csharp
private DateTimeOffset? _lastScanTime;

private bool IsDebounced()
{
    if (_lastScanTime.HasValue &&
        (DateTimeOffset.UtcNow - _lastScanTime.Value).TotalSeconds < 2)
        return true;
    _lastScanTime = DateTimeOffset.UtcNow;
    return false;
}
```

### Manual Entry Fallback

The manual `PropertyNo` `Entry` field MUST always be visible alongside the camera view — never hidden or opt-in. On Windows, if no camera is detected, the camera section is replaced with a static label; manual entry remains the primary lookup method.

### PropertyNo Normalization

Always normalize before lookup:

```csharp
var propertyNo = rawValue.Trim().ToUpperInvariant();
```

---

## Error Handling in ViewModels

```csharp
// ✅ Correct pattern
[ObservableProperty] private string? _errorMessage;
[ObservableProperty] private bool _isLoading;

[RelayCommand]
private async Task LoadAsync(CancellationToken ct)
{
    IsLoading = true;
    ErrorMessage = null;
    try
    {
        var result = await _apiClient.GetICSListAsync(employeeId, ct);
        Items = new ObservableCollection<ICSSummaryDto>(result);
    }
    catch (HttpRequestException ex) when (!Connectivity.Current.NetworkAccess.HasFlag(NetworkAccess.Internet))
    {
        // Load from cache instead
        var cached = await _cacheService.GetCachedICSAsync(_employeeId);
        Items = new ObservableCollection<ICSSummaryDto>(cached.Select(Map));
        ErrorMessage = "No internet connection. Showing cached data.";
    }
    catch (HttpRequestException)
    {
        ErrorMessage = "Could not load data. Pull down to retry.";
    }
    finally
    {
        IsLoading = false;
    }
}
```

| HTTP Status           | ViewModel behavior                                                        |
| --------------------- | ------------------------------------------------------------------------- |
| 401 (first)           | `AuthenticatedHttpHandler` auto-refreshes; ViewModel is unaware           |
| 401 (second)          | `AuthenticatedHttpHandler` fires SessionExpired; ViewModel is unaware     |
| 403                   | Show "You don't have permission to view this." — do NOT navigate to login |
| 404                   | Show "Not found" message in UI — do NOT throw or crash                    |
| Network error offline | Fall back to SQLite cache if available; show banner                       |

---

## Permissions (Platform Manifests)

### Android — `AndroidManifest.xml`

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.CAMERA" />
<uses-feature android:name="android.hardware.camera" android:required="false" />
```

### iOS — `Info.plist`

```xml
<key>NSCameraUsageDescription</key>
<string>Camera is used to scan property barcode stickers.</string>
```

---

## XAML & UI Rules

### Compiled Bindings — Required on Every Page

Always declare `x:DataType` on every `ContentPage` and every `DataTemplate`. This enables compile-time binding verification and is significantly faster than reflection-based bindings.

```xml
<!-- ✅ Correct — compiled binding on page -->
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:Playground.Maui.Features.Inventory"
             x:DataType="vm:InventoryViewModel"
             x:Class="Playground.Maui.Features.Inventory.InventoryPage">

<!-- ✅ Correct — compiled binding on DataTemplate -->
<DataTemplate x:DataType="dto:ICSSummaryDto">
    <Label Text="{Binding ICSNo}" />
</DataTemplate>

<!-- ❌ Wrong — no x:DataType means reflection-based (slow, no compile-time errors) -->
<DataTemplate>
    <Label Text="{Binding ICSNo}" />
</DataTemplate>
```

### Control Choices

Use the right control for the job:

| Scenario                  | Use                                             | Never Use                                                               |
| ------------------------- | ----------------------------------------------- | ----------------------------------------------------------------------- |
| Scrolling item list       | `CollectionView`                                | `ListView`                                                              |
| Single-direction stacking | `VerticalStackLayout` / `HorizontalStackLayout` | `StackLayout` (generic, slower)                                         |
| Borders / rounded corners | `Border`                                        | `Frame` (deprecated)                                                    |
| Complex layouts           | `Grid`                                          | Deeply nested `VerticalStackLayout` inside `HorizontalStackLayout` etc. |

```xml
<!-- ✅ Correct -->
<VerticalStackLayout Spacing="8">
    <Border StrokeShape="RoundRectangle 8" Stroke="{StaticResource BorderColor}">
        <Label Text="{Binding ICSNo}" />
    </Border>
</VerticalStackLayout>

<!-- ❌ Wrong -->
<StackLayout>
    <Frame CornerRadius="8">
        <Label Text="{Binding ICSNo}" />
    </Frame>
</StackLayout>
```

### Avoid Deep Nesting

Deeply nested layouts cause multiple layout passes and kill scroll performance. Prefer a flat `Grid` hierarchy.

```xml
<!-- ✅ Correct — flat Grid -->
<Grid ColumnDefinitions="*,Auto" RowDefinitions="Auto,Auto">
    <Label Grid.Row="0" Grid.Column="0" Text="{Binding ICSNo}" />
    <Label Grid.Row="1" Grid.Column="0" Text="{Binding Date}" />
    <Label Grid.Row="0" Grid.Column="1" Grid.RowSpan="2" Text="{Binding Status}" />
</Grid>

<!-- ❌ Wrong — nested stacks for a simple card -->
<HorizontalStackLayout>
    <VerticalStackLayout>
        <HorizontalStackLayout>
            <Label Text="{Binding ICSNo}" />
        </HorizontalStackLayout>
        <Label Text="{Binding Date}" />
    </VerticalStackLayout>
    <Label Text="{Binding Status}" />
</HorizontalStackLayout>
```

### No Nested Scrollable Views

Never place a `CollectionView` or `ListView` inside a `ScrollView`. Nested scrollable views break virtualization — all items render at once, causing lag and high memory use.

```xml
<!-- ❌ Wrong — destroys virtualization -->
<ScrollView>
    <CollectionView ItemsSource="{Binding Items}" />
</ScrollView>

<!-- ✅ Correct — CollectionView handles its own scrolling -->
<CollectionView ItemsSource="{Binding Items}" />
```

If you need a header above a list, use `CollectionView.Header` instead of wrapping in `ScrollView`.

### Resource Styles — No Inline Styling

Define colors, font sizes, and repeated styles in `Resources/Styles/Styles.xaml`. Never hard-code colors or styles inline.

```xml
<!-- ✅ Correct — use named resources -->
<Label TextColor="{StaticResource Primary}" FontSize="{StaticResource BodyFontSize}" />

<!-- ❌ Wrong — inline styling -->
<Label TextColor="#512BD4" FontSize="16" />
```

Colors used in more than one place **must** be defined in `Application.Resources`. Named styles (`Style x:Key="..."`) must be used for repeated control patterns (e.g., all card labels, all section headers).

### Image Rules

- Use **SVG** (`MauiImage`) for icons and decorative assets — they scale correctly to all screen densities without per-density PNGs.
- For photos loaded from API (e.g., user profile image): resize to the display size before caching; never load a 2 MB image into a 50×50 thumbnail.
- Use `CachingStrategy="RecycleElement"` on image-heavy `CollectionView` items.
- Never use `ImageSource.FromUri()` on every bind cycle — cache the loaded image using `CommunityToolkit.Maui.MediaElement` or a dedicated image caching library.

---

## Async / Background Operations

### Always Use `async/await` — Never `Task.Run()` in MAUI

`Task.Run()` offloads work to a thread pool thread. In MAUI, UI-bound operations must run on the UI thread. Mixing `Task.Run()` with UI updates causes `InvalidOperationException` or silent deadlocks.

```csharp
// ✅ Correct — await I/O directly
var result = await _api.GetICSListAsync(ct);
Items = new ObservableCollection<ICSSummaryDto>(result);

// ❌ Wrong — Task.Run wrapping async I/O is redundant and dangerous
var result = await Task.Run(() => _api.GetICSListAsync(ct));
```

**Exception:** CPU-bound work (e.g., parsing a large JSON blob) may use `Task.Run()` for the CPU work only — but dispatch UI updates back with `MainThread.BeginInvokeOnMainThread()`.

### `MainThread` for UI Updates from Background Contexts

If a non-async event (e.g., `ZXing` barcode decoded callback) fires on a background thread, always marshal to the main thread:

```csharp
private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
{
    var value = e.Results.FirstOrDefault()?.Value;
    if (value is null) return;
    MainThread.BeginInvokeOnMainThread(async () =>
        await ViewModel.OnBarcodeDetectedAsync(value));
}
```

---

## Performance & Release Build

### AOT Compilation and Trimming

Enable full AOT and trimming in `Playground.Maui.csproj` for release builds. This significantly reduces app size and improves cold startup time.

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>full</TrimMode>
    <PublishAot>false</PublishAot>       <!-- Use profiled AOT for MAUI (not full AOT) -->
    <RunAOTCompilation>true</RunAOTCompilation>
    <AndroidEnableProfiledAot>true</AndroidEnableProfiledAot>
    <EnableLLVM>true</EnableLLVM>
</PropertyGroup>
```

> Note: Full AOT (`PublishAot=true`) is only available for iOS. Android uses profiled AOT (`RunAOTCompilation + AndroidEnableProfiledAot`). Both require testing on real devices after enabling.

### Profile on Real Devices, Not Simulators

Simulators and emulators use x86/x64 JIT on desktop hardware — they do not reflect actual ARM performance. Always validate scrolling smoothness, startup time, and memory use on a **physical low-end Android device** (e.g., 2 GB RAM, budget chipset) before considering a feature complete.

Tools: `dotnet-trace`, `dotMemory`, Android GPU profiler, Xcode Instruments (iOS).

### Native Customization — Prefer Platform Code Over Handlers

If you need platform-specific behavior (e.g., custom keyboard behavior on Android, haptic feedback on iOS), use `#if ANDROID` / `#if IOS` platform code rather than overriding `MapHandler`. Handler overrides affect all instances globally and can interfere across features.

```csharp
// ✅ Correct — scoped platform code
#if ANDROID
    entry.HandlerChanged += (s, e) =>
    {
        if (entry.Handler?.PlatformView is Android.Widget.EditText et)
            et.ImeOptions = Android.Views.InputMethods.ImeAction.Search;
    };
#endif
```

---

## What MAUI Must NOT Do

| Forbidden                                         | Why                                                                   |
| ------------------------------------------------- | --------------------------------------------------------------------- |
| Reference any `Modules.*` project                 | Client must be fully decoupled from backend internals                 |
| Use MediatR or Mediator directly                  | Those are server-side libraries                                       |
| Talk to the database directly                     | No EF Core, no DbContext in MAUI                                      |
| Use `Navigation.PushAsync`                        | Use Shell navigation only                                             |
| Store tokens in `Preferences`                     | Use `SecureStorage` / `PasswordVault` — Preferences are not encrypted |
| Catch-all `Exception` silently                    | Always show user-visible error or log; never swallow                  |
| Use `ListView`                                    | Use `CollectionView` — virtualization, better performance             |
| Use `StackLayout`                                 | Use `VerticalStackLayout` or `HorizontalStackLayout`                  |
| Use `Frame`                                       | Use `Border` — `Frame` is deprecated                                  |
| Nest `CollectionView` inside `ScrollView`         | Breaks virtualization — all items render at once                      |
| Use `x:DataType` absent from DataTemplate or Page | Required for compiled bindings                                        |
| Hard-code colors or font sizes inline in XAML     | Define in `Resources/Styles/Styles.xaml`                              |
| Use `Task.Run()` for I/O operations               | Use `async/await` directly; `Task.Run()` causes deadlocks in MAUI     |
| Test performance only on emulator/simulator       | Always validate on a real low-end physical device                     |

