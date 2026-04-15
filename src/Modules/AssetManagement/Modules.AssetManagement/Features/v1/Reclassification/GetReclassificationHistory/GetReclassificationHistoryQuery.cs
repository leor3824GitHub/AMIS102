using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Reclassification.GetReclassificationHistory;

public sealed record GetReclassificationHistoryQuery(int PageNumber = 1, int PageSize = 20)
    : IQuery<PagedReclassificationHistoryResponse>;

public sealed record ReclassificationRecordDto(
    Guid Id,
    Guid PolicyId,
    int TotalReclassified,
    string? Notes,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy);

public sealed record PagedReclassificationHistoryResponse(
    IReadOnlyList<ReclassificationRecordDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
