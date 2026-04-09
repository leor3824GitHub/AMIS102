using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using FSH.Modules.ProcurementAcquisition.Data;
using FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CancelPurchaseOrder;

public sealed class CancelPurchaseOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CancelPurchaseOrderCommand, PurchaseOrderDto>
{
    public async ValueTask<PurchaseOrderDto> Handle(CancelPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var po = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase order '{command.Id}' not found.");

        po.Cancel(command.Reason);
        po.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatePurchaseOrderCommandHandler.MapToDto(po);
    }
}
