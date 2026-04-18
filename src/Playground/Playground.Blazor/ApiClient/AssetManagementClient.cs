using System.Globalization;
using System.Net.Http.Json;
using System.Web;

namespace FSH.Playground.Blazor.ApiClient;

internal enum ReceiptType
{
    Purchase = 0,
    Transfer = 1,
    Donation = 2,
    Others = 3,
}

// Property Item Catalog (unified SE + PPE catalog)

internal sealed record CreatePropertyItemCatalogRequest(
    string Code,
    string Name,
    string? Description = null,
    string? UACSObjectCode = null,
    string UnitOfMeasure = "Piece",
    int? EstimatedUsefulLifeYears = null);

internal sealed record UpdatePropertyItemCatalogRequest(
    string Code,
    string Name,
    string? Description = null,
    string? UACSObjectCode = null,
    string UnitOfMeasure = "Piece",
    int? EstimatedUsefulLifeYears = null,
    bool IsActive = true);

internal sealed record PropertyItemCatalogDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? UACSObjectCode,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    bool IsActive);

internal sealed record PropertyItemCatalogSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? UACSObjectCode,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    bool IsActive);

internal sealed record PropertyItemCatalogDetailsDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? UACSObjectCode,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);

internal sealed record PagedPropertyItemCatalogResponse(
    IReadOnlyList<PropertyItemCatalogSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal sealed record PropertyItemCatalogSummary(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? UACSObjectCode,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    bool IsActive);

internal sealed record PropertyItemCatalogDetails(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string? UACSObjectCode,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);

internal sealed record PropertyItemCatalogPage(
    IReadOnlyList<PropertyItemCatalogSummary> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface IPropertyItemCatalogClient
{
    Task<PropertyItemCatalogPage> SearchForUiAsync(string? keyword = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PropertyItemCatalogDetails?> GetForUiAsync(Guid id, CancellationToken ct = default);
    Task<PropertyItemCatalogSummary> CreateForUiAsync(CreatePropertyItemCatalogRequest request, CancellationToken ct = default);
    Task UpdateForUiAsync(Guid id, UpdatePropertyItemCatalogRequest request, CancellationToken ct = default);

    Task<PagedPropertyItemCatalogResponse> SearchAsync(string? keyword = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PropertyItemCatalogDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
}

internal sealed class PropertyItemCatalogClient(HttpClient http) : IPropertyItemCatalogClient
{
    private const string Base = "api/v1/asset-management/item-catalog";

    public async Task<PropertyItemCatalogPage> SearchForUiAsync(string? keyword = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await SearchAsync(keyword, isActive, page, pageSize, ct);
        return new PropertyItemCatalogPage(
            response.Items.Select(i => new PropertyItemCatalogSummary(
                i.Id,
                i.Code,
                i.Name,
                i.Description,
                i.UACSObjectCode,
                i.UnitOfMeasure,
                i.EstimatedUsefulLifeYears,
                i.IsActive)).ToList(),
            response.PageNumber,
            response.PageSize,
            response.TotalCount);
    }

    public async Task<PropertyItemCatalogDetails?> GetForUiAsync(Guid id, CancellationToken ct = default)
    {
        var response = await GetAsync(id, ct);
        return response is null
            ? null
            : new PropertyItemCatalogDetails(
                response.Id,
                response.Code,
                response.Name,
                response.Description,
                response.UACSObjectCode,
                response.UnitOfMeasure,
                response.EstimatedUsefulLifeYears,
                response.IsActive,
                response.CreatedOnUtc,
                response.CreatedBy,
                response.LastModifiedOnUtc,
                response.LastModifiedBy);
    }

    public async Task<PropertyItemCatalogSummary> CreateForUiAsync(CreatePropertyItemCatalogRequest request, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, request, ct);
        r.EnsureSuccessStatusCode();

        var created = (await r.Content.ReadFromJsonAsync<PropertyItemCatalogDto>(ct))!;
        return new PropertyItemCatalogSummary(
            created.Id,
            created.Code,
            created.Name,
            created.Description,
            created.UACSObjectCode,
            created.UnitOfMeasure,
            created.EstimatedUsefulLifeYears,
            created.IsActive);
    }

    public async Task UpdateForUiAsync(Guid id, UpdatePropertyItemCatalogRequest request, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", request, ct);
        r.EnsureSuccessStatusCode();
    }

    public Task<PagedPropertyItemCatalogResponse> SearchAsync(string? keyword = null, bool? isActive = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (isActive.HasValue) q["IsActive"] = isActive.Value.ToString();
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedPropertyItemCatalogResponse>($"{Base}?{q}", ct)!;
    }

    public Task<PropertyItemCatalogDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PropertyItemCatalogDetailsDto>($"{Base}/{id}", ct);
}

// Semi-Expendable Properties

internal sealed record RegisterSemiExpendablePropertyCommand(
    string PropertyNo,
    Guid ItemId,
    AssetCategory Category,
    string? SerialNo,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    string? FundCluster = null,
    string? Remarks = null,
    int Quantity = 1);

internal sealed record SemiExpendablePropertyDto(
    Guid Id,
    string PropertyNo,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? SerialNo,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    int Quantity,
    string? FundCluster,
    string Status,
    Guid? CurrentCustodianId,
    string? Remarks);

internal sealed record SemiExpendablePropertySummaryDto(
    Guid Id,
    string PropertyNo,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? SerialNo,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    int Quantity,
    string? FundCluster,
    string Status,
    Guid? CurrentCustodianId,
    Guid? SMRRItemId,
    string? Remarks);

internal sealed record SemiExpendablePropertyDetailsDto(
    Guid Id,
    string PropertyNo,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string UnitOfMeasure,
    int? EstimatedUsefulLifeYears,
    string? SerialNo,
    DateOnly AcquisitionDate,
    decimal UnitCost,
    int Quantity,
    string? FundCluster,
    string Status,
    Guid? CurrentCustodianId,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);

internal sealed record PagedSemiExpendablePropertiesResponse(
    IReadOnlyList<SemiExpendablePropertySummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface ISemiExpendablePropertyClient
{
    Task<PagedSemiExpendablePropertiesResponse> SearchAsync(string? keyword = null, Guid? itemId = null, string? status = null, Guid? currentCustodianId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<SemiExpendablePropertyDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<SemiExpendablePropertyDto> RegisterAsync(RegisterSemiExpendablePropertyCommand command, CancellationToken ct = default);
}

internal sealed class SemiExpendablePropertyClient(HttpClient http) : ISemiExpendablePropertyClient
{
    private const string Base = "api/v1/asset-management/semi-expendable-properties";

    public Task<PagedSemiExpendablePropertiesResponse> SearchAsync(string? keyword = null, Guid? itemId = null, string? status = null, Guid? currentCustodianId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (itemId.HasValue) q["ItemId"] = itemId.Value.ToString();
        if (!string.IsNullOrWhiteSpace(status)) q["Status"] = status;
        if (currentCustodianId.HasValue) q["CurrentCustodianId"] = currentCustodianId.Value.ToString();
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedSemiExpendablePropertiesResponse>($"{Base}?{q}", ct)!;
    }

    public Task<SemiExpendablePropertyDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<SemiExpendablePropertyDetailsDto>($"{Base}/{id}", ct);

    public async Task<SemiExpendablePropertyDto> RegisterAsync(RegisterSemiExpendablePropertyCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<SemiExpendablePropertyDto>(ct))!;
    }
}

// Receiving Reports (SMRR)

internal sealed record CreateSMRRItemRequest(
    Guid TangibleItemId,
    string? Reference);

internal sealed record CreateSMRRCommand(
    string SMRRNo,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    ReceiptType ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    string? ReceivedBy,
    string? NotedBy,
    IReadOnlyList<CreateSMRRItemRequest> Items);

internal sealed record CreateSMRRResult(
    Guid SMRRId,
    string SMRRNo,
    int ItemCount);

internal sealed record SMRRSummaryDto(
    Guid Id,
    string SMRRNo,
    DateOnly Date,
    string ReceivedFrom,
    string ReceiptType,
    string? FundCluster,
    int ItemCount);

internal sealed record SMRRItemDetailsDto(
    Guid Id,
    Guid SemiExpendablePropertyId,
    string PropertyNo,
    string? Reference,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    string? Description,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    decimal Amount);

internal sealed record SMRRDetailsDto(
    Guid Id,
    string SMRRNo,
    DateOnly Date,
    string ReceivedFrom,
    string? Address,
    string ReceiptType,
    string? OtherReceiptType,
    string? FundCluster,
    string? ReceivedBy,
    string? NotedBy,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<SMRRItemDetailsDto> Items);

internal sealed record PagedSMRRsResponse(
    IReadOnlyList<SMRRSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface ISMRRClient
{
    Task<PagedSMRRsResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<SMRRDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreateSMRRResult> CreateAsync(CreateSMRRCommand command, CancellationToken ct = default);
}

internal sealed class SMRRClient(HttpClient http) : ISMRRClient
{
    private const string Base = "api/v1/asset-management/receiving-reports";

    public Task<PagedSMRRsResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedSMRRsResponse>($"{Base}?{q}", ct)!;
    }

    public Task<SMRRDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<SMRRDetailsDto>($"{Base}/{id}", ct);

    public async Task<CreateSMRRResult> CreateAsync(CreateSMRRCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreateSMRRResult>(ct))!;
    }
}

// Inventory Custodian Slips (ICS)

internal sealed record CreateICSItemRequest(
    Guid SemiExpendablePropertyId,
    string? Description);

internal sealed record CreateICSCommand(
    string ICSNo,
    DateOnly Date,
    AssetCategory Category,
    string? FundCluster,
    Guid? IssuedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    IReadOnlyList<CreateICSItemRequest> Items);

internal sealed record CreateICSResult(
    Guid ICSId,
    string ICSNo,
    int ItemCount);

internal sealed record ICSSummaryDto(
    Guid Id,
    string ICSNo,
    DateOnly Date,
    string? FundCluster,
    Guid? IssuedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    int ItemCount);

internal sealed record ICSItemDetailsDto(
    Guid Id,
    int ItemNo,
    Guid SemiExpendablePropertyId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string? SerialNo,
    string? Description,
    decimal UnitCost,
    int? EstimatedUsefulLifeYears);

internal sealed record ICSDetailsDto(
    Guid Id,
    string ICSNo,
    DateOnly Date,
    string Category,
    string? FundCluster,
    Guid? IssuedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<ICSItemDetailsDto> Items);

internal sealed record PagedICSListResponse(
    IReadOnlyList<ICSSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface IICSClient
{
    Task<PagedICSListResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, Guid? receivedByEmployeeId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ICSDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreateICSResult> CreateAsync(CreateICSCommand command, CancellationToken ct = default);
}

internal sealed class ICSClient(HttpClient http) : IICSClient
{
    private const string Base = "api/v1/asset-management/inventory-custodian-slips";

    public Task<PagedICSListResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, Guid? receivedByEmployeeId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (receivedByEmployeeId.HasValue) q["ReceivedByEmployeeId"] = receivedByEmployeeId.Value.ToString();
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedICSListResponse>($"{Base}?{q}", ct)!;
    }

    public Task<ICSDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<ICSDetailsDto>($"{Base}/{id}", ct);

    public async Task<CreateICSResult> CreateAsync(CreateICSCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreateICSResult>(ct))!;
    }
}

// Semi-Expendable Issuance Records (SMIR)

internal enum SMIRIssuanceType { Transfer = 0, Donation = 1, Disposal = 2, Sale = 3, Others = 4 }

internal sealed record CreateSMIRItemRequest(
    Guid SemiExpendablePropertyId,
    string? Description);

internal sealed record CreateSMIRCommand(
    string SMIRNo,
    DateOnly Date,
    string? FundCluster,
    SMIRIssuanceType IssuanceType,
    string? TransferredToTenantId,
    string? TransferredToOfficerName,
    Guid? IssuedByEmployeeId,
    string? Remarks,
    IReadOnlyList<CreateSMIRItemRequest> Items);

internal sealed record CreateSMIRResult(Guid SMIRId, string SMIRNo, int ItemCount);

internal sealed record SMIRSummaryDto(
    Guid Id,
    string SMIRNo,
    DateOnly Date,
    string IssuanceType,
    string? TransferredToTenantId,
    string? TransferredToOfficerName,
    Guid? IssuedByEmployeeId,
    int ItemCount,
    DateTimeOffset CreatedOnUtc);

internal sealed record SMIRItemDetailsDto(
    Guid Id,
    Guid SemiExpendablePropertyId,
    string PropertyNo,
    int ItemNo,
    string? Description,
    decimal UnitCost,
    string CategoryAtTimeOfIssuance);

internal sealed record SMIRDetailsDto(
    Guid Id,
    string SMIRNo,
    DateOnly Date,
    string? FundCluster,
    string IssuanceType,
    string? TransferredToTenantId,
    string? TransferredToOfficerName,
    Guid? IssuedByEmployeeId,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<SMIRItemDetailsDto> Items);

internal sealed record PagedSMIRListResponse(
    IReadOnlyList<SMIRSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface ISMIRClient
{
    Task<PagedSMIRListResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, SMIRIssuanceType? issuanceType = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<SMIRDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreateSMIRResult> CreateAsync(CreateSMIRCommand command, CancellationToken ct = default);
}

internal sealed class SMIRClient(HttpClient http) : ISMIRClient
{
    private const string Base = "api/v1/asset-management/semi-expendable-issuance-records";

    public Task<PagedSMIRListResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, SMIRIssuanceType? issuanceType = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (issuanceType.HasValue) q["IssuanceType"] = ((int)issuanceType.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedSMIRListResponse>($"{Base}?{q}", ct)!;
    }

    public Task<SMIRDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<SMIRDetailsDto>($"{Base}/{id}", ct);

    public async Task<CreateSMIRResult> CreateAsync(CreateSMIRCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreateSMIRResult>(ct))!;
    }
}

// Receipt for Returned Semi-Expendable Properties (RRSP)

internal sealed record CreateRRSPCommand(
    string RRSPNo,
    DateOnly Date,
    Guid ICSId,
    string? FundCluster,
    Guid? ReceivedByEmployeeId,
    Guid ReturnedByEmployeeId,
    string? Remarks);

internal sealed record CreateRRSPResult(Guid RRSPId, string RRSPNo, int ItemCount);

internal sealed record RRSPSummaryDto(
    Guid Id,
    string RRSPNo,
    DateOnly Date,
    Guid ICSId,
    string ICSNo,
    Guid? ReceivedByEmployeeId,
    Guid ReturnedByEmployeeId,
    int ItemCount,
    DateTimeOffset CreatedOnUtc);

internal sealed record RRSPItemDetailsDto(
    Guid Id,
    Guid SemiExpendablePropertyId,
    string PropertyNo,
    int ItemNo,
    string? Description,
    decimal UnitCost,
    string CategoryAtTimeOfReturn);

internal sealed record RRSPDetailsDto(
    Guid Id,
    string RRSPNo,
    DateOnly Date,
    Guid ICSId,
    string ICSNo,
    string? FundCluster,
    Guid? ReceivedByEmployeeId,
    Guid ReturnedByEmployeeId,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<RRSPItemDetailsDto> Items);

internal sealed record PagedRRSPListResponse(
    IReadOnlyList<RRSPSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface IRRSPClient
{
    Task<PagedRRSPListResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, Guid? icsId = null, Guid? returnedByEmployeeId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<RRSPDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreateRRSPResult> CreateAsync(CreateRRSPCommand command, CancellationToken ct = default);
}

internal sealed class RRSPClient(HttpClient http) : IRRSPClient
{
    private const string Base = "api/v1/asset-management/receipt-for-returned-properties";

    public Task<PagedRRSPListResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, Guid? icsId = null, Guid? returnedByEmployeeId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (icsId.HasValue) q["ICSId"] = icsId.Value.ToString();
        if (returnedByEmployeeId.HasValue) q["ReturnedByEmployeeId"] = returnedByEmployeeId.Value.ToString();
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedRRSPListResponse>($"{Base}?{q}", ct)!;
    }

    public Task<RRSPDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<RRSPDetailsDto>($"{Base}/{id}", ct);

    public async Task<CreateRRSPResult> CreateAsync(CreateRRSPCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreateRRSPResult>(ct))!;
    }
}

// Property Incident Reports (RLSDDSP)

internal enum PropertyIncidentType { Lost = 0, Stolen = 1, Damaged = 2, Destroyed = 3 }

internal sealed record CreatePropertyIncidentReportCommand(
    string ReportNo,
    DateOnly Date,
    DateOnly? IncidentDate,
    PropertyIncidentType IncidentType,
    string? FundCluster,
    Guid? AccountableEmployeeId,
    string IncidentDetails,
    string? Remarks,
    IReadOnlyList<Guid> PropertyIds);

internal sealed record CreatePropertyIncidentReportResult(Guid ReportId, string ReportNo, int ItemCount);

internal sealed record PropertyIncidentReportSummaryDto(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    DateOnly? IncidentDate,
    string IncidentType,
    Guid? AccountableEmployeeId,
    int ItemCount,
    DateTimeOffset CreatedOnUtc);

internal sealed record PropertyIncidentItemDetailsDto(
    Guid Id,
    Guid SemiExpendablePropertyId,
    string PropertyNo,
    int ItemNo,
    string? Description,
    decimal UnitCost,
    string CategoryAtTimeOfReport);

internal sealed record PropertyIncidentReportDetailsDto(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    DateOnly? IncidentDate,
    string IncidentType,
    string? FundCluster,
    Guid? AccountableEmployeeId,
    string IncidentDetails,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<PropertyIncidentItemDetailsDto> Items);

internal sealed record PagedPropertyIncidentReportListResponse(
    IReadOnlyList<PropertyIncidentReportSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface IPropertyIncidentReportClient
{
    Task<PagedPropertyIncidentReportListResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, PropertyIncidentType? incidentType = null, Guid? accountableEmployeeId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PropertyIncidentReportDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreatePropertyIncidentReportResult> CreateAsync(CreatePropertyIncidentReportCommand command, CancellationToken ct = default);
}

internal sealed class PropertyIncidentReportClient(HttpClient http) : IPropertyIncidentReportClient
{
    private const string Base = "api/v1/asset-management/property-incident-reports";

    public Task<PagedPropertyIncidentReportListResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, PropertyIncidentType? incidentType = null, Guid? accountableEmployeeId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (incidentType.HasValue) q["IncidentType"] = ((int)incidentType.Value).ToString(CultureInfo.InvariantCulture);
        if (accountableEmployeeId.HasValue) q["AccountableEmployeeId"] = accountableEmployeeId.Value.ToString();
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedPropertyIncidentReportListResponse>($"{Base}?{q}", ct)!;
    }

    public Task<PropertyIncidentReportDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PropertyIncidentReportDetailsDto>($"{Base}/{id}", ct);

    public async Task<CreatePropertyIncidentReportResult> CreateAsync(CreatePropertyIncidentReportCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreatePropertyIncidentReportResult>(ct))!;
    }
}

