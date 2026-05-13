using AMIS.Modules.Expendable.Contracts.v1.Purchases;
using AMIS.Modules.Expendable.Domain.Purchases;

namespace AMIS.Modules.Expendable.Features.v1.Purchases;

internal static class PurchaseMapper
{
    internal static PurchaseDto ToPurchaseDto(this Purchase purchase) =>
        new(
            purchase.Id,
            purchase.PurchaseOrderNumber,
            purchase.SupplierId,
            purchase.OrderDate,
            purchase.ExpectedDeliveryDate,
            purchase.ReceiptDate,
            purchase.Status.ToString(),
            purchase.TotalAmount,
            purchase.ReceivingNotes,
            purchase.LineItems.Select(x => new PurchaseLineItemDto(
                x.ProductId,
                x.Quantity,
                x.UnitPrice,
                x.ReceivedQuantity,
                x.RejectedQuantity,
                x.QuantityInspection)).ToList(),
            purchase.CreatedOnUtc,
            purchase.CreatedBy);
}


