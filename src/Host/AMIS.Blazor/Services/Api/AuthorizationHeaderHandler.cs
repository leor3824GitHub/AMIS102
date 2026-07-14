using AMIS.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using System.Net;

namespace AMIS.Blazor.Services.Api;

/// <summary>
/// Delegating handler that adds the JWT token to API requests and handles 401 responses
/// by attempting to refresh the access token. If refresh fails, signs out the user and
/// notifies Blazor components via IAuthStateNotifier.
/// </summary>
internal sealed class AuthorizationHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICircuitTokenCache _circuitTokenCache;
    private readonly ILogger<AuthorizationHeaderHandler> _logger;

    /// <summary>
    /// Track if sign-out has already been initiated to prevent multiple sign-out attempts.
    /// This is scoped per circuit (instance field, not static).
    /// </summary>
    private bool _signOutInitiated;

    /// <summary>
    /// Session expired cooldown state (per-circuit to avoid cross-circuit logout).
    /// </summary>
    private static readonly TimeSpan SessionExpiredCooldown = TimeSpan.FromSeconds(45);
    private DateTime _lastSessionExpiredAtUtc = DateTime.MinValue;

    public AuthorizationHeaderHandler(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        ICircuitTokenCache circuitTokenCache,
        ILogger<AuthorizationHeaderHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _circuitTokenCache = circuitTokenCache;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (IsSessionExpiredCoolingDown())
        {
            return CreateSyntheticUnauthorizedResponse(request);
        }

        // Get current access token from the circuit cache, hydrating from the shared session store
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        // An authenticated cookie with no tokens behind it means the session store entry is gone
        // (evicted, expired, or signed out elsewhere). The cookie is useless — end the session
        // rather than firing off requests that will all 401.
        if (string.IsNullOrEmpty(accessToken) && IsAuthenticated() && !_signOutInitiated)
        {
            _logger.LogInformation("Authenticated cookie has no session tokens - signing out");
            _signOutInitiated = true;
            MarkSessionExpired();
            await SignOutUserAsync();
            return CreateSyntheticUnauthorizedResponse(request);
        }

        // Attach access token to request
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        request.Headers.TryAddWithoutValidation("X-Client-Id", "blazor");

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // If we get a 401, try to refresh the token and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // If sign-out already initiated, don't attempt refresh or sign-out again
            if (_signOutInitiated)
            {
                return response;
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogDebug("Received 401 but no access token available - cannot refresh");
                return response;
            }

            _logger.LogInformation("Received 401, attempting token refresh");

            var newAccessToken = await TryRefreshTokenAsync(cancellationToken);

            if (!string.IsNullOrEmpty(newAccessToken))
            {
                _logger.LogInformation("Token refresh successful, retrying request");

                // Clone the request with new token
                using var retryRequest = await CloneHttpRequestMessageAsync(request);
                retryRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);

                // Dispose the original response before retrying
                response.Dispose();

                // Retry the request with the new token
                response = await base.SendAsync(retryRequest, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Token refresh failed, signing out user");

                // Mark sign-out as initiated to prevent multiple sign-out attempts
                _signOutInitiated = true;
                MarkSessionExpired();

                // Sign out the user since refresh token is also invalid/expired
                await SignOutUserAsync();
            }
        }

        return response;
    }

    private async Task SignOutUserAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                // Try to sign out via cookies, but this may fail in Blazor Server's
                // SignalR context where the response has already started
                try
                {
                    if (!httpContext.Response.HasStarted)
                    {
                        await httpContext.SignOutAsync("Cookies");
                        _logger.LogInformation("User signed out due to expired refresh token");
                    }
                    else
                    {
                        _logger.LogDebug("Response already started, skipping cookie sign-out");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // Expected in Blazor Server SignalR context - headers are read-only
                    _logger.LogDebug(ex, "Could not sign out via cookies (response started), using navigation redirect");
                }

                // Notify Blazor components that session has expired
                // This will trigger navigation to login page with forceLoad:true,
                // which will create a new HTTP request where cookies can be cleared
                var authStateNotifier = _serviceProvider.GetService<IAuthStateNotifier>();
                authStateNotifier?.NotifySessionExpired();
            }
        }
        catch (Microsoft.AspNetCore.Components.NavigationException ex)
        {
            // Expected - NavigateTo with forceLoad throws this to interrupt execution
            _logger.LogDebug(ex, "Navigation to login triggered (NavigationException is expected)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle session expiration");
        }
    }

    private bool IsSessionExpiredCoolingDown()
    {
        return DateTime.UtcNow - _lastSessionExpiredAtUtc < SessionExpiredCooldown;
    }

    private void MarkSessionExpired()
    {
        _lastSessionExpiredAtUtc = DateTime.UtcNow;
    }

    private static HttpResponseMessage CreateSyntheticUnauthorizedResponse(HttpRequestMessage request)
    {
        return new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            RequestMessage = request,
            ReasonPhrase = "Session expired"
        };
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            // In-memory for this circuit, else read the shared session store via the cookie's session id
            return await _circuitTokenCache.GetAccessTokenAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to get access token");
            return null;
        }
    }

    private bool IsAuthenticated() =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    private async Task<string?> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Resolve the token refresh service from the service provider
            // We use IServiceProvider to avoid circular dependency issues
            var tokenRefreshService = _serviceProvider.GetService<ITokenRefreshService>();
            if (tokenRefreshService is null)
            {
                _logger.LogWarning("TokenRefreshService is not registered");
                return null;
            }

            return await tokenRefreshService.TryRefreshTokenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return null;
        }
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Copy headers (except Authorization which we'll set separately)
        foreach (var header in request.Headers.Where(h => !string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)))
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content if present
        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            // Copy content headers
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Copy options
        foreach (var option in request.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }

        return clone;
    }
}

