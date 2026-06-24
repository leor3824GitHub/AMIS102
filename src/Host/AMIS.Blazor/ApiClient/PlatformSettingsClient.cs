using System.Net.Http.Json;
using AMIS.Modules.Multitenancy.Contracts.Dtos;

namespace AMIS.Blazor.ApiClient;

internal interface IPlatformSettingsClient
{
    Task<PlatformSettingsDto> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(PlatformSettingsDto settings, CancellationToken ct = default);
}

internal sealed class PlatformSettingsClient(HttpClient http) : IPlatformSettingsClient
{
    private const string Base = "api/v1/tenants/settings";

    public async Task<PlatformSettingsDto> GetAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<PlatformSettingsDto>(Base, ct) ?? PlatformSettingsDto.Default;

    public async Task UpdateAsync(PlatformSettingsDto settings, CancellationToken ct = default)
    {
        using var response = await http.PutAsJsonAsync(Base, settings, ct);
        response.EnsureSuccessStatusCode();
    }
}
