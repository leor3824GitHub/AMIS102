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
            .OrderBy(pi => pi.ProductCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return inventories.Select(i => i.ToProductInventoryDto()).ToList();
    }
}
