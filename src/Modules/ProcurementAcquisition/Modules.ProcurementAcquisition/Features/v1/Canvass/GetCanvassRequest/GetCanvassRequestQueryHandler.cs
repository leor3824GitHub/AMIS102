using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetCanvassRequest;

public sealed class GetCanvassRequestQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<GetCanvassRequestQuery, CanvassRequestDto?>
{
    public async ValueTask<CanvassRequestDto?> Handle(GetCanvassRequestQuery query, CancellationToken cancellationToken)
    {
        var canvass = await dbContext.CanvassRequests
            .AsNoTracking()
            .Include(x => x.Quotations)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (canvass is null) return null;

        var pr = await dbContext.PurchaseRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == canvass.PurchaseRequestId, cancellationToken)
            .ConfigureAwait(false);

        // Every non-cancelled PO spawned from this canvass — under split award there is one per winning supplier.
        var purchaseOrders = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.CanvassRequestId == canvass.Id && p.Status != PurchaseOrderStatus.Cancelled)
            .OrderBy(p => p.PoNumber)
            .Select(p => new CanvassPurchaseOrderRefDto(p.Id, p.PoNumber, p.SupplierId, p.SupplierName, p.Status))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var linkedPoNumber = purchaseOrders.FirstOrDefault()?.PoNumber;

        var supplierNameById = canvass.Quotations
            .GroupBy(q => q.SupplierId)
            .ToDictionary(g => g.Key, g => g.First().SupplierName);

        return new CanvassRequestDto(
            canvass.Id,
            canvass.RivNumber,
            canvass.PurchaseRequestId,
            pr?.PrNumber ?? string.Empty,
            canvass.ReturnDeadline,
            canvass.Status,
            canvass.AwardedSupplierId,
            null,
            canvass.Quotations.Select(q => new CanvassQuotationDto(
                q.Id,
                q.SupplierId,
                q.SupplierName,
                q.SupplierAddress,
                q.TinNumber,
                q.QuotationDate,
                q.DeliveryTerms,
                q.IsAwarded,
                q.LineItems.Select(li => new CanvassQuotationLineItemDto(
                    li.ItemNo, li.PrItemNo, li.Description, li.Unit, li.Quantity, li.UnitPrice, li.Total)).ToList()
            )).ToList(),
            canvass.CreatedOnUtc,
            canvass.CreatedBy,
            canvass.LineItems.Select(li => new CanvassLineItemDto(
                li.PrItemNo, li.Description, li.Unit, li.Quantity, li.EstimatedUnitCost, li.EstimatedTotalCost,
                li.AwardedQuotationId, li.AwardedSupplierId,
                li.AwardedSupplierId is { } sid && supplierNameById.TryGetValue(sid, out var name) ? name : null,
                li.AwardedUnitPrice)).ToList(),
            linkedPoNumber is not null,
            linkedPoNumber,
            canvass.AwardSignatories
                .Select(s => new CanvassAwardSignatoryDto(s.SortOrder, s.Name, s.Role)).ToList(),
            purchaseOrders);
    }
}

