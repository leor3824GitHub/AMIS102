using System.Net.Http.Json;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AMIS.Maui.Services;

public sealed class SessionExpiredMessage : ValueChangedMessage<string>
{
    public SessionExpiredMessage() : base("Session expired") { }
}

public sealed class AuthenticatedHttpHandler(
    ApiClientOptions options,
    ITokenStorageService tokenStorage) : DelegatingHandler
{
    // Static: the handler is transient, but the refresh token is rotated server-side, so only one
    // refresh may be in flight per app. Parallel 401s must queue and re-use the winner's token —
    // a second refresh with the now-rotated token is rejected and would log the user out.
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await tokenStorage.GetAccessTokenAsync();
        AttachHeaders(request, accessToken);

        var response = await base.SendAsync(request, cancellationToken);

        // Only attempt refresh + SessionExpired when a token existed but was rejected.
        // If there was no token (offline/PIN-unlock mode), return the 401 as-is so
        // ViewModels fall back to cache without triggering the PIN-page loop.
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized
            || string.IsNullOrEmpty(accessToken))
        {
            return response;
        }

        var refresh = await RefreshOnceAsync(accessToken, cancellationToken);
        if (refresh.Token is not null)
        {
            var retryRequest = await CloneRequestAsync(request);
            AttachHeaders(retryRequest, refresh.Token);
            var retryResponse = await base.SendAsync(retryRequest, cancellationToken);

            if (retryResponse.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                return retryResponse;

            retryResponse.Dispose();
            refresh = refresh with { Rejected = true };
        }

        // Only end the session when the server actually rejected us. A refresh that failed on a
        // network error leaves the tokens in place so the ViewModel can fall back to cache.
        if (refresh.Rejected)
        {
            await tokenStorage.ClearAsync();
            WeakReferenceMessenger.Default.Send(new SessionExpiredMessage());
        }

        return response;
    }

    /// <summary>
    /// Refreshes the token pair at most once across all concurrent callers. Callers that arrive
    /// while a refresh is running wait, then re-use the token it produced instead of refreshing again.
    /// </summary>
    private async Task<RefreshOutcome> RefreshOnceAsync(string staleAccessToken, CancellationToken cancellationToken)
    {
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Someone else already refreshed while we were queued — use their token.
            var current = await tokenStorage.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(current) && !string.Equals(current, staleAccessToken, StringComparison.Ordinal))
                return new RefreshOutcome(current, Rejected: false);

            return await TryRefreshTokenAsync(staleAccessToken, cancellationToken);
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private void AttachHeaders(HttpRequestMessage request, string? accessToken)
    {
        request.Headers.Remove("Authorization");
        request.Headers.Remove("tenant");
        request.Headers.Remove("X-Client-Id");

        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

        request.Headers.Add("tenant", options.TenantId);
        request.Headers.Add("X-Client-Id", GetClientId());
    }

    private static string GetClientId()
    {
#if ANDROID
        return "maui-android";
#elif IOS
        return "maui-ios";
#elif WINDOWS
        return "maui-windows";
#else
        return "maui";
#endif
    }

    private async Task<RefreshOutcome> TryRefreshTokenAsync(string staleAccessToken, CancellationToken cancellationToken)
    {
        var refreshToken = await tokenStorage.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshToken))
            return new RefreshOutcome(null, Rejected: true);

        // Absolute URI is required: this goes through base.SendAsync (the inner handler) rather than
        // the HttpClient, and BaseAddress is an HttpClient concept — a relative URI here throws
        // InvalidOperationException instead of being resolved.
        var refreshUri = new UriBuilder(options.BaseUrl) { Path = "api/v1/identity/token/refresh" }.Uri;
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, refreshUri);
        refreshRequest.Headers.Add("tenant", options.TenantId);
        refreshRequest.Headers.Add("X-Client-Id", GetClientId());

        // The endpoint takes BOTH the expired access token (Token) and the refresh token
        // (RefreshToken); it cross-checks that their subjects match. Omitting either fails validation.
        refreshRequest.Content = JsonContent.Create(
            new RefreshTokenRequest(Token: staleAccessToken, RefreshToken: refreshToken));

        try
        {
            using var refreshResponse = await base.SendAsync(refreshRequest, cancellationToken);

            if (!refreshResponse.IsSuccessStatusCode)
            {
                // 401 = revoked/rotated/expired refresh token, 400 = malformed request: both are
                // terminal. A 5xx or 429 is transient — keep the tokens and let the caller retry later.
                var rejected = (int)refreshResponse.StatusCode < 500
                    && refreshResponse.StatusCode != System.Net.HttpStatusCode.TooManyRequests;
                return new RefreshOutcome(null, rejected);
            }

            var tokenResponse = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken);
            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.Token))
                return new RefreshOutcome(null, Rejected: false);

            await tokenStorage.SaveTokensAsync(tokenResponse.Token, tokenResponse.RefreshToken);
            return new RefreshOutcome(tokenResponse.Token, Rejected: false);
        }
        catch (HttpRequestException)
        {
            // Offline / DNS / connection reset — not an auth failure.
            return new RefreshOutcome(null, Rejected: false);
        }
        catch (TaskCanceledException)
        {
            // Request timeout, or the caller cancelled. Never treat this as a logout.
            return new RefreshOutcome(null, Rejected: false);
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    /// <summary>Body of <c>POST /api/v1/identity/token/refresh</c> — mirrors <c>RefreshTokenCommand</c>.</summary>
    private sealed record RefreshTokenRequest(string Token, string RefreshToken);

    /// <summary>
    /// Response of <c>POST /api/v1/identity/token/refresh</c>. Note the new access token is named
    /// <c>Token</c> here, not <c>AccessToken</c> as on the issue endpoint.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by System.Text.Json deserialization")]
    private sealed record RefreshTokenResponse(string Token, string RefreshToken);

    /// <summary>
    /// Result of a refresh attempt. <paramref name="Rejected"/> is true only when the server refused
    /// the credentials — the one case where the session must be torn down.
    /// </summary>
    private sealed record RefreshOutcome(string? Token, bool Rejected);
}
