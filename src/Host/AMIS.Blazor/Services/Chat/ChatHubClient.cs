using System.Net.Http;
using AMIS.Blazor.Services.Api;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Notifications.Contracts.v1.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;

namespace AMIS.Blazor.Services.Chat;

/// <summary>A user came online or went offline (tenant-wide presence).</summary>
public sealed record PresenceNotification(Guid UserId, bool IsOnline);

/// <summary>A user started typing in a channel.</summary>
public sealed record TypingNotification(Guid ChannelId, Guid UserId);

/// <summary>The current user was @mentioned in a channel.</summary>
public sealed record MentionNotification(Guid ChannelId, Guid MessageId, string SenderId);

/// <summary>
/// Circuit-scoped wrapper around the SignalR .NET client that connects to the app-wide <c>AppHub</c>.
/// Feeds the existing JWT (from <see cref="ICircuitTokenCache"/>, falling back to the <c>access_token</c>
/// claim) via <c>AccessTokenProvider</c> — which SignalR re-invokes on every (re)connect, so a token
/// refreshed on the REST path is picked up automatically. Hub callbacks run off the render thread; the
/// page marshals them with <c>InvokeAsync(StateHasChanged)</c>.
/// </summary>
internal sealed class ChatHubClient : IAsyncDisposable
{
    // Wire-contract event names — must match AMIS.Framework.Web.Realtime.RealtimeEvents on the server.
    private const string ChatMessageCreatedEvent = "ChatMessageCreated";
    private const string ChatTypingStartedEvent = "ChatTypingStarted";
    private const string PresenceChangedEvent = "PresenceChanged";
    private const string ChatMentionedEvent = "ChatMentioned";
    private const string NotificationCreatedEvent = "NotificationCreated";
    private const string NotificationReadEvent = "NotificationRead";

    private readonly ICircuitTokenCache _tokenCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ChatHubClient> _logger;

    private HubConnection? _connection;

    public ChatHubClient(
        ICircuitTokenCache tokenCache,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<ChatHubClient> logger)
    {
        _tokenCache = tokenCache;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public event Action<MessageDto>? MessageCreated;
    public event Action<TypingNotification>? TypingStarted;
    public event Action<PresenceNotification>? PresenceChanged;
    public event Action<MentionNotification>? Mentioned;

    /// <summary>Raised when a workflow notification is pushed to the current user's bell (e.g. inspection requested).</summary>
    public event Action<NotificationDto>? NotificationReceived;

    /// <summary>Raised when a workflow marked a bell notification read server-side (e.g. the user accepted the ICS/PAR it pointed at). Payload is the notification id.</summary>
    public event Action<Guid>? NotificationRead;

    /// <summary>Raised when the underlying connection state changes (connect/reconnect/close).</summary>
    public event Action? ConnectionStateChanged;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is not null)
        {
            return;
        }

        var apiBaseUrl = _configuration["Api:BaseUrl"]
            ?? throw new InvalidOperationException("Api:BaseUrl configuration is missing.");
        var apiUri = new Uri(apiBaseUrl);
        var hubUrl = new Uri(apiUri, "api/v1/realtime/hub");
        var bypassCert = IsDevLocalhost(apiUri);

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // SignalR calls this on every (re)connect — always read the freshest token.
                options.AccessTokenProvider = () => Task.FromResult(GetAccessToken());

                if (bypassCert)
                {
                    options.HttpMessageHandlerFactory = handler =>
                    {
                        if (handler is HttpClientHandler httpClientHandler)
                        {
#pragma warning disable S4830
                            httpClientHandler.ServerCertificateCustomValidationCallback =
                                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore S4830
                        }

                        return handler;
                    };
                    options.WebSocketConfiguration = ws =>
                        ws.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                }
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<MessageDto>(ChatMessageCreatedEvent, message => MessageCreated?.Invoke(message));
        _connection.On<TypingNotification>(ChatTypingStartedEvent, notification => TypingStarted?.Invoke(notification));
        _connection.On<PresenceNotification>(PresenceChangedEvent, notification => PresenceChanged?.Invoke(notification));
        _connection.On<MentionNotification>(ChatMentionedEvent, notification => Mentioned?.Invoke(notification));
        _connection.On<NotificationDto>(NotificationCreatedEvent, notification => NotificationReceived?.Invoke(notification));
        _connection.On<Guid>(NotificationReadEvent, id => NotificationRead?.Invoke(id));

        _connection.Reconnecting += _ => { ConnectionStateChanged?.Invoke(); return Task.CompletedTask; };
        _connection.Reconnected += _ => { ConnectionStateChanged?.Invoke(); return Task.CompletedTask; };
        _connection.Closed += _ => { ConnectionStateChanged?.Invoke(); return Task.CompletedTask; };

        try
        {
            await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
            ConnectionStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start the chat hub connection.");
        }
    }

    /// <summary>Joins a channel group for channels created after the initial connection (idempotent, membership-gated).</summary>
    public async Task JoinChannelAsync(Guid channelId)
    {
        if (_connection is { State: HubConnectionState.Connected })
        {
            try
            {
                await _connection.InvokeAsync("JoinChannel", channelId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to join channel {ChannelId} on the hub.", channelId);
            }
        }
    }

    /// <summary>Signals the caller is typing in a channel (server throttles to one broadcast per 3s).</summary>
    public async Task TypingAsync(Guid channelId)
    {
        if (_connection is { State: HubConnectionState.Connected })
        {
            try
            {
                await _connection.InvokeAsync("Typing", channelId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to send typing signal for channel {ChannelId}.", channelId);
            }
        }
    }

    private string? GetAccessToken() =>
        _tokenCache.AccessToken
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("access_token")?.Value;

    private bool IsDevLocalhost(Uri apiUri) =>
        _environment.IsDevelopment() &&
        (string.Equals(apiUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(apiUri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase));

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
