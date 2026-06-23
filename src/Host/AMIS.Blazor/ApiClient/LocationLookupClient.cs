using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AMIS.Playground.Blazor.ApiClient;

// Lightweight read-only lookup over the AssetRegister locations endpoint
// (api/v1/asset-register/locations). Used by AssetRegister UIs that need to pick a
// location Guid (e.g. recording a physical-count found-at-station item). Requires the
// caller to hold AssetRegister Locations.View.
public sealed record LocationLookupDto(Guid Id, string Code, string Name);

internal sealed record PagedLocationLookupResponse(
    IReadOnlyList<LocationLookupDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public interface ILocationLookupClient
{
    Task<IReadOnlyList<LocationLookupDto>> SearchAsync(string? keyword = null, int pageSize = 20, CancellationToken ct = default);
}

internal sealed class LocationLookupClient(HttpClient http) : ILocationLookupClient
{
    private const string Base = "api/v1/asset-register/locations";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<LocationLookupDto>> SearchAsync(string? keyword = null, int pageSize = 20, CancellationToken ct = default)
    {
        var query = $"?pageNumber=1&pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}";
        if (!string.IsNullOrWhiteSpace(keyword))
            query += $"&keyword={Uri.EscapeDataString(keyword)}";
        var result = await http.GetFromJsonAsync<PagedLocationLookupResponse>(Base + query, JsonOptions, ct);
        return result?.Items ?? [];
    }
}
