using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.SearchCanvassRequests;

public sealed class SearchCanvassRequestsQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<SearchCanvassRequestsQuery, PagedResponse<CanvassRequestSummaryDto>>
{
    public async ValueTask<PagedResponse<CanvassRequestSummaryDto>> Handle(SearchCanvassRequestsQuery query, CancellationToken cancellationToken)
    {
        var q = dbContext.CanvassRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            q = q.Where(x => x.RivNumber.ToLower().Contains(kw));
        }

        if (query.PurchaseRequestId.HasValue)
            q = q.Where(x => x.PurchaseRequestId == query.PurchaseRequestId.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var items = await q
            .OrderByDescending(x => x.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CanvassRequestSummaryDto(
                x.Id,
                x.RivNumber,
                x.PurchaseRequestId,
                string.Empty,
                x.ReturnDeadline,
                x.Status,
                x.Quotations.Count,
                x.CreatedOnUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<CanvassRequestSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}

