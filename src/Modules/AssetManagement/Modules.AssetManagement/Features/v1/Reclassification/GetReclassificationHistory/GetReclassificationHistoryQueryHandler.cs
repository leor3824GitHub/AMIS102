using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Reclassification.GetReclassificationHistory;

public sealed class GetReclassificationHistoryQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetReclassificationHistoryQuery, PagedReclassificationHistoryResponse>
{
    public async ValueTask<PagedReclassificationHistoryResponse> Handle(
        GetReclassificationHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 20 : query.PageSize;

        var q = dbContext.ReclassificationRecords.AsQueryable();

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ReclassificationRecordDto(
                x.Id,
                x.ThresholdId,
                x.TotalReclassified,
                x.Notes,
                x.CreatedOnUtc,
                x.CreatedBy))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedReclassificationHistoryResponse(items, pageNumber, pageSize, totalCount);
    }
}
