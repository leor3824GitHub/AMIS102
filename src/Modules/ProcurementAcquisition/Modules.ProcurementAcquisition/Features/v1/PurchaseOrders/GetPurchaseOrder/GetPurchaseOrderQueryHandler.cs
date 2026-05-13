using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.GetPurchaseOrder;

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

