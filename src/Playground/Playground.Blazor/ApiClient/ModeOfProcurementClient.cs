using System.Net.Http.Json;
using System.Web;
using System.Globalization;
using FSH.Modules.MasterData.Contracts.v1.ModesOfProcurement;

namespace FSH.Playground.Blazor.ApiClient;

internal sealed record ModeOfProcurementPagedResponse(
    ICollection<ModeOfProcurementDto>? Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface IModeOfProcurementClient
{
    Task<ModeOfProcurementPagedResponse?> SearchAsync(
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}

internal sealed class ModeOfProcurementClient(HttpClient httpClient) : IModeOfProcurementClient
{
    private const string Base = "api/v1/master-data/modes-of-procurement";

    public Task<ModeOfProcurementPagedResponse?> SearchAsync(
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) query["keyword"] = keyword;
        query["pageNumber"] = pageNumber.ToString(CultureInfo.InvariantCulture);
        query["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);

        return httpClient.GetFromJsonAsync<ModeOfProcurementPagedResponse>($"{Base}?{query}", cancellationToken);
    }
}
