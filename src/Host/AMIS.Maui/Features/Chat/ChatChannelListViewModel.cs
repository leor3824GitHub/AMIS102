using System.Collections.ObjectModel;
using System.Diagnostics;
using AMIS.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMIS.Maui.Features.Chat;

public sealed partial class ChatChannelListViewModel(
    IApiClient apiClient,
    ChatHubService hub) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChannelCount))]
    [NotifyPropertyChangedFor(nameof(ShowSkeleton))]
    public partial ObservableCollection<ChatChannelDto> Channels { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSkeleton))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    public int ChannelCount => Channels.Count;

    // First-load skeleton only; RefreshView's spinner covers reloads.
    public bool ShowSkeleton => IsLoading && Channels.Count == 0;

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            // Connect the realtime hub on entering chat so the conversation opens already live.
            await hub.StartAsync(ct);

            var channels = await apiClient.GetChatChannelsAsync(ct);
            Channels = new ObservableCollection<ChatChannelDto>(channels);
        }
        catch (OperationCanceledException)
        {
            // Pull-to-refresh re-entry or navigating away cancels the in-flight load — expected, not an error.
            // (RefreshView's IsRefreshing two-way binding can re-fire LoadCommand and cancel the prior token.)
        }
        catch (Exception ex)
        {
            // Realtime + REST are best-effort here; an unhandled exception in this async command would be
            // re-thrown on the UI thread by AsyncRelayCommand and hard-crash the (Windows) app. Never let it.
            Debug.WriteLine($"[ChatChannelList] load failed: {ex}");
            ErrorMessage = "Could not load channels. Pull down to retry.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private static async Task OpenChannelAsync(ChatChannelDto? channel)
    {
        if (channel is null) return;
        await Shell.Current.GoToAsync(
            $"{nameof(ChatConversationPage)}?ChannelId={channel.Id}&Title={Uri.EscapeDataString(channel.DisplayName)}");
    }
}
