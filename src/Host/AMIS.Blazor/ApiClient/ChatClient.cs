using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using AMIS.Modules.Chat.Contracts.v1.DTOs;

namespace AMIS.Blazor.ApiClient;

/// <summary>
/// Hand-written typed client for the Chat module REST API (the NSwag <c>Generated.cs</c> is not regenerated
/// here). Uses the same authenticated <see cref="HttpClient"/> as the other manual clients (bearer token +
/// tenant header are added by the pipeline). Returns the shared <c>Modules.Chat.Contracts</c> DTOs directly.
/// </summary>
public interface IChatClient
{
    // Channels
    Task<IReadOnlyList<ChannelDto>> ListMyChannelsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ChannelDto>> DiscoverChannelsAsync(CancellationToken ct = default);
    Task<ChannelDto?> GetChannelAsync(Guid channelId, CancellationToken ct = default);
    Task<ChannelDto?> CreateChannelAsync(string name, string? topic, IReadOnlyList<string>? memberUserIds, CancellationToken ct = default);
    Task<ChannelDto?> FindOrCreateDmAsync(string otherUserId, CancellationToken ct = default);
    Task<ChannelDto?> UpdateChannelAsync(Guid channelId, string name, string? topic, CancellationToken ct = default);
    Task ArchiveChannelAsync(Guid channelId, CancellationToken ct = default);
    Task<ChannelDto?> RestoreChannelAsync(Guid channelId, CancellationToken ct = default);
    Task<ChannelDto?> AddChannelMembersAsync(Guid channelId, IReadOnlyList<string> userIds, CancellationToken ct = default);
    Task RemoveChannelMemberAsync(Guid channelId, string userId, CancellationToken ct = default);
    Task MarkChannelReadAsync(Guid channelId, Guid messageId, CancellationToken ct = default);

    // Messages
    Task<MessagePageDto?> ListMessagesAsync(Guid channelId, Guid? before, int? take, CancellationToken ct = default);
    Task<MessageDto?> SendMessageAsync(Guid channelId, string content, Guid? parentMessageId, CancellationToken ct = default);
    Task<MessageDto?> EditMessageAsync(Guid messageId, string content, CancellationToken ct = default);
    Task<MessageDto?> DeleteMessageAsync(Guid messageId, CancellationToken ct = default);
    Task<MessageDto?> PinMessageAsync(Guid messageId, CancellationToken ct = default);
    Task<MessageDto?> UnpinMessageAsync(Guid messageId, CancellationToken ct = default);
    Task<IReadOnlyList<MessageDto>> ListRepliesAsync(Guid messageId, CancellationToken ct = default);
    Task<IReadOnlyList<MessageDto>> GetPinnedAsync(Guid channelId, CancellationToken ct = default);

    // Reactions
    Task<MessageDto?> AddReactionAsync(Guid messageId, string emoji, CancellationToken ct = default);
    Task<MessageDto?> RemoveReactionAsync(Guid messageId, string emoji, CancellationToken ct = default);

    // Search
    Task<MessagePageDto?> SearchMessagesAsync(Guid channelId, string query, Guid? before, int? take, CancellationToken ct = default);

    // Mention directory
    Task<IReadOnlyList<ChatUserDto>> SearchUsersAsync(string? query, CancellationToken ct = default);
}

internal sealed class ChatClient : IChatClient
{
    private const string Root = "api/v1/chat";

    // Match the API's web JSON contract (camelCase, case-insensitive).
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public ChatClient(HttpClient http) => _http = http;

    // ---- Channels ----

    public async Task<IReadOnlyList<ChannelDto>> ListMyChannelsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<ChannelDto>>($"{Root}/channels", Json, ct).ConfigureAwait(false) ?? [];

    public async Task<IReadOnlyList<ChannelDto>> DiscoverChannelsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<ChannelDto>>($"{Root}/channels/discover", Json, ct).ConfigureAwait(false) ?? [];

    public async Task<ChannelDto?> GetChannelAsync(Guid channelId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{Root}/channels/{channelId}", ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChannelDto>(Json, ct).ConfigureAwait(false);
    }

    public Task<ChannelDto?> CreateChannelAsync(string name, string? topic, IReadOnlyList<string>? memberUserIds, CancellationToken ct = default) =>
        PostAsync<ChannelDto>($"{Root}/channels", new { name, topic, memberUserIds }, ct);

    public Task<ChannelDto?> FindOrCreateDmAsync(string otherUserId, CancellationToken ct = default) =>
        PostAsync<ChannelDto>($"{Root}/channels/dm", new { otherUserId }, ct);

    public Task<ChannelDto?> UpdateChannelAsync(Guid channelId, string name, string? topic, CancellationToken ct = default) =>
        PutAsync<ChannelDto>($"{Root}/channels/{channelId}", new { name, topic }, ct);

