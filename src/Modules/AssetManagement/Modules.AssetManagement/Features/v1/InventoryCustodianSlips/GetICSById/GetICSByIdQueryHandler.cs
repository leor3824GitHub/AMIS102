using FSH.Framework.Core.Exceptions;
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
            throw new NotFoundException($"Inventory Custodian Slip with ID {query.Id} not found.");
        }

        var items = await (
            from icsItem in dbContext.ICSItems.Where(x => x.ICSId == query.Id)
            join prop in dbContext.SemiExpendableProperties on icsItem.SemiExpendablePropertyId equals prop.Id
            join catalogItem in dbContext.PropertyItemCatalog on prop.ItemId equals catalogItem.Id
            orderby icsItem.ItemNo
            select new ICSItemDetailsDto(
                icsItem.Id,
                icsItem.ItemNo,
                prop.Id,
                prop.PropertyNo,
                catalogItem.Code,
                catalogItem.Name,
                prop.SerialNo,
                icsItem.Description,
                icsItem.UnitCost,
                icsItem.EstimatedUsefulLifeYears,
                icsItem.CategoryAtTimeOfIssuance.ToString()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ICSDetailsDto(
            ics.Id,
            ics.ICSNo,
            ics.Date,
            ics.Category.ToString(),
            ics.Status.ToString(),
            ics.ExpiresOn,
            ics.FundCluster,
            ics.IssuedFromEmployeeId,
            ics.ReceivedByEmployeeId,
            ics.RenewedFromICSId,
            ics.RenewedByICSId,
            ics.CancelledByRRSPId,
            ics.CreatedOnUtc,
            ics.CreatedBy,
            items);
    }
}
