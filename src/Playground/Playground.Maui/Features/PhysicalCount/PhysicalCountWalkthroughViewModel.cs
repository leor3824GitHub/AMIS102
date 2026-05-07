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
    [ObservableProperty] private string _stationOffice = "";
    [ObservableProperty] private string _scope = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _syncBanner;
    [ObservableProperty] private bool _isOcrBusy;

    [ObservableProperty] private string _manualPropertyNo = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedFilter = "All";

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
            StationOffice = detail.StationOffice;
            Scope = detail.Scope;
            Status = detail.Status;
            _allEntries = detail.Entries;
            UpdateCounts();
            ApplyFilter();

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
        var propertyNo = ManualPropertyNo.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(propertyNo)) return;
        await ProcessPropertyNoAsync(propertyNo);
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

    [RelayCommand]
    private async Task OpenEntryAsync(PhysicalCountEntryDto entry) =>
        await NavigateToMarkEntryAsync(entry, isScanned: false);

    private async Task ProcessPropertyNoAsync(string propertyNo, string? description = null, decimal? unitCost = null)
    {
        var entry = _allEntries.FirstOrDefault(e =>
            string.Equals(e.PropertyNumber, propertyNo, StringComparison.OrdinalIgnoreCase));

        if (entry is not null)
        {
            await NavigateToMarkEntryAsync(entry, isScanned: true);
            return;
        }

        var route = $"{nameof(PhysicalCountFoundAtStationPage)}" +
                    $"?SessionId={SessionId}" +
                    $"&PropertyNo={Uri.EscapeDataString(propertyNo)}";

        if (!string.IsNullOrWhiteSpace(description))
            route += $"&Desc={Uri.EscapeDataString(description)}";

        if (unitCost.HasValue)
            route += $"&UnitCost={unitCost.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        await Shell.Current.GoToAsync(route);
    }

    private async Task NavigateToMarkEntryAsync(PhysicalCountEntryDto entry, bool isScanned) =>
        await Shell.Current.GoToAsync(
            $"{nameof(PhysicalCountMarkEntryPage)}" +
            $"?SessionId={SessionId}" +
            $"&EntryId={entry.Id}" +
            $"&PropertyNo={Uri.EscapeDataString(entry.PropertyNumber)}" +
            $"&Desc={Uri.EscapeDataString(entry.Description)}" +
            $"&IsScanned={isScanned}");

    // Updates in-memory entry after marking so the list reflects changes immediately
    public void UpdateLocalEntry(Guid entryId, string result, string? condition)
    {
        var existing = _allEntries.FirstOrDefault(e => e.Id == entryId);
        if (existing is null) return;
        var updated = existing with { Result = result, Condition = condition };
        var idx = _allEntries.IndexOf(existing);
        _allEntries[idx] = updated;
        UpdateCounts();
        ApplyFilter();
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
