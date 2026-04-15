using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptsForReturnedPPE.GetRRPList;

public sealed class GetRRPListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetRRPListQuery, PagedRRPResponse>
{
    public async ValueTask<PagedRRPResponse> Handle(GetRRPListQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.ReceiptsForReturnedPPE.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x => x.RRPNo.ToLower().Contains(kw));
        }

        if (query.DateFrom.HasValue)          q = q.Where(x => x.Date >= query.DateFrom.Value);
        if (query.DateTo.HasValue)            q = q.Where(x => x.Date <= query.DateTo.Value);
        if (query.ReturnCategory.HasValue)    q = q.Where(x => x.ReturnCategory == query.ReturnCategory.Value);
        if (query.ReturnedByEmployeeId.HasValue) q = q.Where(x => x.ReturnedByEmployeeId == query.ReturnedByEmployeeId.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var ids = await q
            .OrderByDescending(x => x.Date).ThenBy(x => x.RRPNo)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var itemCounts = await dbContext.RRPItems
            .Where(x => ids.Contains(x.RRPId))
            .GroupBy(x => x.RRPId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var rows = await q
            .OrderByDescending(x => x.Date).ThenBy(x => x.RRPNo)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new RRPSummaryDto(x.Id, x.RRPNo, x.Date, x.ReturnCategory.ToString(), x.ReturnedByEmployeeId, 0))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var result = rows.Select(x => x with { ItemCount = itemCounts.GetValueOrDefault(x.Id) }).ToList();

        return new PagedRRPResponse(result, pageNumber, pageSize, totalCount);
    }
}
