using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
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

        // Accepting this IAR may complete its PO (and, in turn, the originating PR). We mutate the
        // IAR, the PO, and the PR together so a single SaveChanges commits them in one transaction —
        // there is never a window where the PO is Fulfilled but the PR is left Approved.
        var po = await dbContext.PurchaseOrders
            .FirstOrDefaultAsync(x => x.Id == iar.PurchaseOrderId, cancellationToken).ConfigureAwait(false);
        var poNumber = po?.PoNumber ?? string.Empty;

        if (po is not null)
        {
            // Cumulative accepted units = non-rejected lines of every accepted IAR for this PO.
            // The current IAR isn't persisted as Accepted yet, so count it from memory.
            var priorAcceptedIars = await dbContext.AssetIARs
                .Where(x => x.PurchaseOrderId == po.Id && x.Id != iar.Id && x.Status == AssetIARStatus.Accepted)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var acceptedQuantity =
                priorAcceptedIars.SelectMany(x => x.LineItems)
                    .Where(li => li.InspectionResult != LineInspectionResult.Rejected)
                    .Sum(li => li.Quantity)
                + iar.LineItems
                    .Where(li => li.InspectionResult != LineInspectionResult.Rejected)
                    .Sum(li => li.Quantity);

            po.RecordDelivery(acceptedQuantity);

            if (po.Status == PurchaseOrderStatus.Fulfilled)
            {
                var siblingStatuses = await dbContext.PurchaseOrders
                    .Where(p => p.PurchaseRequestId == po.PurchaseRequestId && p.Id != po.Id)
                    .Select(p => p.Status)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);

                var allClosed = siblingStatuses.All(s =>
                    s is PurchaseOrderStatus.Fulfilled or PurchaseOrderStatus.Cancelled);

                if (allClosed)
                {
                    var pr = await dbContext.PurchaseRequests
                        .FirstOrDefaultAsync(p => p.Id == po.PurchaseRequestId, cancellationToken).ConfigureAwait(false);
                    if (pr is not null && pr.Status == PurchaseRequestStatus.Approved)
                        pr.Complete();
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var tenantId = dbContext.TenantInfo?.Identifier;
        var acceptedLines = iar.LineItems
            .Where(li => li.InspectionResult != LineInspectionResult.Rejected)
            .ToList();

        // Supply IARs land their accepted stock into the Expendable module's ProductInventory; Asset IARs
        // materialize fixed-asset rows in AssetRegister. One PR/PO/IAR is wholly one category — see ProcurementCategory.
        if (iar.Category == ProcurementCategory.Supply)
        {
            var supplyEvent = new SupplyIARAcceptedEvent(
                IARId: iar.Id,
                IarNumber: iar.IarNumber,
                PurchaseOrderId: iar.PurchaseOrderId,
                PoNumber: poNumber,
                SupplierId: iar.SupplierId,
                SupplierName: iar.SupplierName,
                AcceptedItems: acceptedLines
                    .Select(li => new SupplyIARAcceptedEventItem(
                        li.StockNumber, li.Description, li.Unit, li.Quantity, li.UnitCost)).ToList(),
                TenantId: tenantId);

            await eventBus.PublishAsync(supplyEvent, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var assetEvent = new AssetIARAcceptedEvent(
                IARId: iar.Id,
                PurchaseOrderId: iar.PurchaseOrderId,
                PoNumber: poNumber,
                SupplierId: iar.SupplierId,
                SupplierName: iar.SupplierName,
                AcceptedItems: acceptedLines
                    .Select(li => new AssetIARAcceptedEventItem(
                        li.Description, li.TechnicalSpecifications, li.Brand, li.Model,
                        li.SerialNo, li.PropertyClassHint, li.Unit, li.Quantity, li.UnitCost,
                        li.StockPropertyNo, li.CatalogItemId, li.UacsObjectCode)).ToList(),
                TenantId: tenantId);

            await eventBus.PublishAsync(assetEvent, cancellationToken).ConfigureAwait(false);
        }

        return AssetIARMapper.ToDto(iar, poNumber);
    }
}
