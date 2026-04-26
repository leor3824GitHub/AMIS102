using FSH.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using FSH.Modules.AssetProcurement.Data;
using FSH.Modules.AssetProcurement.Features.v1.AssetIARs.CreateAssetIAR;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetIARs.GetAssetIAR;

public sealed class GetAssetIARQueryHandler(
    AssetProcurementDbContext dbContext) : IQueryHandler<GetAssetIARQuery, AssetIARDto?>
{
    public async ValueTask<AssetIARDto?> Handle(GetAssetIARQuery query, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (iar is null) return null;

        var poNumber = await dbContext.AssetPurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == iar.PurchaseOrderId)
            .Select(x => x.PoNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return CreateAssetIARCommandHandler.MapToDto(iar, poNumber);
    }
}
