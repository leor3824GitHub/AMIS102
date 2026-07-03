using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AMIS.Maui.Services;
using System.Collections.ObjectModel;

namespace AMIS.Maui.Features.PhysicalCount;

public sealed partial class PhysicalCountSessionListViewModel(
    IApiClient apiClient,
    IPhysicalCountSyncService syncService) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<PhysicalCountSessionSummaryDto> _sessions = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQueuedWork))]
    [NotifyPropertyChangedFor(nameof(SyncBannerText))]
    private int _pendingSyncCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQueuedWork))]
    [NotifyPropertyChangedFor(nameof(HasFailedSync))]
    [NotifyPropertyChangedFor(nameof(SyncBannerText))]
    private int _failedSyncCount;

    [ObservableProperty] private string? _syncStatusMessage;

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
