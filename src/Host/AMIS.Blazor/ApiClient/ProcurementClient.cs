using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using PurchaseOrderContracts = AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;

namespace AMIS.Blazor.ApiClient;

internal sealed class DuplicatePurchaseOrderException(string message) : Exception(message);

internal sealed class DuplicateJobOrderException(string message) : Exception(message);

internal static class ProcurementJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

// ── Purchase Requests ─────────────────────────────────────────────────────────

internal interface IPurchaseRequestClient
{
    Task<PagedResponse<PurchaseRequestSummaryDto>> SearchAsync(string? keyword = null, PurchaseRequestStatus? status = null, int page = 1, int pageSize = 20, DateOnly? fromDate = null, DateOnly? toDate = null, bool excludeFullyCanvassed = false, bool excludeWithIar = false, CancellationToken ct = default);
    Task<IReadOnlyList<PurchaseRequestStatusCountDto>> GetStatusCountsAsync(DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default);
    Task<PurchaseRequestDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<byte[]> GetPrintPdfAsync(Guid id, string? pageWidth = null, string? pageHeight = null, CancellationToken ct = default);
    Task<byte[]> GetFastReportPdfAsync(Guid id, string? pageWidth = null, int? copies = null, string? orientation = null, int? minRows = null, CancellationToken ct = default);
    Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestCommand command, CancellationToken ct = default);
    Task<PurchaseRequestDto> UpdateAsync(Guid id, UpdatePurchaseRequestCommand command, CancellationToken ct = default);
    Task<PurchaseRequestDto> SubmitAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseRequestDto> CertifyFundsAvailableAsync(Guid id, CertifyFundsAvailableCommand command, CancellationToken ct = default);
    Task<PurchaseRequestDto> ApproveAsync(Guid id, string approvedByName, CancellationToken ct = default);
    Task<PurchaseRequestDto> ReturnForRevisionAsync(Guid id, string returnedByName, string reason, CancellationToken ct = default);
    Task<PurchaseRequestDto> RejectAsync(Guid id, string reason, CancellationToken ct = default);
    Task<PurchaseRequestDto> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default);
}

internal sealed class PurchaseRequestClient(HttpClient http) : IPurchaseRequestClient
{
    private const string Base = "api/v1/procurement/purchase-requests";

    public Task<PagedResponse<PurchaseRequestSummaryDto>> SearchAsync(string? keyword = null, PurchaseRequestStatus? status = null, int page = 1, int pageSize = 20, DateOnly? fromDate = null, DateOnly? toDate = null, bool excludeFullyCanvassed = false, bool excludeWithIar = false, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        if (fromDate.HasValue) q["FromDate"] = fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (toDate.HasValue) q["ToDate"] = toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (excludeFullyCanvassed) q["ExcludeFullyCanvassed"] = "true";
        if (excludeWithIar) q["ExcludeWithIar"] = "true";
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<PurchaseRequestSummaryDto>>($"{Base}?{q}", ProcurementJson.Options, ct)!;
    }

    public async Task<IReadOnlyList<PurchaseRequestStatusCountDto>> GetStatusCountsAsync(DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (fromDate.HasValue) q["FromDate"] = fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (toDate.HasValue) q["ToDate"] = toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var qs = q.ToString();
        var url = string.IsNullOrEmpty(qs) ? $"{Base}/status-counts" : $"{Base}/status-counts?{qs}";
        var result = await http.GetFromJsonAsync<List<PurchaseRequestStatusCountDto>>(url, ProcurementJson.Options, ct).ConfigureAwait(false);
        return result ?? [];
    }

    public Task<PurchaseRequestDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PurchaseRequestDto>($"{Base}/{id}", ProcurementJson.Options, ct);

