using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CertifyFundsAvailable;

public sealed class CertifyPurchaseOrderFundsAvailableCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CertifyPurchaseOrderFundsAvailableCommand, PurchaseOrderDto>
{
    public async ValueTask<PurchaseOrderDto> Handle(CertifyPurchaseOrderFundsAvailableCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var po = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Purchase order '{command.Id}' not found.");

        var uacsByLine = command.UacsByLine.ToDictionary(x => x.ItemNo, x => x.UacsObjectCode);
        var accountantId = currentUser.GetUserId();

        po.CertifyFundsAvailable(
            accountantId,
            command.CertifiedByName,
            uacsByLine,
            command.OursBursNumber,
            command.OursBursDate,
            command.FundCluster);

        po.LastModifiedBy = accountantId.ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreatePurchaseOrderCommandHandler.MapToDto(po);
    }
}
