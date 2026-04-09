using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using FSH.Modules.ProcurementAcquisition.Data;
using FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.GetPurchaseOrder;

public sealed class GetPurchaseOrderQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<GetPurchaseOrderQuery, PurchaseOrderDto?>
{
    public async ValueTask<PurchaseOrderDto?> Handle(GetPurchaseOrderQuery query, CancellationToken cancellationToken)
    {
        var po = await dbContext.PurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        return po is null ? null : CreatePurchaseOrderCommandHandler.MapToDto(po);
    }
}
