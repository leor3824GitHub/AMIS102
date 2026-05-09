using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.ReportOfSPIssued;

/// <summary>
/// Generates a Report of Semi-Expendable Property Issued (RSPI) for a period.
/// Shows all ICS line items issued within the date range (or all if no range given),
/// filtered to Active ICS by default. Paginated.
/// </summary>
public sealed record GetRSPIQuery(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    AssetType? AssetType,
    bool ActiveOnly = true,
    int PageNumber  = 1,
    int PageSize    = 20) : IQuery<PagedRSPIResponse>;

public sealed record PagedRSPIResponse(
    IReadOnlyList<RSPISignatoryDto> Signatories,
    IReadOnlyList<RSPISectionDto> Sections,
    IReadOnlyList<RSPIItemDto> Items,
    int PageLineCount,
    decimal PageAmountTotal,
    int PageNumber,
    int PageSize,
    int TotalCount,
    decimal OverallAmountTotal);

public sealed record RSPISignatoryDto(
    int SortOrder,
    string Label,
    string Name,
    string Title);

public sealed record RSPISectionDto(
    Guid ICSId,
    string ICSNo,
    DateOnly ICSDate,
    string? FundCluster,
    string ICSStatus,
    int LineCount,
    decimal AmountTotal);

public sealed record RSPIItemDto(
    Guid ICSId,
    string ICSNo,
    DateOnly ICSDate,
    string ICSStatus,
    string? FundCluster,
    Guid ReceivedByEmployeeId,
    string ReceivedByEmployeeName,
    string? ReceivedByEmployeePositionName,
    string? ReceivedByEmployeeOfficeName,
    Guid? IssuedFromEmployeeId,
    string? IssuedFromEmployeeName,
    string? IssuedFromEmployeePositionName,
    string? IssuedFromEmployeeOfficeName,
    Guid TangibleInventoryItemId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string AssetType,
    decimal UnitCost,
    DateOnly? ExpiresOn);
