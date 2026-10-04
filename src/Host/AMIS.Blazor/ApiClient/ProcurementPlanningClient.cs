using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

namespace AMIS.Blazor.ApiClient;

// The API serializes all enums as strings (global JsonStringEnumConverter in AMIS.Api/Program.cs).
file static class Json
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
}

file static class HttpExtensions
{
    internal static async Task EnsureApiSuccessAsync(this HttpResponseMessage response, CancellationToken ct = default)
    {
        if (response.IsSuccessStatusCode) return;

        string? detail = null;
        List<string>? validationMessages = null;
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(body);

            if (doc.RootElement.TryGetProperty("detail", out var d))
                detail = d.GetString();

            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                validationMessages = [];
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    var messages = property.Value
                        .EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToArray();

                    if (messages.Length > 0)
                        validationMessages.Add($"{property.Name}: {string.Join(", ", messages!)}");
                }
            }
        }
        catch { /* ignore */ }

        var message = validationMessages is { Count: > 0 }
            ? string.Join(" | ", validationMessages)
            : detail;

        throw new HttpRequestException(
            message ?? $"Request failed with status {(int)response.StatusCode}.",
            null,
            response.StatusCode);
    }
}

// ── PPMP ─────────────────────────────────────────────────────────────────────

internal interface IPpmpClient
{
    Task<PagedResponse<PpmpSummaryDto>> SearchAsync(string? keyword = null, int? fiscalYear = null,
        PpmpStatus? status = null, PpmpPhase? phase = null, bool currentOnly = true,
        int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PpmpDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PpmpSummaryDto>> GetVersionsAsync(Guid chainId, CancellationToken ct = default);
    Task<PpmpDto> CreateAsync(CreatePpmpCommand command, CancellationToken ct = default);
    Task<PpmpDto> UpdateAsync(Guid id, UpdatePpmpCommand command, CancellationToken ct = default);
    Task<PpmpDto> SubmitAsync(Guid id, CancellationToken ct = default);
    Task<PpmpDto> ApproveAsync(Guid id, CancellationToken ct = default);
    Task<PpmpDto> RecallAsync(Guid id, CancellationToken ct = default);
    Task<PpmpDto> ReturnAsync(Guid id, string returnReason, CancellationToken ct = default);
    Task<PpmpDto> PromoteToFinalAsync(Guid id, CancellationToken ct = default);
    Task<PpmpDto> CreateUpdateAsync(Guid id, string reason, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<byte[]> GetPdfAsync(Guid id, string? pageWidth = null, CancellationToken ct = default);
    Task<byte[]> GetXlsxAsync(Guid id, CancellationToken ct = default);
}

internal sealed class PpmpClient(HttpClient http) : IPpmpClient
{
    private const string Base = "api/v1/procurement-planning/ppmps";

    // GPPB PPMP form — PDF rendered by QuestPdfReporting (landscape; Long Bond 8.5×13 by default).
    public Task<byte[]> GetPdfAsync(Guid id, string? pageWidth = null, CancellationToken ct = default)
    {
        var url = $"api/v1/quest-pdf-reporting/procurement-planning/ppmps/{id}/pdf";
        if (!string.IsNullOrWhiteSpace(pageWidth)) url += $"?pageWidth={Uri.EscapeDataString(pageWidth)}";
        return http.GetByteArrayAsync(url, ct);
    }

    // Same GPPB PPMP form as an editable workbook (landscape, 8.5×13 Folio page setup).
    public Task<byte[]> GetXlsxAsync(Guid id, CancellationToken ct = default) =>
        http.GetByteArrayAsync($"{Base}/{id}/xlsx", ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.DeleteAsync($"{Base}/{id}", ct);
        await r.EnsureApiSuccessAsync(ct);
    }

    public Task<PagedResponse<PpmpSummaryDto>> SearchAsync(string? keyword = null, int? fiscalYear = null,
        PpmpStatus? status = null, PpmpPhase? phase = null, bool currentOnly = true,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (fiscalYear.HasValue) q["FiscalYear"] = fiscalYear.Value.ToString();
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString();
        if (phase.HasValue) q["Phase"] = ((int)phase.Value).ToString();
        q["CurrentVersionOnly"] = currentOnly.ToString().ToLowerInvariant();
        q["PageNumber"] = page.ToString();
        q["PageSize"] = pageSize.ToString();
        return http.GetFromJsonAsync<PagedResponse<PpmpSummaryDto>>($"{Base}?{q}", Json.Options, ct)!;
    }

    public Task<PpmpDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PpmpDto>($"{Base}/{id}", Json.Options, ct);

    public Task<IReadOnlyList<PpmpSummaryDto>> GetVersionsAsync(Guid chainId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<IReadOnlyList<PpmpSummaryDto>>($"{Base}/versions/{chainId}", Json.Options, ct)!;

    public async Task<PpmpDto> CreateAsync(CreatePpmpCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(Json.Options, ct))!;
    }

    public async Task<PpmpDto> UpdateAsync(Guid id, UpdatePpmpCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(Json.Options, ct))!;
    }

    public async Task<PpmpDto> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/submit", null, ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(Json.Options, ct))!;
    }

