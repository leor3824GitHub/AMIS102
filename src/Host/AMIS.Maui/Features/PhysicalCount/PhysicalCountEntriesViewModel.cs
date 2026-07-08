using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Data.Models;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;

namespace AMIS.Maui.Features.PhysicalCount;

// Review screen for a count session: the full recorded-entries checklist with search + filter,
// grouped by SE / PPE, including locally-queued entries not yet synced (shown with a "Queued" badge).
// Read-only — recording happens on the Scan screen. Reached either standalone (Closed sessions) or as
// the tap-through breakdown from a scan-screen count tile (deep-linked with a Filter query param).
[QueryProperty(nameof(SessionId), "SessionId")]
[QueryProperty(nameof(Filter), "Filter")]
public sealed partial class PhysicalCountEntriesViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly IPhysicalCountSyncService _syncService;
    private List<PhysicalCountEntryDto> _allEntries = [];

    [ObservableProperty] public partial string SessionId { get; set; } = "";
    [ObservableProperty] public partial string SessionNo { get; set; } = "";
    [ObservableProperty] public partial string FundCluster { get; set; } = "";
    [ObservableProperty] public partial string Scope { get; set; } = "";
    [ObservableProperty] public partial string Status { get; set; } = "";
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [ObservableProperty] public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderTitle))]
    public partial string SelectedFilter { get; set; } = "All";

    // Deep-link entry point: a tapped count tile passes ?Filter=Found|Missing|@Station so this screen
    // opens straight to that breakdown. Empty (standalone review) keeps the default "All".
    [ObservableProperty] public partial string Filter { get; set; } = "";

    [ObservableProperty] public partial int FoundCount { get; set; }
    [ObservableProperty] public partial int MissingCount { get; set; }
    [ObservableProperty] public partial int FoundAtStationCount { get; set; }
    [ObservableProperty] public partial int QueuedCount { get; set; }

    [ObservableProperty] public partial ObservableCollection<EntryGroup> FilteredEntries { get; set; } = [];

    public string[] FilterOptions => ["All", "Found", "Missing", "@Station", "Queued"];

    // Page title reflects the active breakdown so a tapped-through view reads "Found", "Missing", etc.
    public string HeaderTitle => SelectedFilter switch
    {
        "Found" => "Found",
        "Missing" => "Missing",
        "@Station" => "Found at station",
        "Queued" => "Queued",
        _ => "Entries",
    };

    public PhysicalCountEntriesViewModel(IApiClient apiClient, IPhysicalCountSyncService syncService)
    {
        _apiClient = apiClient;
        _syncService = syncService;
    }

    partial void OnSessionIdChanged(string value) => _ = LoadAsync();
    partial void OnSelectedFilterChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnFilterChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            SelectedFilter = value; // triggers ApplyFilter via OnSelectedFilterChanged
    }

    // Coverage worklist: the read-only "what's still to count" checklist for this session.
    [RelayCommand]
    private Task OpenChecklistAsync() =>
        Shell.Current.GoToAsync($"{nameof(PhysicalCountChecklistPage)}?SessionId={SessionId}");

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
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Genuine cancellation (navigated away) — the finally still renders any queued entries.
        }
        catch (Exception)
        {
            // Request timeout (TaskCanceledException with no cancellation) or a malformed payload:
            // fall back to queued entries + a message instead of crashing the app.
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

        // Group per SE / PPE. Known assets carry the classification on their snapshot; FoundAtStation
        // and not-yet-synced rows have none, so they fall into the "Unclassified / new" bucket.
        var groups = filtered
            .GroupBy(EntryGroup.KeyOf)
            .OrderBy(g => EntryGroup.SortOrder(g.Key))
            .Select(g => new EntryGroup(g.Key, g))
            .ToList();

        FilteredEntries = new ObservableCollection<EntryGroup>(groups);
    }

    private void UpdateCounts()
    {
        FoundCount = _allEntries.Count(e => e.Result == "Found");
        MissingCount = _allEntries.Count(e => e.Result == "NotFound");
        FoundAtStationCount = _allEntries.Count(e => e.Result == "FoundAtStation");
        QueuedCount = _allEntries.Count(e => e.Result == "Queued");
    }
}

// A SE / PPE section of the entries list. Derives from List&lt;T&gt; so CollectionView grouping binds
// the group's items directly, while Title/Count drive the section header.
public sealed class EntryGroup(string key, IEnumerable<PhysicalCountEntryDto> items)
    : List<PhysicalCountEntryDto>(items)
{
    public string Key { get; } = key;

    public string Title => Key switch
    {
        "SE" => "Semi-Expendable Property",
        "PPE" => "Property, Plant & Equipment",
        _ => "Unclassified / new items",
    };

    // Header count label, e.g. "3 items".
    public string CountLabel => Count == 1 ? "1 item" : $"{Count} items";

    // Classification bucket for one entry: "SE", "PPE", or "Other" (FoundAtStation / queued rows).
    public static string KeyOf(PhysicalCountEntryDto e) => e.AssetType switch
    {
        "SE" => "SE",
        "PPE" => "PPE",
        _ => "Other",
    };

    // SE first, then PPE, then the unclassified bucket last.
    public static int SortOrder(string key) => key switch { "SE" => 0, "PPE" => 1, _ => 2 };
}
