using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PPEReceivingReports.GetPPERRList;

public sealed class GetPPERRListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPPERRListQuery, PagedPPERRResponse>
{
    public async ValueTask<PagedPPERRResponse> Handle(GetPPERRListQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.PPEReceivingReports.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x => x.PPERRNo.ToLower().Contains(kw) || x.ReceivedFrom.ToLower().Contains(kw));
        }

        if (query.DateFrom.HasValue) q = q.Where(x => x.Date >= query.DateFrom.Value);
        if (query.DateTo.HasValue)   q = q.Where(x => x.Date <= query.DateTo.Value);
        if (query.ReceiptNature.HasValue) q = q.Where(x => x.ReceiptNature == query.ReceiptNature.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var ids = await q
            .OrderByDescending(x => x.Date).ThenBy(x => x.PPERRNo)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var itemCounts = await dbContext.PPERRItems
            .Where(x => ids.Contains(x.PPERRId))
            .GroupBy(x => x.PPERRId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var rows = await q
            .OrderByDescending(x => x.Date).ThenBy(x => x.PPERRNo)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new PPERRSummaryDto(x.Id, x.PPERRNo, x.Date, x.ReceivedFrom, x.ReceiptNature.ToString(), 0))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var result = rows.Select(x => x with { ItemCount = itemCounts.GetValueOrDefault(x.Id) }).ToList();

        return new PagedPPERRResponse(result, pageNumber, pageSize, totalCount);
    }
}
