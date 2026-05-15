using System.Globalization;
using System.Net.Http.Json;
using System.Text;
// AssetRegister contract enums. Cannot use `using namespace` alone because
// AssetManagementClient.cs declares enums of the same name (AssetType,
// AssetCategory, DisposalMethod, PropertyIncidentType, ReceiptType) in the
// same ApiClient namespace; same-namespace types shadow `using` aliases per
// C# name-resolution rules. Each conflicting type is fully-qualified at the
// point of use below. Non-conflicting types come in via the namespace import.
using AMIS.Modules.AssetRegister.Contracts.v1;
using ArContracts = AMIS.Modules.AssetRegister.Contracts.v1;

namespace AMIS.Playground.Blazor.ApiClient;

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
    Guid? SourcePurchaseOrderId);

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
        var result = await http.GetFromJsonAsync<ArPagedResponse<AssetRegistrySummaryDto>>(url, ct);
        return result ?? new ArPagedResponse<AssetRegistrySummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<AssetRegistryDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<AssetRegistryDto>($"{Base}/{id}", ct);

    public Task<AssetRegistryDto?> GetByPropertyNoAsync(string propertyNo, CancellationToken ct = default) =>
        http.GetFromJsonAsync<AssetRegistryDto>($"{Base}/by-property-no/{Uri.EscapeDataString(propertyNo)}", ct);

    public async Task<AssetRegistryDto> RegisterAsync(RegisterAssetRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AssetRegistryDto>(cancellationToken: ct))!;
    }

    public async Task<AssetRegistryDto> UpdateConditionAsync(Guid id, ArContracts.AssetCondition condition, CancellationToken ct = default)
    {
        var resp = await http.PutAsJsonAsync($"{Base}/{id}/condition", new UpdateAssetConditionRequest(condition), ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AssetRegistryDto>(cancellationToken: ct))!;
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
    bool IsActive);

internal sealed record CreateArCatalogItemRequest(
    string Code,
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears);

internal sealed record UpdateArCatalogItemRequest(
    string Description,
    string DefaultPropertyClass,
    string DefaultCategoryCode,
    string DefaultUnit,
    string? UacsObjectCode,
    int EstimatedUsefulLifeYears);

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
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArCatalogItemDto>>(url, ct);
        return result ?? new ArPagedResponse<ArCatalogItemDto>([], page, pageSize, 0, 0);
    }

    public Task<ArCatalogItemDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArCatalogItemDto>($"{Base}/{id}", ct);

    public async Task<ArCatalogItemDto> CreateAsync(CreateArCatalogItemRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArCatalogItemDto>(cancellationToken: ct))!;
    }

    public async Task<ArCatalogItemDto> UpdateAsync(Guid id, UpdateArCatalogItemRequest request, CancellationToken ct = default)
    {
        var resp = await http.PutAsJsonAsync($"{Base}/{id}", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArCatalogItemDto>(cancellationToken: ct))!;
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
        return (await resp.Content.ReadFromJsonAsync<ArCatalogItemDto>(cancellationToken: ct))!;
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
    Task<ArPagedResponse<ArAccountabilitySummaryDto>> SearchAsync(string? keyword = null, AccountabilityType? type = null, AccountabilityStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArAccountabilityDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ArAccountabilityDto> IssueAsync(IssueAccountabilityRequest request, CancellationToken ct = default);
    Task<ArAccountabilityDto> ReturnLinesAsync(Guid id, ReturnAccountabilityLinesRequest request, CancellationToken ct = default);
    Task<ArAccountabilityDto> CancelAsync(Guid id, string reason, CancellationToken ct = default);
    Task<ArAccountabilityDto> RenewAsync(Guid id, DateOnly newIssuedOn, DateOnly? newExpiresOn, CancellationToken ct = default);
}

internal sealed class ArAccountabilityClient(HttpClient http) : IArAccountabilityClient
{
    private const string Base = "api/v1/asset-register/accountability";

    public async Task<ArPagedResponse<ArAccountabilitySummaryDto>> SearchAsync(string? keyword = null, AccountabilityType? type = null, AccountabilityStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"] = keyword,
            ["type"] = type?.ToString(),
            ["status"] = status?.ToString(),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArAccountabilitySummaryDto>>(url, ct);
        return result ?? new ArPagedResponse<ArAccountabilitySummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArAccountabilityDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArAccountabilityDto>($"{Base}/{id}", ct);

    public async Task<ArAccountabilityDto> IssueAsync(IssueAccountabilityRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArAccountabilityDto>(cancellationToken: ct))!;
    }

    public async Task<ArAccountabilityDto> ReturnLinesAsync(Guid id, ReturnAccountabilityLinesRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/return", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArAccountabilityDto>(cancellationToken: ct))!;
    }

    public async Task<ArAccountabilityDto> CancelAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/cancel", new CancelAccountabilityRequest(reason), ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArAccountabilityDto>(cancellationToken: ct))!;
    }

    public async Task<ArAccountabilityDto> RenewAsync(Guid id, DateOnly newIssuedOn, DateOnly? newExpiresOn, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/renew", new RenewAccountabilityRequest(newIssuedOn, newExpiresOn), ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArAccountabilityDto>(cancellationToken: ct))!;
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
    int EntryCount);

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
    string? Remarks);

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
    IReadOnlyCollection<ArPhysicalCountEntryDto> Entries);

