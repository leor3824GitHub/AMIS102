using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Features.Shared;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;

namespace AMIS.Maui.Features.PhysicalCount;

// Record-as-you-go physical count. Inherits the shared capture layer (barcode / OCR / manual / serial)
// and supplies the terminal action: resolve the property number against the registry, then confirm and
// record an entry at the selected location. Unlike Scan, it stays in text mode after a hit so the user
// can keep scanning stickers — ShouldSkip + debounce prevent re-recording the one still in frame.
[QueryProperty(nameof(SessionId), "SessionId")]
public sealed partial class PhysicalCountWalkthroughViewModel : PropertyCaptureViewModel
{
    private readonly IApiClient _apiClient;
    private readonly IPhysicalCountSyncService _syncService;
    private List<PhysicalCountEntryDto> _allEntries = [];

    // Guards the resolve/confirm flow so the continuously-firing reader can't stack dialogs, and
    // remembers the last item counted so it isn't re-prompted while still in the camera frame.
    private bool _isResolving;
    private string? _lastCountedPropertyNo;

    // Default condition for a quick scan-to-count add — keeps the loop fast with the common outcome.
    private const string DefaultCountCondition = "InGoodCondition";

    [ObservableProperty] private string _sessionId = "";
    [ObservableProperty] private string _sessionNo = "";
    [ObservableProperty] private string _fundCluster = "";
    [ObservableProperty] private string _scope = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _syncBanner;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedFilter = "All";

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

    public PhysicalCountWalkthroughViewModel(
        IApiClient apiClient, IPhysicalCountSyncService syncService, IOcrService ocr)
        : base(apiClient, ocr)
    {
        _apiClient = apiClient;
        _syncService = syncService;
    }

    // ----- Capture policy (overrides of the shared base) -----

    public override string SearchPlaceholder => SearchBySerial
        ? "Serial number"
        : "Property No. (type or scan)";

    // Keep scanning after each record so the user can sweep sticker to sticker.
    protected override bool StopTextModeOnHit => false;

    // Don't re-prompt for the asset still sitting in the camera frame.
    protected override bool ShouldSkip(string propertyNo) =>
        string.Equals(propertyNo, _lastCountedPropertyNo, StringComparison.Ordinal);

    // Terminal action: record. Barcode/OCR count as scanned; manual/serial as typed.
    protected override Task HandleResolvedPropertyNoAsync(string propertyNo, PropertyInputSource source) =>
        ProcessPropertyNoAsync(propertyNo, isScanned: source is PropertyInputSource.Barcode or PropertyInputSource.Ocr);

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

    // Resolve a scanned/typed property number against the asset registry, then either confirm-and-add
    // a known asset inline (record-as-you-go) or capture an unknown one as found at station.
    private async Task ProcessPropertyNoAsync(string propertyNo, bool isScanned)
    {
        if (_isResolving) return;
        if (SelectedLocation is null)
        {
            ErrorMessage = "Select where you are counting (location) before recording.";
            return;
        }

        _isResolving = true;
        ErrorMessage = null;
        var location = SelectedLocation;
        try
        {
            TangibleInventoryItemDetailDto asset;
            try
            {
                asset = await _apiClient.GetItemByPropertyNoAsync(propertyNo);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Definitively not in the registry → capture as found at station (full classify screen).
                // Remember it so the reader doesn't re-launch that screen while it's still in frame.
                _lastCountedPropertyNo = propertyNo;
                await Shell.Current.GoToAsync(
                    $"{nameof(PhysicalCountFoundAtStationPage)}" +
                    $"?SessionId={SessionId}" +
                    $"&PropertyNo={Uri.EscapeDataString(propertyNo)}" +
                    $"&LocationId={location.Id}");
                return;
            }

            await ConfirmAndRecordAsync(asset, location, isScanned);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not check the registry. Verify your connection and try again.";
        }
        catch (Exception)
        {
            ErrorMessage = "Something went wrong. Please try again.";
        }
        finally
        {
            _isResolving = false;
        }
    }

    // Show the found asset's details and, on confirm, add it to the count at the selected location.
    private async Task ConfirmAndRecordAsync(TangibleInventoryItemDetailDto asset, LocationDto location, bool isScanned)
    {
        var details =
            $"{asset.ItemName}\n\n" +
            $"Property No:  {asset.PropertyNo}\n" +
            $"Unit cost:  ₱{asset.UnitCost:N2}\n" +
            $"Counting at:  {location.Name}";

        var confirmed = await Shell.Current.DisplayAlert("Add to count?", details, "Add", "Cancel");
        if (!confirmed) return;

        ErrorMessage = null;
        try
        {
            var request = new RecordCountEntryRequest(
                asset.Id,
                string.IsNullOrWhiteSpace(asset.ItemName) ? asset.PropertyNo : asset.ItemName,
                string.IsNullOrWhiteSpace(asset.Unit) ? "unit" : asset.Unit,
                asset.UnitCost,
                DefaultCountCondition,
                location.Id,
                null,
                isScanned);

            var synced = await _syncService.RecordEntryAsync(Guid.Parse(SessionId), request);
            _lastCountedPropertyNo = asset.PropertyNo.Trim().ToUpperInvariant();
            ManualPropertyNo = "";

            if (synced)
            {
                // Reload authoritative session state (progress badges + checklist).
                await LoadAsync();
            }
            else
            {
                // Offline: the entry is queued locally. A session reload would fail on the network call,
                // so reflect the pending queue directly instead of leaving the banner stale.
                PendingSyncCount = await _syncService.GetPendingCountAsync();
                SyncBanner = $"{PendingSyncCount} entry(s) queued — will sync when connected.";
                ErrorMessage = "Added offline — it will sync when you're back online.";
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Could not add the item to the count. Please try again.";
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
