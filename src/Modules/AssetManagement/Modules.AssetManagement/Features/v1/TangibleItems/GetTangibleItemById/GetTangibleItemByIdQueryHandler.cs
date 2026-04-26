using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Features.v1.TangibleItems.RegisterTangibleItem;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.TangibleItems.GetTangibleItemById;

public sealed class GetTangibleItemByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetTangibleItemByIdQuery, TangibleItemDto>
{
    public async ValueTask<TangibleItemDto> Handle(
        GetTangibleItemByIdQuery query,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.TangibleItems
            .Include(x => x.Item)
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new KeyNotFoundException($"Tangible item with ID {query.Id} not found.");
        }

        return new TangibleItemDto(
            item.Id,
            item.PropertyNo,
            item.PropertyClass,
            item.CategoryCode,
            item.ItemId,
            item.Item.Code,
            item.Item.Name,
            item.AcquisitionDate,
            item.Quantity,
            item.UnitCost,
            item.Remarks,
            item.PurchaseOrderId,
            item.CreatedOnUtc);
    }
}
