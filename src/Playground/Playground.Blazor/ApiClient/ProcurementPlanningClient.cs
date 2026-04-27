using System.Net.Http.Json;
using System.Web;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.ProcurementPlanning.Contracts.v1.AnnualProcurementPlans;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

namespace FSH.Playground.Blazor.ApiClient;

// ── PPMP ─────────────────────────────────────────────────────────────────────

internal interface IPpmpClient
{
    Task<PagedResponse<PpmpSummaryDto>> SearchAsync(string? keyword = null, int? fiscalYear = null,
        PpmpStatus? status = null, PpmpType? ppmpType = null, bool currentOnly = true,
        int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PpmpDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PpmpSummaryDto>> GetVersionsAsync(Guid chainId, CancellationToken ct = default);
    Task<PpmpDto> CreateAsync(CreatePpmpCommand command, CancellationToken ct = default);
    Task<PpmpDto> UpdateAsync(Guid id, UpdatePpmpCommand command, CancellationToken ct = default);
    Task<PpmpDto> SubmitAsync(Guid id, CancellationToken ct = default);
    Task<PpmpDto> ApproveAsync(Guid id, Guid approvedById, CancellationToken ct = default);
    Task<PpmpDto> RecallAsync(Guid id, CancellationToken ct = default);
    Task<PpmpDto> ReturnAsync(Guid id, string returnReason, Guid returnedById, CancellationToken ct = default);
    Task<PpmpDto> AmendAsync(Guid id, string reason, CancellationToken ct = default);
}

internal sealed class PpmpClient(HttpClient http) : IPpmpClient
{
    private const string Base = "api/v1/procurement-planning/ppmps";

    public Task<PagedResponse<PpmpSummaryDto>> SearchAsync(string? keyword = null, int? fiscalYear = null,
        PpmpStatus? status = null, PpmpType? ppmpType = null, bool currentOnly = true,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (fiscalYear.HasValue) q["FiscalYear"] = fiscalYear.Value.ToString();
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString();
        if (ppmpType.HasValue) q["PpmpType"] = ((int)ppmpType.Value).ToString();
        q["CurrentVersionOnly"] = currentOnly.ToString().ToLowerInvariant();
        q["PageNumber"] = page.ToString();
        q["PageSize"] = pageSize.ToString();
        return http.GetFromJsonAsync<PagedResponse<PpmpSummaryDto>>($"{Base}?{q}", ct)!;
    }

    public Task<PpmpDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PpmpDto>($"{Base}/{id}", ct);

    public Task<IReadOnlyList<PpmpSummaryDto>> GetVersionsAsync(Guid chainId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<IReadOnlyList<PpmpSummaryDto>>($"{Base}/versions/{chainId}", ct)!;

    public async Task<PpmpDto> CreateAsync(CreatePpmpCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(ct))!;
    }

    public async Task<PpmpDto> UpdateAsync(Guid id, UpdatePpmpCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(ct))!;
    }

    public async Task<PpmpDto> SubmitAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/submit", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(ct))!;
    }

    public async Task<PpmpDto> ApproveAsync(Guid id, Guid approvedById, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/approve",
            new ApprovePpmpCommand(id, approvedById), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(ct))!;
    }

    public async Task<PpmpDto> RecallAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/recall", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(ct))!;
    }

    public async Task<PpmpDto> ReturnAsync(Guid id, string returnReason, Guid returnedById, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/return",
            new ReturnPpmpCommand(id, returnReason, returnedById), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(ct))!;
    }

    public async Task<PpmpDto> AmendAsync(Guid id, string reason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/amend",
            new AmendPpmpCommand(id, reason), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PpmpDto>(ct))!;
    }
}

// ── APP ───────────────────────────────────────────────────────────────────────

internal interface IAppClient
{
    Task<PagedResponse<AnnualProcurementPlanSummaryDto>> SearchAsync(string? keyword = null,
        int? fiscalYear = null, AppStatus? status = null, bool currentOnly = true,
        int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PpmpSummaryDto>> GetAvailablePpmpsAsync(int fiscalYear, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> CreateAsync(CreateAnnualProcurementPlanCommand command, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> ConsolidateAsync(Guid id, IReadOnlyList<Guid> ppmpIds, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> PublishAsync(Guid id, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> ApproveAsync(Guid id, Guid approvedById, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> RecallAsync(Guid id, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> ReturnAsync(Guid id, string returnReason, Guid returnedById, CancellationToken ct = default);
    Task<AnnualProcurementPlanDto> AmendAsync(Guid id, string reason, AppRevisionType revisionType, CancellationToken ct = default);
}

internal sealed class AppClient(HttpClient http) : IAppClient
{
    private const string Base = "api/v1/procurement-planning/apps";

    public Task<PagedResponse<AnnualProcurementPlanSummaryDto>> SearchAsync(string? keyword = null,
        int? fiscalYear = null, AppStatus? status = null, bool currentOnly = true,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (fiscalYear.HasValue) q["FiscalYear"] = fiscalYear.Value.ToString();
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString();
        q["CurrentVersionOnly"] = currentOnly.ToString().ToLowerInvariant();
        q["PageNumber"] = page.ToString();
        q["PageSize"] = pageSize.ToString();
        return http.GetFromJsonAsync<PagedResponse<AnnualProcurementPlanSummaryDto>>($"{Base}?{q}", ct)!;
    }

    public Task<AnnualProcurementPlanDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<AnnualProcurementPlanDto>($"{Base}/{id}", ct);

    public Task<IReadOnlyList<PpmpSummaryDto>> GetAvailablePpmpsAsync(int fiscalYear, CancellationToken ct = default) =>
        http.GetFromJsonAsync<IReadOnlyList<PpmpSummaryDto>>($"{Base}/available-ppmps?FiscalYear={fiscalYear}", ct)!;

    public async Task<AnnualProcurementPlanDto> CreateAsync(CreateAnnualProcurementPlanCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(ct))!;
    }

    public async Task<AnnualProcurementPlanDto> ConsolidateAsync(Guid id, IReadOnlyList<Guid> ppmpIds, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/consolidate",
            new ConsolidatePpmpsCommand(id, ppmpIds), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(ct))!;
    }

    public async Task<AnnualProcurementPlanDto> PublishAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/publish", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(ct))!;
    }

    public async Task<AnnualProcurementPlanDto> ApproveAsync(Guid id, Guid approvedById, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/approve",
            new ApproveAppCommand(id, approvedById), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(ct))!;
    }

    public async Task<AnnualProcurementPlanDto> RecallAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{id}/recall", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(ct))!;
    }

    public async Task<AnnualProcurementPlanDto> ReturnAsync(Guid id, string returnReason, Guid returnedById, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/return",
            new ReturnAppCommand(id, returnReason, returnedById), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(ct))!;
    }

    public async Task<AnnualProcurementPlanDto> AmendAsync(Guid id, string reason,
        AppRevisionType revisionType, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{id}/amend",
            new AmendAnnualProcurementPlanCommand(id, reason, revisionType), ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AnnualProcurementPlanDto>(ct))!;
    }
}
