using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AMIS.Blazor.Services.Api;

/// <summary>
/// The API rotates the refresh token on every refresh — the previous one is dead the moment a new
/// pair is issued. Tokens therefore cannot live in the auth cookie: a Blazor circuit cannot rewrite
/// the cookie (the SignalR response has already started, so <c>SignInAsync</c> throws), so the cookie
/// would keep serving the login-time refresh token long after it was rotated away, and the next
/// circuit — a reload, a second tab, a reconnect — would present a revoked token and be signed out.
///
/// Instead the cookie carries only an opaque session id, and the token pair lives here, shared by
/// every circuit of that browser session. Backed by <see cref="IDistributedCache"/>, which Program.cs
/// registers as Redis when <c>CachingOptions:Redis</c> is configured and in-memory otherwise.
/// </summary>
internal interface ISessionTokenStore
{
    Task<SessionTokens?> GetAsync(string sessionId, CancellationToken cancellationToken = default);

    Task SaveAsync(string sessionId, SessionTokens tokens, CancellationToken cancellationToken = default);

    Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default);
}

internal sealed record SessionTokens(string AccessToken, string RefreshToken);

internal sealed class SessionTokenStore(IDistributedCache cache, ILogger<SessionTokenStore> logger)
    : ISessionTokenStore
{
    /// <summary>Matches the auth cookie's 8h sliding expiration — the entry outlives the cookie only if unused.</summary>
    private static readonly DistributedCacheEntryOptions EntryOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(8)
    };

    private static string KeyFor(string sessionId) => $"blazor:session-tokens:{sessionId}";

    public async Task<SessionTokens?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = await cache.GetStringAsync(KeyFor(sessionId), cancellationToken);
            return payload is null ? null : JsonSerializer.Deserialize<SessionTokens>(payload);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A cache outage must not masquerade as a valid empty session — the caller treats null as
            // "signed out", so log loudly enough to tell the two apart in the field.
            logger.LogError(ex, "Failed to read session tokens from the distributed cache");
            return null;
        }
    }

    public async Task SaveAsync(string sessionId, SessionTokens tokens, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(tokens);
        await cache.SetStringAsync(KeyFor(sessionId), payload, EntryOptions, cancellationToken);
    }

    public async Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync(KeyFor(sessionId), cancellationToken);
    }
}
