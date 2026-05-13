using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.AssetProcurement.Data;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.CreateAssetIAR;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.UpdateAssetIAR;

public sealed class UpdateAssetIARCommandHandler(
    AssetProcurementDbContext dbContext) : ICommandHandler<UpdateAssetIARCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(UpdateAssetIARCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset IAR '{command.Id}' not found.");

        iar.Update(
            command.InspectedById,
            command.ReceivedById,
            command.DeliveryReceiptNo,
            command.DeliveryDate,
            command.Remarks,
            command.LineItems);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var po = await dbContext.AssetPurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == iar.PurchaseOrderId, cancellationToken)
            .ConfigureAwait(false);

        return CreateAssetIARCommandHandler.MapToDto(iar, po?.PoNumber ?? string.Empty);
    }
}

