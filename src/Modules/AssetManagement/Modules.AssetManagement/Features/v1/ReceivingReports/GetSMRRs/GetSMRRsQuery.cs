using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.ReceivingReports.GetSMRRs;

public sealed record GetSMRRsQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    ReceiptType? ReceiptType,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedSMRRsResponse>;

public sealed record PagedSMRRsResponse(
    IReadOnlyList<SMRRSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record SMRRSummaryDto(
    Guid Id,
    string SMRRNo,
    DateOnly Date,
    string ReceivedFrom,
    string ReceiptType,
    string? FundCluster,
    int ItemCount);
