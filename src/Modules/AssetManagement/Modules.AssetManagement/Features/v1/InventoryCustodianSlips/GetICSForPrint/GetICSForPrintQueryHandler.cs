using AMIS.Modules.AssetManagement.Contracts.v1.InventoryCustodianSlips;
using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSForPrint;

public sealed class GetICSForPrintQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetICSForPrintQuery, ICSForPrintDto?>
{
    public async ValueTask<ICSForPrintDto?> Handle(GetICSForPrintQuery query, CancellationToken ct)
    {
        var ics = await dbContext.InventoryCustodianSlips
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct)
            .ConfigureAwait(false);

        if (ics is null)
            return null;

        var items = await (
            from icsItem in dbContext.ICSItems.Where(x => x.ICSId == query.Id)
            join invItem in dbContext.TangibleInventoryItems on icsItem.TangibleInventoryItemId equals invItem.Id
            join catalogItem in dbContext.PropertyItemCatalog on invItem.ItemId equals catalogItem.Id
            orderby icsItem.ItemNo
            select new ICSItemForPrintDto(
                icsItem.ItemNo,
                invItem.PropertyNo,
                catalogItem.UnitOfMeasure,
                icsItem.Description,
                catalogItem.Name,
                icsItem.UnitCost,
                icsItem.EstimatedUsefulLifeYears))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new ICSForPrintDto(
            ics.Id,
            ics.ICSNo,
            ics.Date,
            ics.FundCluster,
            ics.IssuedFromEmployeeId,
            ics.ReceivedByEmployeeId,
            items);
    }
}
