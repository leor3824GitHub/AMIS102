using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.IssuePurchaseOrder;

public sealed class IssuePurchaseOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<IssuePurchaseOrderCommand, PurchaseOrderDto>
{
    public async ValueTask<PurchaseOrderDto> Handle(IssuePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var po = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Purchase order '{command.Id}' not found.");

        // Signatory name comes from the authenticated identity (employee profile), not a request value.
        var issuedByName = await SignatoryResolver.ResolveNameAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);
        po.Issue(currentUser.GetUserId(), issuedByName);
        po.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatePurchaseOrderCommandHandler.MapToDto(po);
    }
}