internal sealed record StartPhysicalCountRequest(
    string Code,
    PhysicalCountScope Scope,
    string FundCluster,
    DateOnly AsAt,
    DateOnly StartedOn,
    IReadOnlyList<ArEmployeeRefDto> ConductedBy,
    string? Remarks = null);

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
    DateOnly ClosedOn);

internal interface IArPhysicalCountClient
{
    Task<ArPagedResponse<ArPhysicalCountSummaryDto>> SearchAsync(string? keyword = null, PhysicalCountStatus? status = null, PhysicalCountScope? scope = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> StartAsync(StartPhysicalCountRequest request, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> RecordEntryAsync(Guid sessionId, ArRecordPhysicalCountEntryRequest request, CancellationToken ct = default);
    Task<ArPhysicalCountSessionDto> CloseAsync(Guid sessionId, ClosePhysicalCountRequest request, CancellationToken ct = default);
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
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArPhysicalCountSummaryDto>>(url, ct);
        return result ?? new ArPagedResponse<ArPhysicalCountSummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArPhysicalCountSessionDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArPhysicalCountSessionDto>($"{Base}/{id}", ct);

    public async Task<ArPhysicalCountSessionDto> StartAsync(StartPhysicalCountRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArPhysicalCountSessionDto>(cancellationToken: ct))!;
    }

    public async Task<ArPhysicalCountSessionDto> RecordEntryAsync(Guid sessionId, ArRecordPhysicalCountEntryRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{sessionId}/entries", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArPhysicalCountSessionDto>(cancellationToken: ct))!;
    }

    public async Task<ArPhysicalCountSessionDto> CloseAsync(Guid sessionId, ClosePhysicalCountRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{sessionId}/close", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArPhysicalCountSessionDto>(cancellationToken: ct))!;
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
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArIncidentReportSummaryDto>>(url, ct);
        return result ?? new ArPagedResponse<ArIncidentReportSummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArIncidentReportDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArIncidentReportDto>($"{Base}/{id}", ct);

    public async Task<ArIncidentReportDto> FileAsync(FileIncidentReportRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIncidentReportDto>(cancellationToken: ct))!;
    }

    public async Task<ArIncidentReportDto> NotifyPoliceAsync(Guid id, NotifyPoliceRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/police-notify", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIncidentReportDto>(cancellationToken: ct))!;
    }

    public async Task<ArIncidentReportDto> NotarizeAsync(Guid id, NotarizeIncidentRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/notarize", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIncidentReportDto>(cancellationToken: ct))!;
    }

    public async Task<ArIncidentReportDto> CloseAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/close", new { }, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIncidentReportDto>(cancellationToken: ct))!;
    }
}

// ── Issuance Reports ───────────────────────────────────────────────────────

internal sealed record ArIssuanceReportSummaryDto(
    Guid Id,
    string ReportNo,
    IssuanceReportType ReportType,
    IssuanceReportStatus Status,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    int LineCount,
    decimal TotalAmount);

internal sealed record ArIssuanceReportLineDto(
    Guid Id,
    Guid ReportId,
    Guid AccountabilityId,
    Guid AccountabilityLineId,
    Guid AssetRegistryId,
    ArAssetSnapshotDto Snapshot,
    string? SnapshotResponsibilityCenterCode,
    int SnapshotQuantityIssued,
    decimal SnapshotUnitCost,
    decimal SnapshotAmount);

internal sealed record ArIssuanceReportDto(
    Guid Id,
    string ReportNo,
    IssuanceReportType ReportType,
    string FundCluster,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    IssuanceReportStatus Status,
    ArEmployeeRefDto PreparedBy,
    ArEmployeeRefDto? CertifiedBy,
    ArEmployeeRefDto? PostedBy,
    DateOnly? PostedOn,
    IReadOnlyCollection<ArIssuanceReportLineDto> Lines);

internal sealed record CreateIssuanceReportDraftRequest(
    IssuanceReportType ReportType,
    string FundCluster,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    ArEmployeeRefDto PreparedBy);

internal sealed record AddIssuanceReportLinesRequest(IReadOnlyList<Guid> AccountabilityLineIds);

