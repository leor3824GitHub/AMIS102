using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItems;

public sealed class GetSemiExpendableItemsQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetSemiExpendableItemsQuery, PagedSemiExpendableItemsResponse>
{
    public async ValueTask<PagedSemiExpendableItemsResponse> Handle(GetSemiExpendableItemsQuery query, CancellationToken cancellationToken)
    {
        var itemsQuery = dbContext.SemiExpendableItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.ToLower();
            itemsQuery = itemsQuery.Where(x =>
                x.Code.ToLower().Contains(kw) ||
                x.Name.ToLower().Contains(kw) ||
                (x.UACSObjectCode != null && x.UACSObjectCode.ToLower().Contains(kw)) ||
                (x.Description != null && x.Description.ToLower().Contains(kw)));
        }

        if (query.IsActive.HasValue)
        {
            itemsQuery = itemsQuery.Where(x => x.IsActive == query.IsActive.Value);
        }

        var totalCount = await itemsQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

        var items = await itemsQuery
            .OrderBy(x => x.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SemiExpendableItemSummaryDto(
                x.Id,
                x.Code,
                x.Name,
                x.Description,
                x.UACSObjectCode,
                x.UnitOfMeasure,
                x.EstimatedUsefulLifeYears,
                x.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedSemiExpendableItemsResponse(items, pageNumber, pageSize, totalCount);
    }
}
