using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Products;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetLowStockProducts;

/// <summary>
/// Reorder worklist. For each active product with a configured minimum (&gt; 0), sums on-hand
/// (available + reserved) across all warehouses via a correlated subquery over ProductInventory, and
/// keeps those at or below their minimum. Products that have never been stocked (no inventory rows)
/// count as 0 on-hand and are therefore included. Both the products and the inventory subquery are
/// tenant-scoped by the ambient query filters.
/// </summary>
public sealed class GetLowStockProductsQueryHandler(ExpendableDbContext db)
    : IQueryHandler<GetLowStockProductsQuery, IReadOnlyList<LowStockProductDto>>
{
    public async ValueTask<IReadOnlyList<LowStockProductDto>> Handle(GetLowStockProductsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rows = await db.Products.AsNoTracking()
            .Where(p => p.MinimumStockLevel > 0 && p.Status == ProductStatus.Active)
            .Select(p => new
            {
                p.Id,
                p.StockNo,
                p.Article,
                p.Name,
                p.UnitOfMeasure,
                p.MinimumStockLevel,
                p.ReorderQuantity,
                OnHand = db.ProductInventories
                    .Where(pi => pi.ProductId == p.Id)
                    .Sum(pi => pi.QuantityAvailable + pi.QuantityReserved)
            })
            .Where(x => x.OnHand <= x.MinimumStockLevel)
            .OrderBy(x => x.OnHand)
            .ThenBy(x => x.Name)
            .Select(x => new LowStockProductDto(
                x.Id, x.StockNo, x.Article, x.Name, x.UnitOfMeasure,
                x.OnHand, x.MinimumStockLevel, x.ReorderQuantity))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows;
    }
}
