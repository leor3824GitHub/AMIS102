using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using FSH.Modules.AssetProcurement.Data;
using FSH.Modules.AssetProcurement.Features.v1.AssetIARs.CreateAssetIAR;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetIARs.AcceptAssetIAR;

public sealed class AcceptAssetIARCommandHandler(
    AssetProcurementDbContext dbContext,
    IEventBus eventBus) : ICommandHandler<AcceptAssetIARCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(AcceptAssetIARCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset IAR '{command.Id}' not found.");

        iar.Accept();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var po = await dbContext.AssetPurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == iar.PurchaseOrderId, cancellationToken)
            .ConfigureAwait(false);

        var integrationEvent = new AssetIARAcceptedEvent(
            IARId: iar.Id,
            PurchaseOrderId: iar.PurchaseOrderId,
            PoNumber: po?.PoNumber ?? string.Empty,
            SupplierId: iar.SupplierId,
            SupplierName: iar.SupplierName,
            AcceptedItems: iar.LineItems.Select(li => new AssetIARAcceptedEventItem(
                li.Description, li.TechnicalSpecifications, li.Brand, li.Model,
                li.SerialNo, li.PropertyClassHint, li.Unit, li.Quantity, li.UnitCost)).ToList(),
            TenantId: dbContext.TenantInfo?.Identifier);

        await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        return CreateAssetIARCommandHandler.MapToDto(iar, po?.PoNumber ?? string.Empty);
    }
}
