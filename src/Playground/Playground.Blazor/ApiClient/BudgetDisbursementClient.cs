using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.BudgetUtilizationRequests;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.DisbursementVouchers;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.Settings;

namespace AMIS.Playground.Blazor.ApiClient;

// Shared JSON options that mirror the API's ConfigureHttpJsonOptions: enums are serialized as
// strings ("Draft", not 0). Without this converter, GetFromJsonAsync uses the Web defaults (which
// read enums as numbers) and fails on status fields — e.g. "could not convert ... $.items[0].status".
file static class BudgetDisbursementJsonOptions
{
    internal static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

// ── Disbursement Vouchers ─────────────────────────────────────────────────────

internal interface IDisbursementVoucherClient
{
    Task<DisbursementVoucherSearchResult> SearchAsync(string? keyword = null, DisbursementVoucherStatus? status = null, Guid? purchaseOrderId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<IReadOnlyList<DisbursementVoucherStatusCountDto>> GetStatusCountsAsync(string? keyword = null, CancellationToken ct = default);
    Task<DisbursementVoucherDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateAsync(CreateDisbursementVoucherCommand command, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdateDisbursementVoucherCommand command, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task ApproveAsync(Guid id, CancellationToken ct = default);
    Task PayAsync(Guid id, DateOnly paidDate, string? remarks, CancellationToken ct = default);
    Task CancelAsync(Guid id, string remarks, CancellationToken ct = default);
    Task<byte[]> GetPdfAsync(Guid id, string? pageWidth = null, CancellationToken ct = default);
}

internal sealed class DisbursementVoucherClient(HttpClient http) : IDisbursementVoucherClient
{
    private const string Base = "api/v1/budget-disbursement/disbursement-vouchers";

    public Task<DisbursementVoucherSearchResult> SearchAsync(string? keyword = null, DisbursementVoucherStatus? status = null, Guid? purchaseOrderId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString();
        if (purchaseOrderId.HasValue) q["PurchaseOrderId"] = purchaseOrderId.Value.ToString();
        q["PageNumber"] = page.ToString();
        q["PageSize"] = pageSize.ToString();
        return http.GetFromJsonAsync<DisbursementVoucherSearchResult>($"{Base}?{q}", BudgetDisbursementJsonOptions.Default, ct)!;
    }

    public async Task<IReadOnlyList<DisbursementVoucherStatusCountDto>> GetStatusCountsAsync(string? keyword = null, CancellationToken ct = default)
    {
        var url = $"{Base}/status-counts";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"?Keyword={Uri.EscapeDataString(keyword)}";
        var result = await http.GetFromJsonAsync<List<DisbursementVoucherStatusCountDto>>(url, BudgetDisbursementJsonOptions.Default, ct).ConfigureAwait(false);
        return result ?? [];
    }

    public Task<DisbursementVoucherDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<DisbursementVoucherDto>($"{Base}/{id}", BudgetDisbursementJsonOptions.Default, ct);

    public async Task<Guid> CreateAsync(CreateDisbursementVoucherCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, BudgetDisbursementJsonOptions.Default, ct);
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadFromJsonAsync<CreateIdResponse>(ct);
        return body!.Id;
    }

    public async Task UpdateAsync(Guid id, UpdateDisbursementVoucherCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, BudgetDisbursementJsonOptions.Default, ct);
        r.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.DeleteAsync($"{Base}/{id}", ct);
        r.EnsureSuccessStatusCode();
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

    public Task<byte[]> GetPdfAsync(Guid id, string? pageWidth = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(pageWidth)
            ? $"api/v1/quest-pdf-reporting/budgetdisbursement/disbursement-vouchers/{id}/pdf"
            : $"api/v1/quest-pdf-reporting/budgetdisbursement/disbursement-vouchers/{id}/pdf?pageWidth={pageWidth}";
        return http.GetByteArrayAsync(url, ct);
    }

    private sealed record PayBody(DateOnly PaidDate, string? Remarks);
    private sealed record RemarksBody(string Remarks);
    private sealed record CreateIdResponse(Guid Id);
}

// ── Budget Utilization Requests ────────────────────────────────────────────────

internal interface IBudgetUtilizationRequestClient
{
    Task<BudgetUtilizationRequestSearchResult> SearchAsync(string? keyword = null, BudgetUtilizationRequestStatus? status = null, Guid? purchaseOrderId = null, string? allotmentClass = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<IReadOnlyList<BudgetUtilizationRequestStatusCountDto>> GetStatusCountsAsync(string? keyword = null, CancellationToken ct = default);
    Task<BudgetUtilizationRequestDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateAsync(CreateBudgetUtilizationRequestCommand command, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task ObligateAsync(Guid id, CancellationToken ct = default);
    Task CancelAsync(Guid id, string remarks, CancellationToken ct = default);
    Task<byte[]> GetPdfAsync(Guid id, string? pageWidth = null, CancellationToken ct = default);
}

internal sealed class BudgetUtilizationRequestClient(HttpClient http) : IBudgetUtilizationRequestClient
{
    private const string Base = "api/v1/budget-disbursement/budget-utilization-requests";

    public Task<BudgetUtilizationRequestSearchResult> SearchAsync(string? keyword = null, BudgetUtilizationRequestStatus? status = null, Guid? purchaseOrderId = null, string? allotmentClass = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString();
        if (purchaseOrderId.HasValue) q["PurchaseOrderId"] = purchaseOrderId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(allotmentClass)) q["AllotmentClass"] = allotmentClass;
        q["PageNumber"] = page.ToString();
        q["PageSize"] = pageSize.ToString();
        return http.GetFromJsonAsync<BudgetUtilizationRequestSearchResult>($"{Base}?{q}", BudgetDisbursementJsonOptions.Default, ct)!;
    }

    public async Task<IReadOnlyList<BudgetUtilizationRequestStatusCountDto>> GetStatusCountsAsync(string? keyword = null, CancellationToken ct = default)
    {
        var url = $"{Base}/status-counts";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"?Keyword={Uri.EscapeDataString(keyword)}";
        var result = await http.GetFromJsonAsync<List<BudgetUtilizationRequestStatusCountDto>>(url, BudgetDisbursementJsonOptions.Default, ct).ConfigureAwait(false);
        return result ?? [];
    }

    public Task<BudgetUtilizationRequestDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<BudgetUtilizationRequestDto>($"{Base}/{id}", BudgetDisbursementJsonOptions.Default, ct);

    public async Task<Guid> CreateAsync(CreateBudgetUtilizationRequestCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, BudgetDisbursementJsonOptions.Default, ct);
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadFromJsonAsync<CreateIdResponse>(ct);
        return body!.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.DeleteAsync($"{Base}/{id}", ct);
        r.EnsureSuccessStatusCode();
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

    public Task<byte[]> GetPdfAsync(Guid id, string? pageWidth = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(pageWidth)
            ? $"api/v1/quest-pdf-reporting/budgetdisbursement/budget-utilization-requests/{id}/pdf"
            : $"api/v1/quest-pdf-reporting/budgetdisbursement/budget-utilization-requests/{id}/pdf?pageWidth={pageWidth}";
        return http.GetByteArrayAsync(url, ct);
    }

    private sealed record RemarksBody(string Remarks);
    private sealed record CreateIdResponse(Guid Id);
}

// ── Budget Disbursement Settings ──────────────────────────────────────────────────────────

internal interface IBudgetDisbursementSettingsClient
{
    Task<BudgetDisbursementSettingsDto> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(UpdateBudgetDisbursementSettingsCommand command, CancellationToken ct = default);
}

internal sealed class BudgetDisbursementSettingsClient(HttpClient http) : IBudgetDisbursementSettingsClient
{
    private const string Base = "api/v1/budget-disbursement/settings";

    public Task<BudgetDisbursementSettingsDto> GetAsync(CancellationToken ct = default) =>
        http.GetFromJsonAsync<BudgetDisbursementSettingsDto>(Base, ct)!;

    public async Task UpdateAsync(UpdateBudgetDisbursementSettingsCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync(Base, command, BudgetDisbursementJsonOptions.Default, ct);
        r.EnsureSuccessStatusCode();
    }
}

