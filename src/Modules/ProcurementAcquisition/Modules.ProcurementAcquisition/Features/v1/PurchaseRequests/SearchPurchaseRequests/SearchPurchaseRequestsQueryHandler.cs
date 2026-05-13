using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.SearchPurchaseRequests;

public sealed class SearchPurchaseRequestsQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<SearchPurchaseRequestsQuery, PagedResponse<PurchaseRequestSummaryDto>>
{
    public async ValueTask<PagedResponse<PurchaseRequestSummaryDto>> Handle(SearchPurchaseRequestsQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.PurchaseRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x => x.PrNumber.ToLower().Contains(kw) || x.Purpose.ToLower().Contains(kw));
        }

        if (query.DepartmentId.HasValue)
            q = q.Where(x => x.DepartmentId == query.DepartmentId.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.PrType.HasValue)
            q = q.Where(x => x.PrType == query.PrType.Value);

        if (query.FromDate.HasValue)
            q = q.Where(x => x.PrDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(x => x.PrDate <= query.ToDate.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var items = await q
            .OrderByDescending(x => x.PrDate)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PurchaseRequestSummaryDto(
                x.Id,
                x.PrNumber,
                x.PrDate,
                string.Empty,
                x.Section,
                x.Purpose,
                x.PrType,
                x.Status,
                x.LineItems.Count,
                x.LineItems.Sum(li => li.EstimatedUnitCost * li.Quantity),
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PurchaseRequestSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}

