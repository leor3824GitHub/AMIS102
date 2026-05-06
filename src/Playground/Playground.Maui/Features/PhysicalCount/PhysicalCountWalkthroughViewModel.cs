using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Services;
using System.Collections.ObjectModel;

namespace Playground.Maui.Features.PhysicalCount;

[QueryProperty(nameof(SessionId), "SessionId")]
public sealed partial class PhysicalCountWalkthroughViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly IPhysicalCountSyncService _syncService;
    private List<PhysicalCountEntryDto> _allEntries = [];
    private DateTimeOffset? _lastScanTime;

    [ObservableProperty] private string _sessionId = "";
    [ObservableProperty] private string _sessionNo = "";
    [ObservableProperty] private string _stationOffice = "";
    [ObservableProperty] private string _scope = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _syncBanner;
    [ObservableProperty] private bool _isCameraAvailable;

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

    public PhysicalCountWalkthroughViewModel(IApiClient apiClient, IPhysicalCountSyncService syncService)
    {
        _apiClient = apiClient;
        _syncService = syncService;
        IsCameraAvailable = DeviceInfo.Current.Platform != DevicePlatform.Unknown;
    }

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

    public void OnBarcodeDetected(string rawValue)
    {
        if (IsDebounced()) return;
        var propertyNo = rawValue.Trim().ToUpperInvariant();
        ManualPropertyNo = propertyNo;
        _ = ProcessPropertyNoAsync(propertyNo);
    }

    [RelayCommand]
    private async Task SearchManualAsync(CancellationToken ct = default)
    {
        var propertyNo = ManualPropertyNo.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(propertyNo)) return;
        await ProcessPropertyNoAsync(propertyNo);
    }

    [RelayCommand]
    private async Task OpenEntryAsync(PhysicalCountEntryDto entry) =>
        await NavigateToMarkEntryAsync(entry, isScanned: false);

    private async Task ProcessPropertyNoAsync(string propertyNo)
    {
        var entry = _allEntries.FirstOrDefault(e =>
            string.Equals(e.PropertyNumber, propertyNo, StringComparison.OrdinalIgnoreCase));

        if (entry is not null)
            await NavigateToMarkEntryAsync(entry, isScanned: true);
        else
            await Shell.Current.GoToAsync(
                $"{nameof(PhysicalCountFoundAtStationPage)}?SessionId={SessionId}&PropertyNo={Uri.EscapeDataString(propertyNo)}");
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

    private bool IsDebounced()
    {
        if (_lastScanTime.HasValue &&
            (DateTimeOffset.UtcNow - _lastScanTime.Value).TotalSeconds < 2)
            return true;
        _lastScanTime = DateTimeOffset.UtcNow;
        return false;
    }
}
