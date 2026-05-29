using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetCanvassablePrLines;

public sealed class GetCanvassablePrLinesQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<GetCanvassablePrLinesQuery, IReadOnlyList<CanvassablePrLineDto>>
{
    public async ValueTask<IReadOnlyList<CanvassablePrLineDto>> Handle(GetCanvassablePrLinesQuery query, CancellationToken cancellationToken)
    {
        var pr = await dbContext.PurchaseRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.PurchaseRequestId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase request '{query.PurchaseRequestId}' not found.");

        // A PR line is "covered" when it belongs to a non-cancelled canvass for this PR.
        var activeCanvasses = await dbContext.CanvassRequests
            .AsNoTracking()
            .Where(x => x.PurchaseRequestId == query.PurchaseRequestId
                        && x.Status != CanvassRequestStatus.Cancelled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var coveredBy = new Dictionary<int, string>();
        foreach (var c in activeCanvasses)
            foreach (var no in c.CoveredItemNos)
                coveredBy.TryAdd(no, c.RivNumber);

        return pr.LineItems
            .OrderBy(li => li.ItemNo)
            .Select(li => new CanvassablePrLineDto(
                li.ItemNo,
                li.ItemDescription,
                li.UnitOfIssue,
                li.Quantity,
                li.EstimatedUnitCost,
                coveredBy.ContainsKey(li.ItemNo),
                coveredBy.GetValueOrDefault(li.ItemNo)))
            .ToList();
    }
}
