using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using AMIS.Modules.Notifications.Contracts.v1.DTOs;

namespace AMIS.Blazor.ApiClient;

/// <summary>
/// Hand-written typed client for the Notifications module REST API (the NSwag <c>Generated.cs</c> is not
/// regenerated here). Uses the same authenticated <see cref="HttpClient"/> as the other manual clients
/// (bearer token + tenant header added by the pipeline). Returns the shared
/// <c>Modules.Notifications.Contracts</c> DTOs directly.
/// </summary>
internal interface INotificationsClient
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(bool unreadOnly = false, int take = 50, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(CancellationToken ct = default);
    Task MarkReadAsync(Guid id, CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
    Task DismissAsync(Guid id, CancellationToken ct = default);
}

internal sealed class NotificationsClient : INotificationsClient
{
    private const string Root = "api/v1/notifications";

    // Match the API's web JSON contract (camelCase, case-insensitive).
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public NotificationsClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<NotificationDto>> ListAsync(bool unreadOnly = false, int take = 50, CancellationToken ct = default)
    {
        var url = $"{Root}?unreadOnly={(unreadOnly ? "true" : "false")}&take={take}";
        return await _http.GetFromJsonAsync<List<NotificationDto>>(url, Json, ct).ConfigureAwait(false) ?? [];
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<int>($"{Root}/unread-count", Json, ct).ConfigureAwait(false);

    public async Task MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"{Root}/{id}/read", new { }, Json, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync($"{Root}/read-all", new { }, Json, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DismissAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync(new Uri($"{Root}/{id}", UriKind.Relative), ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
