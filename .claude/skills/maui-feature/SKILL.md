# MAUI Feature Skill

> Add a new screen (Page + ViewModel) to `Playground.Maui` following the MVVM pattern used in this project.

---

## When to Use This Skill

Use this skill when adding any of:

- A new tab/screen to the MAUI app
- A new detail page navigated to from a list
- A new ViewModel that calls a backend API endpoint

---

## Required Information

Before generating, confirm:

1. **Screen name** — e.g., `Inspection`, `Return`, `RequestItem`
2. **Navigation type** — Tab (AppShell tab) or Route (detail page navigated via Shell)
3. **API endpoints consumed** — exact routes + HTTP methods + response DTOs
4. **Query parameters** — if it's a detail page, what is passed via Shell navigation (e.g., `Id`, `PropertyNo`)
5. **Needs offline cache?** — see caching rules in `.claude/rules/maui.md`

---

## Step 1: Create the ViewModel

Location: `src/Playground/Playground.Maui/Features/{FeatureName}/{ScreenName}ViewModel.cs`

### Template — List Screen

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Playground.Maui.Features.{FeatureName};

public sealed partial class {ScreenName}ViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly IAuthStateService _authState;
    // private readonly ICacheService _cache; // add if caching needed

    public {ScreenName}ViewModel(IApiClient api, IAuthStateService authState)
    {
        _api = api;
        _authState = authState;
    }

    [ObservableProperty] private ObservableCollection<{ItemDto}> _items = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private string? _errorMessage;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var result = await _api.Get{Items}Async(ct);
            Items = new ObservableCollection<{ItemDto}>(result);
        }
        catch (HttpRequestException) when (!Connectivity.Current.NetworkAccess.HasFlag(NetworkAccess.Internet))
        {
            // Optional: load from cache
            ErrorMessage = "No internet connection. Showing cached data.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load data. Pull down to retry.";
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct)
    {
        IsRefreshing = true;
        await LoadAsync(ct);
    }
}
```

### Template — Detail Page (with Shell query parameter)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Playground.Maui.Features.{FeatureName};

[QueryProperty(nameof({ParamName}), "{ParamName}")]
public sealed partial class {ScreenName}ViewModel : ObservableObject
{
    private readonly IApiClient _api;

    public {ScreenName}ViewModel(IApiClient api) => _api = api;

    [ObservableProperty] private string _{paramNameCamel} = "";
    [ObservableProperty] private {DetailDto}? _detail;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    partial void On{ParamName}Changed(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty({ParamName})) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Detail = await _api.Get{Entity}By{ParamName}Async({ParamName}, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            ErrorMessage = "{Entity} not found. Check the number and try again.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load details. Check your connection.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

---

## Step 2: Create the Page

Location: `src/Playground/Playground.Maui/Features/{FeatureName}/{ScreenName}Page.xaml`

### Rules

- `BindingContext` set via constructor injection — never in XAML.
- Loading overlay bound to `IsLoading`.
- Error label bound to `ErrorMessage`; hidden when null/empty.
- `RefreshView` wraps list for pull-to-refresh (list screens only).
- No business logic in code-behind — only `InitializeComponent()` + ViewModel assignment.

### Page Code-Behind Template

```csharp
namespace Playground.Maui.Features.{FeatureName};

