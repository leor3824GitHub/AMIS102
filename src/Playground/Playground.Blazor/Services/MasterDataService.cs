using AMIS.Playground.Blazor.ApiClient;
using ApiClient = AMIS.Playground.Blazor.ApiClient;
using System.Net.Http.Json;

namespace AMIS.Playground.Blazor.Services;

/// <summary>
/// Wrapper service for MasterData API Client operations
/// Bridges between Contracts and generated API client types
/// </summary>
public class MasterDataService
{
    private readonly IMaster_dataClient _client;
    private readonly HttpClient _httpClient;

    public MasterDataService(IMaster_dataClient client, HttpClient httpClient)
    {
        _client = client;
        _httpClient = httpClient;
    }

    // Supplier methods
    public async Task<PagedResponseOfSupplierDto?> GetSuppliersAsync(
        string? keyword = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            queryParts.Add($"keyword={Uri.EscapeDataString(keyword.Trim())}");
        }

        if (pageNumber.HasValue)
        {
            queryParts.Add($"pageNumber={pageNumber.Value}");
        }

        if (pageSize.HasValue)
        {
            queryParts.Add($"pageSize={pageSize.Value}");
        }

        var url = "api/v1/master-data/suppliers";
        if (queryParts.Count > 0)
        {
            url = $"{url}?{string.Join("&", queryParts)}";
        }

        return await _httpClient.GetFromJsonAsync<PagedResponseOfSupplierDto>(url, cancellationToken);
    }

    public async Task<ApiClient.SupplierDto> CreateSupplierAsync(
        ApiClient.CreateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        return await _client.SuppliersPostAsync(command, cancellationToken);
    }

    public async Task<ApiClient.SupplierDetailsDto> GetSupplierByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _client.SuppliersGetAsync(id, cancellationToken);
    }

    public async Task UpdateSupplierAsync(
        Guid id,
        ApiClient.UpdateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        await _client.SuppliersPutAsync(id, command, cancellationToken);
    }

    public async Task DeleteSupplierAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _client.SuppliersDeleteAsync(id, cancellationToken);
    }

    // Category methods
    public async Task<PagedResponseOfCategoryDto?> GetCategoriesAsync(
        string? keyword = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            queryParts.Add($"keyword={Uri.EscapeDataString(keyword.Trim())}");
        }

        if (pageNumber.HasValue)
        {
            queryParts.Add($"pageNumber={pageNumber.Value}");
        }

        if (pageSize.HasValue)
        {
            queryParts.Add($"pageSize={pageSize.Value}");
        }

        var url = "api/v1/master-data/categories";
        if (queryParts.Count > 0)
        {
            url = $"{url}?{string.Join("&", queryParts)}";
        }

        return await _httpClient.GetFromJsonAsync<PagedResponseOfCategoryDto>(url, cancellationToken);
    }

    public async Task<ApiClient.CategoryDto> CreateCategoryAsync(
        ApiClient.CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        return await _client.CategoriesPostAsync(command, cancellationToken);
    }

    public async Task<ApiClient.CategoryDetailsDto> GetCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _client.CategoriesGetAsync(id, cancellationToken);
    }

    public async Task UpdateCategoryAsync(
        Guid id,
        ApiClient.UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _client.CategoriesPutAsync(id, command, cancellationToken);
    }

    public async Task DeleteCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _client.CategoriesDeleteAsync(id, cancellationToken);
    }
}

