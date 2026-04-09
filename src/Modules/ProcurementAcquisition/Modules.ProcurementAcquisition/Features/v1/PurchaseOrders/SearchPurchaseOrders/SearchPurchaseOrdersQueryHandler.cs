using FSH.Framework.Shared.Persistence;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using FSH.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.SearchPurchaseOrders;

public sealed class SearchPurchaseOrdersQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<SearchPurchaseOrdersQuery, PagedResponse<PurchaseOrderSummaryDto>>
{
    public async ValueTask<PagedResponse<PurchaseOrderSummaryDto>> Handle(SearchPurchaseOrdersQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.PurchaseOrders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x => x.PoNumber.ToLower().Contains(kw) || x.SupplierName.ToLower().Contains(kw));
        }

        if (query.PurchaseRequestId.HasValue)
            q = q.Where(x => x.PurchaseRequestId == query.PurchaseRequestId.Value);

        if (query.SupplierId.HasValue)
            q = q.Where(x => x.SupplierId == query.SupplierId.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.ModeOfProcurement.HasValue)
            q = q.Where(x => x.ModeOfProcurement == query.ModeOfProcurement.Value);

        if (query.FromDate.HasValue)
            q = q.Where(x => x.PoDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(x => x.PoDate <= query.ToDate.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var items = await q
            .OrderByDescending(x => x.PoDate)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PurchaseOrderSummaryDto(
                x.Id,
                x.PoNumber,
                x.PoDate,
                string.Empty,
                x.SupplierName,
                x.ModeOfProcurement,
                x.Status,
                x.LineItems.Sum(li => li.Quantity * li.UnitCost),
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PurchaseOrderSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
