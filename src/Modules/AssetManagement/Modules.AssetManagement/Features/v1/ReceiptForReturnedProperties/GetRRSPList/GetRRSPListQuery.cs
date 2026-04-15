using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.GetRRSPList;

public sealed record GetRRSPListQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    Guid? ICSId,
    Guid? ReturnedByEmployeeId,
    int PageNumber = 1,
    int PageSize   = 10) : IQuery<PagedRRSPListResponse>;

public sealed record PagedRRSPListResponse(
    IReadOnlyList<RRSPSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record RRSPSummaryDto(
    Guid Id,
    string RRSPNo,
    DateOnly Date,
    Guid ICSId,
    string ICSNo,
    Guid? ReceivedByEmployeeId,
    Guid ReturnedByEmployeeId,
    int ItemCount,
    DateTimeOffset CreatedOnUtc);
