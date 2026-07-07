using System.Net.Http.Json;
using System.Text.Json;

namespace AMIS.Maui.Services;

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
        var a = await httpClient.GetFromJsonAsync<ArScanDetail>(
            $"api/v1/asset-register/assets/by-property-no/{Uri.EscapeDataString(propertyNo)}/scan-detail", ct);
        return new TangibleInventoryItemDetailDto(
            a!.Id, a.PropertyNo, a.Description, a.Description, a.UnitCost, a.AssetType,
            IsIssued: string.Equals(a.LifecycleState, "Assigned", StringComparison.OrdinalIgnoreCase),
            LinkedDocumentType: MapAccountabilityType(a.DocumentType),
            LinkedDocumentNo: a.DocumentNo,
            LinkedDocumentId: a.CurrentAccountabilityId,
            Unit: string.IsNullOrWhiteSpace(a.Unit) ? "unit" : a.Unit,
            CurrentLocationId: a.CurrentLocationId,
            SerialNo: a.SerialNo,
            AcquisitionDate: a.AcquisitionDate,
            LocationName: a.LocationName,
            AccountableOfficer: a.AccountableOfficerName,
            AccountableOfficerDesignation: a.AccountableOfficerDesignation,
            ImageUrl: a.ImageUrl);
    }

    // Server returns the accountability enum (SE_ICS / PPE_PAR); map to the COA form name shown on the sticker.
    private static string? MapAccountabilityType(string? accountabilityType) => accountabilityType switch
    {
        "SE_ICS" => "ICS",
        "PPE_PAR" => "PAR",
        _ => null,
    };

    public async Task<IReadOnlyList<AssetSummaryDto>> SearchAssetsBySerialAsync(string serialNo, CancellationToken ct = default)
    {
        var url = $"api/v1/asset-register/assets?serialNo={Uri.EscapeDataString(serialNo)}&pageNumber=1&pageSize=25";
        var result = await httpClient.GetFromJsonAsync<ArPaged<ArAssetSummary>>(url, ct);
        return result?.Items?
            .Select(a => new AssetSummaryDto(a.Id, a.PropertyNo, a.AssetType, a.Description, a.UnitCost))
            .ToList() ?? [];
    }

    public async Task<List<CatalogItemDto>> SearchCatalogItemsAsync(string keyword, CancellationToken ct = default)
    {
        var url = $"api/v1/asset-register/catalog?isActive=true&pageNumber=1&pageSize=20";
        if (!string.IsNullOrWhiteSpace(keyword))
            url += $"&keyword={Uri.EscapeDataString(keyword)}";
        var result = await httpClient.GetFromJsonAsync<ArPaged<ArCatalogItem>>(url, ct);
        return result?.Items?.Select(c => new CatalogItemDto(c.Id, c.Code, c.Description, c.DefaultUnit)).ToList() ?? [];
    }

    // Locations master data — served by the AssetRegister module.
    public async Task<List<LocationDto>> GetLocationsAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<ArPaged<LocationDto>>(
            "api/v1/asset-register/locations?pageNumber=1&pageSize=200", ct);
        return result?.Items ?? [];
    }

    // ── Physical count — AssetRegister record-as-you-go (AssetManagement deprecated) ──

    public async Task<List<PhysicalCountSessionSummaryDto>> GetPhysicalCountSessionsAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<ArPaged<ArCountSummary>>(
            "api/v1/asset-register/count?pageNumber=1&pageSize=100", ct);
        return result?.Items?.Select(s => new PhysicalCountSessionSummaryDto(
            s.Id, s.Code, s.AsAt, s.FundCluster ?? "", s.Scope, s.Status,
            TotalEntries: s.EntryCount, Found: s.FoundCount, Missing: s.MissingCount, FoundAtStation: s.FoundAtStationCount)).ToList() ?? [];
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
                IsScanned: e.ScannedOnUtc is not null,
                // Known assets (found/missing) carry a snapshot tagged SE or PPE; FoundAtStation has none.
                AssetType: e.Snapshot?.AssetType)).ToList());
    }

    public async Task<PhysicalCountChecklistDto> GetPhysicalCountChecklistAsync(Guid sessionId, CancellationToken ct = default)
    {
        // The server DTO matches PhysicalCountChecklistDto field-for-field (enums as strings), so
        // deserialize straight into it.
        var result = await httpClient.GetFromJsonAsync<PhysicalCountChecklistDto>(
            $"api/v1/asset-register/count/{sessionId}/checklist", ct);
        return result ?? new PhysicalCountChecklistDto(sessionId, "", "", "", "", 0, 0, 0, 0, []);
    }

    public async Task<Stream?> GetAssetImageStreamAsync(Guid assetRegistryId, string? variant = null, CancellationToken ct = default)
    {
        // Authenticated per-asset thumbnail fetch (bearer token added by AuthenticatedHttpHandler).
        // 404 means the asset has no photo → return null so the row falls back to its placeholder.
        var query = string.Equals(variant, "thumb", StringComparison.OrdinalIgnoreCase) ? "?variant=thumb" : "";
        var response = await httpClient.GetAsync(
            $"api/v1/asset-register/assets/{assetRegistryId}/image{query}", ct);
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadAsStreamAsync(ct);
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
        await EnsureSuccessOrThrowAsync(response, ct);
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
        await EnsureSuccessOrThrowAsync(response, ct);
        return new AddFoundAtStationResult(Guid.Empty, request.PropertyNumber);
    }

    // Turns a non-success response into an HttpRequestException carrying the StatusCode (so callers can
    // tell a server rejection from a transport failure) and the server's message where available.
    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        string? message = null;
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            message = ExtractProblemDetail(body);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch { /* fall back to the status code */ }

        throw new HttpRequestException(
            string.IsNullOrWhiteSpace(message) ? $"The server rejected the request ({(int)response.StatusCode})." : message,
            null,
            response.StatusCode);
    }

    // Pulls a human-readable message out of an ASP.NET ProblemDetails / validation-problem body.
    private static string? ExtractProblemDetail(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in errors.EnumerateObject())
                    if (prop.Value.ValueKind == JsonValueKind.Array && prop.Value.GetArrayLength() > 0)
                        return prop.Value[0].GetString();
            }
            if (doc.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString();
            if (doc.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                return title.GetString();
            return null;
        }
        catch (JsonException)
        {
            return body.Length > 300 ? body[..300] : body;
        }
    }

    // ── Chat ──

    public async Task<List<ChatChannelDto>> GetChatChannelsAsync(CancellationToken ct = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<ChatChannelDto>>("api/v1/chat/channels", ct);
        return result ?? [];
    }

    public async Task<ChatMessagePageDto> GetChatMessagesAsync(Guid channelId, Guid? before, int? take, CancellationToken ct = default)
    {
        var url = $"api/v1/chat/channels/{channelId}/messages";
        var query = new List<string>();
        if (before is not null) query.Add($"before={before}");
        if (take is not null) query.Add($"take={take}");
        if (query.Count > 0) url += "?" + string.Join("&", query);

        var result = await httpClient.GetFromJsonAsync<ChatMessagePageDto>(url, ct);
        return result ?? new ChatMessagePageDto([], null, false);
    }

    public async Task<ChatMessageDto?> SendChatMessageAsync(Guid channelId, string content, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/v1/chat/messages", new { channelId, content }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatMessageDto>(ct);
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
        DateOnly StartedOn, DateOnly? ClosedOn, int EntryCount, string? FundCluster,
        int FoundCount, int MissingCount, int FoundAtStationCount);

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

    // Subset of AssetRegister AssetRegistrySummaryDto — only the fields the serial picker needs
    // (extra JSON properties are ignored on deserialization).
    private sealed record ArAssetSummary(
        Guid Id, string PropertyNo, string AssetType, string Description, decimal UnitCost);

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

    // Mirrors AssetRegister AssetScanDetailDto. Enums (AssetType, DocumentType, LifecycleState,
    // CurrentCondition) arrive as strings via the global JsonStringEnumConverter.
    private sealed record ArScanDetail(
        Guid Id, string PropertyNo, string AssetType, string Description,
        string? SerialNo, string? Brand, string? Model, string Unit,
        DateOnly AcquisitionDate, decimal UnitCost, string LifecycleState, string CurrentCondition,
        Guid? CurrentLocationId, string? LocationName,
        Guid? CurrentAccountabilityId, string? DocumentType, string? DocumentNo,
        Guid? AccountableOfficerId, string? AccountableOfficerName, string? AccountableOfficerDesignation,
        string? ImageUrl = null);
}
