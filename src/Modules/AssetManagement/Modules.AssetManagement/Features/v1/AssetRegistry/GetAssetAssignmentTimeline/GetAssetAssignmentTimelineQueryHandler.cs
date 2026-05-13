using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.AssetRegistryQueries.GetAssetAssignmentTimeline;

public sealed class GetAssetAssignmentTimelineQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetAssetAssignmentTimelineQuery, PagedAssetAssignmentTimelineResponse>
{
    public async ValueTask<PagedAssetAssignmentTimelineResponse> Handle(
        GetAssetAssignmentTimelineQuery query,
        CancellationToken cancellationToken)
    {
        var q =
            from history in dbContext.AssetAssignmentHistory
            join location in dbContext.Locations on history.LocationId equals location.Id into locations
            from location in locations.DefaultIfEmpty()
            where history.AssetRegistryId == query.AssetRegistryId
            select new AssetAssignmentTimelineItemDto(
                history.Id,
                history.EventType.ToString(),
                history.OccurredOnUtc,
                history.SourceDocumentType,
                history.SourceDocumentId,
                history.SourceDocumentNo,
                history.FromCustodianId,
                history.ToCustodianId,
                history.LocationId,
                location != null ? location.Name : null,
                history.Remarks);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var items = await q
            .OrderByDescending(x => x.OccurredOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedAssetAssignmentTimelineResponse(items, pageNumber, pageSize, totalCount);
    }
}
