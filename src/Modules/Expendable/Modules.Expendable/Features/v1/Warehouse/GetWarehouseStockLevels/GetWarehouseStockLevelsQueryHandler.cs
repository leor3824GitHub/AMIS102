using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetWarehouseStockLevels;

public sealed class GetWarehouseStockLevelsQueryHandler : IQueryHandler<GetWarehouseStockLevelsQuery, PagedResponse<ProductInventoryDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetWarehouseStockLevelsQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<ProductInventoryDto>> Handle(GetWarehouseStockLevelsQuery query, CancellationToken cancellationToken)
    {
        // Product code/name are joined live (no longer snapshotted). LEFT JOIN keeps a row visible even if its
        // product was soft-deleted, preserving the old always-show-the-row behaviour.
        var rows =
            from pi in _dbContext.ProductInventories.AsNoTracking()
            where pi.WarehouseLocationId == query.WarehouseLocationId
            join p in _dbContext.Products.AsNoTracking() on pi.ProductId equals p.Id into pg
            from p in pg.DefaultIfEmpty()
            orderby (p != null ? p.StockNo : "")
            select new { Inventory = pi, StockNo = p != null ? p.StockNo : "", Name = p != null ? p.Name : "" };

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