    public Task<byte[]> GetFastReportPdfAsync(
        Guid id,
        string? pageWidth = null,
        int? copies = null,
        string? orientation = null,
        int? minRows = null,
        CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(pageWidth))
            query["pageWidth"] = pageWidth;
        if (copies is 1 or 2)
            query["copies"] = copies.Value.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(orientation))
            query["orientation"] = orientation;
        if (minRows is > 0)
            query["minRows"] = minRows.Value.ToString(CultureInfo.InvariantCulture);

        var queryString = query.ToString();
        var url = string.IsNullOrWhiteSpace(queryString)
            ? $"api/v1/fast-reporting/procurement/purchase-requests/{id}/print"
            : $"api/v1/fast-reporting/procurement/purchase-requests/{id}/print?{queryString}";

        return http.GetByteArrayAsync(url, ct);
    }

    public Task<byte[]> GetPrintPdfAsync(Guid id, string? pageWidth = null, string? pageHeight = null, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(pageWidth))
        {
            query["pageWidth"] = pageWidth;
        }

        if (!string.IsNullOrWhiteSpace(pageHeight))
        {
            query["pageHeight"] = pageHeight;
        }

        var queryString = query.ToString();
        var url = string.IsNullOrWhiteSpace(queryString)
            ? $"api/v1/rdlc-reporting/procurement/purchase-requests/{id}/print"
            : $"api/v1/rdlc-reporting/procurement/purchase-requests/{id}/print?{queryString}";

        return http.GetByteArrayAsync(url, ct);
    }

    public async Task<PurchaseRequestDto> CreateAsync(CreatePurchaseRequestCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseRequestDto> UpdateAsync(Guid id, UpdatePurchaseRequestCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseRequestDto> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/submit", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseRequestDto> CertifyFundsAvailableAsync(Guid id, CertifyFundsAvailableCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/certify-funds-available", command with { Id = id }, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseRequestDto> ApproveAsync(Guid id, string approvedByName, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/approve", new ApprovePurchaseRequestCommand(id, approvedByName), ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseRequestDto> ReturnForRevisionAsync(Guid id, string returnedByName, string reason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/return-for-revision",
            new ReturnPurchaseRequestForRevisionCommand(id, returnedByName, reason), ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseRequestDto> RejectAsync(Guid id, string reason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/reject", new RejectPurchaseRequestCommand(id, reason), ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseRequestDto> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/cancel", new CancelPurchaseRequestCommand(id, reason), ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseRequestDto>(ProcurementJson.Options, ct))!;
    }
}

// ── Canvass Requests ──────────────────────────────────────────────────────────

internal interface ICanvassRequestClient
{
    Task<PagedResponse<CanvassRequestSummaryDto>> SearchAsync(string? keyword = null, CanvassRequestStatus? status = null, int page = 1, int pageSize = 20, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default);
    Task<IReadOnlyList<CanvassRequestStatusCountDto>> GetStatusCountsAsync(DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default);
    Task<CanvassRequestDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CanvassablePrLineDto>> GetCanvassablePrLinesAsync(Guid purchaseRequestId, CancellationToken ct = default);
    Task<IReadOnlyList<AwardedPrLineDto>> GetAwardedPrLinesAsync(Guid purchaseRequestId, CancellationToken ct = default);
    Task<byte[]> GetFastReportPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, int? minRows = null, CancellationToken ct = default);
    Task<byte[]> GetRfqFastReportPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, int? minRows = null, CancellationToken ct = default);
    Task<CanvassRequestDto> CreateAsync(CreateCanvassRequestCommand command, CancellationToken ct = default);
    Task<CanvassQuotationDto> AddQuotationAsync(Guid canvassRequestId, AddQuotationCommand command, CancellationToken ct = default);
    Task<CanvassRequestDto> AwardAsync(Guid canvassRequestId, IReadOnlyList<CanvassLineAwardRequest> lineAwards, CancellationToken ct = default);
}

internal sealed class CanvassRequestClient(HttpClient http) : ICanvassRequestClient
{
    private const string Base = "api/v1/procurement/canvass-requests";

    public Task<PagedResponse<CanvassRequestSummaryDto>> SearchAsync(string? keyword = null, CanvassRequestStatus? status = null, int page = 1, int pageSize = 20, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        if (fromDate.HasValue) q["FromDate"] = fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (toDate.HasValue) q["ToDate"] = toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<CanvassRequestSummaryDto>>($"{Base}?{q}", ProcurementJson.Options, ct)!;
    }

    public async Task<IReadOnlyList<CanvassRequestStatusCountDto>> GetStatusCountsAsync(DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (fromDate.HasValue) q["FromDate"] = fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (toDate.HasValue) q["ToDate"] = toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var qs = q.ToString();
        var url = string.IsNullOrEmpty(qs) ? $"{Base}/status-counts" : $"{Base}/status-counts?{qs}";
        var result = await http.GetFromJsonAsync<List<CanvassRequestStatusCountDto>>(url, ProcurementJson.Options, ct).ConfigureAwait(false);
        return result ?? [];
    }

