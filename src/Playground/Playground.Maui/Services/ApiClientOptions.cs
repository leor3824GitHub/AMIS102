namespace Playground.Maui.Services;

public sealed class ApiClientOptions
{
    // Preferences key for the last-used tenant. Not a secret — tokens stay in secure storage;
    // the tenant id only scopes the `tenant` header and pre-fills the login form.
    public const string TenantPreferenceKey = "tenant_id";

    public string BaseUrl { get; set; } = string.Empty;
    public string TenantId { get; set; } = "root";

    // Host override applied only on Android when BaseUrl is loopback. Set to the dev machine's
    // LAN IP (e.g. "10.0.254.4") for physical-device debugging; leave empty to use the
    // emulator alias 10.0.2.2.
    public string? AndroidHost { get; set; }
}
