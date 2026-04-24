using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.PropertyAcknowledgementReceipts.GetPARById;

public sealed class GetPARByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetPARByIdQuery, PARDetailsDto>
{
    public async ValueTask<PARDetailsDto> Handle(GetPARByIdQuery query, CancellationToken cancellationToken)
    {
        var par = await dbContext.PropertyAcknowledgementReceipts
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (par is null)
        {
            throw new KeyNotFoundException($"Property Acknowledgement Receipt with ID {query.Id} not found.");
        }

        var items = await (
            from item in dbContext.PARItems.Where(x => x.PARId == query.Id)
            join inv in dbContext.TangibleInventoryItems.IgnoreQueryFilters()
                on item.TangibleInventoryItemId equals inv.Id
            orderby item.ItemNo
            select new PARItemDto(
                item.Id,
                item.ItemNo,
                item.TangibleInventoryItemId,
                inv.PropertyNo,
                item.Quantity,
                item.Unit,
                item.ItemDescription,
                item.UnitCost,
                item.TotalCost,
                item.EstimatedUsefulLifeYears,
                item.DateAcquired))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PARDetailsDto(
            par.Id,
            par.PARNo,
            par.Date,
            par.PARType.ToString(),
            par.ReceivedFromEmployeeId,
            par.ReceivedByEmployeeId,
            par.ApprovedByEmployeeId,
            par.CreatedOnUtc,
            par.CreatedBy,
            items);
    }
}
