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

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !_isRefreshing)
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

        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

        request.Headers.Add("tenant", options.TenantId);
    }

    private async Task<string?> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        var refreshToken = await tokenStorage.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshToken))
            return null;

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/identity/token/refresh");
        refreshRequest.Headers.Add("tenant", options.TenantId);
        refreshRequest.Content = System.Net.Http.Json.JsonContent.Create(new { Token = refreshToken });

        try
        {
            var refreshResponse = await base.SendAsync(refreshRequest, cancellationToken);
            if (!refreshResponse.IsSuccessStatusCode)
                return null;

            var tokenResponse = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
            if (tokenResponse is null)
                return null;

            await tokenStorage.SaveTokensAsync(tokenResponse.Token, tokenResponse.RefreshToken);
            return tokenResponse.Token;
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

    private sealed record TokenResponse(string Token, string RefreshToken);
}
