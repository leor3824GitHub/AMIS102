using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FSH.Modules.MasterData.Contracts.v1.OrganizationProfile;

namespace FSH.Playground.Blazor.ApiClient;

internal interface IOrganizationProfileClient
{
    Task<OrganizationProfileDto?> GetAsync(CancellationToken cancellationToken = default);
    Task<OrganizationProfileDto> UpsertAsync(UpsertOrganizationProfileCommand command, CancellationToken cancellationToken = default);
}

internal sealed class OrganizationProfileClient(HttpClient httpClient) : IOrganizationProfileClient
{
    public async Task<OrganizationProfileDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("api/v1/master-data/organization-profile", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrganizationProfileDto>(cancellationToken: cancellationToken);
    }

    public async Task<OrganizationProfileDto> UpsertAsync(
        UpsertOrganizationProfileCommand command, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync("api/v1/master-data/organization-profile", command, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OrganizationProfileDto>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Empty response.");
    }
}
