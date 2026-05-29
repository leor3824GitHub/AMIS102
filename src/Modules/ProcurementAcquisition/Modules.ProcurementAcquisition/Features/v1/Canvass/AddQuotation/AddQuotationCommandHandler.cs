using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.Canvass;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AddQuotation;

public sealed class AddQuotationCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<AddQuotationCommand, CanvassQuotationDto>
{
    public async ValueTask<CanvassQuotationDto> Handle(AddQuotationCommand command, CancellationToken cancellationToken)
    {
        var tenantId = GetRequiredTenantId();

        var canvass = await dbContext.CanvassRequests
            .Include(x => x.Quotations)
            .FirstOrDefaultAsync(x => x.Id == command.CanvassRequestId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Canvass request '{command.CanvassRequestId}' not found.");

        if (canvass.Status != CanvassRequestStatus.Open)
            throw new InvalidOperationException("Quotations can only be added to Open canvass requests.");

        if (canvass.Quotations.Count >= 3)
            throw new InvalidOperationException("A canvass request can have at most 3 quotations.");

        if (command.LineItems.Count == 0)
            throw new InvalidOperationException("A quotation must include at least one line item.");

        // Quoted items must be within this canvass's covered scope (partial quotes allowed).
        var coveredDescriptions = canvass.LineItems
            .Select(li => li.Description.Trim().ToLowerInvariant())
            .ToHashSet();

        var outOfScope = command.LineItems
            .Where(li => !coveredDescriptions.Contains((li.Description ?? string.Empty).Trim().ToLowerInvariant()))
            .Select(li => li.Description)
            .ToList();
        if (outOfScope.Count > 0)
            throw new InvalidOperationException(
                $"The following quoted item(s) are not part of this canvass: {string.Join(", ", outOfScope)}.");

        var lineItems = command.LineItems.Select(li =>
            (li.Description, li.Unit, li.Quantity, li.UnitPrice));

        var quotation = CanvassQuotation.Create(
            tenantId,
            command.CanvassRequestId,
            command.SupplierId,
            command.SupplierName,
            command.SupplierAddress,
            command.TinNumber,
            command.QuotationDate,
            command.DeliveryTerms,
            lineItems);

        quotation.CreatedBy = currentUser.GetUserId().ToString();

        dbContext.CanvassQuotations.Add(quotation);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CanvassQuotationDto(
            quotation.Id,
            quotation.SupplierId,
            quotation.SupplierName,
            quotation.SupplierAddress,
            quotation.TinNumber,
            quotation.QuotationDate,
            quotation.DeliveryTerms,
            quotation.IsAwarded,
            quotation.LineItems.Select(li => new CanvassQuotationLineItemDto(
                li.ItemNo, li.Description, li.Unit, li.Quantity, li.UnitPrice, li.Total)).ToList());
    }

    private string GetRequiredTenantId() =>
        currentUser.GetTenant()
        ?? dbContext.TenantInfo?.Identifier
        ?? throw new InvalidOperationException("Tenant ID required.");
}

