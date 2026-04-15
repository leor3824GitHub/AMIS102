using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPPEIRList;

public sealed class GetPPEIRListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPPEIRListQuery, PagedPPEIRResponse>
{
    public async ValueTask<PagedPPEIRResponse> Handle(GetPPEIRListQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.PPEIssuanceReports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x => x.PPEIRNo.ToLower().Contains(kw));
        }

        if (query.DateFrom.HasValue)    q = q.Where(x => x.Date >= query.DateFrom.Value);
        if (query.DateTo.HasValue)      q = q.Where(x => x.Date <= query.DateTo.Value);
        if (query.IssuanceType.HasValue) q = q.Where(x => x.IssuanceType == query.IssuanceType.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var ids = await q
            .OrderByDescending(x => x.Date).ThenBy(x => x.PPEIRNo)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var itemCounts = await dbContext.PPEIRItems
            .Where(x => ids.Contains(x.PPEIRId))
            .GroupBy(x => x.PPEIRId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var rows = await q
            .OrderByDescending(x => x.Date).ThenBy(x => x.PPEIRNo)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new PPEIRSummaryDto(x.Id, x.PPEIRNo, x.Date, x.IssuedToEmployeeId, x.IssuanceType.ToString(), 0))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var result = rows.Select(x => x with { ItemCount = itemCounts.GetValueOrDefault(x.Id) }).ToList();

        return new PagedPPEIRResponse(result, pageNumber, pageSize, totalCount);
    }
}
