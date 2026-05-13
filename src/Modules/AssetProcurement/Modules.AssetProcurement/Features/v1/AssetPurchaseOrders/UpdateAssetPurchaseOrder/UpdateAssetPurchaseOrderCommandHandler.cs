using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using AMIS.Modules.AssetProcurement.Data;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CreateAssetPurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.UpdateAssetPurchaseOrder;

public sealed class UpdateAssetPurchaseOrderCommandHandler(
    AssetProcurementDbContext dbContext) : ICommandHandler<UpdateAssetPurchaseOrderCommand, AssetPurchaseOrderDto>
{
    public async ValueTask<AssetPurchaseOrderDto> Handle(UpdateAssetPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var po = await dbContext.AssetPurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset purchase order '{command.Id}' not found.");

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
            command.OblRequestNumber,
            command.LineItems);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateAssetPurchaseOrderCommandHandler.MapToDto(po, string.Empty);
    }
}

