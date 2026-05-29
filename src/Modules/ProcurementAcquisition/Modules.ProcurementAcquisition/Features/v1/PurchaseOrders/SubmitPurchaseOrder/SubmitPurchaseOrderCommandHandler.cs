using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.SubmitPurchaseOrder;

public sealed class SubmitPurchaseOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<SubmitPurchaseOrderCommand, PurchaseOrderDto>
{
    public async ValueTask<PurchaseOrderDto> Handle(SubmitPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var po = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Purchase order '{command.Id}' not found.");

        po.Submit();
        po.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatePurchaseOrderCommandHandler.MapToDto(po);
    }
}
