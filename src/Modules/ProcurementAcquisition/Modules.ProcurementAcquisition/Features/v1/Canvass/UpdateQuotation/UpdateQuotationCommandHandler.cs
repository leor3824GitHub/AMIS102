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
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Canvass quotation '{command.QuotationId}' not found.");

        // Reload the covered lines so revised quote lines are re-stamped with their PrItemNo (see resolver).
        var canvass = await dbContext.CanvassRequests
            .FirstOrDefaultAsync(x => x.Id == quotation.CanvassRequestId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Canvass request '{quotation.CanvassRequestId}' not found.");

        var prItemNoByDescription = CanvassQuotationLineResolver.BuildPrItemNoLookup(canvass);

        var outOfScope = command.LineItems
            .Where(li => !prItemNoByDescription.ContainsKey((li.Description ?? string.Empty).Trim().ToLowerInvariant()))
            .Select(li => li.Description)
            .ToList();
        if (outOfScope.Count > 0)
            throw new InvalidOperationException(
                $"The following quoted item(s) are not part of this canvass: {string.Join(", ", outOfScope)}.");

        var lineItems = command.LineItems.Select(li =>
            (prItemNoByDescription[(li.Description ?? string.Empty).Trim().ToLowerInvariant()],
             li.Description ?? string.Empty, li.Unit, li.Quantity, li.UnitPrice));

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
                li.ItemNo, li.PrItemNo, li.Description, li.Unit, li.Quantity, li.UnitPrice, li.Total)).ToList());
    }
}

