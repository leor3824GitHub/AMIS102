using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.AssetRegistryQueries.GetAssetAssignmentTimeline;

public sealed record GetAssetAssignmentTimelineQuery(
    Guid AssetRegistryId,
    int PageNumber = 1,
    int PageSize = 50) : IQuery<PagedAssetAssignmentTimelineResponse>;

public sealed record PagedAssetAssignmentTimelineResponse(
    IReadOnlyList<AssetAssignmentTimelineItemDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record AssetAssignmentTimelineItemDto(
    Guid Id,
    string EventType,
    DateTimeOffset OccurredOnUtc,
    string SourceDocumentType,
    Guid SourceDocumentId,
    string SourceDocumentNo,
    Guid? FromCustodianId,
    Guid? ToCustodianId,
    Guid? LocationId,
    string? LocationName,
    string? Remarks);