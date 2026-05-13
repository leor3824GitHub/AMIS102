using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using AMIS.Modules.AssetProcurement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.SearchAssetPurchaseOrders;

public sealed class SearchAssetPurchaseOrdersQueryHandler(
    AssetProcurementDbContext dbContext) : IQueryHandler<SearchAssetPurchaseOrdersQuery, PagedResponse<AssetPurchaseOrderSummaryDto>>
{
    public async ValueTask<PagedResponse<AssetPurchaseOrderSummaryDto>> Handle(
        SearchAssetPurchaseOrdersQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.AssetPurchaseOrders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.PoNumber.Contains(query.Keyword) || x.SupplierName.Contains(query.Keyword));

        if (query.PurchaseRequestId.HasValue)
            q = q.Where(x => x.PurchaseRequestId == query.PurchaseRequestId.Value);

        if (query.SupplierId.HasValue)
            q = q.Where(x => x.SupplierId == query.SupplierId.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.FromDate.HasValue)
            q = q.Where(x => x.PoDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(x => x.PoDate <= query.ToDate.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var prNumbers = await dbContext.AssetPurchaseRequests
            .AsNoTracking()
            .Select(x => new { x.Id, x.PrNumber })
            .ToDictionaryAsync(x => x.Id, x => x.PrNumber, cancellationToken)
            .ConfigureAwait(false);

        var items = await q
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = items.Select(po => new AssetPurchaseOrderSummaryDto(
            po.Id,
            po.PoNumber,
            po.PoDate,
            prNumbers.GetValueOrDefault(po.PurchaseRequestId, string.Empty),
            po.SupplierName,
            po.ModeOfProcurement,
            po.Status,
            po.TotalAmount,
            po.CreatedOnUtc)).ToList();

        return new PagedResponse<AssetPurchaseOrderSummaryDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}

