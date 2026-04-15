using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.GetRRSPList;

public sealed class GetRRSPListQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetRRSPListQuery, PagedRRSPListResponse>
{
    public async ValueTask<PagedRRSPListResponse> Handle(GetRRSPListQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var q = dbContext.ReceiptForReturnedProperties.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            q = q.Where(x => x.RRSPNo.Contains(query.Keyword));
        }

        if (query.DateFrom.HasValue)
        {
            q = q.Where(x => x.Date >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            q = q.Where(x => x.Date <= query.DateTo.Value);
        }

        if (query.ICSId.HasValue)
        {
            q = q.Where(x => x.ICSId == query.ICSId.Value);
        }

        if (query.ReturnedByEmployeeId.HasValue)
        {
            q = q.Where(x => x.ReturnedByEmployeeId == query.ReturnedByEmployeeId.Value);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var rrspIds = await q
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemCounts = await dbContext.RRSPItems
            .Where(x => rrspIds.Contains(x.RRSPId))
            .GroupBy(x => x.RRSPId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        // Load ICS numbers for display.
        var icsIds = await dbContext.ReceiptForReturnedProperties
            .Where(x => rrspIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ICSId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var icsIdSet = icsIds.Select(x => x.ICSId).Distinct().ToList();

        var icsNoMap = await dbContext.InventoryCustodianSlips
            .IgnoreQueryFilters()
            .Where(x => icsIdSet.Contains(x.Id))
            .Select(x => new { x.Id, x.ICSNo })
            .ToDictionaryAsync(x => x.Id, x => x.ICSNo, cancellationToken)
            .ConfigureAwait(false);

        var rrspIcsMap = icsIds.ToDictionary(x => x.Id, x => x.ICSId);

        var items = await dbContext.ReceiptForReturnedProperties
            .Where(x => rrspIds.Contains(x.Id))
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Select(x => new RRSPSummaryDto(
                x.Id,
                x.RRSPNo,
                x.Date,
                x.ICSId,
                string.Empty,
                x.ReceivedByEmployeeId,
                x.ReturnedByEmployeeId,
                0,
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = items
            .Select(x => x with
            {
                ItemCount = itemCounts.GetValueOrDefault(x.Id),
                ICSNo     = icsNoMap.GetValueOrDefault(x.ICSId, string.Empty),
            })
            .ToList();

        return new PagedRRSPListResponse(result, pageNumber, pageSize, totalCount);
    }
}
