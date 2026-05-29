using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CertifyFundsAvailable;

public sealed class CertifyPurchaseOrderFundsAvailableCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<CertifyPurchaseOrderFundsAvailableCommand, PurchaseOrderDto>
{
    public async ValueTask<PurchaseOrderDto> Handle(CertifyPurchaseOrderFundsAvailableCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var po = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Purchase order '{command.Id}' not found.");

        var accountantId = currentUser.GetUserId();
        // Signatory name + designation come from the authenticated identity, never from the request body.
        var certifier = await SignatoryResolver.ResolveSignatoryAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);

        po.CertifyFundsAvailable(
            accountantId,
            certifier.Name,
            command.OursBursNumber,
            command.OursBursDate,
            command.FundCluster,
            certifiedByDesignation: certifier.Designation);

        po.LastModifiedBy = accountantId.ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreatePurchaseOrderCommandHandler.MapToDto(po);
    }
}