// Unserviceable Property Reports (IIRUSP)

internal enum DisposalMethod { Sale = 0, Destruction = 1, Donation = 2, Transfer = 3, Others = 4 }

internal sealed record CreateUnserviceablePropertyItemRequest(
    Guid SemiExpendablePropertyId,
    string? ConditionRemarks);

internal sealed record CreateUnserviceablePropertyReportCommand(
    string ReportNo,
    DateOnly Date,
    DisposalMethod DisposalMethod,
    string? FundCluster,
    Guid? InspectedByEmployeeId,
    Guid? ApprovedByEmployeeId,
    string? Remarks,
    IReadOnlyList<CreateUnserviceablePropertyItemRequest> Items);

internal sealed record CreateUnserviceablePropertyReportResult(Guid ReportId, string ReportNo, int ItemCount);

internal sealed record UnserviceablePropertyReportSummaryDto(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    string DisposalMethod,
    Guid? InspectedByEmployeeId,
    Guid? ApprovedByEmployeeId,
    int ItemCount,
    DateTimeOffset CreatedOnUtc);

internal sealed record UnserviceablePropertyItemDetailsDto(
    Guid Id,
    Guid SemiExpendablePropertyId,
    string PropertyNo,
    int ItemNo,
    string? Description,
    decimal UnitCost,
    string CategoryAtTimeOfReport,
    string? ConditionRemarks);

