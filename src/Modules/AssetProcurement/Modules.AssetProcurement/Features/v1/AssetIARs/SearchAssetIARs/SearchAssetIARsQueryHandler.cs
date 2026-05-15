using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.AssetProcurement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.SearchAssetIARs;

public sealed class SearchAssetIARsQueryHandler(
    AssetProcurementDbContext dbContext) : IQueryHandler<SearchAssetIARsQuery, PagedResponse<AssetIARSummaryDto>>
{
    public async ValueTask<PagedResponse<AssetIARSummaryDto>> Handle(
        SearchAssetIARsQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.AssetIARs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.IarNumber.Contains(query.Keyword) || x.SupplierName.Contains(query.Keyword));

        if (query.PurchaseOrderId.HasValue)
            q = q.Where(x => x.PurchaseOrderId == query.PurchaseOrderId.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.FromDate.HasValue)
            q = q.Where(x => x.IarDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(x => x.IarDate <= query.ToDate.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var poNumbers = await dbContext.AssetPurchaseOrders
            .AsNoTracking()
            .Select(x => new { x.Id, x.PoNumber })
            .ToDictionaryAsync(x => x.Id, x => x.PoNumber, cancellationToken)
            .ConfigureAwait(false);

        var items = await q
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = items.Select(iar => new AssetIARSummaryDto(
            iar.Id,
            iar.IarNumber,
            iar.IarDate,
            poNumbers.GetValueOrDefault(iar.PurchaseOrderId, string.Empty),
            iar.SupplierName,
            iar.LineItems.Count,
            iar.TotalAmount,
            iar.Status,
            iar.CreatedOnUtc,
            iar.InspectedById)).ToList();

        return new PagedResponse<AssetIARSummaryDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}

