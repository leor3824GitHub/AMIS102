namespace Playground.Maui.Services;

public sealed class ApiClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string TenantId { get; set; } = "root";
}
