using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.CreateAssetIAR;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.UpdateAssetIAR;

public sealed class UpdateAssetIARCommandHandler(
    ProcurementDbContext dbContext) : ICommandHandler<UpdateAssetIARCommand, AssetIARDto>
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

        var poNumber = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == iar.PurchaseOrderId)
            .Select(x => x.PoNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return CreateAssetIARCommandHandler.MapToDto(iar, poNumber);
    }
}
