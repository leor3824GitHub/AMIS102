using FSH.Framework.Shared.Persistence;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseRequests;
using FSH.Modules.AssetProcurement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.SearchAssetPurchaseRequests;

public sealed class SearchAssetPurchaseRequestsQueryHandler(
    AssetProcurementDbContext dbContext) : IQueryHandler<SearchAssetPurchaseRequestsQuery, PagedResponse<AssetPurchaseRequestSummaryDto>>
{
    public async ValueTask<PagedResponse<AssetPurchaseRequestSummaryDto>> Handle(
        SearchAssetPurchaseRequestsQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.AssetPurchaseRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.PrNumber.Contains(query.Keyword) || x.Purpose.Contains(query.Keyword));

        if (query.DepartmentId.HasValue)
            q = q.Where(x => x.DepartmentId == query.DepartmentId);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.PrType.HasValue)
            q = q.Where(x => x.PrType == query.PrType.Value);

        if (query.FromDate.HasValue)
            q = q.Where(x => x.PrDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(x => x.PrDate <= query.ToDate.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await q
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new AssetPurchaseRequestSummaryDto(
                x.Id,
                x.PrNumber,
                x.PrDate,
                string.Empty,
                x.Section,
                x.Purpose,
                x.PrType,
                x.Status,
                x.LineItems.Count,
                x.LineItems.Sum(li => li.EstimatedTotalCost),
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<AssetPurchaseRequestSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}
