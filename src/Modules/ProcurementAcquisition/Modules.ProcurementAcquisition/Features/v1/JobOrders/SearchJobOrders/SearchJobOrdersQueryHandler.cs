using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.SearchJobOrders;

public sealed class SearchJobOrdersQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<SearchJobOrdersQuery, PagedResponse<JobOrderSummaryDto>>
{
    public async ValueTask<PagedResponse<JobOrderSummaryDto>> Handle(SearchJobOrdersQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.JobOrders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(x => EF.Functions.ILike(x.JoNumber, $"%{kw}%") || EF.Functions.ILike(x.SupplierName, $"%{kw}%"));
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
            q = q.Where(x => x.JoDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(x => x.JoDate <= query.ToDate.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var items = await q
            .OrderByDescending(x => x.JoDate)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new JobOrderSummaryDto(
                x.Id,
                x.JoNumber,
                x.JoDate,
                x.JobRequestNo,
                x.SupplierName,
                x.ModeOfProcurement,
                x.Status,
                x.LineItems.Sum(li => li.Quantity * li.UnitCost),
                x.CreatedOnUtc,
                x.SignedCopy != null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<JobOrderSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
