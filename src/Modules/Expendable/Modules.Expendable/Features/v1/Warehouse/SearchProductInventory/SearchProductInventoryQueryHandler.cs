using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.SearchProductInventory;

public sealed class SearchProductInventoryQueryHandler : IQueryHandler<SearchProductInventoryQuery, PagedResponse<ProductInventoryDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public SearchProductInventoryQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<ProductInventoryDto>> Handle(SearchProductInventoryQuery query, CancellationToken cancellationToken)
    {
        var inventories = _dbContext.ProductInventories.AsNoTracking();

        if (query.WarehouseLocationId.HasValue && query.WarehouseLocationId != Guid.Empty)
            inventories = inventories.Where(pi => pi.WarehouseLocationId == query.WarehouseLocationId);

        if (!string.IsNullOrWhiteSpace(query.ProductCode))
            inventories = inventories.Where(pi => pi.ProductCode != null && pi.ProductCode.Contains(query.ProductCode));

        if (!string.IsNullOrWhiteSpace(query.ProductName))
            inventories = inventories.Where(pi => pi.ProductName != null && pi.ProductName.Contains(query.ProductName));

        inventories = inventories.OrderBy(pi => pi.ProductCode);

        var projected = inventories.Select(i => i.ToProductInventoryDto());
        return await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }
}

