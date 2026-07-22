using System.Diagnostics;

namespace AMIS.Maui.Services;

/// <summary>
/// Tracks which chat channels hold messages the user hasn't opened yet, so the Chat tab and the
/// channel rows can show an unread dot.
/// </summary>
/// <remarks>
/// The API has no read receipts — <see cref="ChatChannelDto.LastMessageAtUtc"/> is the only signal it
/// gives us. "Read" is therefore a per-channel timestamp kept on the device in <c>Preferences</c>
/// (a read marker is not sensitive, unlike tokens), and a channel counts as unread when its last
/// message is newer than that stamp. Live arrivals ride the existing <see cref="ChatHubService"/>
/// connection: the server pre-joins each connection to every channel the user belongs to, so
/// <c>ChatMessageCreated</c> reaches us for all of them without an explicit join.
/// Singleton — one source of truth for the app session.
/// </remarks>
public sealed class ChatUnreadService
{
    private const string LastReadKeyPrefix = "chat:lastread:";
    private const string IndexKey = "chat:lastread:index";

    private readonly IApiClient _apiClient;
    private readonly ChatHubService _hub;
    private readonly AuthStateService _authState;
    private readonly HashSet<Guid> _unread = [];
    private readonly object _gate = new();

    private Guid _openChannelId;
    private bool _subscribed;

    public ChatUnreadService(IApiClient apiClient, ChatHubService hub, AuthStateService authState)
    {
        _apiClient = apiClient;
        _hub = hub;
        _authState = authState;
    }

    /// <summary>Raised on the UI thread whenever the unread set changes.</summary>
    public event Action? Changed;

    /// <summary>True when at least one channel has messages the user hasn't opened.</summary>
    public bool HasUnread { get; private set; }

    public bool IsUnread(Guid channelId)
    {
        lock (_gate)
        {
            return _unread.Contains(channelId);
        }
    }

    /// <summary>
    /// Connects the realtime hub and reconciles unread state once at app start. Safe to call more
    /// than once — the hub start and the event subscription are both idempotent.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!_subscribed)
        {
            _hub.MessageCreated += OnMessageCreated;
            _subscribed = true;
        }

        await _hub.StartAsync(ct);
        await RefreshAsync(ct);
    }

    /// <summary>
    /// Re-derives unread state from the channel list. Best-effort: offline or a failing API leaves the
    /// current state untouched rather than falsely clearing the dot.
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        List<ChatChannelDto> channels;
        try
        {
            channels = await _apiClient.GetChatChannelsAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return; // navigating away / re-entrant refresh — not an error
        }
        catch (Exception ex)
        {
            // Never let a background unread sync take the app down; the dot just stays as it was.
            Debug.WriteLine($"[ChatUnread] refresh failed: {ex.Message}");
            return;
        }

        Reconcile(channels);
    }

    /// <summary>
    /// Re-derives unread state from a channel list the caller already fetched, so the chat screen
    /// doesn't pay for a second identical request just to refresh the dots.
    /// </summary>
    public void Reconcile(IReadOnlyList<ChatChannelDto> channels)
    {
        lock (_gate)
        {
            _unread.Clear();
            foreach (var channel in channels)
            {
                if (channel.LastMessageAtUtc is { } lastMessage
                    && channel.Id != _openChannelId
                    && lastMessage > GetLastRead(channel.Id))
                {
                    _unread.Add(channel.Id);
                }
            }
        }

        Publish();
    }

    /// <summary>
    /// Marks a channel read up to <paramref name="asOf"/> (defaults to now). Called when the user
    /// opens or leaves that channel's conversation.
    /// </summary>
    public void MarkRead(Guid channelId, DateTimeOffset? asOf = null)
    {
        if (channelId == Guid.Empty) return;

        var stamp = asOf ?? DateTimeOffset.UtcNow;
        if (stamp > GetLastRead(channelId))
        {
            Preferences.Default.Set(LastReadKeyPrefix + channelId.ToString("N"), stamp.UtcTicks);
            RememberChannel(channelId);
        }

        bool changed;
        lock (_gate)
        {
            changed = _unread.Remove(channelId);
        }

        if (changed) Publish();
    }

    /// <summary>
    /// Records which conversation is on screen. Messages arriving in it are read on arrival, so the
    /// dot never lights up for a channel the user is actively looking at.
    /// </summary>
    public void SetOpenChannel(Guid channelId)
    {
        SetOpenChannelId(channelId);
        if (channelId != Guid.Empty) MarkRead(channelId);
    }

    public void ClearOpenChannel() => SetOpenChannelId(Guid.Empty);

    // A Guid is 16 bytes — too wide to read or write atomically. Hub callbacks compare it from a
    // background thread while navigation sets it on the UI thread, so both sides take the lock.
    private void SetOpenChannelId(Guid channelId)
    {
        lock (_gate)
        {
            _openChannelId = channelId;
        }
    }

    private bool IsOpenChannel(Guid channelId)
    {
        lock (_gate)
        {
            return _openChannelId == channelId;
        }
    }

    /// <summary>Wipes every stored read marker. Called on logout so the next user starts clean.</summary>
    public void Clear()
    {
        foreach (var id in Preferences.Default.Get(IndexKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            Preferences.Default.Remove(LastReadKeyPrefix + id);
        }

        Preferences.Default.Remove(IndexKey);

        lock (_gate)
        {
            _unread.Clear();
            _openChannelId = Guid.Empty;
        }

        Publish();
    }

    // ── Realtime ──

    private void OnMessageCreated(ChatMessageDto message)
    {
        // Your own message, or one in the conversation you're reading right now, is already "seen".
        if (IsOpenChannel(message.ChannelId) || IsFromCurrentUser(message))
        {
            MarkRead(message.ChannelId, message.CreatedOnUtc);
            return;
        }

        bool changed;
        lock (_gate)
        {
            changed = _unread.Add(message.ChannelId);
        }

        if (changed) Publish();
    }

    private bool IsFromCurrentUser(ChatMessageDto message) =>
        _authState.UserProfile?.UserId is { } userId
        && string.Equals(message.SenderId, userId, StringComparison.OrdinalIgnoreCase);

    // ── Storage ──

    private static DateTimeOffset GetLastRead(Guid channelId)
    {
        var ticks = Preferences.Default.Get(LastReadKeyPrefix + channelId.ToString("N"), 0L);
        return ticks == 0 ? DateTimeOffset.MinValue : new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    // Preferences can't enumerate keys, so keep our own index of the ones we wrote — Clear() needs it.
    private static void RememberChannel(Guid channelId)
    {
        var id = channelId.ToString("N");
        var index = Preferences.Default.Get(IndexKey, "");
        if (index.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(id, StringComparer.Ordinal)) return;

        Preferences.Default.Set(IndexKey, index.Length == 0 ? id : $"{index},{id}");
    }

    private void Publish()
    {
        bool hasUnread;
        lock (_gate)
        {
            hasUnread = _unread.Count > 0;
        }

        HasUnread = hasUnread;

        // Hub callbacks arrive off the UI thread; consumers bind straight to this, so marshal here
        // once rather than making every subscriber remember to.
        MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke());
    }
}