internal sealed record UnserviceablePropertyReportDetailsDto(
    Guid Id,
    string ReportNo,
    DateOnly Date,
    string DisposalMethod,
    string? FundCluster,
    Guid? InspectedByEmployeeId,
    Guid? ApprovedByEmployeeId,
    string? Remarks,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    IReadOnlyList<UnserviceablePropertyItemDetailsDto> Items);

internal sealed record PagedUnserviceablePropertyReportListResponse(
    IReadOnlyList<UnserviceablePropertyReportSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface IUnserviceablePropertyReportClient
{
    Task<PagedUnserviceablePropertyReportListResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, DisposalMethod? disposalMethod = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<UnserviceablePropertyReportDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreateUnserviceablePropertyReportResult> CreateAsync(CreateUnserviceablePropertyReportCommand command, CancellationToken ct = default);
}

internal sealed class UnserviceablePropertyReportClient(HttpClient http) : IUnserviceablePropertyReportClient
{
    private const string Base = "api/v1/asset-management/unserviceable-property-reports";

    public Task<PagedUnserviceablePropertyReportListResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, DisposalMethod? disposalMethod = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (disposalMethod.HasValue) q["DisposalMethod"] = ((int)disposalMethod.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedUnserviceablePropertyReportListResponse>($"{Base}?{q}", ct)!;
    }

    public Task<UnserviceablePropertyReportDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<UnserviceablePropertyReportDetailsDto>($"{Base}/{id}", ct);

    public async Task<CreateUnserviceablePropertyReportResult> CreateAsync(CreateUnserviceablePropertyReportCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreateUnserviceablePropertyReportResult>(ct))!;
    }
}

