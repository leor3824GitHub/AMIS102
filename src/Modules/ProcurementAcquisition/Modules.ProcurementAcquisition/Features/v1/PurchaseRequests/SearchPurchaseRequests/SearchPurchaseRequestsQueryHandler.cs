using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
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
            var kw = query.Keyword.Trim();
            q = q.Where(x => EF.Functions.ILike(x.PrNumber, $"%{kw}%") || EF.Functions.ILike(x.Purpose, $"%{kw}%"));
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

        if (query.ExcludeWithIar == true)
        {
            // Drop PRs whose goods are already received: some purchase order raised against the PR carries a
            // non-cancelled IAR. IARs hang off a PO (never off a PR or a Job Order directly), so this walks
            // PR → PO → IAR. Both are regular tables, so it translates to a nested SQL NOT EXISTS.
            q = q.Where(x => !dbContext.PurchaseOrders
                .Any(po => po.PurchaseRequestId == x.Id
                           && dbContext.InspectionAcceptanceReports
                               .Any(iar => iar.PurchaseOrderId == po.Id
                                           && iar.Status != InspectionAcceptanceReportStatus.Cancelled)));
        }

        if (query.ExcludeFullyCanvassed == true)
        {
            // A PR may now be canvassed several times over (identical lines per canvass), so coverage no longer
            // disqualifies it — only full AWARD does. Hide a PR from the canvass picker once every one of its lines
            // has been awarded by some non-cancelled canvass. Awarded lines live in a JSON column (not translatable
            // for a cross-row predicate count), so the award tally is computed in memory over the already-filtered
            // (Approved + keyword) candidate set, which is small.
            var candidates = await q
                .Select(x => new { x.Id, LineCount = x.LineItems.Count })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var candidateIds = candidates.Select(c => c.Id).ToList();

            var siblingCanvasses = await dbContext.CanvassRequests
                .AsNoTracking()
                .Where(c => candidateIds.Contains(c.PurchaseRequestId) && c.Status != CanvassRequestStatus.Cancelled)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var awardedLineCountByPr = siblingCanvasses
                .GroupBy(c => c.PurchaseRequestId)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(c => c.LineItems)
                          .Where(li => li.AwardedQuotationId is not null)
                          .Select(li => li.PrItemNo)
                          .Distinct()
                          .Count());

            var fullyAwardedPrIds = candidates
                .Where(c => awardedLineCountByPr.TryGetValue(c.Id, out var awarded) && awarded >= c.LineCount)
                .Select(c => c.Id)
                .ToHashSet();

            if (fullyAwardedPrIds.Count > 0)
                q = q.Where(x => !fullyAwardedPrIds.Contains(x.Id));
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

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
                x.ResponsibilityCenterCode,
                x.Purpose,
                x.PrType,
                x.Status,
                x.LineItems.Count,
                x.LineItems.Sum(li => li.EstimatedUnitCost * li.Quantity),
                x.CreatedOnUtc,
                x.SignedCopy != null,
                x.Category))
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

