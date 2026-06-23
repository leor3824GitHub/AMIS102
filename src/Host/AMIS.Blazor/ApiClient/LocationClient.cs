using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AMIS.Playground.Blazor.ApiClient;

// Full CRUD client over the AssetRegister locations endpoint
// (api/v1/asset-register/locations). Backs the Blazor Locations management page.
// Read access requires AssetRegister Locations.View; writes require Create/Update/Delete.
// (ILocationLookupClient stays the lightweight read-only typeahead used by pickers.)
public enum LocationKind
{
    Warehouse = 0,
    Office = 1,
    Storage = 2,
    Department = 3,
    Other = 4,
}

public sealed record LocationModel(
    Guid Id,
    string Code,
    string Name,
    LocationKind Type,
    Guid? ParentLocationId,
    string? Description);

public sealed record PagedLocations(
    IReadOnlyList<LocationModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal sealed record LocationWriteRequest(
    string Code,
    string Name,
    LocationKind Type,
    Guid? ParentLocationId,
    string? Description);

public interface ILocationClient
{
    Task<PagedLocations> ListAsync(string? keyword = null, int pageNumber = 1, int pageSize = 100, CancellationToken ct = default);
    Task<LocationModel?> CreateAsync(string code, string name, LocationKind type, Guid? parentLocationId, string? description, CancellationToken ct = default);
    Task<LocationModel?> UpdateAsync(Guid id, string code, string name, LocationKind type, Guid? parentLocationId, string? description, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

internal sealed class LocationClient(HttpClient http) : ILocationClient
{
    private const string Base = "api/v1/asset-register/locations";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<PagedLocations> ListAsync(string? keyword = null, int pageNumber = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var query = $"?pageNumber={pageNumber.ToString(CultureInfo.InvariantCulture)}&pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}";
        if (!string.IsNullOrWhiteSpace(keyword))
            query += $"&keyword={Uri.EscapeDataString(keyword)}";
        var result = await http.GetFromJsonAsync<PagedLocations>(Base + query, JsonOptions, ct);
        return result ?? new PagedLocations([], pageNumber, pageSize, 0);
    }

    public async Task<LocationModel?> CreateAsync(string code, string name, LocationKind type, Guid? parentLocationId, string? description, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(
            Base, new LocationWriteRequest(code, name, type, parentLocationId, description), JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LocationModel>(JsonOptions, ct);
    }

    public async Task<LocationModel?> UpdateAsync(Guid id, string code, string name, LocationKind type, Guid? parentLocationId, string? description, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(
            $"{Base}/{id}", new LocationWriteRequest(code, name, type, parentLocationId, description), JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LocationModel>(JsonOptions, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{Base}/{id}", ct);
        response.EnsureSuccessStatusCode();
    }
}
