using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.UpdatePurchaseOrder;

public sealed class UpdatePurchaseOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<UpdatePurchaseOrderCommand, PurchaseOrderDto>
{
    public async ValueTask<PurchaseOrderDto> Handle(UpdatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var po = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Purchase order '{command.Id}' not found.");

        // Recover the screened StockNumber/CatalogItemId from the source PR (the awarded canvass doesn't carry
        // them) and guard Supply POs against unresolved Stock Nos — same rule as PO creation, so an edit can't
        // quietly strip the StockNumber a later IAR acceptance depends on. The PO already knows its own category.
        var (_, prLines) = await PurchaseOrderLineResolver
            .LoadSourcePrAsync(dbContext, po.PurchaseRequestId, cancellationToken)
            .ConfigureAwait(false);

        var lineItems = PurchaseOrderLineResolver.ResolveAndValidate(command.LineItems, po.Category, prLines);

        po.Update(
            command.SupplierId,
            command.SupplierName,
            command.SupplierAddress,
            command.SupplierTin,
            command.ModeOfProcurement,
            command.PlaceOfDelivery,
            command.DateOfDelivery,
            command.DeliveryTerm,
            command.PaymentTerm,
            command.FundCluster,
            command.OursBursNumber,
            lineItems);

        po.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreatePurchaseOrderCommandHandler.MapToDto(po);
    }
}

