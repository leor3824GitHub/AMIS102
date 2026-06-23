using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Playground.Maui.Services;
using System.Collections.ObjectModel;

namespace Playground.Maui.Features.PhysicalCount;

[QueryProperty(nameof(SessionId), "SessionId")]
public sealed partial class PhysicalCountWalkthroughViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly IPhysicalCountSyncService _syncService;
    private readonly IOcrService _ocr;
    private List<PhysicalCountEntryDto> _allEntries = [];

    [ObservableProperty] private string _sessionId = "";
    [ObservableProperty] private string _sessionNo = "";
    [ObservableProperty] private string _fundCluster = "";
    [ObservableProperty] private string _scope = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _syncBanner;
    [ObservableProperty] private bool _isOcrBusy;

    [ObservableProperty] private string _manualPropertyNo = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedFilter = "All";

    // Manual-entry mode: false = look up by property number (default), true = search by serial number.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManualEntryPlaceholder))]
    private bool _searchBySerial;

    public string ManualEntryPlaceholder => SearchBySerial
        ? "Serial number"
        : "Property No. (type or scan)";

    // "Counting at" location — required to record (AssetRegister stores where each item was counted).
    [ObservableProperty] private ObservableCollection<LocationDto> _locations = [];
    [ObservableProperty] private LocationDto? _selectedLocation;

    [ObservableProperty] private ObservableCollection<PhysicalCountEntryDto> _filteredEntries = [];

    // Progress counters
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _foundCount;
    [ObservableProperty] private int _notFoundCount;
    [ObservableProperty] private int _foundAtStationCount;
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private int _pendingSyncCount;

    public string[] FilterOptions => ["All", "Pending", "Found", "Not Found", "Found@Station"];

    public PhysicalCountWalkthroughViewModel(IApiClient apiClient, IPhysicalCountSyncService syncService, IOcrService ocr)
    {
        _apiClient = apiClient;
        _syncService = syncService;
        _ocr = ocr;
    }

    public bool IsOcrSupported => _ocr.IsSupported;

    public void SubscribeMessages() =>
        WeakReferenceMessenger.Default.Register<PhysicalCountBarcodeScannedMessage>(this,
            (_, msg) => MainThread.BeginInvokeOnMainThread(() =>
            {
                ManualPropertyNo = msg.PropertyNo;
                _ = ProcessPropertyNoAsync(msg.PropertyNo);
            }));

    public void UnsubscribeMessages() =>
        WeakReferenceMessenger.Default.Unregister<PhysicalCountBarcodeScannedMessage>(this);

    [RelayCommand]
    private static async Task ScanQrAsync() =>
        await Shell.Current.GoToAsync(nameof(PhysicalCountScanPage));

    partial void OnSessionIdChanged(string value) => _ = LoadAsync();
    partial void OnSelectedFilterChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(SessionId)) return;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var detail = await _apiClient.GetPhysicalCountSessionByIdAsync(Guid.Parse(SessionId), ct);
            SessionNo = detail.SessionNo;
            FundCluster = detail.FundCluster;
            Scope = detail.Scope;
            Status = detail.Status;
            _allEntries = detail.Entries;
            UpdateCounts();
            ApplyFilter();

            if (Locations.Count == 0)
            {
                try
                {
                    var locs = await _apiClient.GetLocationsAsync(ct);
                    Locations = new ObservableCollection<LocationDto>(locs);
                }
                catch (OperationCanceledException) { throw; }
                catch
                {
                    // Non-fatal: locations didn't load. Picker will be empty;
                    // user will see "Select where you are counting" error on first scan.
                }
            }

            PendingSyncCount = await _syncService.GetPendingCountAsync();
            SyncBanner = PendingSyncCount > 0
                ? $"{PendingSyncCount} entry(s) queued — will sync when connected."
                : null;
        }
        catch (HttpRequestException) when (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            ErrorMessage = "No internet connection.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load session. Pull down to retry.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task FlushPendingAsync(CancellationToken ct = default)
    {
        await _syncService.FlushPendingAsync(ct);
        PendingSyncCount = await _syncService.GetPendingCountAsync();
        SyncBanner = PendingSyncCount > 0
            ? $"{PendingSyncCount} entry(s) still pending — some failed."
            : null;
        if (PendingSyncCount == 0)
            await LoadAsync(ct);
    }

    [RelayCommand]
    private async Task SearchManualAsync(CancellationToken ct = default)
    {
        var raw = ManualPropertyNo.Trim();
        if (string.IsNullOrEmpty(raw)) return;

        if (SearchBySerial)
        {
            var propertyNo = await ResolveSerialToPropertyNoAsync(raw, ct);
            if (propertyNo is not null)
                await ProcessPropertyNoAsync(propertyNo);
            return;
        }

        await ProcessPropertyNoAsync(raw.ToUpperInvariant());
    }

    // Resolves a serial number to a single property number so the rest of the count flow is unchanged:
    // none → inline error, one → that asset, many → a tappable action sheet to pick from.
    private async Task<string?> ResolveSerialToPropertyNoAsync(string serial, CancellationToken ct)
    {
        ErrorMessage = null;
        IReadOnlyList<AssetSummaryDto> matches;
        try
        {
            matches = await _apiClient.SearchAssetsBySerialAsync(serial, ct);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Couldn't search by serial number. Check your connection.";
            return null;
        }

        if (matches.Count == 0)
        {
            ErrorMessage = $"No asset found with serial number \"{serial}\".";
            return null;
        }
        if (matches.Count == 1)
            return matches[0].PropertyNo;

        var labels = matches.Select(m => $"{m.PropertyNo} - {m.Description}").ToArray();
        var choice = await Shell.Current.DisplayActionSheetAsync(
            $"{matches.Count} assets match this serial", "Cancel", null, labels);
        var index = string.IsNullOrEmpty(choice) ? -1 : Array.IndexOf(labels, choice);
        return index >= 0 ? matches[index].PropertyNo : null;
    }

    [RelayCommand]
    private async Task ScanTextAsync(CancellationToken ct = default)
    {
        if (IsOcrBusy) return;
        if (!_ocr.IsSupported)
        {
            ErrorMessage = "Text scanning isn't available on this device.";
            return;
        }

        IsOcrBusy = true;
        ErrorMessage = null;
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo is null) return;

            await using var stream = await photo.OpenReadAsync();
            var raw = await _ocr.RecognizeTextAsync(stream, ct);
            var info = PropertyNumberExtractor.Extract(raw);

            if (string.IsNullOrEmpty(info.PropertyNo))
            {
                ManualPropertyNo = (raw ?? string.Empty).Trim();
                ErrorMessage = "Couldn't detect a property number. Edit the text and tap Search.";
                return;
            }

            ManualPropertyNo = info.PropertyNo;
            await ProcessPropertyNoAsync(info.PropertyNo, info.Item, info.Value);
        }
        catch (FeatureNotSupportedException)
        {
            ErrorMessage = "Camera capture isn't supported on this device.";
        }
        catch (PermissionException)
        {
            ErrorMessage = "Camera permission was denied.";
        }
        catch (Exception)
        {
            ErrorMessage = "Could not scan text. Please try again.";
        }
        finally
        {
            IsOcrBusy = false;
        }
    }

    // Resolve a scanned/typed property number against the asset registry, then either record the
    // found asset (known to the registry) or capture it as a found-at-station item (unknown).
    private async Task ProcessPropertyNoAsync(string propertyNo, string? description = null, decimal? unitCost = null)
    {
        if (SelectedLocation is null)
        {
            ErrorMessage = "Select where you are counting (location) before recording.";
            return;
        }

        var locationId = SelectedLocation.Id;
        try
        {
            var asset = await _apiClient.GetItemByPropertyNoAsync(propertyNo);
            // Known asset — record it (condition picked on the next screen).
            await Shell.Current.GoToAsync(
                $"{nameof(PhysicalCountMarkEntryPage)}" +
                $"?SessionId={SessionId}" +
                $"&AssetRegistryId={asset.Id}" +
                $"&PropertyNo={Uri.EscapeDataString(asset.PropertyNo)}" +
                $"&Desc={Uri.EscapeDataString(asset.ItemName)}" +
                $"&Unit={Uri.EscapeDataString(asset.Unit)}" +
                $"&UnitCost={asset.UnitCost.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&LocationId={locationId}" +
                $"&IsScanned=true");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Definitively not in the registry → capture as found at station.
            var route = $"{nameof(PhysicalCountFoundAtStationPage)}" +
                        $"?SessionId={SessionId}" +
                        $"&PropertyNo={Uri.EscapeDataString(propertyNo)}" +
                        $"&LocationId={locationId}";
            if (!string.IsNullOrWhiteSpace(description))
                route += $"&Desc={Uri.EscapeDataString(description)}";
            if (unitCost.HasValue)
                route += $"&UnitCost={unitCost.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            await Shell.Current.GoToAsync(route);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not check the registry. Verify your connection and try again.";
        }
    }

    private void ApplyFilter()
    {
        var filtered = _allEntries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = filtered.Where(e =>
                e.PropertyNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        filtered = SelectedFilter switch
        {
            "Pending" => filtered.Where(e => e.Result is null),
            "Found" => filtered.Where(e => e.Result == "Found"),
            "Not Found" => filtered.Where(e => e.Result == "NotFound"),
            "Found@Station" => filtered.Where(e => e.Result == "FoundAtStation"),
            _ => filtered,
        };

        FilteredEntries = new ObservableCollection<PhysicalCountEntryDto>(filtered);
    }

    private void UpdateCounts()
    {
        TotalCount = _allEntries.Count;
        FoundCount = _allEntries.Count(e => e.Result == "Found");
        NotFoundCount = _allEntries.Count(e => e.Result == "NotFound");
        FoundAtStationCount = _allEntries.Count(e => e.Result == "FoundAtStation");
        PendingCount = _allEntries.Count(e => e.Result is null);
    }

}
