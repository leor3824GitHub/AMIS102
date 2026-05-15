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
    Task<AssetIARDto> SubmitForInspectionAsync(Guid id, CancellationToken ct = default);
    Task<AssetIARDto> ReassignInspectorAsync(Guid id, Guid newInspectorId, CancellationToken ct = default);
    Task<AssetIARDto> RecordInspectionAsync(Guid id, IReadOnlyList<LineInspectionDecision> decisions, CancellationToken ct = default);
    Task<AssetIARDto> AssignPropertyNoAsync(Guid id, int itemNo, string propertyNo, CancellationToken ct = default);
    Task<AssetIARDto> ExpandLineByQuantityAsync(Guid id, int itemNo, CancellationToken ct = default);
    Task<AssetIARDto> CancelAsync(Guid id, CancellationToken ct = default);
    Task<AssetIARDto> AcceptAsync(Guid id, CancellationToken ct = default);
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

    public async Task<AssetIARDto> SubmitForInspectionAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/submit-for-inspection", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }

    public async Task<AssetIARDto> ReassignInspectorAsync(Guid id, Guid newInspectorId, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/reassign-inspector",
            new ReassignInspectorCommand(id, newInspectorId),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }

    public async Task<AssetIARDto> RecordInspectionAsync(Guid id, IReadOnlyList<LineInspectionDecision> decisions, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/record-inspection",
            new RecordIARInspectionCommand(id, decisions),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }

    public async Task<AssetIARDto> AssignPropertyNoAsync(Guid id, int itemNo, string propertyNo, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/lines/{itemNo}/property-no",
            new AssignPropertyNoCommand(id, itemNo, propertyNo),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }

    public async Task<AssetIARDto> ExpandLineByQuantityAsync(Guid id, int itemNo, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/lines/{itemNo}/expand",
            new ExpandLineByQuantityCommand(id, itemNo),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }

    public async Task<AssetIARDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/cancel",
            new CancelAssetIARCommand(id),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }

    public async Task<AssetIARDto> AcceptAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/accept", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(ct))!;
    }
}

// ── Asset Purchase Orders (picker support) ────────────────────────────────────

internal interface IAssetPurchaseOrderClient
{
    Task<PagedResponse<AssetPurchaseOrderSummaryDto>> SearchAsync(
        string? keyword = null, AssetPurchaseOrderStatus? status = null,
        int page = 1, int pageSize = 30, CancellationToken ct = default);
    Task<AssetPurchaseOrderDto?> GetAsync(Guid id, CancellationToken ct = default);
}

internal sealed class AssetPurchaseOrderClient(HttpClient http) : IAssetPurchaseOrderClient
{
    private const string Base = "api/v1/asset-procurement/purchase-orders";

    public Task<PagedResponse<AssetPurchaseOrderSummaryDto>> SearchAsync(
        string? keyword = null, AssetPurchaseOrderStatus? status = null,
        int page = 1, int pageSize = 30, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<AssetPurchaseOrderSummaryDto>>($"{Base}?{q}", ct)!;
    }

    public Task<AssetPurchaseOrderDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<AssetPurchaseOrderDto>($"{Base}/{id}", ct);
}
