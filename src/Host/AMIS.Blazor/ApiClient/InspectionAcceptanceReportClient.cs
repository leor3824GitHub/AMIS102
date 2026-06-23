using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;

namespace AMIS.Blazor.ApiClient;

// ── Asset IARs ────────────────────────────────────────────────────────────────

internal interface IInspectionAcceptanceReportClient
{
    Task<PagedResponse<InspectionAcceptanceReportSummaryDto>> SearchAsync(
        string? keyword = null, InspectionAcceptanceReportStatus? status = null,
        int page = 1, int pageSize = 20, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default);
    Task<IReadOnlyList<IARStatusCountDto>> GetStatusCountsAsync(DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default);
    Task<InspectionAcceptanceReportDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<byte[]> GetFastReportPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, int? minRows = null, CancellationToken ct = default);
    Task<InspectionAcceptanceReportDto> CreateAsync(CreateInspectionAcceptanceReportCommand command, CancellationToken ct = default);
    Task<InspectionAcceptanceReportDto> UpdateAsync(Guid id, UpdateInspectionAcceptanceReportCommand command, CancellationToken ct = default);
    Task<InspectionAcceptanceReportDto> SubmitForInspectionAsync(Guid id, CancellationToken ct = default);
    Task<InspectionAcceptanceReportDto> ReassignInspectorAsync(Guid id, Guid newInspectorId, CancellationToken ct = default);
    Task<InspectionAcceptanceReportDto> RecordInspectionAsync(Guid id, IReadOnlyList<LineInspectionDecision> decisions, CancellationToken ct = default);
    Task<InspectionAcceptanceReportDto> AssignPropertyNoAsync(Guid id, int itemNo, string propertyNo, CancellationToken ct = default);
    Task<InspectionAcceptanceReportDto> ExpandLineByQuantityAsync(Guid id, int itemNo, CancellationToken ct = default);
    Task<InspectionAcceptanceReportDto> CancelAsync(Guid id, CancellationToken ct = default);
    Task<InspectionAcceptanceReportDto> AcceptAsync(Guid id, CancellationToken ct = default);
}

internal sealed class InspectionAcceptanceReportClient(HttpClient http) : IInspectionAcceptanceReportClient
{
    private const string Base = "api/v1/procurement/inspection-acceptance-reports";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<PagedResponse<InspectionAcceptanceReportSummaryDto>> SearchAsync(
        string? keyword = null, InspectionAcceptanceReportStatus? status = null,
        int page = 1, int pageSize = 20, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        if (fromDate.HasValue) q["FromDate"] = fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (toDate.HasValue) q["ToDate"] = toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<InspectionAcceptanceReportSummaryDto>>($"{Base}?{q}", JsonOptions, ct)!;
    }

    public async Task<IReadOnlyList<IARStatusCountDto>> GetStatusCountsAsync(DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (fromDate.HasValue) q["FromDate"] = fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (toDate.HasValue) q["ToDate"] = toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var qs = q.ToString();
        var url = string.IsNullOrEmpty(qs) ? $"{Base}/status-counts" : $"{Base}/status-counts?{qs}";
        var result = await http.GetFromJsonAsync<List<IARStatusCountDto>>(url, JsonOptions, ct).ConfigureAwait(false);
        return result ?? [];
    }

    public Task<InspectionAcceptanceReportDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<InspectionAcceptanceReportDto>($"{Base}/{id}", JsonOptions, ct);

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
            ? $"api/v1/fast-reporting/procurement/inspection-acceptance-reports/{id}/print"
            : $"api/v1/fast-reporting/procurement/inspection-acceptance-reports/{id}/print?{queryString}";

        return http.GetByteArrayAsync(url, ct);
    }

    public async Task<InspectionAcceptanceReportDto> CreateAsync(CreateInspectionAcceptanceReportCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<InspectionAcceptanceReportDto>(JsonOptions, ct))!;
    }

    public async Task<InspectionAcceptanceReportDto> UpdateAsync(Guid id, UpdateInspectionAcceptanceReportCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<InspectionAcceptanceReportDto>(JsonOptions, ct))!;
    }

    public async Task<InspectionAcceptanceReportDto> SubmitForInspectionAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync(new Uri($"{Base}/{id}/submit-for-inspection", UriKind.Relative), null, ct);
        if (!r.IsSuccessStatusCode)
        {
            var detail = await TryReadProblemDetailAsync(r, ct);
            throw new HttpRequestException(
                string.IsNullOrWhiteSpace(detail)
                    ? $"Submit failed with status {(int)r.StatusCode} ({r.StatusCode})."
                    : detail,
                null,
                r.StatusCode);
        }

        return (await r.Content.ReadFromJsonAsync<InspectionAcceptanceReportDto>(JsonOptions, ct))!;
    }

    private static async Task<string?> TryReadProblemDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(contentStream, cancellationToken: ct);
            if (json.RootElement.TryGetProperty("detail", out var detailElement))
            {
                return detailElement.GetString();
            }
        }
        catch
        {
            // Fall back to status code message.
        }

        return null;
    }

    public async Task<InspectionAcceptanceReportDto> ReassignInspectorAsync(Guid id, Guid newInspectorId, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/reassign-inspector",
            new ReassignInspectorCommand(id, newInspectorId),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<InspectionAcceptanceReportDto>(JsonOptions, ct))!;
    }

    public async Task<InspectionAcceptanceReportDto> RecordInspectionAsync(Guid id, IReadOnlyList<LineInspectionDecision> decisions, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/record-inspection",
            new RecordIARInspectionCommand(id, decisions),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<InspectionAcceptanceReportDto>(JsonOptions, ct))!;
    }

    public async Task<InspectionAcceptanceReportDto> AssignPropertyNoAsync(Guid id, int itemNo, string propertyNo, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/lines/{itemNo}/property-no",
            new AssignPropertyNoCommand(id, itemNo, propertyNo),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<InspectionAcceptanceReportDto>(JsonOptions, ct))!;
    }

    public async Task<InspectionAcceptanceReportDto> ExpandLineByQuantityAsync(Guid id, int itemNo, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/lines/{itemNo}/expand",
            new ExpandLineByQuantityCommand(id, itemNo),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<InspectionAcceptanceReportDto>(JsonOptions, ct))!;
    }

    public async Task<InspectionAcceptanceReportDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/cancel",
            new CancelInspectionAcceptanceReportCommand(id),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<InspectionAcceptanceReportDto>(JsonOptions, ct))!;
    }

    public async Task<InspectionAcceptanceReportDto> AcceptAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync(new Uri($"{Base}/{id}/accept", UriKind.Relative), null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<InspectionAcceptanceReportDto>(JsonOptions, ct))!;
    }
}
