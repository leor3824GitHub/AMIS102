using AMIS.Framework.Caching;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Inventory;
using AMIS.Modules.Expendable.Domain.Purchases;
using AMIS.Modules.Expendable.Domain.Warehouse;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.RecordInspection;

public sealed class RecordInspectionCommandHandler : ICommandHandler<RecordInspectionCommand, RecordInspectionResponse>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICacheService _cache;

    public RecordInspectionCommandHandler(ExpendableDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async ValueTask<RecordInspectionResponse> Handle(RecordInspectionCommand command, CancellationToken cancellationToken)
    {
        var purchase = await _dbContext.Purchases
            .FirstOrDefaultAsync(p => p.Id == command.PurchaseId, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase {command.PurchaseId} not found");

        var lineItem = purchase.LineItems.FirstOrDefault(li => li.ProductId == command.ProductId)
            ?? throw new InvalidOperationException($"Line item not found in purchase");

        if (lineItem.QuantityInspection < command.QuantityAccepted + command.QuantityRejected)
            throw new InvalidOperationException("Inspection quantities exceed pending amount");

        var inspection = PurchaseInspection.Create(
            purchase.TenantId,
            command.PurchaseId,
            command.ProductId,
            lineItem.QuantityInspection,
            Guid.NewGuid(),
            purchase.WarehouseLocationId
        );

        var defects = command.Defects == null ? null : new System.Collections.ObjectModel.Collection<InspectionDefect>(
            command.Defects.Select(d => new InspectionDefect
            {
                UnitNumber = d.UnitNumber,
                DefectDescription = d.Description ?? string.Empty,
                Severity = d.Severity
            }).ToList()
        );

        if (command.QuantityAccepted + command.QuantityRejected != lineItem.QuantityInspection)
            throw new InvalidOperationException(
                $"Accepted + Rejected must equal total received ({lineItem.QuantityInspection})");

        if (command.QuantityAccepted > 0 && command.QuantityRejected > 0)
        {
            inspection.MarkPartialAcceptance(
                command.QuantityAccepted,
                command.QuantityRejected,
                command.RejectionReason,
                command.Notes,
                defects
            );
        }
        else if (command.QuantityAccepted == lineItem.QuantityInspection)
        {
            inspection.MarkFullyAccepted(command.Notes);
        }
        else if (command.QuantityRejected == lineItem.QuantityInspection)
        {
            inspection.MarkFullyRejected(command.RejectionReason, command.Notes);
        }

        _dbContext.PurchaseInspections.Add(inspection);
        purchase.CompleteInspection(command.ProductId, command.QuantityAccepted, command.QuantityRejected);

        if (command.QuantityAccepted > 0)
        {
            var productInventory = await _dbContext.ProductInventories
                .FirstOrDefaultAsync(pi =>
                    pi.ProductId == command.ProductId &&
                    pi.WarehouseLocationId == purchase.WarehouseLocationId,
                    cancellationToken);

            if (productInventory == null)
            {
                productInventory = ProductInventory.Create(
                    purchase.TenantId,
                    command.ProductId,
                    lineItem.ProductCode,
                    lineItem.ProductName,
                    purchase.WarehouseLocationId,
                    purchase.WarehouseLocationName
                );
                _dbContext.ProductInventories.Add(productInventory);
            }

            productInventory.ReceiveFromPurchase(
                command.PurchaseId,
                command.ProductId,
                command.QuantityAccepted,
                lineItem.UnitPrice
            );
        }

        if (command.QuantityRejected > 0)
        {
            var rejectedInventory = RejectedInventory.Create(
                command.PurchaseId,
                command.ProductId,
                inspection.Id,
                lineItem.ProductCode,
                lineItem.ProductName,
                purchase.WarehouseLocationId,
                purchase.WarehouseLocationName,
                command.QuantityRejected,
                lineItem.UnitPrice,
                command.RejectionReason,
                purchase.TenantId,
                command.Notes
            );
            _dbContext.RejectedInventories.Add(rejectedInventory);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cache.RemoveItemAsync($"purchase:{command.PurchaseId}", cancellationToken);
        await _cache.RemoveItemAsync($"inventory:{command.ProductId}:{purchase.WarehouseLocationId}", cancellationToken);

        return new RecordInspectionResponse(
            inspection.Id,
            inspection.Status.ToString(),
            inspection.QuantityAccepted,
            inspection.QuantityRejected
        );
    }
}

