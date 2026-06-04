using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetAwardedPrLines;

public sealed class GetAwardedPrLinesQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<GetAwardedPrLinesQuery, IReadOnlyList<AwardedPrLineDto>>
{
    public async ValueTask<IReadOnlyList<AwardedPrLineDto>> Handle(GetAwardedPrLinesQuery query, CancellationToken cancellationToken)
    {
        // Awarded lines live in a JSON column, so flatten in memory over the PR's (few) non-cancelled canvasses.
        var canvasses = await dbContext.CanvassRequests
            .AsNoTracking()
            .Include(c => c.Quotations)
            .Where(c => c.PurchaseRequestId == query.PurchaseRequestId
                        && c.Status != CanvassRequestStatus.Cancelled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<AwardedPrLineDto>();
        foreach (var canvass in canvasses)
        {
            var supplierNameById = canvass.Quotations
                .GroupBy(q => q.SupplierId)
                .ToDictionary(g => g.Key, g => g.First().SupplierName);

            foreach (var li in canvass.LineItems.Where(li => li.AwardedQuotationId is not null))
            {
                var supplierName = li.AwardedSupplierId is { } sid && supplierNameById.TryGetValue(sid, out var name)
                    ? name
                    : null;

                result.Add(new AwardedPrLineDto(li.PrItemNo, canvass.Id, canvass.RivNumber, li.AwardedSupplierId, supplierName));
            }
        }

        return result;
    }
}