internal sealed record PostIssuanceReportRequest(
    ArEmployeeRefDto CertifiedBy,
    ArEmployeeRefDto PostedBy,
    DateOnly PostedOn);

internal interface IArIssuanceReportClient
{
    Task<ArPagedResponse<ArIssuanceReportSummaryDto>> SearchAsync(string? keyword = null, IssuanceReportType? reportType = null, IssuanceReportStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ArIssuanceReportDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<ArIssuanceReportDto> CreateDraftAsync(CreateIssuanceReportDraftRequest request, CancellationToken ct = default);
    Task<ArIssuanceReportDto> AddLinesAsync(Guid id, IReadOnlyList<Guid> accountabilityLineIds, CancellationToken ct = default);
    Task<ArIssuanceReportDto> PostAsync(Guid id, PostIssuanceReportRequest request, CancellationToken ct = default);
    Task<ArIssuanceReportDto> RemoveLineAsync(Guid id, Guid lineId, CancellationToken ct = default);
}

internal sealed class ArIssuanceReportClient(HttpClient http) : IArIssuanceReportClient
{
    private const string Base = "api/v1/asset-register/issuance";

    public async Task<ArPagedResponse<ArIssuanceReportSummaryDto>> SearchAsync(string? keyword = null, IssuanceReportType? reportType = null, IssuanceReportStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build(Base, new()
        {
            ["keyword"] = keyword,
            ["reportType"] = reportType?.ToString(),
            ["status"] = status?.ToString(),
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArIssuanceReportSummaryDto>>(url, ct);
        return result ?? new ArPagedResponse<ArIssuanceReportSummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArIssuanceReportDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArIssuanceReportDto>($"{Base}/{id}", ct);

    public async Task<ArIssuanceReportDto> CreateDraftAsync(CreateIssuanceReportDraftRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIssuanceReportDto>(cancellationToken: ct))!;
    }

    public async Task<ArIssuanceReportDto> AddLinesAsync(Guid id, IReadOnlyList<Guid> accountabilityLineIds, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/lines", new AddIssuanceReportLinesRequest(accountabilityLineIds), ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIssuanceReportDto>(cancellationToken: ct))!;
    }

    public async Task<ArIssuanceReportDto> PostAsync(Guid id, PostIssuanceReportRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/post", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIssuanceReportDto>(cancellationToken: ct))!;
    }

    public async Task<ArIssuanceReportDto> RemoveLineAsync(Guid id, Guid lineId, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"{Base}/{id}/lines/{lineId}", ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArIssuanceReportDto>(cancellationToken: ct))!;
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
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArUnserviceableReportSummaryDto>>(url, ct);
        return result ?? new ArPagedResponse<ArUnserviceableReportSummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArUnserviceableReportDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArUnserviceableReportDto>($"{Base}/{id}", ct);

    public async Task<ArUnserviceableReportDto> CreateDraftAsync(CreateUnserviceableReportRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArUnserviceableReportDto>(cancellationToken: ct))!;
    }

    public async Task<ArUnserviceableReportDto> AddItemAsync(Guid id, AddUnserviceableReportItemRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/items", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArUnserviceableReportDto>(cancellationToken: ct))!;
    }

    public async Task<ArUnserviceableReportDto> SubmitAsync(Guid id, SubmitUnserviceableReportRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync($"{Base}/{id}/submit", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArUnserviceableReportDto>(cancellationToken: ct))!;
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
    string? PropertyClassHint = null);

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
    string? SupplierAddress);

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
        var result = await http.GetFromJsonAsync<ArPagedResponse<ArReceivingReportSummaryDto>>(url, ct);
        return result ?? new ArPagedResponse<ArReceivingReportSummaryDto>([], page, pageSize, 0, 0);
    }

    public Task<ArReceivingReportDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ArReceivingReportDto>($"{Base}/{id}", ct);

    public async Task<ArReceivingReportDto> CreateAsync(CreateReceivingReportRequest request, CancellationToken ct = default)
    {
        var resp = await http.PostAsJsonAsync(Base, request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ArReceivingReportDto>(cancellationToken: ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"{Base}/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<ArPagedResponse<AcceptedIARLineItemDto>> SearchAcceptedIARItemsAsync(
        string? keyword = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var url = ArUrlBuilder.Build("api/v1/procurement/iars/accepted-line-items", new()
        {
            ["keyword"] = keyword,
            ["pageNumber"] = page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = pageSize.ToString(CultureInfo.InvariantCulture),
        });
        var result = await http.GetFromJsonAsync<ArPagedResponse<AcceptedIARLineItemDto>>(url, ct);
        return result ?? new ArPagedResponse<AcceptedIARLineItemDto>([], page, pageSize, 0, 0);
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

