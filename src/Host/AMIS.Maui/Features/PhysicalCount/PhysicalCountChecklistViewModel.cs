using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Features.Asset;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;

namespace AMIS.Maui.Features.PhysicalCount;

// Read-only coverage worklist for a count session: every in-scope asset with a Counted / Missing /
// Uncounted tag, grouped by PPE / SE, filterable by location + accountable officer, and searchable.
// The whole in-scope set is fetched once and filtered/grouped client-side (instant toggles, works
// offline from the SQLite cache). Recording still happens on the Scan screen — this only shows
// what's left to find.
[QueryProperty(nameof(SessionId), "SessionId")]
public sealed partial class PhysicalCountChecklistViewModel : ObservableObject
{
    public const string AllLocations = "All locations";
    public const string AllOfficers = "All officers";

    private readonly IApiClient _apiClient;
    private readonly IPhysicalCountChecklistCache _cache;
    private List<PhysicalCountChecklistItemDto> _allItems = [];

    public PhysicalCountChecklistViewModel(IApiClient apiClient, IPhysicalCountChecklistCache cache)
    {
        _apiClient = apiClient;
        _cache = cache;
    }

    [ObservableProperty] public partial string SessionId { get; set; } = "";
    [ObservableProperty] public partial string SessionCode { get; set; } = "";
    [ObservableProperty] public partial string FundCluster { get; set; } = "";
    [ObservableProperty] public partial string Scope { get; set; } = "";
    [ObservableProperty] public partial string SessionStatus { get; set; } = "";
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string? CachedBannerText { get; set; }

    // ── Filters ──
    // Location/officer facets carry the entity Id (not just the display name) so two same-named
    // locations or officers can't collide into one filter. The "All …" sentinel has a null Id.
    [ObservableProperty] public partial string SelectedType { get; set; } = "All";           // "All" | "PPE" | "SE"
    [ObservableProperty] public partial ObservableCollection<FacetOption> Locations { get; set; } = [new(null, AllLocations)];
    [ObservableProperty] public partial FacetOption? SelectedLocation { get; set; } = new(null, AllLocations);
    [ObservableProperty] public partial ObservableCollection<FacetOption> Officers { get; set; } = [new(null, AllOfficers)];
    [ObservableProperty] public partial FacetOption? SelectedOfficer { get; set; } = new(null, AllOfficers);
    [ObservableProperty] public partial bool UncountedOnly { get; set; }
    [ObservableProperty] public partial string SearchText { get; set; } = "";

