using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using FSH.Modules.ProcurementAcquisition.Data;
using FSH.Modules.ProcurementAcquisition.Domain.PurchaseOrders;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    public async ValueTask<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var poNumber = await GeneratePoNumberAsync(cancellationToken).ConfigureAwait(false);

        var lineItems = command.LineItems.Select(li =>
            (li.StockNumber, li.Unit, li.Description, li.Quantity, li.UnitCost));

        var po = PurchaseOrder.Create(
            poNumber,
            command.PurchaseRequestId,
            command.CanvassRequestId,
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
            command.OursBursNumber,
            lineItems);

        po.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.PurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToDto(po);
    }

    private async Task<string> GeneratePoNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PO-{year}-";

        var lastNumber = await dbContext.PurchaseOrders
            .IgnoreQueryFilters()
            .Where(x => x.PoNumber.StartsWith(prefix))
            .Select(x => x.PoNumber)
            .OrderByDescending(x => x)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var next = 1;
        if (lastNumber != null && int.TryParse(lastNumber[prefix.Length..], out var last))
        {
            next = last + 1;
        }

        return $"{prefix}{next:0000}";
    }

    internal static PurchaseOrderDto MapToDto(PurchaseOrder po)
    {
        return new PurchaseOrderDto(
            po.Id,
            po.PoNumber,
            po.PoDate,
            po.PurchaseRequestId,
            string.Empty,
            po.CanvassRequestId,
            null,
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
            po.OursBursNumber,
            po.Status,
            po.LineItems.Select(li => new PurchaseOrderLineItemDto(
                li.ItemNo, li.StockNumber, li.Unit, li.Description, li.Quantity, li.UnitCost, li.Amount)).ToList(),
            po.TotalAmount,
            AmountToWords(po.TotalAmount),
            po.CreatedOnUtc,
            po.CreatedBy,
            po.LastModifiedOnUtc);
    }

    internal static string AmountToWords(decimal amount)
    {
        // Simple implementation; extend as needed for Philippine peso format
        var whole = (long)Math.Floor(amount);
        var cents = (int)Math.Round((amount - whole) * 100);

        return $"{NumberToWords(whole)} Peso{(whole != 1 ? "s" : string.Empty)}"
               + (cents > 0 ? $" and {cents:D2}/100" : " Only");
    }

    private static string NumberToWords(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0) return "Negative " + NumberToWords(-number);

        string[] ones = ["", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
                        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
                        "Seventeen", "Eighteen", "Nineteen"];
        string[] tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];

        if (number < 20) return ones[number];
        if (number < 100) return tens[number / 10] + (number % 10 != 0 ? "-" + ones[number % 10] : string.Empty);
        if (number < 1_000) return ones[number / 100] + " Hundred" + (number % 100 != 0 ? " " + NumberToWords(number % 100) : string.Empty);
        if (number < 1_000_000) return NumberToWords(number / 1_000) + " Thousand" + (number % 1_000 != 0 ? " " + NumberToWords(number % 1_000) : string.Empty);
        if (number < 1_000_000_000) return NumberToWords(number / 1_000_000) + " Million" + (number % 1_000_000 != 0 ? " " + NumberToWords(number % 1_000_000) : string.Empty);
        return NumberToWords(number / 1_000_000_000) + " Billion" + (number % 1_000_000_000 != 0 ? " " + NumberToWords(number % 1_000_000_000) : string.Empty);
    }
}
