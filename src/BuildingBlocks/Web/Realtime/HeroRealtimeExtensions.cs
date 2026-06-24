using AMIS.Framework.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace AMIS.Framework.Web.Realtime;

/// <summary>
/// Registration and endpoint mapping for the app-wide realtime hub (<see cref="AppHub"/>).
/// </summary>
public static class HeroRealtimeExtensions
{
    private const string RedisChannelPrefix = "amis-signalr";

    /// <summary>
    /// Registers SignalR, the in-memory presence tracker, and — when <c>CachingOptions:Redis</c> is configured —
    /// the StackExchange.Redis backplane (channel prefix <c>amis-signalr</c>) for multi-replica fan-out.
    /// Call before <c>builder.Build()</c>.
    /// </summary>
    /// <remarks>
    /// This does <b>not</b> register <see cref="IUserChannelLookup"/> / <see cref="IChannelMembershipChecker"/>;
    /// a feature module (Chat) must supply those. The hub only resolves them when a client connects, so the
    /// app still boots and the presence endpoint still works without them — but hub connections will fail with
    /// a DI resolution error until a module provides them. See <c>CHAT-PORT-PLAN.md</c> Phase 0.
    /// </remarks>
    public static IHostApplicationBuilder AddHeroRealtime(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();

        var signalR = builder.Services.AddSignalR();

        var redis = builder.Configuration.GetSection(nameof(CachingOptions)).Get<CachingOptions>()?.Redis;
        if (!string.IsNullOrWhiteSpace(redis))
        {
            signalR.AddStackExchangeRedis(redis, options =>
                options.Configuration.ChannelPrefix = RedisChannel.Literal(RedisChannelPrefix));
        }

        return builder;
    }

    /// <summary>
    /// Maps the realtime hub at <see cref="AppHub.HubPath"/> and a <c>GET /api/v1/realtime/presence</c> snapshot
    /// endpoint. Call after <c>app.UseHeroPlatform(...)</c> so routing, authentication, and authorization are in
    /// front of the hub's <c>[Authorize]</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapHeroRealtime(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapHub<AppHub>(AppHub.HubPath);

        endpoints.MapGet("/api/v1/realtime/presence",
                (IPresenceTracker presence) => TypedResults.Ok(new PresenceSnapshot(presence.OnlineUsers())))
            .WithName("Realtime_GetPresence")
            .WithTags("Realtime")
            .WithSummary("Returns the set of users currently online on this host.")
            .RequireAuthorization();

        return endpoints;
    }
}

/// <summary>Snapshot of users currently online, returned by <c>GET /api/v1/realtime/presence</c>.</summary>
public sealed record PresenceSnapshot(IReadOnlyCollection<Guid> OnlineUserIds);
