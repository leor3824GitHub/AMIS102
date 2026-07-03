using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Data.Models;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;

namespace AMIS.Maui.Features.PhysicalCount;

// Review screen for a count session: the full recorded-entries checklist with search + filter,
// including locally-queued entries not yet synced (shown with a "Queued" badge). Read-only —
// recording happens on the Scan screen.
[QueryProperty(nameof(SessionId), "SessionId")]
public sealed partial class PhysicalCountEntriesViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly IPhysicalCountSyncService _syncService;
    private List<PhysicalCountEntryDto> _allEntries = [];

    [ObservableProperty] private string _sessionId = "";
    [ObservableProperty] private string _sessionNo = "";
    [ObservableProperty] private string _fundCluster = "";
    [ObservableProperty] private string _scope = "";
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedFilter = "All";

    [ObservableProperty] private int _foundCount;
    [ObservableProperty] private int _missingCount;
    [ObservableProperty] private int _foundAtStationCount;
    [ObservableProperty] private int _queuedCount;

    [ObservableProperty] private ObservableCollection<PhysicalCountEntryDto> _filteredEntries = [];

    public string[] FilterOptions => ["All", "Found", "Missing", "@Station", "Queued"];

    public PhysicalCountEntriesViewModel(IApiClient apiClient, IPhysicalCountSyncService syncService)
    {
        _apiClient = apiClient;
        _syncService = syncService;
    }

    partial void OnSessionIdChanged(string value) => _ = LoadAsync();
    partial void OnSelectedFilterChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(SessionId) || !Guid.TryParse(SessionId, out var id)) return;
        IsLoading = true;
        ErrorMessage = null;

        // Locally-queued entries are always available (SQLite), so map them first and show them
        // even when the server load fails.
        var queued = (await _syncService.GetUnsyncedEntriesAsync(id)).Select(MapQueued).ToList();

        try
        {
            var detail = await _apiClient.GetPhysicalCountSessionByIdAsync(id, ct);
            SessionNo = detail.SessionNo;
            FundCluster = detail.FundCluster;
            Scope = detail.Scope;
            Status = detail.Status;
            _allEntries = [.. detail.Entries, .. queued];
        }
        catch (HttpRequestException) when (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            ErrorMessage = "You're offline — showing queued entries only.";
            _allEntries = queued;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Couldn't load entries. Pull to refresh.";
            _allEntries = queued;
        }
        finally
        {
            UpdateCounts();
            ApplyFilter();
            IsLoading = false;
        }
    }

    private static PhysicalCountEntryDto MapQueued(PendingCountEntry e) => new(
        Guid.NewGuid(),
        Guid.TryParse(e.AssetRegistryId, out var arId) ? arId : null,
        string.IsNullOrWhiteSpace(e.PropertyNumber) ? e.Article : e.PropertyNumber,
        e.Article,
        e.UnitCost,
        "Queued",
        e.Condition,
        1,
        e.Remarks,
        e.IsScanned);

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
            "Found" => filtered.Where(e => e.Result == "Found"),
            "Missing" => filtered.Where(e => e.Result == "NotFound"),
            "@Station" => filtered.Where(e => e.Result == "FoundAtStation"),
            "Queued" => filtered.Where(e => e.Result == "Queued"),
            _ => filtered,
        };

        FilteredEntries = new ObservableCollection<PhysicalCountEntryDto>(filtered);
    }

    private void UpdateCounts()
    {
        FoundCount = _allEntries.Count(e => e.Result == "Found");
        MissingCount = _allEntries.Count(e => e.Result == "NotFound");
        FoundAtStationCount = _allEntries.Count(e => e.Result == "FoundAtStation");
        QueuedCount = _allEntries.Count(e => e.Result == "Queued");
    }
}
