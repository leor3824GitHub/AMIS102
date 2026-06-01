using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;

namespace AMIS.Playground.Blazor.ApiClient;

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

    Task<List<PhysicalCountItemDto>> GetPhysicalCountReportAsync(
        Guid? warehouseLocationId = null,
        CancellationToken cancellationToken = default);

    Task<StockCardDto?> GetStockCardAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<byte[]> GenerateDepartmentIssuancePdfAsync(
        string? departmentId = null,
        System.DateTimeOffset? from = null,
        System.DateTimeOffset? to = null,
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> GeneratePhysicalCountPdfAsync(
        Guid? warehouseLocationId = null,
        DateTime? asOfDate = null,
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> GenerateStockCardPdfAsync(
        Guid productId,
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null,
        CancellationToken cancellationToken = default);

    Task<byte[]> GenerateEmployeeIssuancePdfAsync(
        string? employeeId = null,
        System.DateTimeOffset? from = null,
        System.DateTimeOffset? to = null,
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null,
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

    public async Task<StockCardDto?> GetStockCardAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<StockCardDto>(
            $"api/v1/expendable/reports/stock-card/{productId}", cancellationToken);
    }

    public async Task<List<PhysicalCountItemDto>> GetPhysicalCountReportAsync(
        Guid? warehouseLocationId = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/expendable/reports/physical-count", new()
        {
            ["warehouseLocationId"] = warehouseLocationId?.ToString()
        });

        var response = await _httpClient.GetFromJsonAsync<List<PhysicalCountItemDto>>(
            url, cancellationToken);

        return response ?? [];
    }

    public async Task<byte[]> GenerateDepartmentIssuancePdfAsync(
        string? departmentId = null,
        System.DateTimeOffset? from = null,
        System.DateTimeOffset? to = null,
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/quest-pdf-reporting/expendable/department-issuance/pdf", new()
        {
            ["departmentId"] = departmentId,
            ["from"] = from?.ToString("O"),
            ["to"] = to?.ToString("O"),
            ["pageWidth"] = pageWidth,
            ["orientation"] = orientation,
            ["marginMm"] = marginMm?.ToString(CultureInfo.InvariantCulture),
        });
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<byte[]> GeneratePhysicalCountPdfAsync(
        Guid? warehouseLocationId = null,
        DateTime? asOfDate = null,
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/quest-pdf-reporting/expendable/physical-count/pdf", new()
        {
            ["warehouseLocationId"] = warehouseLocationId?.ToString(),
            ["asOfDate"] = asOfDate?.ToString("O"),
            ["pageWidth"] = pageWidth,
            ["orientation"] = orientation,
            ["marginMm"] = marginMm?.ToString(CultureInfo.InvariantCulture),
        });
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<byte[]> GenerateStockCardPdfAsync(
        Guid productId,
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl($"api/v1/quest-pdf-reporting/expendable/stock-card/{productId}/pdf", new()
        {
            ["pageWidth"] = pageWidth,
            ["orientation"] = orientation,
            ["marginMm"] = marginMm?.ToString(CultureInfo.InvariantCulture),
        });
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<byte[]> GenerateEmployeeIssuancePdfAsync(
        string? employeeId = null,
        System.DateTimeOffset? from = null,
        System.DateTimeOffset? to = null,
        string? pageWidth = null,
        string? orientation = null,
        double? marginMm = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("api/v1/quest-pdf-reporting/expendable/employee-issuance/pdf", new()
        {
            ["employeeId"] = employeeId,
            ["from"] = from?.ToString("O"),
            ["to"] = to?.ToString("O"),
            ["pageWidth"] = pageWidth,
            ["orientation"] = orientation,
            ["marginMm"] = marginMm?.ToString(CultureInfo.InvariantCulture),
        });
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
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

