using AMIS.Framework.Shared.Identity.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AMIS.Framework.Web.Realtime;

/// <summary>
/// The single, app-wide SignalR hub. It stays decoupled from any feature module: it pushes to well-known
/// groups (<c>user:{id}</c>, <c>tenant:{id}</c>, <c>channel:{id}</c>) and relies only on the
/// <see cref="IUserChannelLookup"/> / <see cref="IChannelMembershipChecker"/> adapters that a feature module
/// (Chat) supplies. Feature modules broadcast inbound by resolving <c>IHubContext&lt;AppHub&gt;</c>.
/// </summary>
/// <remarks>
/// Identity is read from <see cref="HubCallerContext.User"/> (NOT <c>ICurrentUser</c>): <c>ICurrentUser</c>
/// flows through <c>IHttpContextAccessor</c>, whose negotiate <c>HttpContext</c> is not pinned to subsequent
/// hub invocations and returns nulls.
/// </remarks>
[Authorize]
public sealed class AppHub : Hub
{
    /// <summary>Route the hub is mapped at. Kept here so host wiring and the JWT handshake tweak stay in sync.</summary>
    public const string HubPath = "/api/v1/realtime/hub";

    private static readonly DistributedCacheEntryOptions TypingThrottleOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3),
    };

    private static readonly byte[] TypingMarker = [1];

    private readonly IUserChannelLookup _channelLookup;
    private readonly IChannelMembershipChecker _membershipChecker;
    private readonly IPresenceTracker _presence;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AppHub> _logger;

    public AppHub(
        IUserChannelLookup channelLookup,
        IChannelMembershipChecker membershipChecker,
        IPresenceTracker presence,
        IDistributedCache cache,
        ILogger<AppHub> logger)
    {
        _channelLookup = channelLookup;
        _membershipChecker = membershipChecker;
        _presence = presence;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>SignalR group name for a single user's connections.</summary>
    public static string UserGroup(Guid userId) => $"user:{userId}";

    /// <summary>SignalR group name for a tenant (used for presence fan-out).</summary>
    public static string TenantGroup(string tenantId) => $"tenant:{tenantId}";

    /// <summary>SignalR group name for a channel's members.</summary>
    public static string ChannelGroup(Guid channelId) => $"channel:{channelId}";

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var tenantId = Context.User?.GetTenant();
        var ct = Context.ConnectionAborted;

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId), ct).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId), ct).ConfigureAwait(false);
        }

        // Pre-join every channel the user already belongs to so broadcasts reach them without an explicit
        // JoinChannel round-trip. JoinChannel covers channels created after this connection.
        var channelIds = await _channelLookup.GetChannelIdsAsync(userId, ct).ConfigureAwait(false);
        foreach (var channelId in channelIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ChannelGroup(channelId), ct).ConfigureAwait(false);
        }

        // Only announce presence on the user's first live connection (additional tabs/devices are silent).
        if (_presence.Connect(userId) && !string.IsNullOrEmpty(tenantId))
        {
            await Clients.Group(TenantGroup(tenantId))
                .SendAsync(RealtimeEvents.PresenceChanged, new { UserId = userId, IsOnline = true }, ct)
                .ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var tenantId = Context.User?.GetTenant();

        if (_presence.Disconnect(userId) && !string.IsNullOrEmpty(tenantId))
        {
            await Clients.Group(TenantGroup(tenantId))
                .SendAsync(RealtimeEvents.PresenceChanged, new { UserId = userId, IsOnline = false })
                .ConfigureAwait(false);
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    /// <summary>
    /// Joins the caller to a channel group for channels created after the initial connection. Membership-gated
    /// and idempotent (re-joining an already-joined group is a no-op).
    /// </summary>
    public async Task JoinChannel(Guid channelId)
    {
        var userId = GetUserId();
        var isMember = await _membershipChecker
            .IsMemberAsync(channelId, userId, Context.ConnectionAborted)
            .ConfigureAwait(false);

        if (!isMember)
        {
            // HubException surfaces a clean message to the client without leaking channel existence.
            throw new HubException("You are not a member of this channel.");
        }

        await Groups
            .AddToGroupAsync(Context.ConnectionId, ChannelGroup(channelId), Context.ConnectionAborted)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Signals that the caller is typing in a channel. Throttled to one broadcast per user/channel per 3s via
    /// <see cref="IDistributedCache"/> so the camera-style key-stroke firing doesn't flood other clients.
    /// </summary>
    public async Task Typing(Guid channelId)
    {
        var userId = GetUserId();
        var ct = Context.ConnectionAborted;
        var throttleKey = $"realtime:typing:{channelId}:{userId}";

        if (await _cache.GetAsync(throttleKey, ct).ConfigureAwait(false) is not null)
        {
            return; // Already announced within the throttle window.
        }

        await _cache.SetAsync(throttleKey, TypingMarker, TypingThrottleOptions, ct).ConfigureAwait(false);

        await Clients.OthersInGroup(ChannelGroup(channelId))
            .SendAsync(RealtimeEvents.ChatTypingStarted, new { ChannelId = channelId, UserId = userId }, ct)
            .ConfigureAwait(false);
    }

    private Guid GetUserId()
    {
        var raw = Context.User?.GetUserId();
        if (Guid.TryParse(raw, out var userId))
        {
            return userId;
        }

        // [Authorize] should prevent this, but guard so a malformed principal fails loudly rather than
        // silently joining "user:00000000-..." groups.
        _logger.LogWarning("Realtime hub connection {ConnectionId} has no parseable user id claim.", Context.ConnectionId);
        throw new HubException("Could not determine the current user.");
    }
}