public partial class {ScreenName}Page : ContentPage
{
    public {ScreenName}Page({ScreenName}ViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is {ScreenName}ViewModel vm)
            vm.LoadCommand.Execute(null);
    }
}
```

### XAML Skeleton — List Page

> **Always declare `x:DataType` on the page and on every `DataTemplate`.** This enables compiled bindings — faster than reflection and catches binding errors at build time.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:Playground.Maui.Features.{FeatureName}"
             xmlns:dto="clr-namespace:Playground.Maui.Services.Dtos"
             x:DataType="vm:{ScreenName}ViewModel"
             x:Class="Playground.Maui.Features.{FeatureName}.{ScreenName}Page"
             Title="{ScreenTitle}">

    <Grid RowDefinitions="Auto,Auto,*">

        <!-- Error / cached-data banner -->
        <Border Grid.Row="0"
                BackgroundColor="{StaticResource ErrorBackground}"
                IsVisible="{Binding ErrorMessage, Converter={StaticResource NotNullConverter}}"
                Padding="8">
            <Label Text="{Binding ErrorMessage}" TextColor="{StaticResource ErrorText}" />
        </Border>

        <!-- Cached-data indicator -->
        <Label Grid.Row="1"
               Text="{Binding CachedBanner}"
               IsVisible="{Binding IsCached}"
               FontSize="{StaticResource CaptionFontSize}"
               HorizontalOptions="Center" />

        <!-- Pull-to-refresh list (CollectionView handles its own scrolling — never wrap in ScrollView) -->
        <RefreshView Grid.Row="2"
                     IsRefreshing="{Binding IsRefreshing}"
                     Command="{Binding RefreshCommand}">
            <CollectionView ItemsSource="{Binding Items}">
                <CollectionView.ItemTemplate>
                    <!-- x:DataType on DataTemplate enables compiled bindings inside the template -->
                    <DataTemplate x:DataType="dto:{ItemDto}">
                        <!-- Use Border (not Frame), VerticalStackLayout (not StackLayout) -->
                        <Border Margin="8,4" Padding="12"
                                StrokeShape="RoundRectangle 8"
                                Stroke="{StaticResource BorderColor}">
                            <Grid ColumnDefinitions="*,Auto" RowDefinitions="Auto,Auto">
                                <Label Grid.Row="0" Grid.Column="0"
                                       Text="{Binding PrimaryField}"
                                       FontAttributes="Bold" />
                                <Label Grid.Row="1" Grid.Column="0"
                                       Text="{Binding SecondaryField}"
                                       TextColor="{StaticResource SecondaryText}" />
                                <Label Grid.Row="0" Grid.Column="1" Grid.RowSpan="2"
                                       Text="{Binding StatusField}"
                                       VerticalOptions="Center" />
                            </Grid>
                        </Border>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
                <CollectionView.EmptyView>
                    <Label Text="No items found."
                           HorizontalOptions="Center" VerticalOptions="Center"
                           TextColor="{StaticResource SecondaryText}" />
                </CollectionView.EmptyView>
            </CollectionView>
        </RefreshView>

        <!-- Loading overlay -->
        <ActivityIndicator Grid.RowSpan="3"
                           IsRunning="{Binding IsLoading}"
                           IsVisible="{Binding IsLoading}"
                           HorizontalOptions="Center" VerticalOptions="Center" />
    </Grid>
</ContentPage>
```

---

## Step 3: Register in `MauiProgram.cs`

### Tab screen (appears in AppShell tab bar)

```csharp
// In MauiProgram.cs — register ViewModel as Transient
builder.Services.AddTransient<{ScreenName}ViewModel>();
builder.Services.AddTransient<{ScreenName}Page>();
```

Then add to `AppShell.xaml`:

```xml
<TabBar>
    <ShellContent Title="{ScreenTitle}"
                  ContentTemplate="{DataTemplate features:{ScreenName}Page}" />
</TabBar>
```

### Detail / route page

```csharp
// In MauiProgram.cs
builder.Services.AddTransient<{ScreenName}ViewModel>();
builder.Services.AddTransient<{ScreenName}Page>();

// Route registration (MauiProgram.cs or AppShell.xaml.cs)
Routing.RegisterRoute(nameof({ScreenName}Page), typeof({ScreenName}Page));
```

Navigate to it:

```csharp
await Shell.Current.GoToAsync($"{nameof({ScreenName}Page)}?{nameof({ScreenName}ViewModel.{ParamName})}={value}");
```

---

## Step 4: Add API Client Method

Add the method to `IApiClient` and `ApiClient` in `Services/ApiClient.cs`:

```csharp
// In IApiClient
Task<List<{ItemDto}>> Get{Items}Async(CancellationToken ct = default);

// In ApiClient (inject HttpClient via constructor)
public async Task<List<{ItemDto}>> Get{Items}Async(CancellationToken ct = default)
{
    var response = await _http.GetFromJsonAsync<List<{ItemDto}>>("/api/v1/{route}", ct);
    return response ?? [];
}
```

---

## Step 5: Verification Checklist

### ViewModel

- [ ] ViewModel is `sealed partial class : ObservableObject`
- [ ] All bound properties use `[ObservableProperty]`
- [ ] All commands use `[RelayCommand]`
- [ ] Async commands accept `CancellationToken ct`
- [ ] No `Task.Run()` wrapping async I/O — use `await` directly
- [ ] No business logic in page code-behind — only `InitializeComponent()` + `BindingContext = vm`
- [ ] Shell route registered if it's a detail page
- [ ] API method returns typed DTO, not raw `string` / `object`
- [ ] Network error falls back to cache (if screen requires offline support)
- [ ] 404 shows friendly message, not crash
- [ ] ViewModel and Page registered as `Transient` in `MauiProgram.cs`

### XAML

- [ ] `x:DataType` declared on the `ContentPage` element
- [ ] `x:DataType` declared on every `DataTemplate`
- [ ] `CollectionView` used for lists — not `ListView`
- [ ] `VerticalStackLayout` / `HorizontalStackLayout` used — not generic `StackLayout`
- [ ] `Border` used for cards/rounded corners — not `Frame`
- [ ] No `CollectionView` nested inside `ScrollView`
- [ ] Layout is flat (max 2–3 nesting levels) — use `Grid` for complex layouts
- [ ] No hard-coded colors or font sizes inline — use `{StaticResource ...}` from Styles.xaml
- [ ] `IsLoading` overlay present
- [ ] Empty state view present in `CollectionView`
- [ ] Icons use SVG `MauiImage` resources — not per-density PNG files
