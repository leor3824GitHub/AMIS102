using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.AssetProcurement.Data;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.CreateAssetIAR;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.RejectAssetIAR;

public sealed class RejectAssetIARCommandHandler(
    AssetProcurementDbContext dbContext) : ICommandHandler<RejectAssetIARCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(RejectAssetIARCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset IAR '{command.Id}' not found.");

        iar.Reject(command.Reason);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var po = await dbContext.AssetPurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == iar.PurchaseOrderId, cancellationToken)
            .ConfigureAwait(false);

        return CreateAssetIARCommandHandler.MapToDto(iar, po?.PoNumber ?? string.Empty);
    }
}