    public Task<CanvassRequestDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<CanvassRequestDto>($"{Base}/{id}", ProcurementJson.Options, ct);

    public async Task<IReadOnlyList<CanvassablePrLineDto>> GetCanvassablePrLinesAsync(Guid purchaseRequestId, CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<List<CanvassablePrLineDto>>(
            $"{Base}/pr/{purchaseRequestId}/canvassable-lines", ProcurementJson.Options, ct).ConfigureAwait(false);
        return result ?? [];
    }

    public async Task<IReadOnlyList<AwardedPrLineDto>> GetAwardedPrLinesAsync(Guid purchaseRequestId, CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<List<AwardedPrLineDto>>(
            $"{Base}/pr/{purchaseRequestId}/awarded-lines", ProcurementJson.Options, ct).ConfigureAwait(false);
        return result ?? [];
    }

    public Task<byte[]> GetFastReportPdfAsync(
        Guid id,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null,
        CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(pageWidth))
            query["pageWidth"] = pageWidth;
        if (!string.IsNullOrWhiteSpace(orientation))
            query["orientation"] = orientation;
        if (minRows is > 0)
            query["minRows"] = minRows.Value.ToString(CultureInfo.InvariantCulture);

        var queryString = query.ToString();
        var url = string.IsNullOrWhiteSpace(queryString)
            ? $"api/v1/fast-reporting/procurement/canvass-requests/{id}/print"
            : $"api/v1/fast-reporting/procurement/canvass-requests/{id}/print?{queryString}";

        return http.GetByteArrayAsync(url, ct);
    }

    public Task<byte[]> GetRfqFastReportPdfAsync(
        Guid id,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null,
        CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(pageWidth))
            query["pageWidth"] = pageWidth;
        if (!string.IsNullOrWhiteSpace(orientation))
            query["orientation"] = orientation;
        if (minRows is > 0)
            query["minRows"] = minRows.Value.ToString(CultureInfo.InvariantCulture);

        var queryString = query.ToString();
        var url = string.IsNullOrWhiteSpace(queryString)
            ? $"api/v1/fast-reporting/procurement/canvass-requests/{id}/rfq/print"
            : $"api/v1/fast-reporting/procurement/canvass-requests/{id}/rfq/print?{queryString}";

        return http.GetByteArrayAsync(url, ct);
    }

    public async Task<CanvassRequestDto> CreateAsync(CreateCanvassRequestCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CanvassRequestDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<CanvassQuotationDto> AddQuotationAsync(Guid canvassRequestId, AddQuotationCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{canvassRequestId}/quotations", command with { CanvassRequestId = canvassRequestId }, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CanvassQuotationDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<CanvassRequestDto> AwardAsync(Guid canvassRequestId, IReadOnlyList<CanvassLineAwardRequest> lineAwards, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{canvassRequestId}/award", new AwardCanvassCommand(canvassRequestId, lineAwards), ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CanvassRequestDto>(ProcurementJson.Options, ct))!;
    }
}

// ── Purchase Orders ───────────────────────────────────────────────────────────

internal interface IPurchaseOrderClient
{
    Task<PagedResponse<PurchaseOrderSummaryDto>> SearchAsync(string? keyword = null, PurchaseOrderStatus? status = null, int page = 1, int pageSize = 20, Guid? purchaseRequestId = null, Guid? supplierId = null, DateOnly? fromDate = null, DateOnly? toDate = null, bool excludeWithIar = false, CancellationToken ct = default);
    Task<PurchaseOrderDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<byte[]> GetFastReportPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, int? minRows = null, CancellationToken ct = default);
    Task<PurchaseOrderDto> CreateAsync(PurchaseOrderContracts.CreatePurchaseOrderCommand command, CancellationToken ct = default);
    Task<IReadOnlyList<PurchaseOrderDto>> CreateFromCanvassAsync(CreatePurchaseOrdersFromCanvassCommand command, CancellationToken ct = default);
    Task<PurchaseOrderDto> UpdateAsync(Guid id, UpdatePurchaseOrderCommand command, CancellationToken ct = default);
    Task<PurchaseOrderDto> SubmitAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseOrderDto> CertifyFundsAvailableAsync(Guid id, CertifyPurchaseOrderFundsAvailableCommand command, CancellationToken ct = default);
    Task<PurchaseOrderDto> IssueAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseOrderDto> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default);
}

