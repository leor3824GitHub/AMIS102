using System.Net.Http.Json;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Playground.Maui.Services;

public sealed class SessionExpiredMessage : ValueChangedMessage<string>
{
    public SessionExpiredMessage() : base("Session expired") { }
}

public sealed class AuthenticatedHttpHandler(
    ApiClientOptions options,
    ITokenStorageService tokenStorage) : DelegatingHandler
{
    private bool _isRefreshing;

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
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            && !_isRefreshing
            && !string.IsNullOrEmpty(accessToken))
        {
            _isRefreshing = true;
            try
            {
                var newToken = await TryRefreshTokenAsync(cancellationToken);
                if (newToken is not null)
                {
                    var retryRequest = await CloneRequestAsync(request);
                    AttachHeaders(retryRequest, newToken);
                    var retryResponse = await base.SendAsync(retryRequest, cancellationToken);

                    if (retryResponse.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                        return retryResponse;
                }

                // Second 401 or refresh failed — session expired
                await tokenStorage.ClearAsync();
                WeakReferenceMessenger.Default.Send(new SessionExpiredMessage());
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        return response;
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

    private async Task<string?> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        var refreshToken = await tokenStorage.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshToken))
            return null;

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/identity/token/refresh");
        refreshRequest.Headers.Add("tenant", options.TenantId);
        refreshRequest.Headers.Add("X-Client-Id", GetClientId());
        refreshRequest.Content = System.Net.Http.Json.JsonContent.Create(new { Token = refreshToken });

        try
        {
            var refreshResponse = await base.SendAsync(refreshRequest, cancellationToken);
            if (!refreshResponse.IsSuccessStatusCode)
                return null;

            var tokenResponse = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
            if (tokenResponse is null)
                return null;

            await tokenStorage.SaveTokensAsync(tokenResponse.AccessToken, tokenResponse.RefreshToken);
            return tokenResponse.AccessToken;
        }
        catch
        {
            return null;
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by System.Text.Json deserialization")]
    private sealed record TokenResponse(string AccessToken, string RefreshToken);
}
