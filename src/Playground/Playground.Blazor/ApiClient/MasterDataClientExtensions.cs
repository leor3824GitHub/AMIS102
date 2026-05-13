using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AMIS.Playground.Blazor.ApiClient;

/// <summary>
/// Additional response types for MasterData API - paging wrappers
/// </summary>


/// <summary>
/// Extension methods for Master_dataClient
/// </summary>
public static class Master_dataClientExtensions
{
    /// <summary>
    /// Get paginated list of categories
    /// </summary>
    public static async Task<PagedResponseOfCategoryDto?> CategoriesListAsync(
        this IMaster_dataClient client,
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 100,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        if (client is not Master_dataClient masterDataClient)
            return null;

        var httpClient = masterDataClient.GetType().GetField("_httpClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(masterDataClient) as HttpClient;
        if (httpClient == null)
            return null;

        var url = "api/v1/master-data/categories";
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
            query.Add($"keyword={Uri.EscapeDataString(keyword)}");
        if (pageNumber != 1)
            query.Add($"pageNumber={pageNumber}");
        if (pageSize != 100)
            query.Add($"pageSize={pageSize}");
        if (isActive.HasValue)
            query.Add($"isActive={isActive}");

        if (query.Count > 0)
            url += "?" + string.Join("&", query);

        var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<PagedResponseOfCategoryDto>(content, options);
    }

    /// <summary>
    /// Get paginated list of suppliers
    /// </summary>
    public static async Task<PagedResponseOfSupplierDto?> SuppliersListAsync(
        this IMaster_dataClient client,
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 100,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        if (client is not Master_dataClient masterDataClient)
            return null;

        var httpClient = masterDataClient.GetType().GetField("_httpClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(masterDataClient) as HttpClient;
        if (httpClient == null)
            return null;

        var url = "api/v1/master-data/suppliers";
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
            query.Add($"keyword={Uri.EscapeDataString(keyword)}");
        if (pageNumber != 1)
            query.Add($"pageNumber={pageNumber}");
        if (pageSize != 100)
            query.Add($"pageSize={pageSize}");
        if (isActive.HasValue)
            query.Add($"isActive={isActive}");

        if (query.Count > 0)
            url += "?" + string.Join("&", query);

        var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<PagedResponseOfSupplierDto>(content, options);
    }
}

