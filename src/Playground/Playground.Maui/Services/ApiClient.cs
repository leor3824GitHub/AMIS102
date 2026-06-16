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
            a.ExpiresOn?.ToString("yyyy-MM-dd"), a.FundCluster,
            a.Lines.Select(l => new ICSItemDto(
                l.Id, l.Snapshot.PropertyNo, l.Snapshot.Description,
                l.Snapshot.AssetType, l.Snapshot.Unit, l.Snapshot.UnitCost,
                l.Snapshot.EstimatedUsefulLifeYears,
                l.Snapshot.AcquisitionDate.ToString("yyyy-MM-dd"))).ToList());
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
            a!.Id, a.DocumentNo, a.IssuedOn.ToString("yyyy-MM-dd"), "PPE", a.Status, a.FundCluster,
            a.Lines.Select(l => new PARItemDto(
                l.Id, l.Snapshot.PropertyNo, l.Snapshot.Description,
                l.Snapshot.AssetType, l.Snapshot.Unit, l.Snapshot.UnitCost,
                l.IssuedQty, l.Snapshot.EstimatedUsefulLifeYears,
                l.Snapshot.AcquisitionDate.ToString("yyyy-MM-dd"))).ToList());
    }

    public async Task AcceptAccountabilityAsync(Guid id, CancellationToken ct = default)
    {
        // Body mirrors AcceptAccountabilityCommand(AccountabilityId, AcceptedOn). AcceptedOn is
        // today's date; the server validates the route id matches the body id.
        var body = new { AccountabilityId = id, AcceptedOn = DateOnly.FromDateTime(DateTime.Today) };
        var response = await httpClient.PostAsJsonAsync(
            $"api/v1/asset-register/accountability/mine/{id}/accept", body, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TangibleInventoryItemDetailDto> GetItemByPropertyNoAsync(string propertyNo, CancellationToken ct = default)
    {
        var a = await httpClient.GetFromJsonAsync<ArAssetDetail>(
            $"api/v1/asset-register/assets/by-property-no/{Uri.EscapeDataString(propertyNo)}", ct);
        return new TangibleInventoryItemDetailDto(
            a!.Id, a.PropertyNo, a.Description, a.Description, a.UnitCost, a.AssetType,
            IsIssued: string.Equals(a.LifecycleState, "Assigned", StringComparison.OrdinalIgnoreCase),
            LinkedDocumentType: null, LinkedDocumentNo: null, LinkedDocumentId: a.CurrentAccountabilityId,
            Unit: string.IsNullOrWhiteSpace(a.Unit) ? "unit" : a.Unit,
            CurrentLocationId: a.CurrentLocationId);
    }

    public async Task<List<CatalogItemDto>> SearchCatalogItemsAsync(string keyword, CancellationToken ct = default)
    {
        var url = $"api/v1/asset-register/catalog?isActive=true&pageNumber=1&pageSize=20";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"&keyword={Uri.EscapeDataString(keyword)}";
        var result = await httpClient.GetFromJsonAsync<ArPaged<ArCatalogItem>>(url, ct);
        return result?.Items?.Select(c => new CatalogItemDto(c.Id, c.Code, c.Description, c.DefaultUnit)).ToList() ?? [];
    }

    // Locations master data — still served by the AssetManagement module (reference) during migration.
    public async Task<List<LocationDto>> GetLocationsAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<ArPaged<LocationDto>>(
            "api/v1/asset-management/locations?pageNumber=1&pageSize=200", ct);
        return result?.Items ?? [];
    }

    // ── Physical count — AssetRegister record-as-you-go (AssetManagement deprecated) ──

    public async Task<List<PhysicalCountSessionSummaryDto>> GetPhysicalCountSessionsAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<ArPaged<ArCountSummary>>(
            "api/v1/asset-register/count?pageNumber=1&pageSize=100", ct);
        return result?.Items?.Select(s => new PhysicalCountSessionSummaryDto(
            s.Id, s.Code, s.AsAt, s.FundCluster ?? "", s.Scope, s.Status,
            TotalEntries: s.EntryCount, Found: s.EntryCount, NotFound: 0, FoundAtStation: 0, Pending: 0)).ToList() ?? [];
    }

    public async Task<PhysicalCountSessionDetailDto> GetPhysicalCountSessionByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        var s = await httpClient.GetFromJsonAsync<ArCountDetail>(
            $"api/v1/asset-register/count/{sessionId}", ct);
        return new PhysicalCountSessionDetailDto(
            s!.Id, s.Code, s.AsAt, s.FundCluster, s.Scope, s.Status,
            (s.Entries ?? []).Select(e => new PhysicalCountEntryDto(
                e.Id, e.AssetRegistryId, e.Snapshot?.PropertyNo ?? "", e.SnapshotArticle,
                e.SnapshotUnitCost, MapCondition(e.Condition), e.Condition, 1, e.Remarks,
                IsScanned: e.ScannedOnUtc is not null)).ToList());
    }

    public async Task RecordPhysicalCountEntryAsync(Guid sessionId, RecordCountEntryRequest request, CancellationToken ct = default)
    {
        var body = new
        {
            SessionId = sessionId,
            request.AssetRegistryId,
            request.Article,
            request.Unit,
            request.UnitCost,
            request.Condition,
            request.LocationId,
            ScannedOnUtc = request.IsScanned ? DateTimeOffset.UtcNow : (DateTimeOffset?)null,
            request.Remarks
        };
        var response = await httpClient.PostAsJsonAsync(
            $"api/v1/asset-register/count/{sessionId}/entries", body, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<AddFoundAtStationResult> AddFoundAtStationEntryAsync(Guid sessionId, AddFoundAtStationRequest request, CancellationToken ct = default)
    {
        var body = new
        {
            SessionId = sessionId,
            Article = request.Description,
            request.Unit,
            request.UnitCost,
            request.LocationId,
            ProposedPropertyNo = request.PropertyNumber,
            ProposedCatalogItemId = request.ProposedCatalogItemId,
            request.Remarks
        };
        var response = await httpClient.PostAsJsonAsync(
            $"api/v1/asset-register/count/{sessionId}/found-at-station", body, ct);
        response.EnsureSuccessStatusCode();
        return new AddFoundAtStationResult(Guid.Empty, request.PropertyNumber);
    }

    // AssetRegister stores condition as the count outcome; map to the MAUI "result" label for display.
    private static string MapCondition(string? condition) => condition switch
    {
        "Missing" => "NotFound",
        "FoundAtStation" => "FoundAtStation",
        _ => "Found"
    };

    private sealed record PagedResult<T>(List<T> Data, int TotalCount);

    private sealed record ArCountSummary(
        Guid Id, string Code, string Scope, string Status, DateOnly AsAt,
        DateOnly StartedOn, DateOnly? ClosedOn, int EntryCount, string? FundCluster);

    private sealed record ArCountDetail(
        Guid Id, string Code, string Scope, string Status, string FundCluster,
        DateOnly AsAt, List<ArCountEntry>? Entries);

    private sealed record ArCountEntry(
        Guid Id, Guid? AssetRegistryId, ArAssetSnapshot? Snapshot, string SnapshotArticle,
        string SnapshotUnit, decimal SnapshotUnitCost, string Condition,
        DateTimeOffset? ScannedOnUtc, string? Remarks);

    // ── Local mirrors of AssetRegister JSON shapes (MAUI must not reference Modules.*).
    //    Enums are deserialized as strings to avoid pulling in module contracts. ──
    private sealed record ArPaged<T>(List<T>? Items, int TotalCount);

    private sealed record ArCatalogItem(Guid Id, string Code, string Description, string DefaultUnit);

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
        string AssetType, string LifecycleState, Guid? CurrentAccountabilityId,
        string? Unit, Guid? CurrentLocationId);
}