    public async Task<PpmpDto> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/approve",
            new ApprovePpmpCommand(id), ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(Json.Options, ct))!;
    }

    public async Task<PpmpDto> RecallAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/recall", null, ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(Json.Options, ct))!;
    }

    public async Task<PpmpDto> ReturnAsync(Guid id, string returnReason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/return",
            new ReturnPpmpCommand(id, returnReason), ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(Json.Options, ct))!;
    }

    public async Task<PpmpDto> PromoteToFinalAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/promote-to-final", null, ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(Json.Options, ct))!;
    }

    public async Task<PpmpDto> CreateUpdateAsync(Guid id, string reason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/create-update",
            new CreateUpdatePpmpCommand(id, reason), ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(Json.Options, ct))!;
    }
}

// ── APP ───────────────────────────────────────────────────────────────────────

internal interface IAppClient
{
    Task<PagedResponse<AnnualProcurementPlanSummaryDto>> SearchAsync(string? keyword = null,
        int? fiscalYear = null, AppStatus? status = null, AppPhase? phase = null, bool currentOnly = true,
        int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AnnualProcurementPlanSummaryDto>> GetVersionsAsync(Guid chainId, CancellationToken ct = default);
    Task<IReadOnlyList<PpmpSummaryDto>> GetAvailablePpmpsAsync(int fiscalYear, Guid? appId = null, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> CreateAsync(CreateAnnualProcurementPlanCommand command, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> ConsolidateAsync(Guid id, IReadOnlyList<Guid> ppmpIds, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> SubmitAsync(Guid id, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> RemovePpmpFromAppAsync(Guid id, Guid ppmpId, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> ReturnPpmpFromAppAsync(Guid id, Guid ppmpId, string returnReason, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> ApproveAsync(Guid id, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> RecallAsync(Guid id, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> ReturnAsync(Guid id, string returnReason, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> PromoteToFinalAsync(Guid id, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> CreateUpdateAsync(Guid id, string reason, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<byte[]> GetPdfAsync(Guid id, string? pageWidth = null, CancellationToken ct = default);
    Task<byte[]> GetXlsxAsync(Guid id, CancellationToken ct = default);
}

internal sealed class AppClient(HttpClient http) : IAppClient
{
    private const string Base = "api/v1/procurement-planning/apps";

    // GPPB APP form — rendered by QuestPdfReporting (landscape; Long Bond 8.5×13 by default).
    public Task<byte[]> GetPdfAsync(Guid id, string? pageWidth = null, CancellationToken ct = default)
    {
        var url = $"api/v1/quest-pdf-reporting/procurement-planning/apps/{id}/pdf";
        if (!string.IsNullOrWhiteSpace(pageWidth)) url += $"?pageWidth={Uri.EscapeDataString(pageWidth)}";
        return http.GetByteArrayAsync(url, ct);
    }

    // Same GPPB APP form as an editable workbook (landscape, 8.5×13 Folio page setup).
    public Task<byte[]> GetXlsxAsync(Guid id, CancellationToken ct = default) =>
        http.GetByteArrayAsync($"{Base}/{id}/xlsx", ct);

    public Task<PagedResponse<AnnualProcurementPlanSummaryDto>> SearchAsync(string? keyword = null,
        int? fiscalYear = null, AppStatus? status = null, AppPhase? phase = null, bool currentOnly = true,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (fiscalYear.HasValue) q["FiscalYear"] = fiscalYear.Value.ToString();
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString();
        if (phase.HasValue) q["Phase"] = ((int)phase.Value).ToString();
        q["CurrentVersionOnly"] = currentOnly.ToString().ToLowerInvariant();
        q["PageNumber"] = page.ToString();
        q["PageSize"] = pageSize.ToString();
        return http.GetFromJsonAsync<PagedResponse<AnnualProcurementPlanSummaryDto>>($"{Base}?{q}", Json.Options, ct)!;
    }

    public Task<AnnualProcurementPlanDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<AnnualProcurementPlanDto>($"{Base}/{id}", Json.Options, ct);

    public Task<IReadOnlyList<AnnualProcurementPlanSummaryDto>> GetVersionsAsync(Guid chainId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<IReadOnlyList<AnnualProcurementPlanSummaryDto>>($"{Base}/versions/{chainId}", Json.Options, ct)!;

    public Task<IReadOnlyList<PpmpSummaryDto>> GetAvailablePpmpsAsync(int fiscalYear, Guid? appId = null, CancellationToken ct = default)
    {
        var url = $"{Base}/available-ppmps?FiscalYear={fiscalYear}";
        if (appId.HasValue) url += $"&AppId={appId}";
        return http.GetFromJsonAsync<IReadOnlyList<PpmpSummaryDto>>(url, Json.Options, ct)!;
    }

    public async Task<AnnualProcurementPlanDto> CreateAsync(CreateAnnualProcurementPlanCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(Json.Options, ct))!;
    }

    public async Task<AnnualProcurementPlanDto> ConsolidateAsync(Guid id, IReadOnlyList<Guid> ppmpIds, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/consolidate",
            new ConsolidatePpmpsCommand(id, ppmpIds), ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(Json.Options, ct))!;
    }

    public async Task<AnnualProcurementPlanDto> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/submit", null, ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(Json.Options, ct))!;
    }

    public async Task<AnnualProcurementPlanDto> RemovePpmpFromAppAsync(Guid id, Guid ppmpId, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/ppmps/{ppmpId}/remove", null, ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(Json.Options, ct))!;
    }

    public async Task<AnnualProcurementPlanDto> ReturnPpmpFromAppAsync(Guid id, Guid ppmpId, string returnReason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/ppmps/{ppmpId}/return",
            new ReturnPpmpFromAppCommand(id, ppmpId, returnReason), ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(Json.Options, ct))!;
    }

    public async Task<AnnualProcurementPlanDto> ApproveAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/approve",
            new ApproveAppCommand(id), ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(Json.Options, ct))!;
    }

    public async Task<AnnualProcurementPlanDto> RecallAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/recall", null, ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(Json.Options, ct))!;
    }

    public async Task<AnnualProcurementPlanDto> ReturnAsync(Guid id, string returnReason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/return",
            new ReturnAppCommand(id, returnReason), ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(Json.Options, ct))!;
    }

    public async Task<AnnualProcurementPlanDto> PromoteToFinalAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/promote-to-final", null, ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(Json.Options, ct))!;
    }

    public async Task<AnnualProcurementPlanDto> CreateUpdateAsync(Guid id, string reason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/create-update",
            new CreateUpdateAppCommand(id, reason), ct);
        await r.EnsureApiSuccessAsync(ct);
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(Json.Options, ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.DeleteAsync($"{Base}/{id}", ct);
        await r.EnsureApiSuccessAsync(ct);
    }
}

