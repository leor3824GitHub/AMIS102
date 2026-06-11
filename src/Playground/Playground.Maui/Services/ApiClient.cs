using System.Net.Http.Json;

namespace Playground.Maui.Services;

public sealed class ApiClient(HttpClient httpClient) : IApiClient
{
    public async Task<TokenResponse> IssueTokenAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/v1/identity/token/issue",
            new TokenIssueRequest(email, password),
            ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>(ct))!;
    }

    public async Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<UserProfileDto>("api/v1/identity/profile", ct);
        return result!;
    }

    public async Task<MyEmployeeDto> GetMyEmployeeAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<MyEmployeeDto>("api/v1/master-data/employees/me", ct);
        return result!;
    }

    // ── ICS / PAR / asset lookup — served by AssetRegister (AssetManagement deprecated) ──
    // The employeeId argument is retained for signature compatibility but the AssetRegister
    // "/mine" endpoints are already scoped to the authenticated employee server-side.

    public async Task<List<ICSSummaryDto>> GetMyICSListAsync(Guid employeeId, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<ArPaged<ArAccountabilitySummary>>(
            "api/v1/asset-register/accountability/mine?type=SE_ICS&pageNumber=1&pageSize=200", ct);
        return result?.Items?.Select(a => new ICSSummaryDto(
            a.Id, a.DocumentNo, a.IssuedOn.ToString("yyyy-MM-dd"), a.Status,
            a.ExpiresOn?.ToString("yyyy-MM-dd"), a.LineCount)).ToList() ?? [];
    }

    public async Task<ICSDetailDto> GetICSByIdAsync(Guid id, CancellationToken ct = default)
    {
        var a = await httpClient.GetFromJsonAsync<ArAccountabilityDetail>(
            $"api/v1/asset-register/accountability/mine/{id}", ct);
        return new ICSDetailDto(
            a!.Id, a.DocumentNo, a.IssuedOn.ToString("yyyy-MM-dd"), a.Status,
            a.ExpiresOn?.ToString("yyyy-MM-dd"),
            a.Lines.Select(l => new ICSItemDto(
                l.Id, l.Snapshot.PropertyNo, l.Snapshot.Description,
                l.Snapshot.UnitCost, l.Snapshot.EstimatedUsefulLifeYears)).ToList());
    }

    public async Task<List<PARSummaryDto>> GetMyPARListAsync(Guid employeeId, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<ArPaged<ArAccountabilitySummary>>(
            "api/v1/asset-register/accountability/mine?type=PPE_PAR&pageNumber=1&pageSize=200", ct);
        return result?.Items?.Select(a => new PARSummaryDto(
            a.Id, a.DocumentNo, a.IssuedOn.ToString("yyyy-MM-dd"), "PPE", a.LineCount)).ToList() ?? [];
    }

    public async Task<PARDetailDto> GetPARByIdAsync(Guid id, CancellationToken ct = default)
    {
        var a = await httpClient.GetFromJsonAsync<ArAccountabilityDetail>(
            $"api/v1/asset-register/accountability/mine/{id}", ct);
        return new PARDetailDto(
            a!.Id, a.DocumentNo, a.IssuedOn.ToString("yyyy-MM-dd"), "PPE",
            a.Lines.Select(l => new PARItemDto(
                l.Id, l.Snapshot.PropertyNo, l.Snapshot.Description, l.Snapshot.UnitCost,
                l.IssuedQty, l.Snapshot.EstimatedUsefulLifeYears,
                l.Snapshot.AcquisitionDate.ToString("yyyy-MM-dd"))).ToList());
    }

    public async Task<TangibleInventoryItemDetailDto> GetItemByPropertyNoAsync(string propertyNo, CancellationToken ct = default)
    {
        var a = await httpClient.GetFromJsonAsync<ArAssetDetail>(
            $"api/v1/asset-register/assets/by-property-no/{Uri.EscapeDataString(propertyNo)}", ct);
        return new TangibleInventoryItemDetailDto(
            a!.Id, a.PropertyNo, a.Description, a.Description, a.UnitCost, a.AssetType,
            IsIssued: string.Equals(a.LifecycleState, "Assigned", StringComparison.OrdinalIgnoreCase),
            LinkedDocumentType: null, LinkedDocumentNo: null, LinkedDocumentId: a.CurrentAccountabilityId);
    }

    public async Task<List<PhysicalCountSessionSummaryDto>> GetPhysicalCountSessionsAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<PagedPhysicalCountListResponse>(
            "api/v1/asset-management/physical-count?PageSize=100", ct);
        return result?.Items?.ToList() ?? [];
    }

    public async Task<PhysicalCountSessionDetailDto> GetPhysicalCountSessionByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<PhysicalCountSessionDetailDto>(
            $"api/v1/asset-management/physical-count/{sessionId}", ct);
        return result!;
    }

    public async Task RecordPhysicalCountEntryAsync(Guid sessionId, Guid entryId, RecordCountEntryRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/v1/asset-management/physical-count/{sessionId}/entries/{entryId}", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<AddFoundAtStationResult> AddFoundAtStationEntryAsync(Guid sessionId, AddFoundAtStationRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/v1/asset-management/physical-count/{sessionId}/found-at-station", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AddFoundAtStationResult>(ct))!;
    }

    private sealed record PagedResult<T>(List<T> Data, int TotalCount);
    private sealed record PagedPhysicalCountListResponse(
        IReadOnlyList<PhysicalCountSessionSummaryDto>? Items,
        int PageNumber, int PageSize, int TotalCount);

    // ── Local mirrors of AssetRegister JSON shapes (MAUI must not reference Modules.*).
    //    Enums are deserialized as strings to avoid pulling in module contracts. ──
    private sealed record ArPaged<T>(List<T>? Items, int TotalCount);

    private sealed record ArAccountabilitySummary(
        Guid Id, string DocumentNo, string AccountabilityType, string Status,
        DateOnly IssuedOn, DateOnly? ExpiresOn, int LineCount);

    private sealed record ArAccountabilityDetail(
        Guid Id, string DocumentNo, string AccountabilityType, string FundCluster,
        DateOnly IssuedOn, DateOnly? ExpiresOn, string Status, List<ArAccountabilityLine> Lines);

    private sealed record ArAccountabilityLine(
        Guid Id, Guid AssetRegistryId, ArAssetSnapshot Snapshot, int IssuedQty);

    private sealed record ArAssetSnapshot(
        string PropertyNo, string Description, string AssetType, decimal UnitCost,
        string Unit, int EstimatedUsefulLifeYears, DateOnly AcquisitionDate);

    private sealed record ArAssetDetail(
        Guid Id, string PropertyNo, string Description, decimal UnitCost,
        string AssetType, string LifecycleState, Guid? CurrentAccountabilityId);
}
