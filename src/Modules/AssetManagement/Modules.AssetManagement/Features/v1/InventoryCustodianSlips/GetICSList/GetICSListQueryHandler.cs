using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSList;

public sealed class GetICSListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetICSListQuery, PagedICSListResponse>
{
    public async ValueTask<PagedICSListResponse> Handle(GetICSListQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.InventoryCustodianSlips.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x => x.ICSNo.ToLower().Contains(kw));
        }

        if (query.DateFrom.HasValue)
        {
            q = q.Where(x => x.Date >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            q = q.Where(x => x.Date <= query.DateTo.Value);
        }

        if (query.ReceivedByEmployeeId.HasValue)
        {
            q = q.Where(x => x.ReceivedByEmployeeId == query.ReceivedByEmployeeId.Value);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var icsIds = await q
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.ICSNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemCounts = await dbContext.ICSItems
            .Where(x => icsIds.Contains(x.ICSId))
            .GroupBy(x => x.ICSId)
            .Select(g => new { ICSId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ICSId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var slips = await q
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.ICSNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ICSSummaryDto(
                x.Id,
                x.ICSNo,
                x.Date,
                x.FundCluster,
                x.IssuedFromEmployeeId,
                x.ReceivedByEmployeeId,
                0))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = slips
            .Select(x => x with { ItemCount = itemCounts.GetValueOrDefault(x.Id) })
            .ToList();

        return new PagedICSListResponse(result, pageNumber, pageSize, totalCount);
    }
}
