using AMIS.Playground.Blazor.ApiClient;

namespace AMIS.Playground.Blazor.Services.Api;

public interface IApiHealthMonitor
{
    ApiConnectionStatus Status { get; }
    event Action? OnStatusChanged;
    Task StartMonitoringAsync();
    void StopMonitoring();
}

public enum ApiConnectionStatus
{
    Healthy,
    Unhealthy,
    Unknown
}

internal sealed class ApiHealthMonitor : IApiHealthMonitor, IDisposable
{
    private readonly IHealthClient _healthClient;
    private readonly ILogger<ApiHealthMonitor> _logger;
    private Timer? _timer;
    private ApiConnectionStatus _status = ApiConnectionStatus.Unknown;

    public ApiConnectionStatus Status => _status;
    public event Action? OnStatusChanged;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    public ApiHealthMonitor(IHealthClient healthClient, ILogger<ApiHealthMonitor> logger)
    {
        _healthClient = healthClient;
        _logger = logger;
    }

    public Task StartMonitoringAsync()
    {
        _timer = new Timer(
            async _ => await CheckHealthAsync(),
            null,
            TimeSpan.Zero,  // Start immediately
            CheckInterval);

        return Task.CompletedTask;
    }

    public void StopMonitoring()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private async Task CheckHealthAsync()
    {
        try
        {
            var result = await _healthClient.ReadyAsync(CancellationToken.None);
            var newStatus = result?.Status?.Contains("Healthy") == true
                ? ApiConnectionStatus.Healthy
                : ApiConnectionStatus.Unhealthy;

            if (newStatus != _status)
            {
                _status = newStatus;
                _logger.LogDebug("API health status changed to {Status}", _status);
                NotifyStatusChangedSafely();
            }
        }
        catch (Exception ex)
        {
            if (_status != ApiConnectionStatus.Unhealthy)
            {
                _status = ApiConnectionStatus.Unhealthy;
                _logger.LogDebug(ex, "API health check failed");
                NotifyStatusChangedSafely();
            }
        }
    }

    private void NotifyStatusChangedSafely()
    {
        Action? handlers = OnStatusChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (Delegate handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action)handler).Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "API status change handler threw an exception");
            }
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}

