using System.Globalization;
using System.Net.Http.Json;
using System.Web;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using PurchaseOrderContracts = AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

namespace AMIS.Playground.Blazor.ApiClient;

// ── Purchase Requests ─────────────────────────────────────────────────────────

internal interface IPurchaseRequestClient
{
    Task<PagedResponse<PurchaseRequestSummaryDto>> SearchAsync(string? keyword = null, PurchaseRequestStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PurchaseRequestDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestCommand command, CancellationToken ct = default);
    Task<PurchaseRequestDto> UpdateAsync(Guid id, UpdatePurchaseRequestCommand command, CancellationToken ct = default);
    Task<PurchaseRequestDto> SubmitAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseRequestDto> ApproveAsync(Guid id, Guid approvedById, CancellationToken ct = default);
    Task<PurchaseRequestDto> RejectAsync(Guid id, string reason, CancellationToken ct = default);
    Task<PurchaseRequestDto> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default);
}

internal sealed class PurchaseRequestClient(HttpClient http) : IPurchaseRequestClient
{
    private const string Base = "api/v1/procurement/purchase-requests";

    public Task<PagedResponse<PurchaseRequestSummaryDto>> SearchAsync(string? keyword = null, PurchaseRequestStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<PurchaseRequestSummaryDto>>($"{Base}?{q}", ct)!;
    }

    public Task<PurchaseRequestDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PurchaseRequestDto>($"{Base}/{id}", ct);

    public async Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ct))!;
    }

    public async Task<PurchaseRequestDto> UpdateAsync(Guid id, UpdatePurchaseRequestCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ct))!;
    }

    public async Task<PurchaseRequestDto> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/submit", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ct))!;
    }

    public async Task<PurchaseRequestDto> ApproveAsync(Guid id, Guid approvedById, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/approve", new ApprovePurchaseRequestCommand(id, approvedById), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ct))!;
    }

    public async Task<PurchaseRequestDto> RejectAsync(Guid id, string reason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/reject", new RejectPurchaseRequestCommand(id, reason), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ct))!;
    }

    public async Task<PurchaseRequestDto> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/cancel", new CancelPurchaseRequestCommand(id, reason), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ct))!;
    }
}

// ── Canvass Requests ──────────────────────────────────────────────────────────

internal interface ICanvassRequestClient
{
    Task<PagedResponse<CanvassRequestSummaryDto>> SearchAsync(string? keyword = null, CanvassRequestStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<CanvassRequestDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CanvassRequestDto> CreateAsync(CreateCanvassRequestCommand command, CancellationToken ct = default);
    Task<CanvassQuotationDto> AddQuotationAsync(Guid canvassRequestId, AddQuotationCommand command, CancellationToken ct = default);
    Task<CanvassRequestDto> AwardAsync(Guid canvassRequestId, Guid awardedQuotationId, CancellationToken ct = default);
}

internal sealed class CanvassRequestClient(HttpClient http) : ICanvassRequestClient
{
    private const string Base = "api/v1/procurement/canvass-requests";

    public Task<PagedResponse<CanvassRequestSummaryDto>> SearchAsync(string? keyword = null, CanvassRequestStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<CanvassRequestSummaryDto>>($"{Base}?{q}", ct)!;
    }

    public Task<CanvassRequestDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<CanvassRequestDto>($"{Base}/{id}", ct);

    public async Task<CanvassRequestDto> CreateAsync(CreateCanvassRequestCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CanvassRequestDto>(ct))!;
    }

    public async Task<CanvassQuotationDto> AddQuotationAsync(Guid canvassRequestId, AddQuotationCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{canvassRequestId}/quotations", command with { CanvassRequestId = canvassRequestId }, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CanvassQuotationDto>(ct))!;
    }

    public async Task<CanvassRequestDto> AwardAsync(Guid canvassRequestId, Guid awardedQuotationId, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{canvassRequestId}/award", new AwardCanvassCommand(canvassRequestId, awardedQuotationId), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CanvassRequestDto>(ct))!;
    }
}

// ── Purchase Orders ───────────────────────────────────────────────────────────

internal interface IPurchaseOrderClient
{
    Task<PagedResponse<PurchaseOrderSummaryDto>> SearchAsync(string? keyword = null, PurchaseOrderStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PurchaseOrderDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseOrderDto> CreateAsync(PurchaseOrderContracts.CreatePurchaseOrderCommand command, CancellationToken ct = default);
    Task<PurchaseOrderDto> UpdateAsync(Guid id, UpdatePurchaseOrderCommand command, CancellationToken ct = default);
    Task<PurchaseOrderDto> IssueAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseOrderDto> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default);
}

internal sealed class PurchaseOrderClient(HttpClient http) : IPurchaseOrderClient
{
    private const string Base = "api/v1/procurement/purchase-orders";

    public Task<PagedResponse<PurchaseOrderSummaryDto>> SearchAsync(string? keyword = null, PurchaseOrderStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<PurchaseOrderSummaryDto>>($"{Base}?{q}", ct)!;
    }

    public Task<PurchaseOrderDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PurchaseOrderDto>($"{Base}/{id}", ct);

    public async Task<PurchaseOrderDto> CreateAsync(PurchaseOrderContracts.CreatePurchaseOrderCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseOrderDto>(ct))!;
    }

    public async Task<PurchaseOrderDto> UpdateAsync(Guid id, UpdatePurchaseOrderCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseOrderDto>(ct))!;
    }

    public async Task<PurchaseOrderDto> IssueAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PatchAsync($"{Base}/{id}/issue", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseOrderDto>(ct))!;
    }

    public async Task<PurchaseOrderDto> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        using var r = await http.PatchAsJsonAsync($"{Base}/{id}/cancel", new CancelPoBody(reason), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseOrderDto>(ct))!;
    }

    private sealed record CancelPoBody(string? Reason);
}

