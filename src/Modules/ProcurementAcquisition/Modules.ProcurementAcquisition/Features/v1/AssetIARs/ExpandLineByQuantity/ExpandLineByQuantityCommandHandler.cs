using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.CreateAssetIAR;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.ExpandLineByQuantity;

public sealed class ExpandLineByQuantityCommandHandler(
    ProcurementDbContext dbContext) : ICommandHandler<ExpandLineByQuantityCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(ExpandLineByQuantityCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset IAR '{command.Id}' not found.");

        iar.ExpandLineByQuantity(command.ItemNo);
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
