using System.Security.Claims;

namespace AMIS.Blazor.Services.Api;

/// <summary>
/// Per-circuit view of the current session's tokens.
///
/// L1 is an in-memory copy scoped to this circuit; L2 is <see cref="ISessionTokenStore"/>, shared by
/// every circuit of the same browser session. A fresh circuit (page reload, second tab, SignalR
/// reconnect) starts with an empty L1 and hydrates from L2, so it picks up whatever token pair the
/// previous circuit rotated to instead of replaying the dead login-time token from the cookie.
///
/// Registered as Scoped, so each Blazor circuit gets its own instance.
/// </summary>
internal interface ICircuitTokenCache
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a freshly rotated pair to both this circuit and the shared session store.</summary>
    Task UpdateTokensAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Ends the session: drops this circuit's copy and deletes the shared pair.</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

internal sealed class CircuitTokenCache(
    ISessionTokenStore tokenStore,
    IHttpContextAccessor httpContextAccessor) : ICircuitTokenCache
{
    /// <summary>Claim that carries the session id. The token pair itself is never put in the cookie.</summary>
    internal const string SessionIdClaim = "amis_session_id";

    private SessionTokens? _tokens;

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        (await GetTokensAsync(cancellationToken))?.AccessToken;

    public async Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default) =>
        (await GetTokensAsync(cancellationToken))?.RefreshToken;

    public async Task UpdateTokensAsync(
        string accessToken,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        _tokens = new SessionTokens(accessToken, refreshToken);

        var sessionId = GetSessionId();
        if (sessionId is not null)
        {
            await tokenStore.SaveAsync(sessionId, _tokens, cancellationToken);
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _tokens = null;

        var sessionId = GetSessionId();
        if (sessionId is not null)
        {
            await tokenStore.RemoveAsync(sessionId, cancellationToken);
        }
    }

    private async Task<SessionTokens?> GetTokensAsync(CancellationToken cancellationToken)
    {
        if (_tokens is not null)
        {
            return _tokens;
        }

        var sessionId = GetSessionId();
        if (sessionId is null)
        {
            return null;
        }

        _tokens = await tokenStore.GetAsync(sessionId, cancellationToken);
        return _tokens;
    }

    private string? GetSessionId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true
            ? user.FindFirst(SessionIdClaim)?.Value
            : null;
    }
}

/// <summary>Reads the session id claim off a principal — shared by the login/logout endpoints.</summary>
internal static class SessionIdClaimExtensions
{
    public static string? GetSessionId(this ClaimsPrincipal? user) =>
        user?.FindFirst(CircuitTokenCache.SessionIdClaim)?.Value;
}
