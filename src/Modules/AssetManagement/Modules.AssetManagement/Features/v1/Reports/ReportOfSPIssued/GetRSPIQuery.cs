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
    IReadOnlyList<RSPIItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record RSPIItemDto(
    Guid ICSId,
    string ICSNo,
    DateOnly ICSDate,
    string ICSStatus,
    Guid ReceivedByEmployeeId,
    Guid? IssuedFromEmployeeId,
    Guid TangibleInventoryItemId,
    string PropertyNo,
    string ItemCode,
    string ItemName,
    string AssetType,
    decimal UnitCost,
    DateOnly? ExpiresOn);
