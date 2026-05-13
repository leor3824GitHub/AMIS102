using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using AMIS.Modules.AssetProcurement.Data;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CreateAssetPurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.IssueAssetPurchaseOrder;

public sealed class IssueAssetPurchaseOrderCommandHandler(
    AssetProcurementDbContext dbContext) : ICommandHandler<IssueAssetPurchaseOrderCommand, AssetPurchaseOrderDto>
{
    public async ValueTask<AssetPurchaseOrderDto> Handle(IssueAssetPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var po = await dbContext.AssetPurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset purchase order '{command.Id}' not found.");

        po.Issue();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return CreateAssetPurchaseOrderCommandHandler.MapToDto(po, string.Empty);
    }
}

