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
        // Product code/name are no longer snapshotted on the inventory row — join Product live so the filters,
        // ordering, and DTO all reflect the current product. LEFT JOIN keeps inventory rows visible even if the
        // product was soft-deleted (matches the old snapshot behaviour of always showing the row).
        var rows =
            from pi in _dbContext.ProductInventories.AsNoTracking()
            join p in _dbContext.Products.AsNoTracking() on pi.ProductId equals p.Id into pg
            from p in pg.DefaultIfEmpty()
            select new { Inventory = pi, StockNo = p != null ? p.StockNo : "", Name = p != null ? p.Name : "" };

        if (query.WarehouseLocationId.HasValue && query.WarehouseLocationId != Guid.Empty)
            rows = rows.Where(x => x.Inventory.WarehouseLocationId == query.WarehouseLocationId);

        if (!string.IsNullOrWhiteSpace(query.ProductCode))
            rows = rows.Where(x => x.StockNo.Contains(query.ProductCode));

        if (!string.IsNullOrWhiteSpace(query.ProductName))
            rows = rows.Where(x => x.Name.Contains(query.ProductName));

        rows = rows.OrderBy(x => x.StockNo);

        var paged = await rows.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
        var items = paged.Items
            .Select(x => x.Inventory.ToProductInventoryDto(
                x.StockNo, x.Name, ExpendableModuleConstants.ResolveWarehouseName(x.Inventory.WarehouseLocationId)))
            .ToList();

        return new PagedResponse<ProductInventoryDto>
        {
            Items = items,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            TotalPages = paged.TotalPages
        };
    }
}

