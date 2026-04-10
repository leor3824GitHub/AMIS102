using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSById;

public sealed class GetICSByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetICSByIdQuery, ICSDetailsDto>
{
    public async ValueTask<ICSDetailsDto> Handle(GetICSByIdQuery query, CancellationToken cancellationToken)
    {
        var ics = await dbContext.InventoryCustodianSlips
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (ics is null)
        {
            throw new KeyNotFoundException($"Inventory Custodian Slip with ID {query.Id} not found.");
        }

        var items = await dbContext.ICSItems
            .Where(x => x.ICSId == query.Id)
            .Join(
                dbContext.SemiExpendableProperties.Include(p => p.SemiExpendableItem),
                icsItem => icsItem.SemiExpendablePropertyId,
                prop => prop.Id,
                (icsItem, prop) => new ICSItemDetailsDto(
                    icsItem.Id,
                    icsItem.ItemNo,
                    prop.Id,
                    prop.PropertyNo,
                    prop.SemiExpendableItem.Code,
                    prop.SemiExpendableItem.Name,
                    prop.SerialNo,
                    icsItem.Description,
                    icsItem.UnitCost,
                    icsItem.EstimatedUsefulLifeYears))
            .OrderBy(x => x.ItemNo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ICSDetailsDto(
            ics.Id,
            ics.ICSNo,
            ics.Date,
            ics.FundCluster,
            ics.IssuedFromEmployeeId,
            ics.ReceivedByEmployeeId,
            ics.CreatedOnUtc,
            ics.CreatedBy,
            items);
    }
}
