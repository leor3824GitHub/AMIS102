using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;

namespace AMIS.Maui.Features.PhysicalCount;

public sealed partial class PhysicalCountSessionListViewModel(
    IApiClient apiClient,
    IPhysicalCountSyncService syncService) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SessionCount))]
    [NotifyPropertyChangedFor(nameof(OngoingCount))]
    [NotifyPropertyChangedFor(nameof(ShowSkeleton))]
    public partial ObservableCollection<PhysicalCountSessionSummaryDto> Sessions { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSkeleton))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    public int SessionCount => Sessions.Count;
    public int OngoingCount => Sessions.Count(s => s.Status == "Ongoing");

    // First-load skeleton only: once rows exist, RefreshView's own spinner covers reloads.
    public bool ShowSkeleton => IsLoading && Sessions.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQueuedWork))]
    [NotifyPropertyChangedFor(nameof(SyncBannerText))]
    public partial int PendingSyncCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQueuedWork))]
    [NotifyPropertyChangedFor(nameof(HasFailedSync))]
    [NotifyPropertyChangedFor(nameof(SyncBannerText))]
    public partial int FailedSyncCount { get; set; }

    [ObservableProperty] public partial string? SyncStatusMessage { get; set; }

    public bool HasQueuedWork => PendingSyncCount > 0 || FailedSyncCount > 0;
    public bool HasFailedSync => FailedSyncCount > 0;

    public string SyncBannerText => FailedSyncCount > 0
        ? $"{PendingSyncCount} pending · {FailedSyncCount} failed to sync"
        : $"{PendingSyncCount} entry(s) pending sync — tap Sync to upload.";

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var list = await apiClient.GetPhysicalCountSessionsAsync(ct);
            Sessions = new ObservableCollection<PhysicalCountSessionSummaryDto>(
                list.OrderByDescending(s => s.CountDate));

            await RefreshSyncCountsAsync();

            if (PendingSyncCount > 0 && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                await syncService.FlushPendingAsync(ct);
                await RefreshSyncCountsAsync();
            }
        }
        catch (HttpRequestException) when (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            ErrorMessage = "Offline — connect to load sessions.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Could not load sessions. Pull down to retry.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FlushPendingAsync(CancellationToken ct = default)
    {
        SyncStatusMessage = null;
        var result = await syncService.FlushPendingAsync(ct);
        await RefreshSyncCountsAsync();
        if (result.Failed > 0)
            SyncStatusMessage = $"{result.Sent} synced, {result.Failed} failed: {result.LastError}";
        else if (result.Sent > 0)
            SyncStatusMessage = $"Synced {result.Sent} entry(s).";
        else
            SyncStatusMessage = "Nothing to sync.";
        // Reload so the freshly-synced entries show up in the session progress numbers.
        await LoadAsync(ct);
    }

    [RelayCommand]
    private async Task DiscardFailedAsync()
    {
        await syncService.DiscardFailedAsync();
        await RefreshSyncCountsAsync();
        SyncStatusMessage = "Discarded failed entries.";
    }

    [RelayCommand]
    public async Task OpenSessionAsync(PhysicalCountSessionSummaryDto session, CancellationToken ct = default)
    {
        // Finished (Closed) sessions open the read-only Entries review; everything else opens the Scan
        // screen so the scan-to-add UI is always reachable (Draft shows a "recording disabled" notice).
        var route = session.IsReviewOnly ? nameof(PhysicalCountEntriesPage) : nameof(PhysicalCountScanPage);
        await Shell.Current.GoToAsync($"{route}?SessionId={session.Id}");
    }

    private async Task RefreshSyncCountsAsync()
    {
        PendingSyncCount = await syncService.GetPendingCountAsync();
        FailedSyncCount = await syncService.GetFailedCountAsync();
    }
}
