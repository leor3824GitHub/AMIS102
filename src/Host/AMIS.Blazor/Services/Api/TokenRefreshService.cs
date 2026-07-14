using AMIS.Blazor.ApiClient;
using System.Diagnostics.Metrics;
using System.Security.Claims;

namespace AMIS.Blazor.Services.Api;

/// <summary>
/// Service responsible for refreshing expired access tokens using the refresh token.
/// </summary>
internal interface ITokenRefreshService
{
    Task<string?> TryRefreshTokenAsync(CancellationToken cancellationToken = default);
}

internal sealed class TokenRefreshService : ITokenRefreshService, IDisposable
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenClient _tokenClient;
    private readonly ICircuitTokenCache _circuitTokenCache;
    private readonly ILogger<TokenRefreshService> _logger;
    // Instance-scoped lock & caches: scope these to the circuit (scoped service).
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private static readonly TimeSpan RefreshCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FailedTokenCacheDuration = TimeSpan.FromHours(24);  // Session-long cache to prevent retry spam

    // Process-level metrics (safe to aggregate across circuits)
    private static readonly Meter RefreshMeter = new("AMIS.Blazor.Auth", "1.0.0");
    private static readonly Counter<long> RefreshAttemptsCounter = RefreshMeter.CreateCounter<long>("blazor_token_refresh_attempts_total");
    private static readonly Counter<long> RefreshSuccessCounter = RefreshMeter.CreateCounter<long>("blazor_token_refresh_success_total");
    private static readonly Counter<long> RefreshFailuresCounter = RefreshMeter.CreateCounter<long>("blazor_token_refresh_failures_total");

    // Per-instance cache / failure state (scoped to the Blazor circuit)
    private string? _lastRefreshedToken;
    private string? _cachedForRefreshToken;
    private DateTime _lastRefreshTime = DateTime.MinValue;
    private string? _failedRefreshToken;
    private DateTime _failedRefreshTime = DateTime.MinValue;
    private bool _permanentFailureFlag;  // Fast-fail once a token is permanently invalid for this circuit

    public TokenRefreshService(
        IHttpContextAccessor httpContextAccessor,
        ITokenClient tokenClient,
        ICircuitTokenCache circuitTokenCache,
        ILogger<TokenRefreshService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenClient = tokenClient;
        _circuitTokenCache = circuitTokenCache;
        _logger = logger;
    }

    public async Task<string?> TryRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            _logger.LogDebug("HttpContext is not available for token refresh");
            return null;
        }

        var currentRefreshToken = await _circuitTokenCache.GetRefreshTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(currentRefreshToken))
        {
            _logger.LogDebug("No refresh token available");
            return null;
        }

        if (IsTokenRecentlyFailed(currentRefreshToken))
        {
            _logger.LogDebug("Skipping refresh - refresh token is permanently invalid for this session");
            return null;
        }

        if (_permanentFailureFlag)
        {
            _logger.LogDebug("Skipping refresh - session already marked as requiring re-authentication");
            return null;
        }

        var cachedToken = TryGetCachedToken(currentRefreshToken);
        if (cachedToken is not null)
        {
            return cachedToken;
        }

        return await RefreshWithLockAsync(httpContext, currentRefreshToken, cancellationToken);
    }

    private bool IsTokenRecentlyFailed(string refreshToken) =>
        _failedRefreshToken == refreshToken &&
        DateTime.UtcNow - _failedRefreshTime < FailedTokenCacheDuration;

    private string? TryGetCachedToken(string currentRefreshToken)
    {
        if (_lastRefreshedToken is not null &&
            _cachedForRefreshToken == currentRefreshToken &&
            DateTime.UtcNow - _lastRefreshTime < RefreshCacheDuration)
        {
            return _lastRefreshedToken;
        }
        return null;
    }

    private async Task<string?> RefreshWithLockAsync(
        HttpContext httpContext,
        string currentRefreshToken,
        CancellationToken cancellationToken)
    {
        if (!await _refreshLock.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken))
        {
            _logger.LogWarning("Token refresh lock acquisition timed out");
            return null;
        }

        try
        {
            // Re-check cache after acquiring lock
            var cachedToken = TryGetCachedToken(currentRefreshToken);
            if (cachedToken is not null)
            {
                return cachedToken;
            }

            return await ExecuteRefreshAsync(httpContext, currentRefreshToken, cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string?> ExecuteRefreshAsync(
        HttpContext httpContext,
        string currentRefreshToken,
        CancellationToken cancellationToken)
    {
        RefreshAttemptsCounter.Add(1);

        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var tokens = await GetCurrentTokensAsync(user, cancellationToken);
        if (tokens is null)
        {
            return null;
        }

        try
        {
            var refreshResponse = await CallRefreshApiAsync(tokens.Value, cancellationToken);
            if (refreshResponse is null)
            {
                return null;
            }

            // Persist to the shared session store — this is what lets the next circuit (reload,
            // second tab, reconnect) use the rotated pair instead of the dead login-time one.
            await UpdateCachesAsync(refreshResponse, currentRefreshToken, cancellationToken);

            RefreshSuccessCounter.Add(1);
            _logger.LogInformation("Access token refreshed successfully");
            return refreshResponse.Token;
        }
        catch (ApiException ex) when (ex.StatusCode == 400 || ex.StatusCode == 401)
        {
            var reasonCode = ResolveFailureReasonCode(ex);
            await HandleRefreshFailureAsync(currentRefreshToken, ex, reasonCode, cancellationToken);
            return null;
        }
        catch (Exception ex)
        {
            RefreshFailuresCounter.Add(1, new KeyValuePair<string, object?>("reason", RefreshFailureReasonCodes.UnexpectedError));
            _logger.LogError(ex, "Failed to refresh access token");
            return null;
        }
    }

    private async Task<(string AccessToken, string RefreshToken, string Tenant)?> GetCurrentTokensAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var currentAccessToken = await _circuitTokenCache.GetAccessTokenAsync(cancellationToken);
        var refreshToken = await _circuitTokenCache.GetRefreshTokenAsync(cancellationToken);
        var tenant = user.FindFirst("tenant")?.Value ?? "root";

        if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(currentAccessToken))
        {
            return null;
        }

        return (currentAccessToken, refreshToken, tenant);
    }

    private async Task<RefreshTokenCommandResponse?> CallRefreshApiAsync(
        (string AccessToken, string RefreshToken, string Tenant) tokens,
        CancellationToken cancellationToken)
    {
        var refreshResponse = await _tokenClient.RefreshAsync(
            tokens.Tenant,
            new RefreshTokenCommand
            {
                Token = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken
            },
            cancellationToken);

        if (refreshResponse is null || string.IsNullOrEmpty(refreshResponse.Token))
        {
            _logger.LogWarning("Token refresh returned empty response");
            return null;
        }

        return refreshResponse;
    }

    private async Task UpdateCachesAsync(
        RefreshTokenCommandResponse response,
        string oldRefreshToken,
        CancellationToken cancellationToken)
    {
        await _circuitTokenCache.UpdateTokensAsync(response.Token, response.RefreshToken, cancellationToken);

        _lastRefreshedToken = response.Token;
        _cachedForRefreshToken = oldRefreshToken;
        _lastRefreshTime = DateTime.UtcNow;
    }

    private async Task HandleRefreshFailureAsync(
        string currentRefreshToken,
        ApiException ex,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        await _circuitTokenCache.ClearAsync(cancellationToken);
        _lastRefreshedToken = null;
        _cachedForRefreshToken = null;
        _lastRefreshTime = DateTime.MinValue;
        _failedRefreshToken = currentRefreshToken;
        _failedRefreshTime = DateTime.UtcNow;
        _permanentFailureFlag = true;  // Mark session as permanently failed to fast-fail all subsequent attempts

        RefreshFailuresCounter.Add(1,
            new KeyValuePair<string, object?>("reason", reasonCode),
            new KeyValuePair<string, object?>("status", ex.StatusCode));

        _logger.LogWarning(ex,
            "Refresh token failed. ReasonCode={ReasonCode}, StatusCode={StatusCode}. User will be signed out.",
            reasonCode,
            ex.StatusCode);
    }

    private static string ResolveFailureReasonCode(ApiException ex)
    {
        var body = ex.Response ?? string.Empty;

        if (body.Contains("Session has been revoked", StringComparison.OrdinalIgnoreCase))
        {
            return RefreshFailureReasonCodes.SessionRevoked;
        }

        if (body.Contains("Access token subject mismatch", StringComparison.OrdinalIgnoreCase))
        {
            return RefreshFailureReasonCodes.SubjectMismatch;
        }

        if (body.Contains("Invalid refresh token", StringComparison.OrdinalIgnoreCase) ||
            body.Contains("invalid or expired", StringComparison.OrdinalIgnoreCase))
        {
            return RefreshFailureReasonCodes.InvalidOrExpired;
        }

        if (ex.StatusCode == 400)
        {
            return RefreshFailureReasonCodes.BadRequest;
        }

        if (ex.StatusCode == 401)
        {
            return RefreshFailureReasonCodes.Unauthorized;
        }

        return RefreshFailureReasonCodes.Unknown;
    }

    private static class RefreshFailureReasonCodes
    {
        internal const string InvalidOrExpired = "invalid_or_expired";
        internal const string SessionRevoked = "session_revoked";
        internal const string SubjectMismatch = "subject_mismatch";
        internal const string BadRequest = "bad_request";
        internal const string Unauthorized = "unauthorized";
        internal const string UnexpectedError = "unexpected_error";
        internal const string Unknown = "unknown";
    }

    public void Dispose()
    {
        _refreshLock?.Dispose();
    }
}

