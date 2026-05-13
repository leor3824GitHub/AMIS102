using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using AMIS.Modules.AssetProcurement.Data;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CreateAssetPurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CancelAssetPurchaseOrder;

public sealed class CancelAssetPurchaseOrderCommandHandler(
    AssetProcurementDbContext dbContext) : ICommandHandler<CancelAssetPurchaseOrderCommand, AssetPurchaseOrderDto>
{
    public async ValueTask<AssetPurchaseOrderDto> Handle(CancelAssetPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var po = await dbContext.AssetPurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset purchase order '{command.Id}' not found.");

        po.Cancel(command.Reason);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateAssetPurchaseOrderCommandHandler.MapToDto(po, string.Empty);
    }
}

