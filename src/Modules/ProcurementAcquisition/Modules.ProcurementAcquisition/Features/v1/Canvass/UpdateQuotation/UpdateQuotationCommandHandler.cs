using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.UpdateQuotation;

public sealed class UpdateQuotationCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<UpdateQuotationCommand, CanvassQuotationDto>
{
    public async ValueTask<CanvassQuotationDto> Handle(UpdateQuotationCommand command, CancellationToken cancellationToken)
    {
        var quotation = await dbContext.CanvassQuotations
            .FirstOrDefaultAsync(x => x.Id == command.QuotationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Canvass quotation '{command.QuotationId}' not found.");

        var lineItems = command.LineItems.Select(li =>
            (li.Description, li.Unit, li.Quantity, li.UnitPrice));

        quotation.Update(
            command.SupplierName,
            command.SupplierAddress,
            command.TinNumber,
            command.QuotationDate,
            command.DeliveryTerms,
            lineItems);

        quotation.LastModifiedBy = currentUser.GetUserId().ToString();

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
}

