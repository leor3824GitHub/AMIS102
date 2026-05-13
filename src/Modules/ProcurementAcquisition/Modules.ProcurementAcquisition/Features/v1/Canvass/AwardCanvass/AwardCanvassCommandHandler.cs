using AMIS.Framework.Core.Context;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.CreateCanvassRequest;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AwardCanvass;

public sealed class AwardCanvassCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<AwardCanvassCommand, CanvassRequestDto>
{
    public async ValueTask<CanvassRequestDto> Handle(AwardCanvassCommand command, CancellationToken cancellationToken)
    {
        var canvass = await dbContext.CanvassRequests
            .Include(x => x.Quotations)
            .FirstOrDefaultAsync(x => x.Id == command.CanvassRequestId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Canvass request '{command.CanvassRequestId}' not found.");

        var awardedQuotation = canvass.Quotations.FirstOrDefault(q => q.Id == command.AwardedQuotationId)
            ?? throw new KeyNotFoundException($"Quotation '{command.AwardedQuotationId}' not found in canvass request.");

        // Clear previous award flags
        foreach (var q in canvass.Quotations)
        {
            q.ClearAwarded();
        }

        awardedQuotation.MarkAwarded();
        canvass.Award(awardedQuotation.SupplierId);
        canvass.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var pr = await dbContext.PurchaseRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == canvass.PurchaseRequestId, cancellationToken)
            .ConfigureAwait(false);

        return CreateCanvassRequestCommandHandler.MapToDto(canvass, pr?.PrNumber ?? string.Empty);
    }
}

