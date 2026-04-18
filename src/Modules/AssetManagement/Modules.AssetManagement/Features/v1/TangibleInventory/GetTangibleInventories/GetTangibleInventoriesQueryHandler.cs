using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleInventory.GetTangibleInventories;

public sealed class GetTangibleInventoriesQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetTangibleInventoriesQuery, PagedTangibleInventoriesResponse>
{
    public async ValueTask<PagedTangibleInventoriesResponse> Handle(
        GetTangibleInventoriesQuery query,
        CancellationToken cancellationToken)
    {
        var q = dbContext.TangibleInventories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x =>
                x.ReportNo.ToLower().Contains(kw) ||
                x.ReceivedFrom.ToLower().Contains(kw));
        }

        if (query.DateFrom.HasValue)
            q = q.Where(x => x.Date >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            q = q.Where(x => x.Date <= query.DateTo.Value);

        if (query.ReceiptType.HasValue)
            q = q.Where(x => x.ReceiptType == query.ReceiptType.Value);

        // AssetType filter — join to items if a filter is requested
        IQueryable<Guid> inventoryIds;
        if (query.AssetType.HasValue)
        {
            var filteredType = query.AssetType.Value;
            inventoryIds = dbContext.TangibleInventoryItems
                .Where(i => i.AssetType == filteredType)
                .Select(i => i.TangibleInventoryId)
                .Distinct();
            q = q.Where(x => inventoryIds.Contains(x.Id));
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 10 : query.PageSize;

        var pagedIds = await q
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.ReportNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Compute SE / PPE counts per inventory
        var itemCounts = await dbContext.TangibleInventoryItems
            .Where(i => pagedIds.Contains(i.TangibleInventoryId))
            .GroupBy(i => new { i.TangibleInventoryId, i.AssetType })
            .Select(g => new { g.Key.TangibleInventoryId, g.Key.AssetType, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var seMap  = itemCounts.Where(x => x.AssetType == AssetType.SE)
                               .ToDictionary(x => x.TangibleInventoryId, x => x.Count);
        var ppeMap = itemCounts.Where(x => x.AssetType == AssetType.PPE)
                               .ToDictionary(x => x.TangibleInventoryId, x => x.Count);

        var inventories = await q
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.ReportNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TangibleInventorySummaryDto(
                x.Id,
                x.ReportNo,
                x.Date,
                x.ReceivedFrom,
                x.ReceiptType.ToString(),
                x.FundCluster,
                0,
                0))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = inventories
            .Select(x => x with
            {
                SEItemCount  = seMap.GetValueOrDefault(x.Id),
                PPEItemCount = ppeMap.GetValueOrDefault(x.Id)
            })
            .ToList();

        return new PagedTangibleInventoriesResponse(result, pageNumber, pageSize, totalCount);
    }
}
