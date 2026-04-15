using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.GetPARList;

public sealed class GetPARListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPARListQuery, PagedPARResponse>
{
    public async ValueTask<PagedPARResponse> Handle(GetPARListQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.PropertyAcknowledgementReceipts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x => x.PARNo.ToLower().Contains(kw));
        }

        if (query.DateFrom.HasValue) q = q.Where(x => x.Date >= query.DateFrom.Value);
        if (query.DateTo.HasValue)   q = q.Where(x => x.Date <= query.DateTo.Value);
        if (query.PARType.HasValue)  q = q.Where(x => x.PARType == query.PARType.Value);
        if (query.ReceivedByEmployeeId.HasValue) q = q.Where(x => x.ReceivedByEmployeeId == query.ReceivedByEmployeeId.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var ids = await q
            .OrderByDescending(x => x.Date).ThenBy(x => x.PARNo)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var itemCounts = await dbContext.PARItems
            .Where(x => ids.Contains(x.PARId))
            .GroupBy(x => x.PARId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var rows = await q
            .OrderByDescending(x => x.Date).ThenBy(x => x.PARNo)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new PARSummaryDto(x.Id, x.PARNo, x.Date, x.PARType.ToString(), x.ReceivedByEmployeeId, 0))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var result = rows.Select(x => x with { ItemCount = itemCounts.GetValueOrDefault(x.Id) }).ToList();

        return new PagedPARResponse(result, pageNumber, pageSize, totalCount);
    }
}
