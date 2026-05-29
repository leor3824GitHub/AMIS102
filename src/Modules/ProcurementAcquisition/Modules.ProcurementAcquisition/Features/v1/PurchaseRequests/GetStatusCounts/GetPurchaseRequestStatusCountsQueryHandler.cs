using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.GetStatusCounts;

public sealed class GetPurchaseRequestStatusCountsQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<GetPurchaseRequestStatusCountsQuery, IReadOnlyList<PurchaseRequestStatusCountDto>>
{
    public async ValueTask<IReadOnlyList<PurchaseRequestStatusCountDto>> Handle(
        GetPurchaseRequestStatusCountsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.PurchaseRequests.AsNoTracking();

        if (query.FromDate.HasValue)
            q = q.Where(x => x.PrDate >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            q = q.Where(x => x.PrDate <= query.ToDate.Value);

        var counts = await q
            .GroupBy(x => x.Status)
            .Select(g => new PurchaseRequestStatusCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return counts;
    }
}
