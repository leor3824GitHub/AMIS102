using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;

namespace AMIS.Playground.Blazor.ApiClient;

// ── Asset IARs ────────────────────────────────────────────────────────────────

internal interface IAssetIarClient
{
    Task<PagedResponse<AssetIARSummaryDto>> SearchAsync(
        string? keyword = null, AssetIARStatus? status = null,
        int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<AssetIARDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<byte[]> GetFastReportPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, int? minRows = null, CancellationToken ct = default);
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
    private const string Base = "api/v1/procurement/iars";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<PagedResponse<AssetIARSummaryDto>> SearchAsync(
        string? keyword = null, AssetIARStatus? status = null,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedResponse<AssetIARSummaryDto>>($"{Base}?{q}", JsonOptions, ct)!;
    }

    public Task<AssetIARDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<AssetIARDto>($"{Base}/{id}", JsonOptions, ct);

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
            ? $"api/v1/fast-reporting/procurement/iars/{id}/print"
            : $"api/v1/fast-reporting/procurement/iars/{id}/print?{queryString}";

        return http.GetByteArrayAsync(url, ct);
    }

    public async Task<AssetIARDto> CreateAsync(CreateAssetIARCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(JsonOptions, ct))!;
    }

    public async Task<AssetIARDto> UpdateAsync(Guid id, UpdateAssetIARCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(JsonOptions, ct))!;
    }

    public async Task<AssetIARDto> SubmitForInspectionAsync(Guid id, CancellationToken ct = default)
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

        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(JsonOptions, ct))!;
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

    public async Task<AssetIARDto> ReassignInspectorAsync(Guid id, Guid newInspectorId, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/reassign-inspector",
            new ReassignInspectorCommand(id, newInspectorId),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(JsonOptions, ct))!;
    }

    public async Task<AssetIARDto> RecordInspectionAsync(Guid id, IReadOnlyList<LineInspectionDecision> decisions, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/record-inspection",
            new RecordIARInspectionCommand(id, decisions),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(JsonOptions, ct))!;
    }

    public async Task<AssetIARDto> AssignPropertyNoAsync(Guid id, int itemNo, string propertyNo, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/lines/{itemNo}/property-no",
            new AssignPropertyNoCommand(id, itemNo, propertyNo),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(JsonOptions, ct))!;
    }

    public async Task<AssetIARDto> ExpandLineByQuantityAsync(Guid id, int itemNo, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/lines/{itemNo}/expand",
            new ExpandLineByQuantityCommand(id, itemNo),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(JsonOptions, ct))!;
    }

    public async Task<AssetIARDto> CancelAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(
            $"{Base}/{id}/cancel",
            new CancelAssetIARCommand(id),
            ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(JsonOptions, ct))!;
    }

    public async Task<AssetIARDto> AcceptAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync(new Uri($"{Base}/{id}/accept", UriKind.Relative), null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AssetIARDto>(JsonOptions, ct))!;
    }
}
