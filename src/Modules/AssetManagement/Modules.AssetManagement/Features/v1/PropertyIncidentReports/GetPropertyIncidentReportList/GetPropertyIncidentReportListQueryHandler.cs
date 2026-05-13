using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.PropertyIncidentReports.GetPropertyIncidentReportList;

public sealed class GetPropertyIncidentReportListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPropertyIncidentReportListQuery, PagedPropertyIncidentReportListResponse>
{
    public async ValueTask<PagedPropertyIncidentReportListResponse> Handle(
        GetPropertyIncidentReportListQuery query,
        CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var q = dbContext.PropertyIncidentReports.AsQueryable();

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

        if (query.IncidentType.HasValue)
        {
            q = q.Where(x => x.IncidentType == query.IncidentType.Value);
        }

        if (query.AccountableEmployeeId.HasValue)
        {
            q = q.Where(x => x.AccountableEmployeeId == query.AccountableEmployeeId.Value);
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

        var itemCounts = await dbContext.PropertyIncidentItems
            .Where(x => reportIds.Contains(x.ReportId))
            .GroupBy(x => x.ReportId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var items = await dbContext.PropertyIncidentReports
            .Where(x => reportIds.Contains(x.Id))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Select(x => new PropertyIncidentReportSummaryDto(
                x.Id,
                x.ReportNo,
                x.Date,
                x.IncidentDate,
                x.IncidentType.ToString(),
                x.AccountableEmployeeId,
                0,
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = items
            .Select(x => x with { ItemCount = itemCounts.GetValueOrDefault(x.Id) })
            .ToList();

        return new PagedPropertyIncidentReportListResponse(result, pageNumber, pageSize, totalCount);
    }
}

