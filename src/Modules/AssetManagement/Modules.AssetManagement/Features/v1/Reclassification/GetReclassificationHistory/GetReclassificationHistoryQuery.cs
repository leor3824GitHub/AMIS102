using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.Reclassification.GetReclassificationHistory;

public sealed record GetReclassificationHistoryQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedReclassificationHistoryResponse>;

public sealed record ReclassificationRecordDto(
    Guid Id,
    Guid ThresholdId,
    int TotalReclassified,
    string? Notes,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy);

public sealed record PagedReclassificationHistoryResponse(
    IReadOnlyList<ReclassificationRecordDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

