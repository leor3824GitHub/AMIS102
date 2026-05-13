using AMIS.Playground.Blazor.ApiClient;
using Microsoft.AspNetCore.Authentication;
using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AMIS.Playground.Blazor.Services.Api;

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
    private static readonly Meter RefreshMeter = new("AMIS.Playground.Blazor.Auth", "1.0.0");
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

        var currentRefreshToken = GetCurrentRefreshToken(httpContext);
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

    private string? GetCurrentRefreshToken(HttpContext httpContext)
    {
        var circuitRefreshToken = _circuitTokenCache.RefreshToken;
        var claimsRefreshToken = httpContext.User?.FindFirst("refresh_token")?.Value;

        return !string.IsNullOrEmpty(circuitRefreshToken) ? circuitRefreshToken : claimsRefreshToken;
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

        var tokens = GetCurrentTokens(user);
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

            var newClaims = BuildNewClaims(user, refreshResponse);
            UpdateCaches(refreshResponse, currentRefreshToken);
            await TryUpdateCookieAsync(httpContext, newClaims);

            RefreshSuccessCounter.Add(1);
            _logger.LogInformation("Access token refreshed successfully");
            return refreshResponse.Token;
        }
        catch (ApiException ex) when (ex.StatusCode == 400 || ex.StatusCode == 401)
        {
            var reasonCode = ResolveFailureReasonCode(ex);
            HandleRefreshFailure(currentRefreshToken, ex, reasonCode);
            return null;
        }
        catch (Exception ex)
        {
            RefreshFailuresCounter.Add(1, new KeyValuePair<string, object?>("reason", RefreshFailureReasonCodes.UnexpectedError));
            _logger.LogError(ex, "Failed to refresh access token");
            return null;
        }
    }

    private (string AccessToken, string RefreshToken, string Tenant)? GetCurrentTokens(ClaimsPrincipal user)
    {
        var currentAccessToken = !string.IsNullOrEmpty(_circuitTokenCache.AccessToken)
            ? _circuitTokenCache.AccessToken
            : user.FindFirst("access_token")?.Value;

        var refreshToken = !string.IsNullOrEmpty(_circuitTokenCache.RefreshToken)
            ? _circuitTokenCache.RefreshToken
            : user.FindFirst("refresh_token")?.Value;

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

    private static List<Claim> BuildNewClaims(ClaimsPrincipal user, RefreshTokenCommandResponse response)
    {
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwtToken = jwtHandler.ReadJwtToken(response.Token);
        var tenant = user.FindFirst("tenant")?.Value ?? "root";

        var newClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier,
                jwtToken.Subject
                ?? jwtToken.Claims.FirstOrDefault(c =>
                        c.Type == ClaimTypes.NameIdentifier ||
                        c.Type == "nameid" ||
                        c.Type == "sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty),
            new("access_token", response.Token),
            new("refresh_token", response.RefreshToken),
            new("tenant", tenant),
        };

        AddNameClaim(newClaims, jwtToken);
        AddRoleClaims(newClaims, jwtToken);

        return newClaims;
    }

    private static void AddNameClaim(List<Claim> claims, JwtSecurityToken jwtToken)
    {
        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == ClaimTypes.Name);
        if (nameClaim != null)
        {
            claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
        }
    }

    private static void AddRoleClaims(List<Claim> claims, JwtSecurityToken jwtToken)
    {
        var roleClaims = jwtToken.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role);
        claims.AddRange(roleClaims.Select(r => new Claim(ClaimTypes.Role, r.Value)));
    }

    private void UpdateCaches(RefreshTokenCommandResponse response, string oldRefreshToken)
    {
        _circuitTokenCache.UpdateTokens(response.Token, response.RefreshToken);

        _lastRefreshedToken = response.Token;
        _cachedForRefreshToken = oldRefreshToken;
        _lastRefreshTime = DateTime.UtcNow;
    }

    private static async Task TryUpdateCookieAsync(HttpContext httpContext, List<Claim> newClaims)
    {
        try
        {
            var identity = new ClaimsIdentity(newClaims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
        }
        catch (InvalidOperationException)
        {
            // Expected in Blazor Server SignalR context
        }
    }

    private void HandleRefreshFailure(string currentRefreshToken, ApiException ex, string reasonCode)
    {
        _circuitTokenCache.Clear();
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

