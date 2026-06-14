using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
// AssetRegister contract enums. Cannot use `using namespace` alone because
// AssetManagementClient.cs declares enums of the same name (AssetType,
// AssetCategory, DisposalMethod, PropertyIncidentType, ReceiptType) in the
// same ApiClient namespace; same-namespace types shadow `using` aliases per
// C# name-resolution rules. Each conflicting type is fully-qualified at the
// point of use below. Non-conflicting types come in via the namespace import.
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Contracts.v1.Depreciation;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using ArContracts = AMIS.Modules.AssetRegister.Contracts.v1;

namespace AMIS.Playground.Blazor.ApiClient;

// Shared JSON options that mirror the API's ConfigureHttpJsonOptions:
// enums are serialized/deserialized as strings ("PPERR", not 0).
file static class ArJsonOptions
{
    internal static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

// Surfaces the server's ProblemDetails message (e.g. a 409 from the close-handler true-up)
// instead of the opaque "Response status code does not indicate success" of EnsureSuccessStatusCode.
file static class ArErrorReader
{
    internal static async Task<string> ExtractAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        try
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                        return detail.GetString()!;
                    if (doc.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                        return title.GetString()!;
                    if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                        return msg.GetString()!;
                }
            }
        }
        catch (JsonException) { /* fall through to status text */ }
        catch (OperationCanceledException) { throw; }
        return $"Request failed ({(int)resp.StatusCode} {resp.ReasonPhrase}).";
    }
}

// ── Shared DTOs ────────────────────────────────────────────────────────────

public sealed record ArPagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ArEmployeeRefDto(Guid EmployeeId, string PrintedName, string? Designation);

internal sealed record ArAssetSnapshotDto(
    string PropertyNo,
    string Description,
    ArContracts.AssetType AssetType,
    decimal UnitCost,
    string Unit,
    int EstimatedUsefulLifeYears,
    DateOnly AcquisitionDate,
    string? UacsObjectCode,
    string? SerialNo,
    string? Brand,
    string? Model);

// ── Asset Registry ─────────────────────────────────────────────────────────

internal sealed record AssetRegistrySummaryDto(
    Guid Id,
    string PropertyNo,
    ArContracts.AssetType AssetType,
    string Description,
    decimal UnitCost,
    DateOnly AcquisitionDate,
    LifecycleState LifecycleState,
    ArContracts.AssetCondition CurrentCondition,
    Guid? CurrentCustodianId);

internal sealed record AssetRegistryDto(
    Guid Id,
    string PropertyNo,
    Guid ItemId,
    ArContracts.AssetType AssetType,
    ArContracts.AssetCategory Category,
    string PropertyClass,
    string CategoryCode,
    string Description,
    string? SerialNo,
    string? Brand,
    string? Model,
    string Unit,
    string FundCluster,
    string UacsObjectCode,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    int EstimatedUsefulLifeYears,
    decimal AccumulatedDepreciation,
    decimal AccumulatedImpairmentLosses,
    decimal CarryingAmount,
    LifecycleState LifecycleState,
    ArContracts.AssetCondition CurrentCondition,
    Guid? CurrentCustodianId,
    Guid? CurrentLocationId,
    Guid? CurrentAccountabilityId,
    Guid? SourceIARId,
    Guid? SourcePurchaseOrderId,
    decimal ResidualValue = 0m,
    DepreciationMethod DepreciationMethod = DepreciationMethod.StraightLine);

internal sealed record RegisterAssetRequest(
    Guid CatalogItemId,
    ArContracts.AssetType AssetType,
    ArContracts.AssetCategory Category,
    string Description,
    string FundCluster,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    string LocationCode,
    string SubMajorAccount,
    string GeneralLedgerAccount,
    string? SerialNo = null,
    string? Brand = null,
    string? Model = null,
    Guid? SourceIARId = null,
    Guid? SourcePurchaseOrderId = null);

internal sealed record UpdateAssetConditionRequest(ArContracts.AssetCondition Condition);

internal interface IAssetRegistryClient
{
    Task<ArPagedResponse<AssetRegistrySummaryDto>> SearchAsync(string? keyword = null, ArContracts.AssetType? assetType = null, LifecycleState? lifecycleState = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<AssetRegistryDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AssetRegistryDto?> GetByPropertyNoAsync(string propertyNo, CancellationToken ct = default);
    Task<AssetRegistryDto> RegisterAsync(RegisterAssetRequest request, CancellationToken ct = default);
    Task<AssetRegistryDto> UpdateConditionAsync(Guid id, ArContracts.AssetCondition condition, CancellationToken ct = default);
    Task<AssetRegistryDto> UpdateDepreciationAsync(Guid id, decimal residualValue, int estimatedUsefulLifeYears, DepreciationMethod method, CancellationToken ct = default);
}

internal sealed class AssetRegistryClient(HttpClient http) : IAssetRegistryClient
{
    private const string Base = "api/v1/asset-register/assets";

