using System.Net.Http.Json;
using AMIS.Blazor.ApiClient;

namespace AMIS.Blazor.Services;

/// <summary>Warehouse location as returned by the unpaged warehouse-locations lookup.</summary>
internal sealed record WarehouseLocationItem(Guid Id, string Name);

/// <summary>
/// Expendable reads that must not be truncated by the 100-row page-size clamp, and batch reads that
/// replace per-row fetch loops. The paged <c>inventory/search</c> and <c>products</c> endpoints are fine
/// for browsing, but pages that derive warehouse pickers, per-product balances, or product names for a
/// set of ids need a complete answer in one call — those go through the endpoints here.
/// </summary>
internal interface IInventoryDataService
{
    Task<IReadOnlyList<WarehouseLocationItem>> GetWarehouseLocationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductInventoryDto>> GetInventoriesByProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetProductsByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);
}

internal sealed class InventoryDataService(HttpClient httpClient) : IInventoryDataService
{
    public async Task<IReadOnlyList<WarehouseLocationItem>> GetWarehouseLocationsAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient
            .GetFromJsonAsync<List<WarehouseLocationItem>>("api/v1/expendable/warehouse/locations", cancellationToken)
            .ConfigureAwait(false);

        return result ?? [];
    }

    public Task<IReadOnlyList<ProductInventoryDto>> GetInventoriesByProductsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
        => PostIdsAsync<ProductInventoryDto>("api/v1/expendable/warehouse/inventory/by-products", productIds, cancellationToken);

    public Task<IReadOnlyList<ProductDto>> GetProductsByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
        => PostIdsAsync<ProductDto>("api/v1/expendable/products/by-ids", productIds, cancellationToken);

    private async Task<IReadOnlyList<T>> PostIdsAsync<T>(
        string url,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];

        var response = await httpClient.PostAsJsonAsync(url, ids, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<List<T>>(cancellationToken)
            .ConfigureAwait(false);

        return result ?? [];
    }
}
