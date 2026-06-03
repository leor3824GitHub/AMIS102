using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Framework.Eventing.Abstractions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.AcceptInspectionAcceptanceReport;

public sealed class AcceptInspectionAcceptanceReportCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator,
    IEventBus eventBus) : ICommandHandler<AcceptInspectionAcceptanceReportCommand, InspectionAcceptanceReportDto>
{
    public async ValueTask<InspectionAcceptanceReportDto> Handle(AcceptInspectionAcceptanceReportCommand command, CancellationToken cancellationToken)
    {
        var iar = await dbContext.InspectionAcceptanceReports
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Asset IAR '{command.Id}' not found.");

        // Acceptance signatory comes from the authenticated identity, not a request value, so a user
        // holding the permission cannot accept under someone else's name.
        var acceptor = await SignatoryResolver.ResolveSignatoryAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false);

        try { iar.Accept(currentUser.GetUserId(), acceptor.Name, acceptor.Designation); }
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
            var priorAcceptedIars = await dbContext.InspectionAcceptanceReports
                .Where(x => x.PurchaseOrderId == po.Id && x.Id != iar.Id && x.Status == InspectionAcceptanceReportStatus.Accepted)
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

        return InspectionAcceptanceReportMapper.ToDto(iar, poNumber);
    }
}
