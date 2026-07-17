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
                dbContext.PurchaseRequests
                    .Where(pr => pr.Id == x.PurchaseRequestId)
                    .Select(pr => pr.PrNumber)
                    .FirstOrDefault() ?? string.Empty,
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
                x.SignedCopy != null,
                dbContext.PurchaseOrders.Count(p => p.CanvassRequestId == x.Id && p.Status != PurchaseOrderStatus.Cancelled)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Awarded-line tallies drive the award button. Awarded lines live in a JSON column (no cross-row predicate
        // translation), so compute in memory over the page's PRs and their (few) non-cancelled sibling canvasses.
        // A line is awardable on a canvass only while no canvass of the same PR has awarded it yet.
        var pagePrIds = items.Select(i => i.PurchaseRequestId).Distinct().ToList();

        var relatedCanvasses = await dbContext.CanvassRequests
            .AsNoTracking()
            .Where(c => pagePrIds.Contains(c.PurchaseRequestId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var awardedItemNosByPr = relatedCanvasses
            .Where(c => c.Status != CanvassRequestStatus.Cancelled)
            .GroupBy(c => c.PurchaseRequestId)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(c => c.LineItems)
                      .Where(li => li.AwardedQuotationId is not null)
                      .Select(li => li.PrItemNo)
                      .ToHashSet());

        var canvassById = relatedCanvasses.ToDictionary(c => c.Id);

        items = items.Select(dto =>
        {
            if (!canvassById.TryGetValue(dto.Id, out var canvass))
                return dto;

            var awardedHere = canvass.LineItems.Count(li => li.AwardedQuotationId is not null);
            var awardedAnywhere = awardedItemNosByPr.GetValueOrDefault(dto.PurchaseRequestId) ?? [];
            var remaining = canvass.LineItems.Count(li => !awardedAnywhere.Contains(li.PrItemNo));

            return dto with { AwardedLineCount = awardedHere, RemainingAwardableLineCount = remaining };
        }).ToList();

        return new PagedResponse<CanvassRequestSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}

