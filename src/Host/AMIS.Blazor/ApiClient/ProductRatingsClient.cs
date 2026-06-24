using System.Net.Http.Json;
using AMIS.Modules.Expendable.Contracts.v1.Products;

namespace AMIS.Blazor.ApiClient;

/// <summary>
/// Manual client for product rating endpoints (pending NSwag regeneration).
/// Talks to the Expendable products API.
/// </summary>
public interface IProductRatingsClient
{
    /// <summary>Create or update the current user's rating (1-5) for a product.</summary>
    Task RateAsync(Guid productId, int value, CancellationToken ct = default);

    /// <summary>Rating summaries (average + count) for every product in the tenant.</summary>
    Task<List<ProductRatingSummaryDto>> GetSummariesAsync(CancellationToken ct = default);

    /// <summary>Rating summary (average + count) for a single product.</summary>
    Task<ProductRatingSummaryDto> GetSummaryAsync(Guid productId, CancellationToken ct = default);

    /// <summary>The current user's rating for a product, or null if not yet rated.</summary>
    Task<MyProductRatingDto?> GetMyRatingAsync(Guid productId, CancellationToken ct = default);

    /// <summary>The individual raters (name + value) for a product, newest first.</summary>
    Task<List<ProductRaterDto>> GetRatersAsync(Guid productId, CancellationToken ct = default);
}

public sealed class ProductRatingsClient(HttpClient http) : IProductRatingsClient
{
    private const string Base = "api/v1/expendable/products";

    public async Task RateAsync(Guid productId, int value, CancellationToken ct = default)
    {
        var command = new RateProductCommand(productId, value);
        using var response = await http.PostAsJsonAsync($"{Base}/{productId}/ratings", command, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<ProductRatingSummaryDto>> GetSummariesAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<ProductRatingSummaryDto>>($"{Base}/ratings/summaries", ct)
            ?? new List<ProductRatingSummaryDto>();

    public async Task<ProductRatingSummaryDto> GetSummaryAsync(Guid productId, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<ProductRatingSummaryDto>($"{Base}/{productId}/ratings", ct)
            ?? new ProductRatingSummaryDto(productId, 0d, 0);

    public async Task<MyProductRatingDto?> GetMyRatingAsync(Guid productId, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<MyProductRatingDto?>($"{Base}/{productId}/ratings/mine", ct);

    public async Task<List<ProductRaterDto>> GetRatersAsync(Guid productId, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<ProductRaterDto>>($"{Base}/{productId}/ratings/raters", ct)
            ?? new List<ProductRaterDto>();
}
