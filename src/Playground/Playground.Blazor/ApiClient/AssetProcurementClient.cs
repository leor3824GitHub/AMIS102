using System.Globalization;
using System.Net.Http.Json;
using System.Web;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;

namespace AMIS.Playground.Blazor.ApiClient;

// ── Asset IARs ────────────────────────────────────────────────────────────────

internal interface IAssetIarClient
{
    Task<PagedResponse<AssetIARSummaryDto>> SearchAsync(
        string? keyword = null, AssetIARStatus? status = null,
        int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<AssetIARDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AssetIARDto> CreateAsync(CreateAssetIARCommand command, CancellationToken ct = default);
    Task<AssetIARDto> UpdateAsync(Guid id, UpdateAssetIARCommand command, CancellationToken ct = default);
    Task<AssetIARDto> AcceptAsync(Guid id, CancellationToken ct = default);
    Task<AssetIARDto> RejectAsync(Guid id, string reason, CancellationToken ct = default);
}

internal sealed class AssetIarClient(HttpClient http) : IAssetIarClient
{
    private const string Base = "api/v1/asset-procurement/iars";

    public Task<PagedResponse<AssetIARSummaryDto>> SearchAsync(
        string? keyword = null, AssetIARStatus? status = null,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<AssetIARSummaryDto>>($"{Base}?{q}", ct)!;
    }

    public Task<AssetIARDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<AssetIARDto>($"{Base}/{id}", ct);

    public async Task<AssetIARDto> CreateAsync(CreateAssetIARCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }

    public async Task<AssetIARDto> UpdateAsync(Guid id, UpdateAssetIARCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }

    public async Task<AssetIARDto> AcceptAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/accept", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }

    public async Task<AssetIARDto> RejectAsync(Guid id, string reason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/reject", new RejectAssetIARCommand(id, reason), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }
}

// ── Asset Purchase Orders (picker support) ────────────────────────────────────

internal interface IAssetPurchaseOrderClient
{
    Task<PagedResponse<AssetPurchaseOrderSummaryDto>> SearchAsync(
        string? keyword = null, int page = 1, int pageSize = 30, CancellationToken ct = default);
}

internal sealed class AssetPurchaseOrderClient(HttpClient http) : IAssetPurchaseOrderClient
{
    private const string Base = "api/v1/asset-procurement/purchase-orders";

    public Task<PagedResponse<AssetPurchaseOrderSummaryDto>> SearchAsync(
        string? keyword = null, int page = 1, int pageSize = 30, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<AssetPurchaseOrderSummaryDto>>($"{Base}?{q}", ct)!;
    }
}
