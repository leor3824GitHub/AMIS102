using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.GetPPERRList;

public sealed record GetPPERRListQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    PPEReceiptNature? ReceiptNature,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedPPERRResponse>;

public sealed record PagedPPERRResponse(
    IReadOnlyList<PPERRSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record PPERRSummaryDto(
    Guid Id,
    string PPERRNo,
    DateOnly Date,
    string ReceivedFrom,
    string ReceiptNature,
    int ItemCount);
