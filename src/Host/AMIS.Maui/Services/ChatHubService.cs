using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;

namespace AMIS.Maui.Services;

/// <summary>A user started typing in a channel.</summary>
public sealed record ChatTypingDto(Guid ChannelId, Guid UserId);

/// <summary>
/// App-wide SignalR client for the realtime chat hub (<c>/api/v1/realtime/hub</c>). Singleton: one
/// connection per app session. The access token is fed via <c>AccessTokenProvider</c> (SignalR re-reads it
/// on every (re)connect from <see cref="ITokenStorageService"/>, so a refreshed token is picked up).
/// Hub callbacks run off the UI thread — ViewModels marshal them with <c>MainThread</c>.
/// </summary>
public sealed class ChatHubService : IAsyncDisposable
{
    // Must match AMIS.Framework.Web.Realtime.RealtimeEvents on the server.
    private const string ChatMessageCreated = "ChatMessageCreated";
    private const string ChatTypingStarted = "ChatTypingStarted";

    private readonly ApiClientOptions _options;
    private readonly ITokenStorageService _tokenStorage;

    private HubConnection? _connection;

    public ChatHubService(ApiClientOptions options, ITokenStorageService tokenStorage)
    {
        _options = options;
        _tokenStorage = tokenStorage;
    }

    public event Action<ChatMessageDto>? MessageCreated;
    public event Action<ChatTypingDto>? TypingStarted;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_connection is not null)
        {
            return;
        }

        try
        {
            var hubUrl = $"{_options.BaseUrl.TrimEnd('/')}/api/v1/realtime/hub";

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = async () => await _tokenStorage.GetAccessTokenAsync();
#if DEBUG
                    // Dev only: emulators/simulators don't trust the .NET HTTPS dev cert. The whole
                    // block is compiled out of Release, so shipped builds validate certificates
                    // normally — CA5359/S4830 are reporting the Debug-only path.
#pragma warning disable CA5359, S4830
                    options.HttpMessageHandlerFactory = _ => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    };
                    options.WebSocketConfiguration = ws =>
                        ws.RemoteCertificateValidationCallback = (_, _, _, _) => true;
#pragma warning restore CA5359, S4830
#endif
                })
                .WithAutomaticReconnect()
                .Build();

            connection.On<ChatMessageDto>(ChatMessageCreated, message => MessageCreated?.Invoke(message));
            connection.On<ChatTypingDto>(ChatTypingStarted, notification => TypingStarted?.Invoke(notification));

            // Publish only after a successful build so a build failure leaves _connection null and retryable.
            _connection = connection;

            await connection.StartAsync(ct);
        }
        catch (Exception ex)
        {
            // Realtime is best-effort — the screens still work over REST. Auto-reconnect retries.
            Debug.WriteLine($"[ChatHub] start failed: {ex.Message}");
        }
    }

    public async Task JoinChannelAsync(Guid channelId)
    {
        if (_connection is { State: HubConnectionState.Connected })
        {
            try
            {
                await _connection.InvokeAsync("JoinChannel", channelId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatHub] join {channelId} failed: {ex.Message}");
            }
        }
    }

    public async Task TypingAsync(Guid channelId)
    {
        if (_connection is { State: HubConnectionState.Connected })
        {
            try
            {
                await _connection.InvokeAsync("Typing", channelId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatHub] typing {channelId} failed: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
