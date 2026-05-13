using System.Net.Http.Json;
using System.Web;
using AMIS.Modules.Finance.Contracts.v1.BudgetUtilizationRecords;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;

namespace AMIS.Playground.Blazor.ApiClient;

// ── Disbursement Vouchers ─────────────────────────────────────────────────────

internal interface IDisbursementVoucherClient
{
    Task<DisbursementVoucherSearchResult> SearchAsync(string? keyword = null, DisbursementVoucherStatus? status = null, Guid? purchaseOrderId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<DisbursementVoucherDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateAsync(CreateDisbursementVoucherCommand command, CancellationToken ct = default);
    Task ApproveAsync(Guid id, CancellationToken ct = default);
    Task PayAsync(Guid id, DateOnly paidDate, string? remarks, CancellationToken ct = default);
    Task CancelAsync(Guid id, string remarks, CancellationToken ct = default);
}

internal sealed class DisbursementVoucherClient(HttpClient http) : IDisbursementVoucherClient
{
    private const string Base = "api/v1/finance/disbursement-vouchers";

    public Task<DisbursementVoucherSearchResult> SearchAsync(string? keyword = null, DisbursementVoucherStatus? status = null, Guid? purchaseOrderId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString();
        if (purchaseOrderId.HasValue) q["PurchaseOrderId"] = purchaseOrderId.Value.ToString();
        q["PageNumber"] = page.ToString();
        q["PageSize"] = pageSize.ToString();
        return http.GetFromJsonAsync<DisbursementVoucherSearchResult>($"{Base}?{q}", ct)!;
    }

    public Task<DisbursementVoucherDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<DisbursementVoucherDto>($"{Base}/{id}", ct);

    public async Task<Guid> CreateAsync(CreateDisbursementVoucherCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadFromJsonAsync<CreateIdResponse>(ct);
        return body!.Id;
    }

    public async Task ApproveAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/approve", null, ct);
        r.EnsureSuccessStatusCode();
    }

    public async Task PayAsync(Guid id, DateOnly paidDate, string? remarks, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/pay", new PayBody(paidDate, remarks), ct);
        r.EnsureSuccessStatusCode();
    }

    public async Task CancelAsync(Guid id, string remarks, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/cancel", new RemarksBody(remarks), ct);
        r.EnsureSuccessStatusCode();
    }

    private sealed record PayBody(DateOnly PaidDate, string? Remarks);
    private sealed record RemarksBody(string Remarks);
    private sealed record CreateIdResponse(Guid Id);
}

// ── Budget Utilization Records ────────────────────────────────────────────────

internal interface IBudgetUtilizationRecordClient
{
    Task<BudgetUtilizationRecordSearchResult> SearchAsync(string? keyword = null, BudgetUtilizationRecordStatus? status = null, Guid? purchaseOrderId = null, string? allotmentClass = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<BudgetUtilizationRecordDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateAsync(CreateBudgetUtilizationRecordCommand command, CancellationToken ct = default);
    Task ObligateAsync(Guid id, CancellationToken ct = default);
    Task CancelAsync(Guid id, string remarks, CancellationToken ct = default);
}

internal sealed class BudgetUtilizationRecordClient(HttpClient http) : IBudgetUtilizationRecordClient
{
    private const string Base = "api/v1/finance/budget-utilization-records";

    public Task<BudgetUtilizationRecordSearchResult> SearchAsync(string? keyword = null, BudgetUtilizationRecordStatus? status = null, Guid? purchaseOrderId = null, string? allotmentClass = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString();
        if (purchaseOrderId.HasValue) q["PurchaseOrderId"] = purchaseOrderId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(allotmentClass)) q["AllotmentClass"] = allotmentClass;
        q["PageNumber"] = page.ToString();
        q["PageSize"] = pageSize.ToString();
        return http.GetFromJsonAsync<BudgetUtilizationRecordSearchResult>($"{Base}?{q}", ct)!;
    }

    public Task<BudgetUtilizationRecordDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<BudgetUtilizationRecordDto>($"{Base}/{id}", ct);

    public async Task<Guid> CreateAsync(CreateBudgetUtilizationRecordCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadFromJsonAsync<CreateIdResponse>(ct);
        return body!.Id;
    }

    public async Task ObligateAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/obligate", null, ct);
        r.EnsureSuccessStatusCode();
    }

    public async Task CancelAsync(Guid id, string remarks, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/cancel", new RemarksBody(remarks), ct);
        r.EnsureSuccessStatusCode();
    }

    private sealed record RemarksBody(string Remarks);
    private sealed record CreateIdResponse(Guid Id);
}

