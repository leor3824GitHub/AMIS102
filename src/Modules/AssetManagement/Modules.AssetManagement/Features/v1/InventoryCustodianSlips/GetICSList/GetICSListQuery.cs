using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSList;

public sealed record GetICSListQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    Guid? ReceivedByEmployeeId,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedICSListResponse>;

public sealed record PagedICSListResponse(
    IReadOnlyList<ICSSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record ICSSummaryDto(
    Guid Id,
    string ICSNo,
    DateOnly Date,
    string? FundCluster,
    Guid? IssuedFromEmployeeId,
    Guid ReceivedByEmployeeId,
    int ItemCount);
