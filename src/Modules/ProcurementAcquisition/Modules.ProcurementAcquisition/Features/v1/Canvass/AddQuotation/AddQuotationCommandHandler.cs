using FSH.Framework.Core.Context;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using FSH.Modules.ProcurementAcquisition.Data;
using FSH.Modules.ProcurementAcquisition.Domain.Canvass;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.Canvass.AddQuotation;

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
