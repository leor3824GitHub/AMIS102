using System.Collections.ObjectModel;
using System.Text.Json;
using AMIS.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Dispatching;

namespace AMIS.Maui.Features.Chat;

[QueryProperty(nameof(ChannelId), "ChannelId")]
[QueryProperty(nameof(Title), "Title")]
public sealed partial class ChatConversationViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly ChatHubService _hub;
    private readonly AuthStateService _authState;

    private Guid _channelGuid;
    private Guid? _nextCursor;
    private DateTimeOffset _lastTypingSentUtc = DateTimeOffset.MinValue;
    private IDispatcherTimer? _typingTimer;
    private bool _subscribed;

    public ChatConversationViewModel(IApiClient apiClient, ChatHubService hub, AuthStateService authState)
    {
        _apiClient = apiClient;
        _hub = hub;
        _authState = authState;
    }

    /// <summary>Raised when the view should scroll to the newest message (initial load + new arrivals).</summary>
    public event Action? ScrollToBottomRequested;

    [ObservableProperty] private string _channelId = "";
    [ObservableProperty] private string _title = "Chat";
    [ObservableProperty] private ObservableCollection<ChatMessageItem> _messages = [];
    [ObservableProperty] private string _draft = "";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isLoadingOlder;
    [ObservableProperty] private bool _isSending;
    [ObservableProperty] private bool _someoneTyping;
    [ObservableProperty] private bool _hasMore;
    [ObservableProperty] private string? _errorMessage;

    private string? CurrentUserId => _authState.UserProfile?.UserId;

    /// <summary>Called from the page's OnAppearing: connect realtime, subscribe, and load the latest page.</summary>
    public async Task InitializeAsync()
    {
        if (!Guid.TryParse(ChannelId, out _channelGuid)) return;

        if (!_subscribed)
        {
            _hub.MessageCreated += OnMessageCreated;
            _hub.TypingStarted += OnTypingStarted;
            _subscribed = true;
        }

        await _hub.StartAsync();
        await _hub.JoinChannelAsync(_channelGuid);
        await LoadAsync();
    }

    /// <summary>Called from the page's OnDisappearing: stop listening (the hub connection stays alive).</summary>
    public void Teardown()
    {
        if (_subscribed)
        {
            _hub.MessageCreated -= OnMessageCreated;
            _hub.TypingStarted -= OnTypingStarted;
            _subscribed = false;
        }

        _typingTimer?.Stop();
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct = default)
    {
        if (_channelGuid == Guid.Empty) return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var page = await _apiClient.GetChatMessagesAsync(_channelGuid, before: null, take: 30, ct);
            // API returns newest-first; reverse to oldest→newest so the newest sits at the bottom.
            var items = Enumerable.Reverse(page.Items).Select(ToItem);
            Messages = new ObservableCollection<ChatMessageItem>(items);
            _nextCursor = page.NextCursor;
            HasMore = page.HasMore;
            ScrollToBottomRequested?.Invoke();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            ErrorMessage = "Could not load messages. Tap retry.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Loads the next older page (keyset by the oldest message we hold) and prepends it.</summary>
    [RelayCommand]
    private async Task LoadOlderAsync(CancellationToken ct = default)
    {
        if (_channelGuid == Guid.Empty || !HasMore || _nextCursor is null || IsLoadingOlder || IsLoading) return;

        IsLoadingOlder = true;
        try
        {
            var page = await _apiClient.GetChatMessagesAsync(_channelGuid, before: _nextCursor, take: 30, ct);
            // Same newest-first payload; reverse and prepend so the window stays oldest→newest. The cursor is
            // exclusive, so these never overlap what we already hold. KeepItemsInView keeps the view anchored.
            var older = Enumerable.Reverse(page.Items).Select(ToItem).ToList();
            for (var i = 0; i < older.Count; i++)
            {
                Messages.Insert(i, older[i]);
            }
            _nextCursor = page.NextCursor;
            HasMore = page.HasMore;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            ErrorMessage = "Could not load earlier messages. Try again.";
        }
        finally
        {
            IsLoadingOlder = false;
        }
    }

    [RelayCommand]
    private async Task SendAsync(CancellationToken ct = default)
    {
        var content = Draft?.Trim();
        if (_channelGuid == Guid.Empty || string.IsNullOrWhiteSpace(content) || IsSending) return;

        IsSending = true;
        try
        {
            var sent = await _apiClient.SendChatMessageAsync(_channelGuid, content, ct);
            if (sent is not null)
            {
                AddOrUpdate(sent); // the hub echo (if any) is de-duped by id
            }
            Draft = "";
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            ErrorMessage = "Could not send. Check your connection and try again.";
        }
        finally
        {
            IsSending = false;
        }
    }

    partial void OnDraftChanged(string value)
    {
        // Throttle typing signals to ~once per 2s (server also throttles to 3s).
        if (_channelGuid != Guid.Empty
            && !string.IsNullOrEmpty(value)
            && (DateTimeOffset.UtcNow - _lastTypingSentUtc).TotalSeconds >= 2)
        {
            _lastTypingSentUtc = DateTimeOffset.UtcNow;
            _ = _hub.TypingAsync(_channelGuid);
        }
    }

    // ── Realtime callbacks (off the UI thread → marshal with MainThread) ──

    private void OnMessageCreated(ChatMessageDto message)
    {
        if (message.ChannelId != _channelGuid) return;
        MainThread.BeginInvokeOnMainThread(() => AddOrUpdate(message));
    }

    private void OnTypingStarted(ChatTypingDto notification)
    {
        if (notification.ChannelId != _channelGuid) return;
        if (CurrentUserId is not null &&
            string.Equals(notification.UserId.ToString(), CurrentUserId, StringComparison.OrdinalIgnoreCase))
        {
            return; // ignore our own typing echo
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            SomeoneTyping = true;
            _typingTimer ??= CreateTypingTimer();
            _typingTimer.Stop();
            _typingTimer.Start();
        });
    }

    private IDispatcherTimer CreateTypingTimer()
    {
        var timer = Application.Current!.Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(4);
        timer.IsRepeating = false;
        timer.Tick += (_, _) => SomeoneTyping = false;
        return timer;
    }

    private void AddOrUpdate(ChatMessageDto dto)
    {
        var item = ToItem(dto);
        for (var i = 0; i < Messages.Count; i++)
        {
            if (Messages[i].Id == item.Id)
            {
                Messages[i] = item;
                return;
            }
        }

        Messages.Add(item); // newest appended last (ascending order)
        ScrollToBottomRequested?.Invoke();
    }

    private ChatMessageItem ToItem(ChatMessageDto dto) => new(dto, CurrentUserId);
}
