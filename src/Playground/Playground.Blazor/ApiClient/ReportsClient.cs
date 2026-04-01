using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Expendable.Contracts.v1.Requests;

namespace FSH.Playground.Blazor.ApiClient;

public interface IReportsClient
{
    Task<PagedResponse<DepartmentIssuanceSummaryDto>> GetDepartmentIssuanceReportAsync(
        string? departmentId = null,
        System.DateTimeOffset? from = null,
        System.DateTimeOffset? toDate = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<EmployeeIssuanceDto>> GetEmployeeIssuanceHistoryAsync(
        string? employeeId = null,
        System.DateTimeOffset? from = null,
        System.DateTimeOffset? toDate = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
}

public sealed class ReportsClient : IReportsClient
{
    private readonly HttpClient _httpClient;

    public ReportsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResponse<DepartmentIssuanceSummaryDto>> GetDepartmentIssuanceReportAsync(
        string? departmentId = null,
        System.DateTimeOffset? from = null,
        System.DateTimeOffset? toDate = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/expendable/reports/department-issuance", new()
        {
            ["departmentId"] = departmentId,
            ["from"] = from?.ToString("O"),
            ["to"] = toDate?.ToString("O"),
            ["pageNumber"] = pageNumber?.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize?.ToString(CultureInfo.InvariantCulture)
        });

        var response = await _httpClient.GetFromJsonAsync<PagedResponse<DepartmentIssuanceSummaryDto>>(
            url, cancellationToken);

        return response ?? new PagedResponse<DepartmentIssuanceSummaryDto> { Items = [] };
    }

    public async Task<PagedResponse<EmployeeIssuanceDto>> GetEmployeeIssuanceHistoryAsync(
        string? employeeId = null,
        System.DateTimeOffset? from = null,
        System.DateTimeOffset? toDate = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/expendable/reports/employee-issuance", new()
        {
            ["employeeId"] = employeeId,
            ["from"] = from?.ToString("O"),
            ["to"] = toDate?.ToString("O"),
            ["pageNumber"] = pageNumber?.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize?.ToString(CultureInfo.InvariantCulture)
        });

        var response = await _httpClient.GetFromJsonAsync<PagedResponse<EmployeeIssuanceDto>>(
            url, cancellationToken);

        return response ?? new PagedResponse<EmployeeIssuanceDto> { Items = [] };
    }

    private static string BuildUrl(string basePath, Dictionary<string, string?> queryParams)
    {
        var query = new StringBuilder();
        foreach (var (key, value) in queryParams)
        {
            if (string.IsNullOrEmpty(value)) continue;
            query.Append(query.Length == 0 ? '?' : '&');
            query.Append(Uri.EscapeDataString(key));
            query.Append('=');
            query.Append(Uri.EscapeDataString(value));
        }
        return basePath + query;
    }
}