    public async Task<ArPagedResponse<AssetRegistrySummaryDto>> SearchAsync(string? keyword = null, ArContracts.AssetType? assetType = null, LifecycleState? lifecycleState = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"] = keyword,
            ["assetType"] = assetType?.ToString(),
            ["lifecycleState"] = lifecycleState?.ToString(),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<AssetRegistrySummaryDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<AssetRegistrySummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<AssetRegistryDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<AssetRegistryDto>($"{Base}/{id}", ArJsonOptions.Default, ct);

    public async Task<AssetRegistryDto?> GetByPropertyNoAsync(string propertyNo, CancellationToken ct = default)
    {
        // The endpoint returns 404 when the property number isn't in the registry — honor the
        // nullable contract (a found-at-station candidate) instead of throwing.
        var resp = await http.GetAsync($"{Base}/by-property-no/{Uri.EscapeDataString(propertyNo)}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<AssetRegistryDto>(ArJsonOptions.Default, ct);
    }

    public async Task<AssetRegistryDto> RegisterAsync(RegisterAssetRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AssetRegistryDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<AssetRegistryDto> UpdateConditionAsync(Guid id, ArContracts.AssetCondition condition, CancellationToken ct = default)
    {
        var resp = await http.PutAsJsonAsync($"{Base}/{id}/condition", new UpdateAssetConditionRequest(condition), ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AssetRegistryDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<AssetRegistryDto> UpdateDepreciationAsync(Guid id, decimal residualValue, int estimatedUsefulLifeYears, DepreciationMethod method, CancellationToken ct = default)
    {
        var body = new { AssetRegistryId = id, ResidualValue = residualValue, EstimatedUsefulLifeYears = estimatedUsefulLifeYears, DepreciationMethod = method };
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/depreciation", body, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AssetRegistryDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }
}

// ── Depreciation (run + PPE Ledger Card + Property Card) ────────────────────

internal interface IArDepreciationClient
{
    Task<RunDepreciationResultDto> RunAsync(DateOnly? asOfPeriod = null, CancellationToken ct = default);
    Task<PpeLedgerCardDto?> GetLedgerCardAsync(string propertyNo, CancellationToken ct = default);
    Task<PropertyCardDto?> GetPropertyCardAsync(string propertyNo, CancellationToken ct = default);
}

internal sealed class ArDepreciationClient(HttpClient http) : IArDepreciationClient
{
    public async Task<RunDepreciationResultDto> RunAsync(DateOnly? asOfPeriod = null, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync("api/v1/asset-register/depreciation/run", new { AsOfPeriod = asOfPeriod }, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<RunDepreciationResultDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<PpeLedgerCardDto?> GetLedgerCardAsync(string propertyNo, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"api/v1/asset-register/depreciation/ledger-card/{Uri.EscapeDataString(propertyNo)}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<PpeLedgerCardDto>(ArJsonOptions.Default, ct);
    }

    public async Task<PropertyCardDto?> GetPropertyCardAsync(string propertyNo, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"api/v1/asset-register/reports/property-card/{Uri.EscapeDataString(propertyNo)}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<PropertyCardDto>(ArJsonOptions.Default, ct);
    }
}

// ── Property Item Catalog ──────────────────────────────────────────────────

internal sealed record ArCatalogItemDto(
    Guid Id,
    string Code,
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears,
    bool IsActive,
    CatalogItemStatus Status = CatalogItemStatus.Ready,
    decimal ResidualValuePercent = 5m,
    DepreciationMethod DepreciationMethod = DepreciationMethod.StraightLine);

internal sealed record CreateArCatalogItemRequest(
    string Code,
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears,
    decimal ResidualValuePercent = 5m,
    DepreciationMethod DepreciationMethod = DepreciationMethod.StraightLine);

internal sealed record UpdateArCatalogItemRequest(
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears,
    decimal ResidualValuePercent = 5m,
    DepreciationMethod DepreciationMethod = DepreciationMethod.StraightLine);

internal interface IArCatalogClient
{
    Task<ArPagedResponse<ArCatalogItemDto>> SearchAsync(string? keyword = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArCatalogItemDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ArCatalogItemDto> CreateAsync(CreateArCatalogItemRequest request, CancellationToken ct = default);
    Task<ArCatalogItemDto> UpdateAsync(Guid id, UpdateArCatalogItemRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ArCatalogItemDto> SetActivationAsync(Guid id, bool isActive, CancellationToken ct = default);
}

internal sealed class ArCatalogClient(HttpClient http) : IArCatalogClient
{
    private const string Base = "api/v1/asset-register/catalog";

    public async Task<ArPagedResponse<ArCatalogItemDto>> SearchAsync(string? keyword = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"] = keyword,
            ["isActive"] = isActive?.ToString(CultureInfo.InvariantCulture),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArCatalogItemDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<ArCatalogItemDto>([], page, pageSize, 0, 0);
    }

    public Task<ArCatalogItemDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArCatalogItemDto>($"{Base}/{id}", ArJsonOptions.Default, ct);

    public async Task<ArCatalogItemDto> CreateAsync(CreateArCatalogItemRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArCatalogItemDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArCatalogItemDto> UpdateAsync(Guid id, UpdateArCatalogItemRequest request, CancellationToken ct = default)
    {
        var resp = await http.PutAsJsonAsync($"{Base}/{id}", request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArCatalogItemDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"{Base}/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<ArCatalogItemDto> SetActivationAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var resp = await http.PutAsJsonAsync($"{Base}/{id}/activation", new { IsActive = isActive }, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArCatalogItemDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }
}

// ── Property Accountability ────────────────────────────────────────────────

internal sealed record ArAccountabilitySummaryDto(
    Guid Id,
    string DocumentNo,
    AccountabilityType AccountabilityType,
    AccountabilityStatus Status,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    int LineCount);

internal sealed record ArAccountabilityLineDto(
    Guid Id,
    Guid AccountabilityId,
    Guid AssetRegistryId,
    ArAssetSnapshotDto Snapshot,
    string SnapshotItemNo,
    string? SnapshotResponsibilityCenterCode,
    int IssuedQty,
    int ReturnedQty,
    AccountabilityLineStatus LineStatus,
    DateOnly? ReturnedOn,
    ArContracts.AssetCondition? ReturnedConditionAtReturn,
    Guid? LostOnIncidentId);

internal sealed record ArAccountabilityDto(
    Guid Id,
    string DocumentNo,
    AccountabilityType AccountabilityType,
    string FundCluster,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    AccountabilityStatus Status,
    string? CancellationReason,
    Guid? SupersededByAccountabilityId,
    Guid? SupersedesAccountabilityId,
    ArEmployeeRefDto IssuedBy,
    ArEmployeeRefDto ReceivedBy,
    IReadOnlyCollection<ArAccountabilityLineDto> Lines);

internal sealed record IssueAccountabilityLineRequest(
    Guid AssetRegistryId,
    string ItemNo,
    Guid LocationId,
    string? ResponsibilityCenterCode,
    int? OdometerAtIssue = null,
    string? PlateNumber = null,
    string? EngineNumber = null,
    string? ChassisNumber = null);

internal sealed record IssueAccountabilityRequest(
    AccountabilityType AccountabilityType,
    string FundCluster,
    ArEmployeeRefDto IssuedBy,
    ArEmployeeRefDto ReceivedBy,
    DateOnly IssuedOn,
    DateOnly? ExpiresOn,
    IReadOnlyList<IssueAccountabilityLineRequest> Lines);

internal sealed record ReturnAccountabilityLineRequest(Guid LineId, int? OdometerAtReturn = null);

internal sealed record ReturnAccountabilityLinesRequest(
    IReadOnlyList<ReturnAccountabilityLineRequest> Lines,
    DateOnly ReturnedOn,
    ArContracts.AssetCondition ConditionAtReturn);

internal sealed record CancelAccountabilityRequest(string Reason);

internal sealed record RenewAccountabilityRequest(DateOnly NewIssuedOn, DateOnly? NewExpiresOn);

internal interface IArAccountabilityClient
{
    Task<ArPagedResponse<ArAccountabilitySummaryDto>> SearchAsync(string? keyword = null, AccountabilityType? type = null, AccountabilityStatus? status = null, Guid? receivedByEmployeeId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArPagedResponse<ArAccountabilitySummaryDto>> GetMineAsync(string? keyword = null, AccountabilityType? type = null, AccountabilityStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArAccountabilityDto?> GetMineDetailAsync(Guid id, CancellationToken ct = default);
    Task<ArAccountabilityDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ArAccountabilityDto> IssueAsync(IssueAccountabilityRequest request, CancellationToken ct = default);
    Task<ArAccountabilityDto> ReturnLinesAsync(Guid id, ReturnAccountabilityLinesRequest request, CancellationToken ct = default);
    Task<ArAccountabilityDto> CancelAsync(Guid id, string reason, CancellationToken ct = default);
    Task<ArAccountabilityDto> RenewAsync(Guid id, DateOnly newIssuedOn, DateOnly? newExpiresOn, CancellationToken ct = default);
}

internal sealed class ArAccountabilityClient(HttpClient http) : IArAccountabilityClient
{
    private const string Base = "api/v1/asset-register/accountability";

    public async Task<ArPagedResponse<ArAccountabilitySummaryDto>> SearchAsync(string? keyword = null, AccountabilityType? type = null, AccountabilityStatus? status = null, Guid? receivedByEmployeeId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"] = keyword,
            ["type"] = type?.ToString(),
            ["status"] = status?.ToString(),
            ["receivedByEmployeeId"] = receivedByEmployeeId?.ToString(),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArAccountabilitySummaryDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<ArAccountabilitySummaryDto>([], page, pageSize, 0, 0);
    }

    public async Task<ArPagedResponse<ArAccountabilitySummaryDto>> GetMineAsync(string? keyword = null, AccountabilityType? type = null, AccountabilityStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build($"{Base}/mine", new()
        {
            ["keyword"] = keyword,
            ["type"] = type?.ToString(),
            ["status"] = status?.ToString(),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArAccountabilitySummaryDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<ArAccountabilitySummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArAccountabilityDto?> GetMineDetailAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArAccountabilityDto>($"{Base}/mine/{id}", ArJsonOptions.Default, ct);

    public Task<ArAccountabilityDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArAccountabilityDto>($"{Base}/{id}", ArJsonOptions.Default, ct);

    public async Task<ArAccountabilityDto> IssueAsync(IssueAccountabilityRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArAccountabilityDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArAccountabilityDto> ReturnLinesAsync(Guid id, ReturnAccountabilityLinesRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/return", request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArAccountabilityDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArAccountabilityDto> CancelAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/cancel", new CancelAccountabilityRequest(reason), ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArAccountabilityDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArAccountabilityDto> RenewAsync(Guid id, DateOnly newIssuedOn, DateOnly? newExpiresOn, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/renew", new RenewAccountabilityRequest(newIssuedOn, newExpiresOn), ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArAccountabilityDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }
}

// ── Physical Count Sessions ────────────────────────────────────────────────

internal sealed record ArPhysicalCountSummaryDto(
    Guid Id,
    string Code,
    PhysicalCountScope Scope,
    PhysicalCountStatus Status,
    DateOnly AsAt,
    DateOnly StartedOn,
    DateOnly? ClosedOn,
    int EntryCount,
    string? OfficeOrderNo = null,
    DateTimeOffset? FrozenOnUtc = null);

internal sealed record ArPhysicalCountEntryDto(
    Guid Id,
    Guid SessionId,
    Guid? AssetRegistryId,
    ArAssetSnapshotDto? Snapshot,
    string SnapshotArticle,
    string SnapshotUnit,
    decimal SnapshotUnitCost,
    PhysicalCountCondition Condition,
    DateTimeOffset? ScannedOnUtc,
    string? PhotoPath,
    Guid? ScannedByEmployeeId,
    Guid LocationId,
    string? Remarks,
    string? ProposedPropertyClass = null,
    string? ProposedCategoryCode = null,
    DateOnly? ProposedAcquisitionDate = null,
    decimal? ProposedUnitCost = null,
    bool NeedsRecount = false,
    string? RecountReason = null,
    string? ProposedPropertyNo = null,
    Guid? ProposedCatalogItemId = null);

internal sealed record ArPhysicalCountSessionDto(
    Guid Id,
    string Code,
    PhysicalCountScope Scope,
    PhysicalCountStatus Status,
    string FundCluster,
    DateOnly StartedOn,
    DateOnly? ClosedOn,
    DateOnly AsAt,
    string? Remarks,
    IReadOnlyCollection<ArEmployeeRefDto> ConductedBy,
    ArEmployeeRefDto? ApprovedBy,
    ArEmployeeRefDto? WitnessedBy,
    IReadOnlyCollection<ArPhysicalCountEntryDto> Entries,
    string? OfficeOrderNo = null,
    DateTimeOffset? FrozenOnUtc = null);

// ── Reconciliation read model ──────────────────────────────────────────────

internal enum ArReconciliationRowStatus { Matched = 0, Shortage = 1, Overage = 2, Uncounted = 3 }

internal sealed record ArReconciliationRowDto(
    Guid? EntryId,
    Guid? AssetRegistryId,
    string? PropertyNo,
    string Article,
    string Unit,
    decimal UnitCost,
    int BookQty,
    int CountedQty,
    ArReconciliationRowStatus RowStatus,
    PhysicalCountCondition? Condition,
    bool NeedsRecount,
    string? RecountReason,
    Guid? LocationId,
    string? Remarks);

internal sealed record ArReconciliationReportDto(
    Guid SessionId,
    string Code,
    PhysicalCountStatus Status,
    string FundCluster,
    PhysicalCountScope Scope,
    string? OfficeOrderNo,
    DateTimeOffset? FrozenOnUtc,
    IReadOnlyList<ArReconciliationRowDto> Rows,
    int MatchedCount,
    int ShortageCount,
    int OverageCount,
    int UncountedCount,
    decimal ShortageValue,
    decimal OverageValue);

internal sealed record StartPhysicalCountRequest(
    string Code,
    PhysicalCountScope Scope,
    string FundCluster,
    DateOnly AsAt,
    DateOnly StartedOn,
    IReadOnlyList<ArEmployeeRefDto> ConductedBy,
    string? Remarks = null,
    string? OfficeOrderNo = null);

internal sealed record ArRecordPhysicalCountEntryRequest(
    Guid AssetRegistryId,
    string Article,
    string Unit,
    decimal UnitCost,
    PhysicalCountCondition Condition,
    Guid LocationId,
    string? Remarks = null);

internal sealed record ClosePhysicalCountRequest(
    ArEmployeeRefDto ApprovedBy,
    ArEmployeeRefDto? WitnessedBy,
    DateOnly ClosedOn,
    string? Station = null);

internal sealed record ArFreezePhysicalCountRequest(string OfficeOrderNo);

internal sealed record ArAddFoundAtStationRequest(
    Guid SessionId,
    string Article,
    string Unit,
    decimal UnitCost,
    Guid LocationId,
    string? ProposedPropertyClass = null,
    string? ProposedCategoryCode = null,
    DateOnly? ProposedAcquisitionDate = null,
    decimal? ProposedUnitCost = null,
    Guid? ScannedByEmployeeId = null,
    string? Remarks = null,
    string? ProposedPropertyNo = null,
    Guid? ProposedCatalogItemId = null);

internal sealed record ArMarkMissingRequest(
    Guid SessionId,
    Guid AssetRegistryId,
    Guid LocationId,
    string? Remarks = null);

internal sealed record ArRequestRecountRequest(string? Reason);

internal interface IArPhysicalCountClient
{
    Task<ArPagedResponse<ArPhysicalCountSummaryDto>> SearchAsync(string? keyword = null, PhysicalCountStatus? status = null, PhysicalCountScope? scope = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> StartAsync(StartPhysicalCountRequest request, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> FreezeAsync(Guid sessionId, string officeOrderNo, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> RecordEntryAsync(Guid sessionId, ArRecordPhysicalCountEntryRequest request, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> AddFoundAtStationAsync(Guid sessionId, ArAddFoundAtStationRequest request, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> MarkMissingAsync(Guid sessionId, ArMarkMissingRequest request, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> ReconcileAsync(Guid sessionId, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> RequestRecountAsync(Guid sessionId, Guid entryId, string? reason, CancellationToken ct = default);
    Task<ArReconciliationReportDto?> GetReconciliationAsync(Guid sessionId, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> CloseAsync(Guid sessionId, ClosePhysicalCountRequest request, CancellationToken ct = default);
    /// <summary>annexKind: "b" = Found at Station, "c" = Non-Existing/Missing. Returns the COA annex PDF bytes.</summary>
    Task<byte[]> GetCountAnnexPdfAsync(Guid sessionId, string annexKind, string? pageWidth = null, CancellationToken ct = default);
}

internal sealed class ArPhysicalCountClient(HttpClient http) : IArPhysicalCountClient
{
    private const string Base = "api/v1/asset-register/count";

    public async Task<ArPagedResponse<ArPhysicalCountSummaryDto>> SearchAsync(string? keyword = null, PhysicalCountStatus? status = null, PhysicalCountScope? scope = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"] = keyword,
            ["status"] = status?.ToString(),
            ["scope"] = scope?.ToString(),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArPhysicalCountSummaryDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<ArPhysicalCountSummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArPhysicalCountSessionDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArPhysicalCountSessionDto>($"{Base}/{id}", ArJsonOptions.Default, ct);

    public async Task<ArPhysicalCountSessionDto> StartAsync(StartPhysicalCountRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArPhysicalCountSessionDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArPhysicalCountSessionDto> FreezeAsync(Guid sessionId, string officeOrderNo, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{sessionId}/freeze", new ArFreezePhysicalCountRequest(officeOrderNo), ArJsonOptions.Default, ct);
        return await ReadSessionAsync(resp, ct);
    }

    public async Task<ArPhysicalCountSessionDto> RecordEntryAsync(Guid sessionId, ArRecordPhysicalCountEntryRequest request, CancellationToken ct = default)
    {
        // The command carries SessionId and the endpoint rejects a route/body mismatch, so merge it in.
        var body = new
        {
            SessionId = sessionId,
            request.AssetRegistryId,
            request.Article,
            request.Unit,
            request.UnitCost,
            request.Condition,
            request.LocationId,
            request.Remarks
        };
        var resp = await http.PostAsJsonAsync($"{Base}/{sessionId}/entries", body, ArJsonOptions.Default, ct);
        return await ReadSessionAsync(resp, ct);
    }

    public async Task<ArPhysicalCountSessionDto> AddFoundAtStationAsync(Guid sessionId, ArAddFoundAtStationRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{sessionId}/found-at-station", request, ArJsonOptions.Default, ct);
        return await ReadSessionAsync(resp, ct);
    }

    public async Task<ArPhysicalCountSessionDto> MarkMissingAsync(Guid sessionId, ArMarkMissingRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{sessionId}/missing", request, ArJsonOptions.Default, ct);
        return await ReadSessionAsync(resp, ct);
    }

    public async Task<ArPhysicalCountSessionDto> ReconcileAsync(Guid sessionId, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{sessionId}/reconcile", new { }, ct);
        return await ReadSessionAsync(resp, ct);
    }

    public async Task<ArPhysicalCountSessionDto> RequestRecountAsync(Guid sessionId, Guid entryId, string? reason, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{sessionId}/entries/{entryId}/recount", new ArRequestRecountRequest(reason), ArJsonOptions.Default, ct);
        return await ReadSessionAsync(resp, ct);
    }

    public Task<ArReconciliationReportDto?> GetReconciliationAsync(Guid sessionId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArReconciliationReportDto>($"{Base}/{sessionId}/reconciliation", ArJsonOptions.Default, ct);

    public Task<byte[]> GetCountAnnexPdfAsync(Guid sessionId, string annexKind, string? pageWidth = null, CancellationToken ct = default)
    {
        var url = $"api/v1/quest-pdf-reporting/asset-register/physical-count/{sessionId}/annex-{annexKind.ToLowerInvariant()}/pdf";
        if (!string.IsNullOrWhiteSpace(pageWidth))
            url += $"?pageWidth={pageWidth}";
        return http.GetByteArrayAsync(url, ct);
    }

    public async Task<ArPhysicalCountSessionDto> CloseAsync(Guid sessionId, ClosePhysicalCountRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{sessionId}/close", request, ArJsonOptions.Default, ct);
        return await ReadSessionAsync(resp, ct);
    }

    private static async Task<ArPhysicalCountSessionDto> ReadSessionAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(await ArErrorReader.ExtractAsync(resp, ct));
        return (await resp.Content.ReadFromJsonAsync<ArPhysicalCountSessionDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }
}

// ── Incident Reports ───────────────────────────────────────────────────────

internal sealed record ArIncidentReportSummaryDto(
    Guid Id,
    string IncidentNo,
    ArContracts.PropertyIncidentType IncidentType,
    PropertyIncidentStatus Status,
    DateOnly IncidentDate,
    int ItemCount);

internal sealed record ArIncidentReportItemDto(
    Guid Id,
    Guid ReportId,
    Guid AssetRegistryId,
    ArAssetSnapshotDto Snapshot,
    decimal SnapshotAcquisitionCost,
    decimal SnapshotCurrentReplacementCost,
    IncidentItemResolution ItemResolution,
    DateOnly? ResolvedOn);

internal sealed record ArIncidentReportDto(
    Guid Id,
    string IncidentNo,
    ArContracts.PropertyIncidentType IncidentType,
    DateOnly IncidentDate,
    string FundCluster,
    string DepartmentOffice,
    string Circumstances,
    ArEmployeeRefDto AccountableOfficer,
    string AccountableOfficerDesignation,
    bool PoliceNotified,
    string? PoliceStation,
    DateOnly? PoliceNotifiedOn,
    string? PoliceBlotterRef,
    DateOnly? NotarizedOn,
    PropertyIncidentStatus Status,
    decimal? AmountSettled,
    DateOnly? RecoveredOn,
    IReadOnlyCollection<ArIncidentReportItemDto> Items);

internal sealed record FileIncidentItemRequest(Guid AssetRegistryId, Guid? AccountabilityLineId = null);

internal sealed record FileIncidentReportRequest(
    ArContracts.PropertyIncidentType IncidentType,
    DateOnly IncidentDate,
    string FundCluster,
    string DepartmentOffice,
    string Circumstances,
    ArEmployeeRefDto AccountableOfficer,
    string AccountableOfficerDesignation,
    IReadOnlyList<FileIncidentItemRequest> Items);

internal sealed record NotifyPoliceRequest(string Station, DateOnly NotifiedOn, string BlotterRef);

internal sealed record NotarizeIncidentRequest(DateOnly NotarizedOn, string DocNo, string PageNo, string BookNo, string SeriesOf);

internal interface IArIncidentReportClient
{
    Task<ArPagedResponse<ArIncidentReportSummaryDto>> SearchAsync(string? keyword = null, ArContracts.PropertyIncidentType? incidentType = null, PropertyIncidentStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArIncidentReportDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ArIncidentReportDto> FileAsync(FileIncidentReportRequest request, CancellationToken ct = default);
    Task<ArIncidentReportDto> NotifyPoliceAsync(Guid id, NotifyPoliceRequest request, CancellationToken ct = default);
    Task<ArIncidentReportDto> NotarizeAsync(Guid id, NotarizeIncidentRequest request, CancellationToken ct = default);
    Task<ArIncidentReportDto> CloseAsync(Guid id, CancellationToken ct = default);
}

internal sealed class ArIncidentReportClient(HttpClient http) : IArIncidentReportClient
{
    private const string Base = "api/v1/asset-register/incidents";

    public async Task<ArPagedResponse<ArIncidentReportSummaryDto>> SearchAsync(string? keyword = null, ArContracts.PropertyIncidentType? incidentType = null, PropertyIncidentStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"] = keyword,
            ["incidentType"] = incidentType?.ToString(),
            ["status"] = status?.ToString(),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArIncidentReportSummaryDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<ArIncidentReportSummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArIncidentReportDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArIncidentReportDto>($"{Base}/{id}", ArJsonOptions.Default, ct);

    public async Task<ArIncidentReportDto> FileAsync(FileIncidentReportRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIncidentReportDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArIncidentReportDto> NotifyPoliceAsync(Guid id, NotifyPoliceRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/police-notify", request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIncidentReportDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArIncidentReportDto> NotarizeAsync(Guid id, NotarizeIncidentRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/notarize", request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIncidentReportDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArIncidentReportDto> CloseAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/close", new { }, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIncidentReportDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }
}

// ── Issuance Reports ───────────────────────────────────────────────────────

internal sealed record ArIssuanceReportSummaryDto(
    Guid Id,
    string ReportNo,
    IssuanceReportType ReportType,
    IssuanceNature Nature,
    DateOnly Date,
    int LineCount,
    decimal TotalAmount);

internal sealed record ArIssuanceReportLineDto(
    Guid Id,
    Guid ReportId,
    Guid AssetRegistryId,
    int ItemNo,
    ArAssetSnapshotDto Snapshot,
    decimal SnapshotUnitCost,
    decimal SnapshotAmount,
    decimal? AccumulatedDepreciation,
    decimal? BookValue);

internal sealed record ArIssuanceReportDto(
    Guid Id,
    string ReportNo,
    IssuanceReportType ReportType,
    string FundCluster,
    DateOnly Date,
    IssuanceNature Nature,
    ArEmployeeRefDto IssuedBy,
    ArEmployeeRefDto ApprovedBy,
    ArEmployeeRefDto IssuedTo,
    string IssuedToOfficeAddress,
    ArEmployeeRefDto ReceivedBy,
    DateOnly? DateReceived,
    string? DriverName,
    string? BillOfLadingNo,
    string? Remarks,
    IReadOnlyCollection<ArIssuanceReportLineDto> Lines);

internal sealed record CreateIssuanceReportRequest(
    IssuanceReportType ReportType,
    DateOnly Date,
    string FundCluster,
    IssuanceNature Nature,
    ArEmployeeRefDto IssuedBy,
    ArEmployeeRefDto ApprovedBy,
    ArEmployeeRefDto IssuedTo,
    string IssuedToOfficeAddress,
    ArEmployeeRefDto ReceivedBy,
    DateOnly? DateReceived,
    string? DriverName,
    string? BillOfLadingNo,
    string? Remarks,
    IReadOnlyList<Guid> AssetRegistryIds);

internal interface IArIssuanceReportClient
{
    Task<ArPagedResponse<ArIssuanceReportSummaryDto>> SearchAsync(string? keyword = null, IssuanceReportType? reportType = null, IssuanceNature? nature = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArIssuanceReportDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ArIssuanceReportDto> CreateAsync(CreateIssuanceReportRequest request, CancellationToken ct = default);
    Task<byte[]> GetFastReportPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, int? minRows = null, bool? dataOnly = null, double? offsetX = null, double? offsetY = null, CancellationToken ct = default);
    Task<byte[]> GetSmirFastReportPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, int? minRows = null, CancellationToken ct = default);
}

internal sealed class ArIssuanceReportClient(HttpClient http) : IArIssuanceReportClient
{
    private const string Base = "api/v1/asset-register/issuance";

    public async Task<ArPagedResponse<ArIssuanceReportSummaryDto>> SearchAsync(string? keyword = null, IssuanceReportType? reportType = null, IssuanceNature? nature = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"] = keyword,
            ["reportType"] = reportType?.ToString(),
            ["nature"] = nature?.ToString(),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArIssuanceReportSummaryDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<ArIssuanceReportSummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArIssuanceReportDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArIssuanceReportDto>($"{Base}/{id}", ArJsonOptions.Default, ct);

    public async Task<ArIssuanceReportDto> CreateAsync(CreateIssuanceReportRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIssuanceReportDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public Task<byte[]> GetFastReportPdfAsync(
        Guid id,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null,
        bool? dataOnly = null,
        double? offsetX = null,
        double? offsetY = null,
        CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build($"api/v1/fast-reporting/asset-register/issuance-reports/{id}/print", new()
        {
            ["pageWidth"] = pageWidth,
            ["orientation"] = orientation,
            ["minRows"] = minRows?.ToString(CultureInfo.InvariantCulture),
            ["dataOnly"] = dataOnly == true ? "true" : null,
            ["offsetX"] = offsetX is { } ox && ox != 0 ? ox.ToString(CultureInfo.InvariantCulture) : null,
            ["offsetY"] = offsetY is { } oy && oy != 0 ? oy.ToString(CultureInfo.InvariantCulture) : null,
        });
        return http.GetByteArrayAsync(url, ct);
    }

    public Task<byte[]> GetSmirFastReportPdfAsync(
        Guid id,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null,
        CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build($"api/v1/fast-reporting/asset-register/smir/{id}/print", new()
        {
            ["pageWidth"] = pageWidth,
            ["orientation"] = orientation,
            ["minRows"] = minRows?.ToString(CultureInfo.InvariantCulture),
        });
        return http.GetByteArrayAsync(url, ct);
    }
}

// ── Unserviceable Reports ──────────────────────────────────────────────────

internal sealed record ArUnserviceableReportSummaryDto(
    Guid Id,
    string ReportNo,
    UnserviceableReportType ReportType,
    UnserviceableReportStatus Status,
    DateOnly AsAt,
    int ItemCount);

internal sealed record ArUnserviceableReportItemDto(
    Guid Id,
    Guid ReportId,
    Guid AssetRegistryId,
    ArAssetSnapshotDto Snapshot,
    DateOnly SnapshotDateAcquired,
    decimal SnapshotAcquisitionCost,
    decimal SnapshotAccumulatedDepreciation,
    decimal SnapshotCarryingAmount,
    string? Remarks,
    ArContracts.DisposalMethod? DisposalMethod,
    decimal? AppraisedValue,
    DateOnly? DisposalRecordedOn);

internal sealed record ArUnserviceableReportDto(
    Guid Id,
    string ReportNo,
    UnserviceableReportType ReportType,
    DateOnly AsAt,
    string FundCluster,
    string Station,
    UnserviceableReportStatus Status,
    ArEmployeeRefDto AccountableOfficer,
    ArEmployeeRefDto? ApprovedBy,
    ArEmployeeRefDto? InspectedBy,
    DateOnly? InspectedOn,
    ArEmployeeRefDto? WitnessedBy,
    DateOnly? WitnessedOn,
    IReadOnlyCollection<ArUnserviceableReportItemDto> Items);

internal sealed record CreateUnserviceableReportRequest(
    UnserviceableReportType ReportType,
    string FundCluster,
    string Station,
    DateOnly AsAt,
    ArEmployeeRefDto AccountableOfficer);

internal sealed record AddUnserviceableReportItemRequest(Guid AssetRegistryId, string? Remarks = null);

internal sealed record SubmitUnserviceableReportRequest(ArEmployeeRefDto ApprovedBy);

internal interface IArUnserviceableReportClient
{
    Task<ArPagedResponse<ArUnserviceableReportSummaryDto>> SearchAsync(string? keyword = null, UnserviceableReportType? reportType = null, UnserviceableReportStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArUnserviceableReportDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ArUnserviceableReportDto> CreateDraftAsync(CreateUnserviceableReportRequest request, CancellationToken ct = default);
    Task<ArUnserviceableReportDto> AddItemAsync(Guid id, AddUnserviceableReportItemRequest request, CancellationToken ct = default);
    Task<ArUnserviceableReportDto> SubmitAsync(Guid id, SubmitUnserviceableReportRequest request, CancellationToken ct = default);
}

internal sealed class ArUnserviceableReportClient(HttpClient http) : IArUnserviceableReportClient
{
    private const string Base = "api/v1/asset-register/unserviceable";

    public async Task<ArPagedResponse<ArUnserviceableReportSummaryDto>> SearchAsync(string? keyword = null, UnserviceableReportType? reportType = null, UnserviceableReportStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"] = keyword,
            ["reportType"] = reportType?.ToString(),
            ["status"] = status?.ToString(),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArUnserviceableReportSummaryDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<ArUnserviceableReportSummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArUnserviceableReportDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArUnserviceableReportDto>($"{Base}/{id}", ArJsonOptions.Default, ct);

    public async Task<ArUnserviceableReportDto> CreateDraftAsync(CreateUnserviceableReportRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArUnserviceableReportDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArUnserviceableReportDto> AddItemAsync(Guid id, AddUnserviceableReportItemRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/items", request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArUnserviceableReportDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArUnserviceableReportDto> SubmitAsync(Guid id, SubmitUnserviceableReportRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/submit", request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArUnserviceableReportDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }
}

// ── Receiving Reports (PPERR / SMRR) ───────────────────────────────────────

public sealed record ArReceivingReportSummaryDto(
    Guid Id,
    ReceivingDocumentKind DocumentKind,
    string ReportNo,
    DateOnly Date,
    string ReceivedFrom,
    ArContracts.ReceiptType ReceiptType,
    int ItemCount,
    decimal TotalAmount);

public sealed record ArReceivingReportItemDto(
    Guid Id,
    Guid ReportId,
    Guid CatalogItemId,
    string? Reference,
    string PropertyNo,
    string Description,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    decimal Amount,
    string? SerialNo,
    string? Brand,
    string? Model);

public sealed record ArReceivingReportDto(
    Guid Id,
    ReceivingDocumentKind DocumentKind,
    string ReportNo,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    ArContracts.ReceiptType ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    ArEmployeeRefDto ReceivedBy,
    ArEmployeeRefDto? NotedBy,
    DateOnly? DateReceived,
    IReadOnlyCollection<ArReceivingReportItemDto> Items);

public sealed record CreateReceivingReportItemRequest(
    Guid? CatalogItemId,
    string? Reference,
    string Description,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    string PropertyNo,
    string? SerialNo,
    string? Brand,
    string? Model,
    Guid? SourceIARId = null,
    string? PropertyClassHint = null,
    string? UacsObjectCode = null,
    string? SourceAgencyName = null,
    string? SourcePropertyNo = null,
    string? SourceDocumentRef = null,
    DateOnly? OriginalAcquisitionDate = null);

// Mirror of ProcurementAcquisition's AcceptedIARLineItemDto — kept here so the
// Blazor receiving form can stay in the AssetRegister client namespace.
public sealed record AcceptedIARLineItemDto(
    Guid IARId,
    string IARNumber,
    DateOnly IARDate,
    int ItemNo,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitCost,
    string? PropertyClassHint,
    string? SerialNo,
    string? Brand,
    string? Model,
    string? StockPropertyNo,
    string SupplierName,
    string? SupplierAddress,
    Guid? CatalogItemId = null,
    string? UacsObjectCode = null);

public sealed record CreateReceivingReportRequest(
    ReceivingDocumentKind DocumentKind,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    ArContracts.ReceiptType ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    ArEmployeeRefDto ReceivedBy,
    ArEmployeeRefDto? NotedBy,
    DateOnly? DateReceived,
    IReadOnlyList<CreateReceivingReportItemRequest> Items);

public sealed record ArPPERRFormSeriesDto(
    Guid Id,
    string Label,
    int StartSerial,
    int EndSerial,
    int NextSerial,
    int Remaining,
    bool IsActive,
    bool IsExhausted,
    bool IsUnused);

public sealed record CreateArPPERRFormSeriesRequest(string Label, int StartSerial, int EndSerial);

public sealed record UpdateArPPERRFormSeriesRequest(string Label, int StartSerial, int EndSerial);

public interface IArReceivingReportClient
{
    Task<ArPagedResponse<ArReceivingReportSummaryDto>> SearchAsync(
        string? keyword = null, ReceivingDocumentKind? documentKind = null, ArContracts.ReceiptType? receiptType = null,
        DateOnly? fromDate = null, DateOnly? toDate = null,
        int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArReceivingReportDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ArReceivingReportDto> CreateAsync(CreateReceivingReportRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ArPagedResponse<AcceptedIARLineItemDto>> SearchAcceptedIARItemsAsync(
        string? keyword = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<byte[]> GetFastReportPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, int? minRows = null, bool? dataOnly = null, double? offsetX = null, double? offsetY = null, CancellationToken ct = default);
    Task<byte[]> GetSmrrFastReportPdfAsync(Guid id, string? pageWidth = null, string? orientation = null, int? minRows = null, CancellationToken ct = default);

    // PPERR Form Series
    Task<ArPagedResponse<ArPPERRFormSeriesDto>> SearchSeriesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArPPERRFormSeriesDto?> GetActiveSeriesAsync(CancellationToken ct = default);
    Task<ArPPERRFormSeriesDto> CreateSeriesAsync(CreateArPPERRFormSeriesRequest request, CancellationToken ct = default);
    Task<ArPPERRFormSeriesDto> UpdateSeriesAsync(Guid id, UpdateArPPERRFormSeriesRequest request, CancellationToken ct = default);
    Task DeleteSeriesAsync(Guid id, CancellationToken ct = default);
    Task<ArPPERRFormSeriesDto> ActivateSeriesAsync(Guid id, CancellationToken ct = default);
    Task<ArPPERRFormSeriesDto> DeactivateSeriesAsync(Guid id, CancellationToken ct = default);
}

public sealed class ArReceivingReportClient(HttpClient http) : IArReceivingReportClient
{
    private const string Base = "api/v1/asset-register/receiving";

    public async Task<ArPagedResponse<ArReceivingReportSummaryDto>> SearchAsync(
        string? keyword = null, ReceivingDocumentKind? documentKind = null, ArContracts.ReceiptType? receiptType = null,
        DateOnly? fromDate = null, DateOnly? toDate = null,
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"] = keyword,
            ["documentKind"] = documentKind?.ToString(),
            ["receiptType"] = receiptType?.ToString(),
            ["fromDate"] = fromDate?.ToString("o", CultureInfo.InvariantCulture),
            ["toDate"] = toDate?.ToString("o", CultureInfo.InvariantCulture),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArReceivingReportSummaryDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<ArReceivingReportSummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArReceivingReportDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArReceivingReportDto>($"{Base}/{id}", ArJsonOptions.Default, ct);

    public async Task<ArReceivingReportDto> CreateAsync(CreateReceivingReportRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArReceivingReportDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"{Base}/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<ArPagedResponse<AcceptedIARLineItemDto>> SearchAcceptedIARItemsAsync(
        string? keyword = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build("api/v1/procurement/inspection-acceptance-reports/accepted-line-items", new()
        {
            ["keyword"] = keyword,
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<AcceptedIARLineItemDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<AcceptedIARLineItemDto>([], page, pageSize, 0, 0);
    }

    public Task<byte[]> GetFastReportPdfAsync(
        Guid id,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null,
        bool? dataOnly = null,
        double? offsetX = null,
        double? offsetY = null,
        CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build($"api/v1/fast-reporting/asset-register/receiving-reports/{id}/print", new()
        {
            ["pageWidth"] = pageWidth,
            ["orientation"] = orientation,
            ["minRows"] = minRows?.ToString(CultureInfo.InvariantCulture),
            ["dataOnly"] = dataOnly == true ? "true" : null,
            ["offsetX"] = offsetX is { } ox && ox != 0 ? ox.ToString(CultureInfo.InvariantCulture) : null,
            ["offsetY"] = offsetY is { } oy && oy != 0 ? oy.ToString(CultureInfo.InvariantCulture) : null,
        });
        return http.GetByteArrayAsync(url, ct);
    }

    public Task<byte[]> GetSmrrFastReportPdfAsync(
        Guid id,
        string? pageWidth = null,
        string? orientation = null,
        int? minRows = null,
        CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build($"api/v1/fast-reporting/asset-register/smrr/{id}/print", new()
        {
            ["pageWidth"] = pageWidth,
            ["orientation"] = orientation,
            ["minRows"] = minRows?.ToString(CultureInfo.InvariantCulture),
        });
        return http.GetByteArrayAsync(url, ct);
    }

    private const string SeriesBase = "api/v1/asset-register/pperr-series";

    public async Task<ArPagedResponse<ArPPERRFormSeriesDto>> SearchSeriesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(SeriesBase, new()
        {
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArPPERRFormSeriesDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<ArPPERRFormSeriesDto>([], page, pageSize, 0, 0);
    }

    public async Task<ArPPERRFormSeriesDto?> GetActiveSeriesAsync(CancellationToken ct = default)
    {
        try { return await http.GetFromJsonAsync<ArPPERRFormSeriesDto>($"{SeriesBase}/active", ArJsonOptions.Default, ct); }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NoContent) { return null; }
    }

    public async Task<ArPPERRFormSeriesDto> CreateSeriesAsync(CreateArPPERRFormSeriesRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(SeriesBase, request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArPPERRFormSeriesDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArPPERRFormSeriesDto> UpdateSeriesAsync(Guid id, UpdateArPPERRFormSeriesRequest request, CancellationToken ct = default)
    {
        var resp = await http.PutAsJsonAsync($"{SeriesBase}/{id}", request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArPPERRFormSeriesDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task DeleteSeriesAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"{SeriesBase}/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<ArPPERRFormSeriesDto> ActivateSeriesAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PutAsync($"{SeriesBase}/{id}/activate", null, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArPPERRFormSeriesDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArPPERRFormSeriesDto> DeactivateSeriesAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PutAsync($"{SeriesBase}/{id}/deactivate", null, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArPPERRFormSeriesDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }
}

// ── Returned Property Receipts (RRSP / RRP) ───────────────────────────────

internal sealed record ArReturnedPropertyReceiptItemDto(
    Guid Id,
    Guid ReceiptId,
    Guid AccountabilityLineId,
    Guid AssetRegistryId,
    int ItemNo,
    ArAssetSnapshotDto Snapshot);

internal sealed record ArReturnedPropertyReceiptDto(
    Guid Id,
    string? ReceiptNo,
    ArContracts.ReturnedPropertyReceiptType ReceiptType,
    ArContracts.ReturnedPropertyReceiptStatus Status,
    DateOnly Date,
    Guid AccountabilityId,
    string AccountabilityDocumentNo,
    ArEmployeeRefDto ReturnedBy,
    ArEmployeeRefDto? ReceivedBy,
    string? Remarks,
    string? RejectionReason,
    string? CancellationReason,
    IReadOnlyCollection<ArReturnedPropertyReceiptItemDto> Items);

internal sealed record ArReturnedPropertyReceiptSummaryDto(
    Guid Id,
    string? ReceiptNo,
    ArContracts.ReturnedPropertyReceiptType ReceiptType,
    ArContracts.ReturnedPropertyReceiptStatus Status,
    DateOnly Date,
    string AccountabilityDocumentNo,
    int ItemCount,
    decimal TotalUnitCost);

internal sealed record ArReturnedPropertyStatusCountDto(
    ArContracts.ReturnedPropertyReceiptStatus Status,
    int Count);

internal sealed record CreateReturnedPropertyReceiptRequest(
    ArContracts.ReturnedPropertyReceiptType ReceiptType,
    DateOnly Date,
    Guid AccountabilityId,
    IReadOnlyList<Guid> AccountabilityLineIds,
    ArEmployeeRefDto ReturnedBy,
    string? Remarks);

internal sealed record AcceptReturnedPropertyReceiptRequest(ArEmployeeRefDto ReceivedBy);
internal sealed record RejectReturnedPropertyReceiptRequest(string Reason);
internal sealed record CancelReturnedPropertyReceiptRequest(string? Reason);

internal interface IArReturnedPropertyClient
{
    Task<ArPagedResponse<ArReturnedPropertyReceiptSummaryDto>> SearchAsync(
        string? keyword = null,
        ArContracts.ReturnedPropertyReceiptType? receiptType = null,
        ArContracts.ReturnedPropertyReceiptStatus? status = null,
        Guid? returnedByEmployeeId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int page = 1,
        int pageSize = 15,
        CancellationToken ct = default);

    Task<IReadOnlyList<ArReturnedPropertyStatusCountDto>> GetStatusCountsAsync(
        Guid? returnedByEmployeeId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ArReturnedPropertyReceiptDto?> GetAsync(Guid id, CancellationToken ct = default);

    Task<ArReturnedPropertyReceiptDto> CreateAsync(
        CreateReturnedPropertyReceiptRequest request, CancellationToken ct = default);

    Task<ArReturnedPropertyReceiptDto> AcceptAsync(Guid id, ArEmployeeRefDto receivedBy, CancellationToken ct = default);
    Task<ArReturnedPropertyReceiptDto> RejectAsync(Guid id, string reason, CancellationToken ct = default);
    Task<ArReturnedPropertyReceiptDto> CancelAsync(Guid id, string? reason, CancellationToken ct = default);
}

internal sealed class ArReturnedPropertyClient(HttpClient http) : IArReturnedPropertyClient
{
    private const string Base = "api/v1/asset-register/returned-property";

    public async Task<ArPagedResponse<ArReturnedPropertyReceiptSummaryDto>> SearchAsync(
        string? keyword = null,
        ArContracts.ReturnedPropertyReceiptType? receiptType = null,
        ArContracts.ReturnedPropertyReceiptStatus? status = null,
        Guid? returnedByEmployeeId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int page = 1,
        int pageSize = 15,
        CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"]     = keyword,
            ["receiptType"] = receiptType?.ToString(),
            ["status"]      = status?.ToString(),
            ["returnedByEmployeeId"] = returnedByEmployeeId?.ToString(),
            ["fromDate"]    = fromDate?.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            ["toDate"]      = toDate?.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            ["pageNumber"]  = page.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["pageSize"]    = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArReturnedPropertyReceiptSummaryDto>>(url, ArJsonOptions.Default, ct);
        return result ?? new ArPagedResponse<ArReturnedPropertyReceiptSummaryDto>([], page, pageSize, 0, 0);
    }

    public async Task<IReadOnlyList<ArReturnedPropertyStatusCountDto>> GetStatusCountsAsync(
        Guid? returnedByEmployeeId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build($"{Base}/status-counts", new()
        {
            ["returnedByEmployeeId"] = returnedByEmployeeId?.ToString(),
            ["fromDate"] = fromDate?.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            ["toDate"]   = toDate?.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<List<ArReturnedPropertyStatusCountDto>>(url, ArJsonOptions.Default, ct);
        return result ?? [];
    }

    public Task<ArReturnedPropertyReceiptDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArReturnedPropertyReceiptDto>($"{Base}/{id}", ArJsonOptions.Default, ct);

    public async Task<ArReturnedPropertyReceiptDto> CreateAsync(
        CreateReturnedPropertyReceiptRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArReturnedPropertyReceiptDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArReturnedPropertyReceiptDto> AcceptAsync(Guid id, ArEmployeeRefDto receivedBy, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/accept", new AcceptReturnedPropertyReceiptRequest(receivedBy), ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArReturnedPropertyReceiptDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArReturnedPropertyReceiptDto> RejectAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/reject", new RejectReturnedPropertyReceiptRequest(reason), ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArReturnedPropertyReceiptDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }

    public async Task<ArReturnedPropertyReceiptDto> CancelAsync(Guid id, string? reason, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/cancel", new CancelReturnedPropertyReceiptRequest(reason), ArJsonOptions.Default, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArReturnedPropertyReceiptDto>(ArJsonOptions.Default, cancellationToken: ct))!;
    }
}

// ── URL builder helper ─────────────────────────────────────────────────────

internal static class ArUrlBuilder
{
    public static string Build(string path, Dictionary<string, string?> query)
    {
        var sb = new StringBuilder(path);
        var first = true;
        foreach (var (key, value) in query)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            sb.Append(first ? '?' : '&');
            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
            first = false;
        }

        return sb.ToString();
    }
}

