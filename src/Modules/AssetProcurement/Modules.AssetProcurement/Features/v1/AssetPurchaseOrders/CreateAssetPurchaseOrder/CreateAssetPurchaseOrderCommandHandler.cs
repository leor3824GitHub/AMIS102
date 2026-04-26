using FSH.Framework.Core.Context;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetPurchaseOrders;
using FSH.Modules.AssetProcurement.Data;
using FSH.Modules.AssetProcurement.Domain.AssetPurchaseOrders;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CreateAssetPurchaseOrder;

public sealed class CreateAssetPurchaseOrderCommandHandler(
    AssetProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateAssetPurchaseOrderCommand, AssetPurchaseOrderDto>
{
    public async ValueTask<AssetPurchaseOrderDto> Handle(CreateAssetPurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var poNumber = await GeneratePoNumberAsync(cancellationToken).ConfigureAwait(false);

        var po = AssetPurchaseOrder.Create(
            poNumber,
            command.PurchaseRequestId,
            command.SupplierId,
            command.SupplierName,
            command.SupplierAddress,
            command.SupplierTin,
            command.ModeOfProcurement,
            command.PlaceOfDelivery,
            command.DateOfDelivery,
            command.DeliveryTerm,
            command.PaymentTerm,
            command.FundCluster,
            command.OblRequestNumber,
            command.LineItems);

        po.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.AssetPurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToDto(po, string.Empty);
    }

    private async Task<string> GeneratePoNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"APO-{year}-";

        var lastNumber = await dbContext.AssetPurchaseOrders
            .IgnoreQueryFilters()
            .Where(x => x.PoNumber.StartsWith(prefix))
            .Select(x => x.PoNumber)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (lastNumber != null && int.TryParse(lastNumber[prefix.Length..], out var last))
            next = last + 1;

        return $"{prefix}{next:0000}";
    }

    internal static AssetPurchaseOrderDto MapToDto(AssetPurchaseOrder po, string prNumber) =>
        new(po.Id,
            po.PoNumber,
            po.PoDate,
            po.PurchaseRequestId,
            prNumber,
            po.SupplierId,
            po.SupplierName,
            po.SupplierAddress,
            po.SupplierTin,
            po.ModeOfProcurement,
            po.PlaceOfDelivery,
            po.DateOfDelivery,
            po.DeliveryTerm,
            po.PaymentTerm,
            po.FundCluster,
            po.OblRequestNumber,
            po.Status,
            po.LineItems.Select(li => new AssetPurchaseOrderLineItemDto(
                li.ItemNo, li.Unit, li.Description, li.TechnicalSpecifications,
                li.Brand, li.Model, li.PropertyClassHint, li.Quantity, li.UnitCost, li.Amount)).ToList(),
            po.TotalAmount,
            po.CreatedOnUtc,
            po.CreatedBy,
            po.LastModifiedOnUtc);
}
