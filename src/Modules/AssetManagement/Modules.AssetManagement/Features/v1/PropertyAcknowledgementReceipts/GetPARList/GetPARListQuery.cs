using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.GetPARList;

public sealed record GetPARListQuery(
    string? Keyword,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    PARType? PARType,
    Guid? ReceivedByEmployeeId,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedPARResponse>;

public sealed record PagedPARResponse(
    IReadOnlyList<PARSummaryDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record PARSummaryDto(
    Guid Id,
    string PARNo,
    DateOnly Date,
    string PARType,
    Guid ReceivedByEmployeeId,
    int ItemCount);
