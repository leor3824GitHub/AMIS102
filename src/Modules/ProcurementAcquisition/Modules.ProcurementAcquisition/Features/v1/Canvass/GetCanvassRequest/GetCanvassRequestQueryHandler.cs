using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetCanvassRequest;

public sealed class GetCanvassRequestQueryHandler(ProcurementDbContext dbContext)
    : IQueryHandler<GetCanvassRequestQuery, CanvassRequestDto?>
{
    public async ValueTask<CanvassRequestDto?> Handle(GetCanvassRequestQuery query, CancellationToken cancellationToken)
    {
        var canvass = await dbContext.CanvassRequests
            .AsNoTracking()
            .Include(x => x.Quotations)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (canvass is null) return null;

        var pr = await dbContext.PurchaseRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == canvass.PurchaseRequestId, cancellationToken)
            .ConfigureAwait(false);

        return new CanvassRequestDto(
            canvass.Id,
            canvass.RivNumber,
            canvass.PurchaseRequestId,
            pr?.PrNumber ?? string.Empty,
            canvass.ReturnDeadline,
            canvass.Status,
            canvass.AwardedSupplierId,
            null,
            canvass.Quotations.Select(q => new CanvassQuotationDto(
                q.Id,
                q.SupplierId,
                q.SupplierName,
                q.SupplierAddress,
                q.TinNumber,
                q.QuotationDate,
                q.DeliveryTerms,
                q.IsAwarded,
                q.LineItems.Select(li => new CanvassQuotationLineItemDto(
                    li.ItemNo, li.Description, li.Unit, li.Quantity, li.UnitPrice, li.Total)).ToList()
            )).ToList(),
            canvass.CreatedOnUtc,
            canvass.CreatedBy);
    }
}

