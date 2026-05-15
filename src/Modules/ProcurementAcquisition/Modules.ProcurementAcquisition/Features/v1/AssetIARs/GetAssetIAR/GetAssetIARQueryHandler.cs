using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.CreateAssetIAR;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.GetAssetIAR;

public sealed class GetAssetIARQueryHandler(
    ProcurementDbContext dbContext) : IQueryHandler<GetAssetIARQuery, AssetIARDto?>
{
    public async ValueTask<AssetIARDto?> Handle(GetAssetIARQuery query, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (iar is null) return null;

        var poNumber = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == iar.PurchaseOrderId)
            .Select(x => x.PoNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return CreateAssetIARCommandHandler.MapToDto(iar, poNumber);
    }
}
