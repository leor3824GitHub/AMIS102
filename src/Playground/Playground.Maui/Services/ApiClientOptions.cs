namespace Playground.Maui.Services;

public sealed class ApiClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string TenantId { get; set; } = "root";

    // Host override applied only on Android when BaseUrl is loopback. Set to the dev machine's
    // LAN IP (e.g. "192.168.0.120") for physical-device debugging; leave empty to use the
    // emulator alias 10.0.2.2.
    public string? AndroidHost { get; set; }
}