    public async Task ArchiveChannelAsync(Guid channelId, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"{Root}/channels/{channelId}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public Task<ChannelDto?> RestoreChannelAsync(Guid channelId, CancellationToken ct = default) =>
        PostAsync<ChannelDto>($"{Root}/channels/{channelId}/restore", new { }, ct);

    public Task<ChannelDto?> AddChannelMembersAsync(Guid channelId, IReadOnlyList<string> userIds, CancellationToken ct = default) =>
        PostAsync<ChannelDto>($"{Root}/channels/{channelId}/members", new { userIds }, ct);

    public async Task RemoveChannelMemberAsync(Guid channelId, string userId, CancellationToken ct = default)
    {
        using var response = await _http
            .DeleteAsync($"{Root}/channels/{channelId}/members/{Uri.EscapeDataString(userId)}", ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task MarkChannelReadAsync(Guid channelId, Guid messageId, CancellationToken ct = default)
    {
        using var response = await _http
            .PostAsJsonAsync($"{Root}/channels/{channelId}/read", new { messageId }, Json, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    // ---- Messages ----

    public async Task<MessagePageDto?> ListMessagesAsync(Guid channelId, Guid? before, int? take, CancellationToken ct = default)
    {
        var url = $"{Root}/channels/{channelId}/messages{BuildQuery(("before", before?.ToString()), ("take", take?.ToString()))}";
        return await _http.GetFromJsonAsync<MessagePageDto>(url, Json, ct).ConfigureAwait(false);
    }

    public Task<MessageDto?> SendMessageAsync(Guid channelId, string content, Guid? parentMessageId, CancellationToken ct = default) =>
        PostAsync<MessageDto>($"{Root}/messages", new { channelId, content, parentMessageId }, ct);

    public Task<MessageDto?> EditMessageAsync(Guid messageId, string content, CancellationToken ct = default) =>
        PutAsync<MessageDto>($"{Root}/messages/{messageId}", new { content }, ct);

    public async Task<MessageDto?> DeleteMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync($"{Root}/messages/{messageId}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageDto>(Json, ct).ConfigureAwait(false);
    }

    public Task<MessageDto?> PinMessageAsync(Guid messageId, CancellationToken ct = default) =>
        PostAsync<MessageDto>($"{Root}/messages/{messageId}/pin", new { }, ct);

    public Task<MessageDto?> UnpinMessageAsync(Guid messageId, CancellationToken ct = default) =>
        PostAsync<MessageDto>($"{Root}/messages/{messageId}/unpin", new { }, ct);

    public async Task<IReadOnlyList<MessageDto>> ListRepliesAsync(Guid messageId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<MessageDto>>($"{Root}/messages/{messageId}/replies", Json, ct).ConfigureAwait(false) ?? [];

    public async Task<IReadOnlyList<MessageDto>> GetPinnedAsync(Guid channelId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<MessageDto>>($"{Root}/channels/{channelId}/pinned", Json, ct).ConfigureAwait(false) ?? [];

    // ---- Reactions ----

    public Task<MessageDto?> AddReactionAsync(Guid messageId, string emoji, CancellationToken ct = default) =>
        PostAsync<MessageDto>($"{Root}/messages/{messageId}/reactions", new { emoji }, ct);

    public async Task<MessageDto?> RemoveReactionAsync(Guid messageId, string emoji, CancellationToken ct = default)
    {
        using var response = await _http
            .DeleteAsync($"{Root}/messages/{messageId}/reactions?emoji={Uri.EscapeDataString(emoji)}", ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MessageDto>(Json, ct).ConfigureAwait(false);
    }

    // ---- Search ----

    public async Task<MessagePageDto?> SearchMessagesAsync(Guid channelId, string query, Guid? before, int? take, CancellationToken ct = default)
    {
        var url = $"{Root}/channels/{channelId}/messages/search{BuildQuery(("q", query), ("before", before?.ToString()), ("take", take?.ToString()))}";
        return await _http.GetFromJsonAsync<MessagePageDto>(url, Json, ct).ConfigureAwait(false);
    }

    // ---- Mention directory ----

    public async Task<IReadOnlyList<ChatUserDto>> SearchUsersAsync(string? query, CancellationToken ct = default)
    {
        var url = $"{Root}/users{BuildQuery(("q", query))}";
        return await _http.GetFromJsonAsync<List<ChatUserDto>>(url, Json, ct).ConfigureAwait(false) ?? [];
    }

    // ---- Helpers ----

    private async Task<T?> PostAsync<T>(string url, object body, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync(url, body, Json, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(Json, ct).ConfigureAwait(false);
    }

    private async Task<T?> PutAsync<T>(string url, object body, CancellationToken ct)
    {
        using var response = await _http.PutAsJsonAsync(url, body, Json, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(Json, ct).ConfigureAwait(false);
    }

    private static string BuildQuery(params (string Key, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}")
            .ToList();
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
