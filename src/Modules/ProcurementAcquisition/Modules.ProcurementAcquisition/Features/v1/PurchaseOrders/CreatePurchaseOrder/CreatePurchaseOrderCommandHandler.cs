using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.PurchaseOrders;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;

public sealed class CreatePurchaseOrderCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    public async ValueTask<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();

        if (!command.AllowDuplicate)
        {
            await EnsureNoDuplicateAsync(command, cancellationToken).ConfigureAwait(false);
        }

        // The PO inherits Asset/Supply from its source PR (one PR is wholly one or the other). Drives the
        // downstream IAR acceptance rules and which acceptance event fires. The PR's lines also let us recover
        // the screened StockNumber/CatalogItemId that the awarded canvass doesn't carry — see the resolver.
        // These are stable across number-allocation retries, so resolve them once, before the loop.
        var (category, prLines) = await PurchaseOrderLineResolver
            .LoadSourcePrAsync(dbContext, command.PurchaseRequestId, cancellationToken)
            .ConfigureAwait(false);

        var lineItems = PurchaseOrderLineResolver.ResolveAndValidate(command.LineItems, category, prLines);

        // Allocate the PO number from a per-(tenant, year, month) counter guarded by xmin optimistic
        // concurrency, retrying on conflict — same race-safe pattern as PR/IAR. Replaces the old
        // "max(PoNumber) + 1" scan, which could mint duplicate numbers (or fail the unique index) when two
        // POs were created concurrently.
        var now = DateTime.UtcNow;
        for (var attempt = 0; attempt < SequenceAllocation.MaxAttempts; attempt++)
        {
            var sequence = await dbContext.PoNumberSequences
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenantId && x.Year == now.Year && x.Month == now.Month,
                    cancellationToken)
                .ConfigureAwait(false);

            if (sequence is null)
            {
                sequence = PoNumberSequence.Create(tenantId, now.Year, now.Month);
                dbContext.PoNumberSequences.Add(sequence);
            }

            var serial = sequence.NextSerial();
            var poNumber = $"{now.Year:D4}-{now.Month:D2}-{serial:D3}";

            var po = PurchaseOrder.Create(
                tenantId,
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
                lineItems,
                category);

            po.CreatedBy = currentUser.GetUserId().ToString();
            dbContext.PurchaseOrders.Add(po);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return MapToDto(po);
            }
            catch (DbUpdateException ex) when (SequenceAllocation.IsRetryableAllocationConflict(ex))
            {
                // Counter row advanced (xmin) or the generated number collided with an existing PO (unique
                // violation) between our read and save. Drop tracked state and retry with the latest value.
                dbContext.ChangeTracker.Clear();
            }
        }

        throw new CustomException("Failed to allocate a unique PO number. Please try again.",
            Enumerable.Empty<string>(), HttpStatusCode.Conflict);
    }

    private async Task EnsureNoDuplicateAsync(CreatePurchaseOrderCommand command, CancellationToken ct)
    {
        // A canvass (RIV) can be awarded only once, so it must map to a single active PO.
        // Reject outright if a non-cancelled PO already references this canvass.
        if (command.CanvassRequestId.HasValue)
        {
            var existingFromCanvass = await dbContext.PurchaseOrders
                .AsNoTracking()
                .Where(p => p.CanvassRequestId == command.CanvassRequestId.Value
                            && p.Status != PurchaseOrderStatus.Cancelled)
                .Select(p => new { p.PoNumber, p.Status })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (existingFromCanvass is not null)
            {
                var canvassMessage = $"A purchase order has already been created from this canvass (RIV). "
                              + $"Existing PO: {existingFromCanvass.PoNumber} ({existingFromCanvass.Status}).";
                throw new CustomException(canvassMessage, Enumerable.Empty<string>(), HttpStatusCode.Conflict);
            }
        }

        var incomingDescriptions = command.LineItems
            .Select(li => (li.Description ?? string.Empty).Trim().ToLowerInvariant())
            .Where(d => d.Length > 0)
            .ToHashSet();

        if (incomingDescriptions.Count == 0)
        {
            return;
        }

        var query = dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.PurchaseRequestId == command.PurchaseRequestId
                        && p.SupplierId == command.SupplierId
                        && p.Status != PurchaseOrderStatus.Cancelled);

        if (command.CanvassRequestId.HasValue)
        {
            query = query.Where(p => p.CanvassRequestId == command.CanvassRequestId.Value);
        }

        var existing = await query
            .Select(p => new
            {
                p.PoNumber,
                p.Status,
                Descriptions = p.LineItems.Select(li => li.Description).ToList()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var conflicts = existing
            .Where(p => p.Descriptions.Any(d => incomingDescriptions.Contains((d ?? string.Empty).Trim().ToLowerInvariant())))
            .ToList();

        if (conflicts.Count == 0)
        {
            return;
        }

        var poList = string.Join(", ", conflicts.Select(p => $"{p.PoNumber} ({p.Status})"));
        var message = $"A purchase order already exists for this purchase request and supplier with overlapping item(s). Existing PO(s): {poList}.";

        throw new CustomException(message, Enumerable.Empty<string>(), HttpStatusCode.Conflict);
    }

    private string GetRequiredTenantId() =>
        currentUser.GetTenant()
        ?? dbContext.TenantInfo?.Identifier
        ?? throw new InvalidOperationException("Tenant ID required.");

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
            po.OursBursDate,
            po.Status,
            po.LineItems.Select(li => new PurchaseOrderLineItemDto(
                li.ItemNo, li.StockNumber, li.Unit, li.Description, li.Quantity, li.UnitCost, li.Amount,
                li.CatalogItemId)).ToList(),
            po.TotalAmount,
            AmountToWords(po.TotalAmount),
            po.CreatedOnUtc,
            po.CreatedBy,
            po.LastModifiedOnUtc,
            po.FundsAvailableCertifiedById,
            po.FundsAvailableCertifiedByName,
            po.FundsAvailableCertifiedOnUtc,
            po.IssuedById,
            po.IssuedByName,
            po.IssuedOnUtc,
            FundsAvailableCertifiedByDesignation: po.FundsAvailableCertifiedByDesignation,
            IssuedByDesignation: po.IssuedByDesignation,
            Category: po.Category);
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

