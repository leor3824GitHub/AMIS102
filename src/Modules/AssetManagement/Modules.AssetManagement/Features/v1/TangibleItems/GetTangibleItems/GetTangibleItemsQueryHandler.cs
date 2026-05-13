using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleItems.GetTangibleItems;

public sealed class GetTangibleItemsQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetTangibleItemsQuery, PagedTangibleItemsResponse>
{
    public async ValueTask<PagedTangibleItemsResponse> Handle(
        GetTangibleItemsQuery query,
        CancellationToken cancellationToken)
    {
        var itemsQuery = dbContext.TangibleItems
            .Include(x => x.Item)
            .AsQueryable();

        if (query.ExcludeLinked == true)
        {
            itemsQuery = itemsQuery.Where(x => x.TangibleInventoryItemId == null);
        }

        if (!string.IsNullOrWhiteSpace(query.PropertyClass))
        {
            itemsQuery = itemsQuery.Where(x => x.PropertyClass == query.PropertyClass);
        }

        if (!string.IsNullOrWhiteSpace(query.CategoryCode))
        {
            itemsQuery = itemsQuery.Where(x => x.CategoryCode == query.CategoryCode);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            itemsQuery = itemsQuery.Where(x =>
                x.PropertyNo.ToLower().Contains(kw) ||
                x.Item.Code.ToLower().Contains(kw) ||
                x.Item.Name.ToLower().Contains(kw));
        }

        var totalCount = await itemsQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize   = query.PageSize   <= 0 ? 20 : query.PageSize;

        var items = await itemsQuery
            .OrderBy(x => x.PropertyNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TangibleItemSummaryDto(
                x.Id,
                x.PropertyNo,
                x.PropertyClass,
                x.CategoryCode,
                x.ItemId,
                x.Item.Code,
                x.Item.Name,
                x.AcquisitionDate,
                x.Quantity,
                x.UnitCost,
                x.Remarks,
                x.PurchaseOrderId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedTangibleItemsResponse(items, pageNumber, pageSize, totalCount);
    }
}

