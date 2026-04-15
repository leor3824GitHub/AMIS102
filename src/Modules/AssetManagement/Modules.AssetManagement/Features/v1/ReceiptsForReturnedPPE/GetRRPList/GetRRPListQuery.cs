using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.GetRRPList;

public sealed record GetRRPListQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    PPEReturnCategory? ReturnCategory,
    Guid? ReturnedByEmployeeId,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedRRPResponse>;

public sealed record PagedRRPResponse(
    IReadOnlyList<RRPSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record RRPSummaryDto(
    Guid Id,
    string RRPNo,
    DateOnly Date,
    string ReturnCategory,
    Guid ReturnedByEmployeeId,
    int ItemCount);
