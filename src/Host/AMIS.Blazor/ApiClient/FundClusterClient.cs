using System.Net.Http.Json;
using System.Web;
using System.Globalization;
using AMIS.Modules.MasterData.Contracts.v1.FundClusters;

namespace AMIS.Blazor.ApiClient;

internal sealed record FundClusterPagedResponse(
    ICollection<FundClusterDto>? Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface IFundClusterClient
{
    Task<FundClusterPagedResponse?> SearchAsync(
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<FundClusterDto?> CreateAsync(
        CreateFundClusterCommand command,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UpdateFundClusterCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

internal sealed class FundClusterClient(HttpClient httpClient) : IFundClusterClient
{
    private const string Base = "api/v1/master-data/fund-clusters";

    public Task<FundClusterPagedResponse?> SearchAsync(
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) query["keyword"] = keyword;
        query["pageNumber"] = pageNumber.ToString(CultureInfo.InvariantCulture);
        query["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);

        return httpClient.GetFromJsonAsync<FundClusterPagedResponse>($"{Base}?{query}", cancellationToken);
    }

    public async Task<FundClusterDto?> CreateAsync(
        CreateFundClusterCommand command,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(Base, command, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FundClusterDto>(cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(
        UpdateFundClusterCommand command,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"{Base}/{command.Id}", command, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"{Base}/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
