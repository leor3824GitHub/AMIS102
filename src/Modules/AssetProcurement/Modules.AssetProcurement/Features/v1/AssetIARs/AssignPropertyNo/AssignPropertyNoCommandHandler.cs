using AMIS.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.AssetProcurement.Data;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.CreateAssetIAR;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.AssignPropertyNo;

public sealed class AssignPropertyNoCommandHandler(
    AssetProcurementDbContext dbContext) : ICommandHandler<AssignPropertyNoCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(AssignPropertyNoCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset IAR '{command.Id}' not found.");

        iar.AssignPropertyNo(command.ItemNo, command.PropertyNo);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var poNumber = await dbContext.AssetPurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == iar.PurchaseOrderId)
            .Select(x => x.PoNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        return CreateAssetIARCommandHandler.MapToDto(iar, poNumber);
    }
}
