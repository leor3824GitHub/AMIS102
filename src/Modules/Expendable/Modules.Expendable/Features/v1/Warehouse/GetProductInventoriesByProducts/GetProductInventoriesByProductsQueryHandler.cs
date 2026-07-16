using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetProductInventoriesByProducts;

public sealed class GetProductInventoriesByProductsQueryHandler(ExpendableDbContext dbContext)
    : IQueryHandler<GetProductInventoriesByProductsQuery, IReadOnlyList<ProductInventoryDto>>
{
    public async ValueTask<IReadOnlyList<ProductInventoryDto>> Handle(
        GetProductInventoriesByProductsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ProductIds.Count == 0)
            return [];

        var productIds = query.ProductIds.Distinct().ToArray();

        var inventories = await dbContext.ProductInventories.AsNoTracking()
            .Where(pi => productIds.Contains(pi.ProductId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Join product code/name live (no longer snapshotted); one round-trip for the bounded product set.
        var products = await dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.StockNo, p.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var productById = products.ToDictionary(p => p.Id);

        return inventories
            .OrderBy(pi => productById.TryGetValue(pi.ProductId, out var p) ? p.StockNo : string.Empty, StringComparer.Ordinal)
            .Select(pi =>
            {
                productById.TryGetValue(pi.ProductId, out var p);
                return pi.ToProductInventoryDto(
                    p?.StockNo ?? string.Empty,
                    p?.Name ?? string.Empty,
                    ExpendableModuleConstants.ResolveWarehouseName(pi.WarehouseLocationId));
            })
            .ToList();
    }
}
