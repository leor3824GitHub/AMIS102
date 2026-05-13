using AMIS.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using AMIS.Modules.AssetProcurement.Data;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CreateAssetPurchaseOrder;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.GetAssetPurchaseOrder;

public sealed class GetAssetPurchaseOrderQueryHandler(
    AssetProcurementDbContext dbContext) : IQueryHandler<GetAssetPurchaseOrderQuery, AssetPurchaseOrderDto?>
{
    public async ValueTask<AssetPurchaseOrderDto?> Handle(GetAssetPurchaseOrderQuery query, CancellationToken cancellationToken)
    {
        var po = await dbContext.AssetPurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (po is null) return null;

        var prNumber = await dbContext.AssetPurchaseRequests
            .AsNoTracking()
            .Where(x => x.Id == po.PurchaseRequestId)
            .Select(x => x.PrNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return CreateAssetPurchaseOrderCommandHandler.MapToDto(po, prNumber);
    }
}

