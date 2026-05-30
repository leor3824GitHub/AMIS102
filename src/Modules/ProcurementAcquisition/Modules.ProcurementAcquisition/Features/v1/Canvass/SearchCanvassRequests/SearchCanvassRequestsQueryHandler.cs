using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
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
            var kw = query.Keyword.Trim();
            q = q.Where(x => EF.Functions.ILike(x.RivNumber, $"%{kw}%"));
        }

        if (query.PurchaseRequestId.HasValue)
            q = q.Where(x => x.PurchaseRequestId == query.PurchaseRequestId.Value);

        if (query.Status.HasValue)
            q = q.Where(x => x.Status == query.Status.Value);

        if (query.FromDate.HasValue)
        {
            var from = new DateTimeOffset(query.FromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(x => x.CreatedOnUtc >= from);
        }

        if (query.ToDate.HasValue)
        {
            var toExclusive = new DateTimeOffset(query.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(x => x.CreatedOnUtc < toExclusive);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

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
                x.LineItems.Count,
                x.CreatedOnUtc,
                dbContext.PurchaseOrders.Any(p => p.CanvassRequestId == x.Id && p.Status != PurchaseOrderStatus.Cancelled),
                dbContext.PurchaseOrders
                    .Where(p => p.CanvassRequestId == x.Id && p.Status != PurchaseOrderStatus.Cancelled)
                    .Select(p => p.PoNumber)
                    .FirstOrDefault(),
                dbContext.SignedDocuments.Any(sd =>
                    sd.DocumentType == ProcurementDocumentType.AbstractOfCanvass && sd.DocumentId == x.Id)))
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

