using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetExpiringICS;

public sealed class GetExpiringICSQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetExpiringICSQuery, PagedExpiringICSResponse>
{
    public async ValueTask<PagedExpiringICSResponse> Handle(
        GetExpiringICSQuery query,
        CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 20 : query.PageSize;
        var daysAhead  = query.DaysAhead  <  0 ? 0  : query.DaysAhead;

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(daysAhead));

        var q = dbContext.InventoryCustodianSlips
            .Where(x => x.Status == ICSStatus.Active
                     && x.ExpiresOn.HasValue
                     && x.ExpiresOn.Value <= cutoff)
            .OrderBy(x => x.ExpiresOn);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var icsIds = await q
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemCounts = await dbContext.ICSItems
            .Where(x => icsIds.Contains(x.ICSId))
            .GroupBy(x => x.ICSId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        var items = await dbContext.InventoryCustodianSlips
            .Where(x => icsIds.Contains(x.Id))
            .OrderBy(x => x.ExpiresOn)
            .Select(x => new ExpiringICSSummaryDto(
                x.Id,
                x.ICSNo,
                x.Date,
                x.ExpiresOn!.Value,
                x.AssetType.ToString(),
                x.ReceivedByEmployeeId,
                x.IssuedFromEmployeeId,
                x.FundCluster,
                0))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Merge item counts.
        var result = items
            .Select(x => x with { ItemCount = itemCounts.GetValueOrDefault(x.Id) })
            .ToList();

        return new PagedExpiringICSResponse(result, pageNumber, pageSize, totalCount);
    }
}
