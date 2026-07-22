using System.Collections.ObjectModel;
using System.Diagnostics;
using AMIS.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AMIS.Maui.Features.Chat;

public sealed partial class ChatChannelListViewModel(
    IApiClient apiClient,
    ChatHubService hub,
    ChatUnreadService unread) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChannelCount))]
    [NotifyPropertyChangedFor(nameof(ShowSkeleton))]
    public partial ObservableCollection<ChatChannelItem> Channels { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSkeleton))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    public int ChannelCount => Channels.Count;

    // First-load skeleton only; RefreshView's spinner covers reloads.
    public bool ShowSkeleton => IsLoading && Channels.Count == 0;

    private bool _subscribed;

    /// <summary>Called from the page's OnAppearing: follow live unread changes while the list is shown.</summary>
    public void Attach()
    {
        // Guarded: OnAppearing can fire more than once per OnDisappearing, and the service is a
        // singleton — a double subscription would outlive this VM by one handler.
        if (_subscribed) return;
        unread.Changed += OnUnreadChanged;
        _subscribed = true;
    }

    /// <summary>Called from the page's OnDisappearing: the unread service outlives this VM, so let go.</summary>
    public void Detach()
    {
        if (!_subscribed) return;
        unread.Changed -= OnUnreadChanged;
        _subscribed = false;
    }

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

            // Hand the service the list we just fetched instead of letting it request its own copy.
            unread.Reconcile(channels);

            Channels = new ObservableCollection<ChatChannelItem>(
                channels.Select(channel => new ChatChannelItem(channel, unread.IsUnread(channel.Id))));
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
    private async Task OpenChannelAsync(ChatChannelItem? item)
    {
        if (item is null) return;

        // Clear the row's dot immediately — the conversation itself also marks the channel read, but
        // doing it here means the list is already correct when the user navigates back.
        unread.MarkRead(item.Id);

        await Shell.Current.GoToAsync(
            $"{nameof(ChatConversationPage)}?ChannelId={item.Id}&Title={Uri.EscapeDataString(item.DisplayName)}");
    }

    // A message can land while the list is on screen; repaint the affected rows in place.
    private void OnUnreadChanged()
    {
        foreach (var item in Channels)
        {
            item.IsUnread = unread.IsUnread(item.Id);
        }
    }
}