// Asset Management Reports (SPC, RegSPI, RSPI, Property History)

internal enum AssetCategory { LowValuedSemi = 0, HighValuedSemi = 1, PPE = 2 }
internal enum ICSStatus { Active = 0, Renewed = 1, CancelledByReturn = 2, Expired = 3 }

internal sealed record SPCEntryDto(
    DateOnly Date,
    string DocumentType,
    string DocumentNo,
    int QuantityIn,
    int QuantityOut,
    decimal UnitCost,
    int RunningBalance,
    string? Remarks);

internal sealed record SPCDto(
    Guid ItemId,
    string ItemCode,
    string ItemName,
    IReadOnlyList<SPCEntryDto> Entries);

internal sealed record RegSPIEntryDto(
    Guid ICSId,
    string ICSNo,
    DateOnly Date,
    string? FundCluster,
    Guid PropertyId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string Category,
    decimal UnitCost,
    int? EstimatedUsefulLifeYears,
    DateOnly? ExpiresOn,
    string ICSStatus);

internal sealed record PagedRegSPIResponse(
    Guid EmployeeId,
    IReadOnlyList<RegSPIEntryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal sealed record RSPIItemDto(
    Guid ICSId,
    string ICSNo,
    DateOnly ICSDate,
    string ICSStatus,
    Guid ReceivedByEmployeeId,
    Guid? IssuedFromEmployeeId,
    Guid PropertyId,
    string PropertyNo,
    string? SerialNo,
    string ItemCode,
    string ItemName,
    string Category,
    decimal UnitCost,
    DateOnly? ExpiresOn);

internal sealed record PagedRSPIResponse(
    IReadOnlyList<RSPIItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal sealed record PropertyHistoryEventDto(
    DateOnly EventDate,
    string EventType,
    string DocumentType,
    string DocumentNo,
    string? Details);

internal sealed record PropertyHistoryDto(
    Guid PropertyId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string? SerialNo,
    string Category,
    decimal UnitCost,
    string CurrentStatus,
    Guid? CurrentCustodianId,
    IReadOnlyList<PropertyHistoryEventDto> Events);

internal interface IAssetManagementReportsClient
{
    Task<SPCDto?> GetSPCAsync(Guid itemId, DateOnly? dateFrom = null, DateOnly? dateTo = null, CancellationToken ct = default);
    Task<PagedRegSPIResponse?> GetRegSPIAsync(Guid employeeId, AssetCategory? category = null, ICSStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PagedRSPIResponse?> GetRSPIAsync(DateOnly? dateFrom = null, DateOnly? dateTo = null, AssetCategory? category = null, bool activeOnly = true, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PropertyHistoryDto?> GetPropertyHistoryAsync(Guid propertyId, CancellationToken ct = default);
}

internal sealed class AssetManagementReportsClient(HttpClient http) : IAssetManagementReportsClient
{
    private const string Base = "api/v1/asset-management/reports";

    public Task<SPCDto?> GetSPCAsync(Guid itemId, DateOnly? dateFrom = null, DateOnly? dateTo = null, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var qs = q.Count > 0 ? $"?{q}" : string.Empty;
        return http.GetFromJsonAsync<SPCDto>($"{Base}/spc/{itemId}{qs}", ct);
    }

    public Task<PagedRegSPIResponse?> GetRegSPIAsync(Guid employeeId, AssetCategory? category = null, ICSStatus? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        q["EmployeeId"] = employeeId.ToString();
        if (category.HasValue) q["Category"] = ((int)category.Value).ToString(CultureInfo.InvariantCulture);
        if (status.HasValue) q["Status"] = ((int)status.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedRegSPIResponse>($"{Base}/reg-spi?{q}", ct);
    }

    public Task<PagedRSPIResponse?> GetRSPIAsync(DateOnly? dateFrom = null, DateOnly? dateTo = null, AssetCategory? category = null, bool activeOnly = true, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (category.HasValue) q["Category"] = ((int)category.Value).ToString(CultureInfo.InvariantCulture);
        q["ActiveOnly"] = activeOnly ? "true" : "false";
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedRSPIResponse>($"{Base}/rspi?{q}", ct);
    }

    public Task<PropertyHistoryDto?> GetPropertyHistoryAsync(Guid propertyId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PropertyHistoryDto>($"{Base}/property-history/{propertyId}", ct);
}

// ─── PPE Track ───────────────────────────────────────────────────────────────

internal enum PPEReceiptNature { Purchase = 0, Transfer = 1, Donation = 2, Others = 3 }
internal enum PARType { NewPurchase = 0, Transfer = 1 }
internal enum PPEIssuanceType { TransferCO = 0, TransferRO = 1, TransferPO = 2, Donation = 3, Dumping = 4, Destruction = 5, Sale = 6, Others = 7 }
internal enum PPEReturnCategory { Serviceable = 0, Junked = 1 }

// PPE Receiving Reports (PPERR)

internal sealed record CreatePPERRItemRequest(
    string? ClassCode,
    string? ItemCode,
    string? PropertyCode,
    string Description,
    string? SerialNumber,
    DateOnly DateAcquired,
    int Quantity,
    decimal UnitCost,
    int EstimatedUsefulLifeYears);

internal sealed record CreatePPERRCommand(
    string PPERRNo,
    DateOnly Date,
    string ReceivedFrom,
    string Address,
    PPEReceiptNature ReceiptNature,
    Guid ReceivedByEmployeeId,
    Guid NotedByEmployeeId,
    IReadOnlyList<CreatePPERRItemRequest> Items);

internal sealed record CreatePPERRResult(Guid PPERRId, string PPERRNo, int PPEItemsCreated);

internal sealed record PPERRSummaryDto(Guid Id, string PPERRNo, DateOnly Date, string ReceivedFrom, string ReceiptNature, int ItemCount);

internal sealed record PPERRItemDto(Guid Id, int ItemNo, string PropertyCode, string Description, DateOnly DateAcquired, int Quantity, decimal UnitCost, decimal Amount);

internal sealed record PPERRDetailsDto(
    Guid Id, string PPERRNo, DateOnly Date,
    string ReceivedFrom, string Address, string ReceiptNature,
    Guid ReceivedByEmployeeId, Guid NotedByEmployeeId,
    DateTimeOffset CreatedOnUtc, string? CreatedBy,
    IReadOnlyList<PPERRItemDto> Items);

internal sealed record PagedPPERRResponse(IReadOnlyList<PPERRSummaryDto> Items, int PageNumber, int PageSize, int TotalCount);

internal interface IPPERRClient
{
    Task<PagedPPERRResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, PPEReceiptNature? receiptNature = null, int page = 1, int pageSize = 15, CancellationToken ct = default);
    Task<PPERRDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreatePPERRResult> CreateAsync(CreatePPERRCommand command, CancellationToken ct = default);
}

internal sealed class PPERRClient(HttpClient http) : IPPERRClient
{
    private const string Base = "api/v1/asset-management/ppe-receiving-reports";

    public Task<PagedPPERRResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, PPEReceiptNature? receiptNature = null, int page = 1, int pageSize = 15, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (receiptNature.HasValue) q["ReceiptNature"] = ((int)receiptNature.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedPPERRResponse>($"{Base}?{q}", ct)!;
    }

    public Task<PPERRDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PPERRDetailsDto>($"{Base}/{id}", ct);

    public async Task<CreatePPERRResult> CreateAsync(CreatePPERRCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreatePPERRResult>(ct))!;
    }
}

// Property Acknowledgement Receipts (PAR)

internal sealed record CreatePARItemRequest(Guid PPEItemId, int Quantity, string Unit, string ItemDescription);

internal sealed record CreatePARCommand(
    string PARNo, DateOnly Date, PARType PARType,
    Guid ReceivedFromEmployeeId, Guid ReceivedByEmployeeId, Guid ApprovedByEmployeeId,
    IReadOnlyList<CreatePARItemRequest> Items);

internal sealed record CreatePARResult(Guid PARId, string PARNo, int ItemCount);

internal sealed record PARSummaryDto(Guid Id, string PARNo, DateOnly Date, string PARType, Guid ReceivedByEmployeeId, int ItemCount);

internal sealed record PARItemDto(Guid Id, int ItemNo, Guid PPEItemId, string PropertyCode, string PropertyNumber, int Quantity, string Unit, string ItemDescription, decimal UnitCost, decimal TotalCost, int EstimatedUsefulLifeYears, DateOnly DateAcquired);

internal sealed record PARDetailsDto(
    Guid Id, string PARNo, DateOnly Date, string PARType,
    Guid ReceivedFromEmployeeId, Guid ReceivedByEmployeeId, Guid ApprovedByEmployeeId,
    DateTimeOffset CreatedOnUtc, string? CreatedBy,
    IReadOnlyList<PARItemDto> Items);

internal sealed record PagedPARResponse(IReadOnlyList<PARSummaryDto> Items, int PageNumber, int PageSize, int TotalCount);

internal interface IPARClient
{
    Task<PagedPARResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, PARType? parType = null, int page = 1, int pageSize = 15, CancellationToken ct = default);
    Task<PARDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreatePARResult> CreateAsync(CreatePARCommand command, CancellationToken ct = default);
}

internal sealed class PARClient(HttpClient http) : IPARClient
{
    private const string Base = "api/v1/asset-management/property-acknowledgement-receipts";

    public Task<PagedPARResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, PARType? parType = null, int page = 1, int pageSize = 15, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (parType.HasValue) q["PARType"] = ((int)parType.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedPARResponse>($"{Base}?{q}", ct)!;
    }

    public Task<PARDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PARDetailsDto>($"{Base}/{id}", ct);

    public async Task<CreatePARResult> CreateAsync(CreatePARCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreatePARResult>(ct))!;
    }
}

// PPE Issuance Reports (PPEIR)

internal sealed record CreatePPEIRItemRequest(Guid PPEItemId);

internal sealed record CreatePPEIRCommand(
    string PPEIRNo, DateOnly Date,
    Guid IssuedToEmployeeId, string IssuedToOfficeAddress, PPEIssuanceType IssuanceType,
    Guid IssuedByEmployeeId, Guid ReceivedByEmployeeId, Guid ApprovedByEmployeeId,
    DateOnly? DateReceived, string? DriverName, string? BillOfLadingNo,
    IReadOnlyList<CreatePPEIRItemRequest> Items);

internal sealed record CreatePPEIRResult(Guid PPEIRId, string PPEIRNo, int ItemCount);

internal sealed record PPEIRSummaryDto(Guid Id, string PPEIRNo, DateOnly Date, Guid IssuedToEmployeeId, string IssuanceType, int ItemCount);

internal sealed record PPEIRItemDto(Guid Id, int ItemNo, Guid PPEItemId, string PropertyCode, string? SerialNumber, string PPESpecification, DateOnly DateAcquired, decimal AcquisitionCost, decimal? AccumulatedDepreciation, decimal? BookValue);

internal sealed record PPEIRDetailsDto(
    Guid Id, string PPEIRNo, DateOnly Date,
    Guid IssuedToEmployeeId, string IssuedToOfficeAddress, string IssuanceType,
    Guid IssuedByEmployeeId, Guid ReceivedByEmployeeId, DateOnly? DateReceived, Guid ApprovedByEmployeeId,
    string? DriverName, string? BillOfLadingNo,
    DateTimeOffset CreatedOnUtc, string? CreatedBy,
    IReadOnlyList<PPEIRItemDto> Items);

internal sealed record PagedPPEIRResponse(IReadOnlyList<PPEIRSummaryDto> Items, int PageNumber, int PageSize, int TotalCount);

internal sealed record PPEIRItemDepreciationRequest(Guid ItemId, decimal AccumulatedDepreciation, decimal BookValue);
internal sealed record UpdatePPEIRDepreciationRequest(IReadOnlyList<PPEIRItemDepreciationRequest> Items);
internal sealed record UpdatePPEIRDepreciationResult(Guid PPEIRId, int ItemsUpdated);

// PTR (Property Transfer Report — derived from PPEIR, no separate entity)

internal sealed record PTRItemDto(
    int ItemNo,
    DateOnly DateAcquired,
    string PropertyNumber,
    string Description,
    decimal Amount,
    string? Condition,
    string? ReasonForTransfer);

internal sealed record PTRDto(
    string PTRNo,
    DateOnly Date,
    Guid FromAccountableOfficerId,
    Guid ToAccountableOfficerId,
    string ToOfficeAddress,
    string TransferType,
    Guid ApprovedByEmployeeId,
    Guid ReleasedByEmployeeId,
    Guid ReceivedByEmployeeId,
    IReadOnlyList<PTRItemDto> Items);

internal interface IPPEIRClient
{
    Task<PagedPPEIRResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, PPEIssuanceType? issuanceType = null, int page = 1, int pageSize = 15, CancellationToken ct = default);
    Task<PPEIRDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreatePPEIRResult> CreateAsync(CreatePPEIRCommand command, CancellationToken ct = default);
    Task<UpdatePPEIRDepreciationResult> UpdateDepreciationAsync(Guid ppeirId, UpdatePPEIRDepreciationRequest request, CancellationToken ct = default);
    Task<PTRDto?> GetPTRAsync(Guid ppeirId, CancellationToken ct = default);
}

internal sealed class PPEIRClient(HttpClient http) : IPPEIRClient
{
    private const string Base = "api/v1/asset-management/ppe-issuance-reports";

    public Task<PagedPPEIRResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, PPEIssuanceType? issuanceType = null, int page = 1, int pageSize = 15, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (issuanceType.HasValue) q["IssuanceType"] = ((int)issuanceType.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedPPEIRResponse>($"{Base}?{q}", ct)!;
    }

    public Task<PPEIRDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PPEIRDetailsDto>($"{Base}/{id}", ct);

    public async Task<CreatePPEIRResult> CreateAsync(CreatePPEIRCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreatePPEIRResult>(ct))!;
    }

    public async Task<UpdatePPEIRDepreciationResult> UpdateDepreciationAsync(Guid ppeirId, UpdatePPEIRDepreciationRequest request, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{ppeirId}/depreciation", request, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<UpdatePPEIRDepreciationResult>(ct))!;
    }

    public Task<PTRDto?> GetPTRAsync(Guid ppeirId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PTRDto>($"{Base}/{ppeirId}/ptr", ct);
}

// Receipts for Returned PPE (RRP)

internal sealed record CreateRRPItemRequest(Guid PPEItemId, string? SourceDocumentRef, int Quantity);

internal sealed record CreateRRPCommand(
    string RRPNo, DateOnly Date, PPEReturnCategory ReturnCategory,
    Guid ReturnedByEmployeeId, Guid ApprovedByEmployeeId, Guid SignedByEmployeeId,
    bool PropertyInspectorCertified,
    IReadOnlyList<CreateRRPItemRequest> Items);

internal sealed record CreateRRPResult(Guid RRPId, string RRPNo, int ItemCount);

internal sealed record RRPSummaryDto(Guid Id, string RRPNo, DateOnly Date, string ReturnCategory, Guid ReturnedByEmployeeId, int ItemCount);

internal sealed record RRPItemDto(Guid Id, int ItemNo, Guid PPEItemId, string? SourceDocumentRef, string PropertyCode, string Description, int Quantity, decimal UnitCost, decimal TotalCost);

internal sealed record RRPDetailsDto(
    Guid Id, string RRPNo, DateOnly Date, string ReturnCategory,
    Guid ReturnedByEmployeeId, Guid ApprovedByEmployeeId, Guid SignedByEmployeeId,
    bool PropertyInspectorCertified,
    DateTimeOffset CreatedOnUtc, string? CreatedBy,
    IReadOnlyList<RRPItemDto> Items);

internal sealed record PagedRRPResponse(IReadOnlyList<RRPSummaryDto> Items, int PageNumber, int PageSize, int TotalCount);

internal interface IRRPClient
{
    Task<PagedRRPResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, PPEReturnCategory? returnCategory = null, int page = 1, int pageSize = 15, CancellationToken ct = default);
    Task<RRPDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CreateRRPResult> CreateAsync(CreateRRPCommand command, CancellationToken ct = default);
}

