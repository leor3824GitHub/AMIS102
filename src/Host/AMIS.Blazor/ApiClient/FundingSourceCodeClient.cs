using System.Net.Http.Json;
using System.Web;
using System.Globalization;
using AMIS.Modules.MasterData.Contracts.v1.FundingSourceCodes;

namespace AMIS.Blazor.ApiClient;

internal sealed record FundingSourceCodePagedResponse(
    ICollection<FundingSourceCodeDto>? Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface IFundingSourceCodeClient
{
    Task<FundingSourceCodePagedResponse?> SearchAsync(
        string? keyword = null,
        string? fundClusterCode = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<FundingSourceCodeDto?> CreateAsync(
        CreateFundingSourceCodeCommand command,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UpdateFundingSourceCodeCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

internal sealed class FundingSourceCodeClient(HttpClient httpClient) : IFundingSourceCodeClient
{
    private const string Base = "api/v1/master-data/funding-source-codes";

    public Task<FundingSourceCodePagedResponse?> SearchAsync(
        string? keyword = null,
        string? fundClusterCode = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) query["keyword"] = keyword;
        if (!string.IsNullOrWhiteSpace(fundClusterCode)) query["fundClusterCode"] = fundClusterCode;
        query["pageNumber"] = pageNumber.ToString(CultureInfo.InvariantCulture);
        query["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);

        return httpClient.GetFromJsonAsync<FundingSourceCodePagedResponse>($"{Base}?{query}", cancellationToken);
    }

    public async Task<FundingSourceCodeDto?> CreateAsync(
        CreateFundingSourceCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(Base, command, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FundingSourceCodeDto>(cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(
        UpdateFundingSourceCodeCommand command,
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
