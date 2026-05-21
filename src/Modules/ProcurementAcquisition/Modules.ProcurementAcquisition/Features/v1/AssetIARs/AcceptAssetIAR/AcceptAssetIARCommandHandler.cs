using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.AssetIARs.AcceptAssetIAR;

public sealed class AcceptAssetIARCommandHandler(
    ProcurementDbContext dbContext,
    IEventBus eventBus) : ICommandHandler<AcceptAssetIARCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(AcceptAssetIARCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.AssetIARs
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Asset IAR '{command.Id}' not found.");

        try { iar.Accept(); }
        catch (InvalidOperationException ex)
        {
            throw new CustomException(ex.Message, [], System.Net.HttpStatusCode.BadRequest);
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var poNumber = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == iar.PurchaseOrderId)
            .Select(x => x.PoNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        var integrationEvent = new AssetIARAcceptedEvent(
            IARId: iar.Id,
            PurchaseOrderId: iar.PurchaseOrderId,
            PoNumber: poNumber,
            SupplierId: iar.SupplierId,
            SupplierName: iar.SupplierName,
            AcceptedItems: iar.LineItems
                .Where(li => li.InspectionResult != LineInspectionResult.Rejected)
                .Select(li => new AssetIARAcceptedEventItem(
                    li.Description, li.TechnicalSpecifications, li.Brand, li.Model,
                    li.SerialNo, li.PropertyClassHint, li.Unit, li.Quantity, li.UnitCost,
                    li.StockPropertyNo)).ToList(),
            TenantId: dbContext.TenantInfo?.Identifier);

        await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        return AssetIARMapper.ToDto(iar, poNumber);
    }
}