internal sealed class RRPClient(HttpClient http) : IRRPClient
{
    private const string Base = "api/v1/asset-management/receipts-for-returned-ppe";

    public Task<PagedRRPResponse> SearchAsync(string? keyword = null, DateOnly? dateFrom = null, DateOnly? dateTo = null, PPEReturnCategory? returnCategory = null, int page = 1, int pageSize = 15, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (dateFrom.HasValue) q["DateFrom"] = dateFrom.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dateTo.HasValue) q["DateTo"] = dateTo.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (returnCategory.HasValue) q["ReturnCategory"] = ((int)returnCategory.Value).ToString(CultureInfo.InvariantCulture);
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedRRPResponse>($"{Base}?{q}", ct)!;
    }

    public Task<RRPDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<RRPDetailsDto>($"{Base}/{id}", ct);

    public async Task<CreateRRPResult> CreateAsync(CreateRRPCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreateRRPResult>(ct))!;
    }
}

// PPE Items (read-only + autocomplete)

internal sealed record PPEItemSummaryDto(Guid Id, string PropertyCode, string PropertyNumber, string Description, string? SerialNumber, DateOnly DateAcquired, decimal UnitCost, string Status, Guid? CurrentAccountableEmployeeId);

internal sealed record PPEItemDetailsDto(Guid Id, string PropertyCode, string PropertyNumber, string Description, string? SerialNumber, DateOnly DateAcquired, decimal UnitCost, int EstimatedUsefulLifeYears, string Status, Guid? CurrentAccountableEmployeeId, Guid? SourcePPERRId, DateTimeOffset CreatedOnUtc, string? CreatedBy);

