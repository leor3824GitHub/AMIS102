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
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Purchase request '{query.PurchaseRequestId}' not found.");

        // A PR may have several canvasses covering identical lines. Coverage no longer locks a line at creation
        // (the "one canvass per awarded line" invariant is enforced at award time), so every PR line stays
        // selectable on every canvass. IsCovered is always false here, kept on the DTO for back-compat.
        return pr.LineItems
            .OrderBy(li => li.ItemNo)
            .Select(li => new CanvassablePrLineDto(
                li.ItemNo,
                li.ItemDescription,
                li.UnitOfIssue,
                li.Quantity,
                li.EstimatedUnitCost,
                IsCovered: false,
                CoveringRivNumber: null))
            .ToList();
    }
}
