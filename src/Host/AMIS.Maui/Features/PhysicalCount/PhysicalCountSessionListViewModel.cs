using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Playground.Maui.Services;
using System.Collections.ObjectModel;

namespace Playground.Maui.Features.PhysicalCount;

public sealed partial class PhysicalCountSessionListViewModel(
    IApiClient apiClient,
    IPhysicalCountSyncService syncService) : ObservableObject
{
    [ObservableProperty] private ObservableCollection<PhysicalCountSessionSummaryDto> _sessions = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _pendingSyncCount;
    [ObservableProperty] private bool _hasPendingSync;

    partial void OnPendingSyncCountChanged(int value) => HasPendingSync = value > 0;

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

            PendingSyncCount = await syncService.GetPendingCountAsync();

            if (PendingSyncCount > 0 && Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
                await syncService.FlushPendingAsync(ct);
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
    public async Task OpenSessionAsync(PhysicalCountSessionSummaryDto session, CancellationToken ct = default) =>
        await Shell.Current.GoToAsync($"{nameof(PhysicalCountWalkthroughPage)}?SessionId={session.Id}");
}