    // ── Progress (whole session, unfiltered) ──
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResolvedCount), nameof(CountedFraction), nameof(ProgressSummary), nameof(RemainingSummary), nameof(IsComplete))]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResolvedCount), nameof(CountedFraction), nameof(ProgressSummary), nameof(RemainingSummary), nameof(IsComplete))]
    public partial int CountedCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResolvedCount), nameof(CountedFraction), nameof(ProgressSummary), nameof(RemainingSummary), nameof(IsComplete))]
    public partial int MissingCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResolvedCount), nameof(CountedFraction), nameof(ProgressSummary), nameof(RemainingSummary), nameof(IsComplete))]
    public partial int UncountedCount { get; set; }

    [ObservableProperty] public partial ObservableCollection<ChecklistGroup> Groups { get; set; } = [];

    // An item is "resolved" once it's Counted or marked Missing; Uncounted is what's still to find.
    public int ResolvedCount => CountedCount + MissingCount;
    public double CountedFraction => TotalCount == 0 ? 0 : (double)ResolvedCount / TotalCount;
    public string ProgressSummary => $"{ResolvedCount} / {TotalCount} · {(int)Math.Round(CountedFraction * 100)}%";
    public string RemainingSummary => $"{UncountedCount} remaining · {MissingCount} missing";
    public bool IsComplete => TotalCount > 0 && UncountedCount == 0;

    partial void OnSessionIdChanged(string value) => _ = LoadAsync();
    partial void OnSelectedTypeChanged(string value) => ApplyFilter();
    partial void OnSelectedLocationChanged(FacetOption? value) => ApplyFilter();
    partial void OnSelectedOfficerChanged(FacetOption? value) => ApplyFilter();
    partial void OnUncountedOnlyChanged(bool value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (!Guid.TryParse(SessionId, out var id)) return;
        IsLoading = true;
        ErrorMessage = null;
        CachedBannerText = null;

        // Stale-while-revalidate: paint the cache immediately (first load only), then refresh.
        var (cached, cachedAt) = await _cache.GetAsync(id);
        if (cached.Count > 0 && _allItems.Count == 0)
        {
            _allItems = cached;
            RebuildFacetsAndCounts();
            ApplyFilter();
        }

        try
        {
            var dto = await _apiClient.GetPhysicalCountChecklistAsync(id, ct);
            SessionCode = dto.Code;
            FundCluster = dto.FundCluster;
            Scope = dto.Scope;
            SessionStatus = dto.Status;
            _allItems = dto.Items;
            await _cache.SaveAsync(id, dto.Items);
            RebuildFacetsAndCounts();
            ApplyFilter();
        }
        catch (HttpRequestException) when (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            ReportLoadFailure(offline: true, cachedAt);
        }
        catch (HttpRequestException)
        {
            ReportLoadFailure(offline: false, cachedAt);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Genuine cancellation (page closed / refresh superseded) — nothing to surface.
        }
        catch (Exception)
        {
            // Request timeout (TaskCanceledException with no cancellation) or a malformed payload:
            // report gracefully instead of letting the RefreshView command rethrow on the UI thread.
            ReportLoadFailure(offline: false, cachedAt);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Picks the right non-fatal banner after a load/refresh failure. Key it off whether a list is
    // already on screen (from the SQLite cache-paint or a prior successful load) — not just this call's
    // cache snapshot — so a failed refresh over visible data reads as a soft amber "showing what you
    // have" note instead of a red "couldn't load" error. The hard error is reserved for a truly empty
    // screen where there's nothing to fall back to.
    private void ReportLoadFailure(bool offline, DateTimeOffset? cachedAt)
    {
        if (_allItems.Count > 0)
            CachedBannerText = offline
                ? $"Offline — showing the checklist cached {FormatAge(cachedAt)}."
                : $"Couldn't refresh — showing the checklist cached {FormatAge(cachedAt)}.";
        else
            ErrorMessage = offline
                ? "You're offline and this session isn't cached yet. Connect once to download the checklist."
                : "Couldn't load the checklist. Pull down to retry.";
    }

    [RelayCommand]
    private void SelectType(string type) => SelectedType = type;

    // Tap a row to inspect the asset (location, custodian, ICS/PAR link) on the shared detail screen.
    // Read-only — recording stays on the Scan screen, consistent with this worklist's purpose.
    [RelayCommand]
    private Task OpenAsset(PhysicalCountChecklistItemDto? item) =>
        item is null
            ? Task.CompletedTask
            : Shell.Current.GoToAsync($"{nameof(AssetDetailPage)}?PropertyNo={Uri.EscapeDataString(item.PropertyNo)}");

    // Rebuilds the location/officer filter option lists and the session-wide progress counts from
    // the current universe. Preserves the active selection when it still exists after a refresh.
    private void RebuildFacetsAndCounts()
    {
        TotalCount = _allItems.Count;
        CountedCount = _allItems.Count(i => i.IsCounted);
        MissingCount = _allItems.Count(i => i.IsMissing);
        UncountedCount = _allItems.Count(i => i.IsUncounted);

        var locOptions = _allItems
            .Where(i => i.LocationId is not null && !string.IsNullOrWhiteSpace(i.LocationName))
            .Select(i => new FacetOption(i.LocationId, i.LocationName!))
            .DistinctBy(o => o.Id)
            .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var keepLoc = SelectedLocation;
        Locations = new ObservableCollection<FacetOption>([new(null, AllLocations), .. locOptions]);
        SelectedLocation = keepLoc is not null && Locations.Contains(keepLoc) ? keepLoc : Locations[0];

        var offOptions = _allItems
            .Where(i => i.CustodianId is not null && !string.IsNullOrWhiteSpace(i.AccountableOfficer))
            .Select(i => new FacetOption(i.CustodianId, i.AccountableOfficer!))
            .DistinctBy(o => o.Id)
            .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var keepOff = SelectedOfficer;
        Officers = new ObservableCollection<FacetOption>([new(null, AllOfficers), .. offOptions]);
        SelectedOfficer = keepOff is not null && Officers.Contains(keepOff) ? keepOff : Officers[0];
    }

    private void ApplyFilter()
    {
        IEnumerable<PhysicalCountChecklistItemDto> q = _allItems;

        if (SelectedType is "PPE" or "SE")
            q = q.Where(i => i.AssetType == SelectedType);
        if (SelectedLocation?.Id is { } locId)
            q = q.Where(i => i.LocationId == locId);
        if (SelectedOfficer?.Id is { } offId)
            q = q.Where(i => i.CustodianId == offId);
        if (UncountedOnly)
            q = q.Where(i => i.IsUncounted);
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var t = SearchText.Trim();
            q = q.Where(i =>
                i.PropertyNo.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = q.ToList();
        var groups = new ObservableCollection<ChecklistGroup>();

        // Fixed PPE-then-SE order; within a group, uncounted float to the top (the worklist target).
        foreach (var (type, title) in new[]
        {
            ("PPE", "PPE · Property, Plant & Equipment"),
            ("SE", "SE · Semi-Expendable"),
        })
        {
            var rows = filtered
                .Where(i => i.AssetType == type)
                .OrderBy(i => StatusRank(i.Status))
                .ThenBy(i => i.PropertyNo, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (rows.Count == 0) continue;

            var resolved = rows.Count(i => !i.IsUncounted);
            groups.Add(new ChecklistGroup(title, $"{resolved} / {rows.Count}", rows));
        }

        Groups = groups;
    }

    private static int StatusRank(string status) => status switch
    {
        "Uncounted" => 0,
        "Missing" => 1,
        _ => 2,
    };

    private static string FormatAge(DateTimeOffset? at)
    {
        if (at is null) return "recently";
        var span = DateTimeOffset.UtcNow - at.Value;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} h ago";
        return $"{(int)span.TotalDays} d ago";
    }
}

// A location/officer filter choice: the entity Id (null for the "All …" sentinel) plus its display
// name. Record value-equality lets the picker preserve the active selection across a refresh.
public sealed record FacetOption(Guid? Id, string Name);

// CollectionView group: one AssetType section (PPE / SE) with a coverage header (e.g. "8 / 20").
public sealed class ChecklistGroup : List<PhysicalCountChecklistItemDto>
{
    public string Title { get; }
    public string Coverage { get; }

    public ChecklistGroup(string title, string coverage, IEnumerable<PhysicalCountChecklistItemDto> items)
        : base(items.ToList())
    {
        Title = title;
        Coverage = coverage;
    }
}
