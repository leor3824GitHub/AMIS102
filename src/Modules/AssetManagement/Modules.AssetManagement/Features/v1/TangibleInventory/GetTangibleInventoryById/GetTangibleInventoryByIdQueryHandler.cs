using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.GetTangibleInventoryById;

public sealed class GetTangibleInventoryByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetTangibleInventoryByIdQuery, TangibleInventoryDetailDto>
{
    public async ValueTask<TangibleInventoryDetailDto> Handle(
        GetTangibleInventoryByIdQuery query,
        CancellationToken cancellationToken)
    {
        var inventory = await dbContext.TangibleInventories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (inventory is null)
            throw new KeyNotFoundException($"Tangible Inventory with ID {query.Id} not found.");

        var items = await dbContext.TangibleInventoryItems
            .AsNoTracking()
            .Where(x => x.TangibleInventoryId == query.Id)
            .OrderBy(x => x.PropertyNo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itemDtos = items
            .Select(i => new TangibleInventoryItemDto(
                i.Id,
                i.TangibleItemId,
                i.Reference,
                i.AssetType.ToString(),
                i.ThresholdAmountUsed,
                i.IsIssued,
                i.PropertyNo,
                i.ItemId,
                i.Description,
                i.AcquisitionDate,
                i.Quantity,
                i.UnitCost,
                i.Amount))
            .ToList();

        return new TangibleInventoryDetailDto(
            inventory.Id,
            inventory.ReportNo,
            inventory.Date,
            inventory.ReceivedFrom,
            inventory.Address,
            inventory.ReceiptType.ToString(),
            inventory.OtherReceiptType,
            inventory.FundCluster,
            inventory.ReceivedByEmployeeId,
            inventory.NotedByEmployeeId,
            itemDtos);
    }
}

