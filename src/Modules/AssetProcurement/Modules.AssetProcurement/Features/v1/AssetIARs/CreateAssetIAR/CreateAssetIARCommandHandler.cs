using FSH.Framework.Core.Context;
using FSH.Modules.AssetProcurement.Contracts.v1.AssetInspectionAcceptanceReports;
using FSH.Modules.AssetProcurement.Data;
using FSH.Modules.AssetProcurement.Domain.AssetInspectionAcceptanceReports;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetProcurement.Features.v1.AssetIARs.CreateAssetIAR;

public sealed class CreateAssetIARCommandHandler(
    AssetProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateAssetIARCommand, AssetIARDto>
{
    public async ValueTask<AssetIARDto> Handle(CreateAssetIARCommand command, CancellationToken cancellationToken)
    {
        var po = await dbContext.AssetPurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.PurchaseOrderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset purchase order '{command.PurchaseOrderId}' not found.");

        var iarNumber = await GenerateIARNumberAsync(cancellationToken).ConfigureAwait(false);

        var iar = AssetInspectionAcceptanceReport.Create(
            iarNumber,
            command.PurchaseOrderId,
            po.SupplierId,
            po.SupplierName,
            command.InspectedById,
            command.ReceivedById,
            command.DeliveryReceiptNo,
            command.DeliveryDate,
            command.Remarks,
            command.LineItems);

        iar.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.AssetIARs.Add(iar);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToDto(iar, po.PoNumber);
    }

    private async Task<string> GenerateIARNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"IAR-{year}-";

        var lastNumber = await dbContext.AssetIARs
            .IgnoreQueryFilters()
            .Where(x => x.IarNumber.StartsWith(prefix))
            .Select(x => x.IarNumber)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (lastNumber != null && int.TryParse(lastNumber[prefix.Length..], out var last))
            next = last + 1;

        return $"{prefix}{next:0000}";
    }

    internal static AssetIARDto MapToDto(AssetInspectionAcceptanceReport iar, string poNumber) =>
        new(iar.Id,
            iar.IarNumber,
            iar.IarDate,
            iar.PurchaseOrderId,
            poNumber,
            iar.SupplierId,
            iar.SupplierName,
            iar.InspectedById,
            string.Empty,
            iar.ReceivedById,
            string.Empty,
            iar.DeliveryReceiptNo,
            iar.DeliveryDate,
            iar.Status,
            iar.Remarks,
            iar.LineItems.Select(li => new AssetIARLineItemDto(
                li.ItemNo, li.Description, li.TechnicalSpecifications,
                li.Brand, li.Model, li.SerialNo, li.PropertyClassHint,
                li.Unit, li.Quantity, li.UnitCost, li.Amount, li.InspectionRemarks)).ToList(),
            iar.TotalAmount,
            iar.CreatedOnUtc,
            iar.CreatedBy,
            iar.LastModifiedOnUtc);
}