internal sealed class PurchaseOrderClient(HttpClient http) : IPurchaseOrderClient
{
    private const string Base = "api/v1/procurement/purchase-orders";

    public Task<PagedResponse<PurchaseOrderSummaryDto>> SearchAsync(string? keyword = null, PurchaseOrderStatus? status = null, int page = 1, int pageSize = 20, Guid? purchaseRequestId = null, Guid? supplierId = null, DateOnly? fromDate = null, DateOnly? toDate = null, bool excludeWithIar = false, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        if (purchaseRequestId.HasValue) q["PurchaseRequestId"] = purchaseRequestId.Value.ToString();
        if (supplierId.HasValue) q["SupplierId"] = supplierId.Value.ToString();
        if (fromDate.HasValue) q["FromDate"] = fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (toDate.HasValue) q["ToDate"] = toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (excludeWithIar) q["ExcludeWithIar"] = "true";
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<PurchaseOrderSummaryDto>>($"{Base}?{q}", ProcurementJson.Options, ct)!;
    }

    public Task<PurchaseOrderDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PurchaseOrderDto>($"{Base}/{id}", ProcurementJson.Options, ct);

    public Task<byte[]> GetFastReportPdfAsync(
        Guid id,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null,
        CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(pageWidth))
            query["pageWidth"] = pageWidth;
        if (!string.IsNullOrWhiteSpace(orientation))
            query["orientation"] = orientation;
        if (minRows is > 0)
            query["minRows"] = minRows.Value.ToString(CultureInfo.InvariantCulture);

        var queryString = query.ToString();
        var url = string.IsNullOrWhiteSpace(queryString)
            ? $"api/v1/fast-reporting/procurement/purchase-orders/{id}/print"
            : $"api/v1/fast-reporting/procurement/purchase-orders/{id}/print?{queryString}";

        return http.GetByteArrayAsync(url, ct);
    }

    public async Task<PurchaseOrderDto> CreateAsync(PurchaseOrderContracts.CreatePurchaseOrderCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ProcurementJson.Options, ct);

        if (r.StatusCode == HttpStatusCode.Conflict)
        {
            var detail = await ReadProblemDetailAsync(r, ct);
            throw new DuplicatePurchaseOrderException(detail ?? "A duplicate purchase order already exists.");
        }

        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<IReadOnlyList<PurchaseOrderDto>> CreateFromCanvassAsync(CreatePurchaseOrdersFromCanvassCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/from-canvass", command, ProcurementJson.Options, ct);

        if (r.StatusCode == HttpStatusCode.Conflict)
        {
            var detail = await ReadProblemDetailAsync(r, ct);
            throw new DuplicatePurchaseOrderException(detail ?? "Purchase orders could not be generated from this canvass.");
        }

        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<IReadOnlyList<PurchaseOrderDto>>(ProcurementJson.Options, ct))!;
    }

    private static async Task<string?> ReadProblemDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsLite>(ProcurementJson.Options, ct);
            return problem?.Detail;
        }
        catch
        {
            return null;
        }
    }

    private sealed record ProblemDetailsLite(string? Title, string? Detail);

    public async Task<PurchaseOrderDto> UpdateAsync(Guid id, UpdatePurchaseOrderCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseOrderDto> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/submit", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseOrderDto> CertifyFundsAvailableAsync(Guid id, CertifyPurchaseOrderFundsAvailableCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/certify-funds-available", command with { Id = id }, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseOrderDto> IssueAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PatchAsync($"{Base}/{id}/issue", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<PurchaseOrderDto> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        using var r = await http.PatchAsJsonAsync($"{Base}/{id}/cancel", new CancelPoBody(reason), ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PurchaseOrderDto>(ProcurementJson.Options, ct))!;
    }

    private sealed record CancelPoBody(string? Reason);
}

// ── Job Orders ────────────────────────────────────────────────────────────────

