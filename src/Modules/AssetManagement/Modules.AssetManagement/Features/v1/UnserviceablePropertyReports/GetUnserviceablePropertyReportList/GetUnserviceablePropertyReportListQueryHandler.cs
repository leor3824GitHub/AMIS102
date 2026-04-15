using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.UnserviceablePropertyReports.GetUnserviceablePropertyReportList;

public sealed class GetUnserviceablePropertyReportListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetUnserviceablePropertyReportListQuery, PagedUnserviceablePropertyReportListResponse>
{
    public async ValueTask<PagedUnserviceablePropertyReportListResponse> Handle(
        GetUnserviceablePropertyReportListQuery query,
        CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var q = dbContext.UnserviceablePropertyReports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            q = q.Where(x => x.ReportNo.Contains(query.Keyword));
        }

        if (query.DateFrom.HasValue)
        {
            q = q.Where(x => x.Date >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            q = q.Where(x => x.Date <= query.DateTo.Value);
        }

        if (query.DisposalMethod.HasValue)
        {
            q = q.Where(x => x.DisposalMethod == query.DisposalMethod.Value);
        }

        if (query.InspectedByEmployeeId.HasValue)
        {
            q = q.Where(x => x.InspectedByEmployeeId == query.InspectedByEmployeeId.Value);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var reportIds = await q
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemCounts = await dbContext.UnserviceablePropertyItems
            .Where(x => reportIds.Contains(x.ReportId))
            .GroupBy(x => x.ReportId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var items = await dbContext.UnserviceablePropertyReports
            .Where(x => reportIds.Contains(x.Id))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Select(x => new UnserviceablePropertyReportSummaryDto(
                x.Id,
                x.ReportNo,
                x.Date,
                x.DisposalMethod.ToString(),
                x.InspectedByEmployeeId,
                x.ApprovedByEmployeeId,
                0,
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = items
            .Select(x => x with { ItemCount = itemCounts.GetValueOrDefault(x.Id) })
            .ToList();

        return new PagedUnserviceablePropertyReportListResponse(result, pageNumber, pageSize, totalCount);
    }
}