internal sealed record PagedPPEItemResponse(IReadOnlyList<PPEItemSummaryDto> Items, int PageNumber, int PageSize, int TotalCount);

internal interface IPPEItemClient
{
    Task<PagedPPEItemResponse> SearchAsync(string? keyword = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<PPEItemDetailsDto?> GetAsync(Guid id, CancellationToken ct = default);
}

internal sealed class PPEItemClient(HttpClient http) : IPPEItemClient
{
    private const string Base = "api/v1/asset-management/ppe-items";

    public Task<PagedPPEItemResponse> SearchAsync(string? keyword = null, string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (!string.IsNullOrWhiteSpace(status)) q["Status"] = status;
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedPPEItemResponse>($"{Base}?{q}", ct)!;
    }

    public Task<PPEItemDetailsDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PPEItemDetailsDto>($"{Base}/{id}", ct);
}

// ─── Physical Count ──────────────────────────────────────────────────────────

internal sealed record CreatePhysicalCountSessionRequest(
    string SessionNo,
    DateOnly CountDate,
    string StationOffice,
    string Scope,
    Guid PreparedByEmployeeId,
    Guid CertifiedByEmployeeId,
    Guid ApprovedByEmployeeId);

internal sealed record CreatePhysicalCountSessionResult(Guid SessionId, string SessionNo, int EntriesCreated);

internal sealed record PhysicalCountSessionSummaryDto(
    Guid Id, string SessionNo, DateOnly CountDate, string StationOffice,
    string Scope, string Status,
    int TotalEntries, int Found, int NotFound, int FoundAtStation, int Pending,
    DateTimeOffset CreatedOnUtc);

internal sealed record PhysicalCountEntryDto(
    Guid Id,
    Guid? PPEItemId,
    Guid? SemiExpendablePropertyId,
    string PropertyNumber,
    string Description,
    decimal UnitCost,
    string? Result,
    string? Condition,
    int QuantityOnHand,
    string? Remarks,
    bool IsScanned,
    DateTimeOffset? ScannedOnUtc,
    string? PhotoPath);

internal sealed record PhysicalCountSessionDetailsDto(
    Guid Id, string SessionNo, DateOnly CountDate, string StationOffice,
    string Scope, string Status,
    Guid PreparedByEmployeeId, Guid CertifiedByEmployeeId, Guid ApprovedByEmployeeId,
    DateTimeOffset? SubmittedOnUtc,
    DateTimeOffset CreatedOnUtc, string? CreatedBy,
    IReadOnlyList<PhysicalCountEntryDto> Entries);

internal sealed record PagedPhysicalCountSessionResponse(
    IReadOnlyList<PhysicalCountSessionSummaryDto> Items, int PageNumber, int PageSize, int TotalCount);

internal sealed record RecordPhysicalCountEntryRequest(
    string Result,
    string? Condition,
    int QuantityOnHand,
    string? Remarks,
    bool IsScanned,
    string? PhotoPath);

internal sealed record RecordPhysicalCountEntryResult(
    Guid EntryId, string PropertyNumber, string Result, string? Condition, bool IsScanned);

internal sealed record AddFoundAtStationEntryRequest(
    string PropertyNumber,
    string Description,
    decimal UnitCost,
    string Condition,
    string? Remarks,
    string? PhotoPath);

internal sealed record AddFoundAtStationEntryResult(Guid EntryId, string PropertyNumber);

internal sealed record SubmitPhysicalCountSessionResult(
    Guid SessionId, string SessionNo,
    int TotalEntries, int Found, int NotFound, int FoundAtStation,
    DateTimeOffset SubmittedOnUtc);

// RPCPPE report DTOs
internal sealed record RPCPPELineItemDto(
    int LineNo, string PropertyCode, string Description, string PropertyNumber,
    DateOnly DateAcquired, decimal UnitCost,
    decimal? AccumulatedDepreciation, decimal? BookValue,
    int QuantityPerCard, int QuantityOnHand, int Shortage, int Overage,
    string? Condition, string? Remarks, string Result, bool IsScanned);

internal sealed record RPCPPESummaryDto(
    int TotalItems, int Found, int NotFound, int FoundAtStation, int Pending,
    decimal TotalUnitCost, decimal? TotalAccumulatedDepreciation, decimal? TotalBookValue,
    int TotalShortage, int TotalOverage);

internal sealed record RPCPPEReportDto(
    Guid SessionId, string SessionNo, DateOnly CountDate, string StationOffice,
    Guid PreparedByEmployeeId, Guid CertifiedByEmployeeId, Guid ApprovedByEmployeeId,
    DateTimeOffset? SubmittedOnUtc,
    IReadOnlyList<RPCPPELineItemDto> Items,
    RPCPPESummaryDto Summary);

internal interface IPhysicalCountClient
{
    Task<PagedPhysicalCountSessionResponse> SearchAsync(string? keyword = null, int page = 1, int pageSize = 15, string? status = null, CancellationToken ct = default);
    Task<PhysicalCountSessionDetailsDto?> GetAsync(Guid sessionId, CancellationToken ct = default);
    Task<CreatePhysicalCountSessionResult> CreateAsync(CreatePhysicalCountSessionRequest request, CancellationToken ct = default);
    Task<RecordPhysicalCountEntryResult> RecordEntryAsync(Guid sessionId, Guid entryId, RecordPhysicalCountEntryRequest request, CancellationToken ct = default);
    Task<AddFoundAtStationEntryResult> AddFoundAtStationAsync(Guid sessionId, AddFoundAtStationEntryRequest request, CancellationToken ct = default);
    Task<SubmitPhysicalCountSessionResult> SubmitAsync(Guid sessionId, CancellationToken ct = default);
    Task<RPCPPEReportDto?> GetRPCPPEAsync(Guid sessionId, CancellationToken ct = default);
}

internal sealed class PhysicalCountClient(HttpClient http) : IPhysicalCountClient
{
    private const string Base = "api/v1/asset-management/physical-count";