internal interface IJobOrderClient
{
    Task<PagedResponse<JobOrderSummaryDto>> SearchAsync(string? keyword = null, JobOrderStatus? status = null, int page = 1, int pageSize = 20, Guid? purchaseRequestId = null, Guid? supplierId = null, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default);
    Task<JobOrderDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<byte[]> GetPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, CancellationToken ct = default);
    Task<JobOrderDto> CreateAsync(CreateJobOrderCommand command, CancellationToken ct = default);
    Task<JobOrderDto> UpdateAsync(Guid id, UpdateJobOrderCommand command, CancellationToken ct = default);
    Task<JobOrderDto> SubmitAsync(Guid id, CancellationToken ct = default);
    Task<JobOrderDto> CertifyFundsAvailableAsync(Guid id, CertifyJobOrderFundsAvailableCommand command, CancellationToken ct = default);
    Task<JobOrderDto> IssueAsync(Guid id, CancellationToken ct = default);
    Task<JobOrderDto> InspectAsync(Guid id, InspectJobOrderCommand command, CancellationToken ct = default);
    Task<JobOrderDto> AcceptAsync(Guid id, AcceptJobOrderCommand command, CancellationToken ct = default);
    Task<JobOrderDto> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default);
}

internal sealed class JobOrderClient(HttpClient http) : IJobOrderClient
{
    private const string Base = "api/v1/procurement/job-orders";

    public Task<PagedResponse<JobOrderSummaryDto>> SearchAsync(string? keyword = null, JobOrderStatus? status = null, int page = 1, int pageSize = 20, Guid? purchaseRequestId = null, Guid? supplierId = null, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        if (purchaseRequestId.HasValue) q["PurchaseRequestId"] = purchaseRequestId.Value.ToString();
        if (supplierId.HasValue) q["SupplierId"] = supplierId.Value.ToString();
        if (fromDate.HasValue) q["FromDate"] = fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (toDate.HasValue) q["ToDate"] = toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<JobOrderSummaryDto>>($"{Base}?{q}", ProcurementJson.Options, ct)!;
    }

    public Task<JobOrderDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<JobOrderDto>($"{Base}/{id}", ProcurementJson.Options, ct);

    public Task<byte[]> GetPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(pageWidth)) query["pageWidth"] = pageWidth;
        if (!string.IsNullOrWhiteSpace(orientation)) query["orientation"] = orientation;

        var queryString = query.ToString();
        var url = string.IsNullOrWhiteSpace(queryString)
            ? $"api/v1/quest-pdf-reporting/procurement/job-orders/{id}/pdf"
            : $"api/v1/quest-pdf-reporting/procurement/job-orders/{id}/pdf?{queryString}";

        return http.GetByteArrayAsync(url, ct);
    }

    public async Task<JobOrderDto> CreateAsync(CreateJobOrderCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ProcurementJson.Options, ct);

        if (r.StatusCode == HttpStatusCode.Conflict)
        {
            var detail = await ReadProblemDetailAsync(r, ct);
            throw new DuplicateJobOrderException(detail ?? "A duplicate job order already exists.");
        }

        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JobOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<JobOrderDto> UpdateAsync(Guid id, UpdateJobOrderCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command with { Id = id }, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JobOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<JobOrderDto> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/submit", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JobOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<JobOrderDto> CertifyFundsAvailableAsync(Guid id, CertifyJobOrderFundsAvailableCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/certify-funds-available", command with { Id = id }, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JobOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<JobOrderDto> IssueAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PatchAsync($"{Base}/{id}/issue", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JobOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<JobOrderDto> InspectAsync(Guid id, InspectJobOrderCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/inspect", command with { Id = id }, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JobOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<JobOrderDto> AcceptAsync(Guid id, AcceptJobOrderCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/accept", command with { Id = id }, ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JobOrderDto>(ProcurementJson.Options, ct))!;
    }

    public async Task<JobOrderDto> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        using var r = await http.PatchAsJsonAsync($"{Base}/{id}/cancel", new CancelJoBody(reason), ProcurementJson.Options, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JobOrderDto>(ProcurementJson.Options, ct))!;
    }

    private static async Task<string?> ReadProblemDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsLite>(ProcurementJson.Options, ct);
            return problem?.Detail;
        }
        catch
        {
            return null;
        }
    }

    private sealed record ProblemDetailsLite(string? Title, string? Detail);
    private sealed record CancelJoBody(string? Reason);
}