    public Task<PagedPhysicalCountSessionResponse> SearchAsync(string? keyword = null, int page = 1, int pageSize = 15, string? status = null, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (!string.IsNullOrWhiteSpace(status))  q["Status"]  = status;
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"]   = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedPhysicalCountSessionResponse>($"{Base}?{q}", ct)!;
    }

    public Task<PhysicalCountSessionDetailsDto?> GetAsync(Guid sessionId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<PhysicalCountSessionDetailsDto>($"{Base}/{sessionId}", ct);

    public async Task<CreatePhysicalCountSessionResult> CreateAsync(CreatePhysicalCountSessionRequest request, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, request, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<CreatePhysicalCountSessionResult>(ct))!;
    }

    public async Task<RecordPhysicalCountEntryResult> RecordEntryAsync(Guid sessionId, Guid entryId, RecordPhysicalCountEntryRequest request, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{sessionId}/entries/{entryId}", request, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<RecordPhysicalCountEntryResult>(ct))!;
    }

    public async Task<AddFoundAtStationEntryResult> AddFoundAtStationAsync(Guid sessionId, AddFoundAtStationEntryRequest request, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"{Base}/{sessionId}/found-at-station", request, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AddFoundAtStationEntryResult>(ct))!;
    }

    public async Task<SubmitPhysicalCountSessionResult> SubmitAsync(Guid sessionId, CancellationToken ct = default)
    {
        using var r = await http.PostAsync($"{Base}/{sessionId}/submit", null, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<SubmitPhysicalCountSessionResult>(ct))!;
    }

    public Task<RPCPPEReportDto?> GetRPCPPEAsync(Guid sessionId, CancellationToken ct = default) =>
        http.GetFromJsonAsync<RPCPPEReportDto>($"{Base}/{sessionId}/rpcppe", ct);
}

// Tangible Items

internal sealed record RegisterTangibleItemCommand(
    Guid ItemId,
    string PropertyNo,
    string PropertyClass,
    string CategoryCode,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    string? Remarks = null);

internal sealed record UpdateTangibleItemCommand(
    Guid Id,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    string? Remarks = null);

internal sealed record TangibleItemDto(
    Guid Id,
    string PropertyNo,
    string PropertyClass,
    string CategoryCode,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    string? Remarks,
    DateTimeOffset CreatedOnUtc);

internal sealed record TangibleItemSummaryDto(
    Guid Id,
    string PropertyNo,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    DateOnly AcquisitionDate,
    int Quantity,
    decimal UnitCost,
    string? Remarks);

internal sealed record PagedTangibleItemsResponse(
    IReadOnlyList<TangibleItemSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

internal interface ITangibleItemClient
{
    Task<PagedTangibleItemsResponse> SearchAsync(string? keyword = null, bool? excludeLinked = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<TangibleItemDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<TangibleItemDto> RegisterAsync(RegisterTangibleItemCommand command, CancellationToken ct = default);
    Task<TangibleItemDto> UpdateAsync(Guid id, UpdateTangibleItemCommand command, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> GetNextSequenceAsync(int year, string officeCode, string classCode, string itemCode, CancellationToken ct = default);
}

internal sealed class TangibleItemClient(HttpClient http) : ITangibleItemClient
{
    private const string Base = "api/v1/asset-management/tangible-items";

    public Task<PagedTangibleItemsResponse> SearchAsync(string? keyword = null, bool? excludeLinked = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(keyword)) q["Keyword"] = keyword;
        if (excludeLinked.HasValue) q["ExcludeLinked"] = excludeLinked.Value.ToString().ToLowerInvariant();
        q["PageNumber"] = page.ToString(CultureInfo.InvariantCulture);
        q["PageSize"] = pageSize.ToString(CultureInfo.InvariantCulture);
        return http.GetFromJsonAsync<PagedTangibleItemsResponse>($"{Base}?{q}", ct)!;
    }

    public Task<TangibleItemDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        http.GetFromJsonAsync<TangibleItemDto>($"{Base}/{id}", ct);

    public async Task<TangibleItemDto> RegisterAsync(RegisterTangibleItemCommand command, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync(Base, command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<TangibleItemDto>(ct))!;
    }

    public async Task<TangibleItemDto> UpdateAsync(Guid id, UpdateTangibleItemCommand command, CancellationToken ct = default)
    {
        using var r = await http.PutAsJsonAsync($"{Base}/{id}", command, ct);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<TangibleItemDto>(ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        using var r = await http.DeleteAsync($"{Base}/{id}", ct);
        r.EnsureSuccessStatusCode();
    }

    public async Task<int> GetNextSequenceAsync(int year, string officeCode, string classCode, string itemCode, CancellationToken ct = default)
    {
        var q = HttpUtility.ParseQueryString(string.Empty);
        q["year"] = year.ToString(CultureInfo.InvariantCulture);
        q["officeCode"] = officeCode;
        q["classCode"] = classCode;
        q["itemCode"] = itemCode;
        var result = await http.GetFromJsonAsync<NextSequenceResponse>($"{Base}/next-sequence?{q}", ct);
        return result?.NextSequence ?? 1;
    }

    private sealed record NextSequenceResponse(int NextSequence);
}
