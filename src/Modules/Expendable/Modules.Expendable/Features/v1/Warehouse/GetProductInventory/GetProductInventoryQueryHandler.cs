using AMIS.Framework.Caching;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetProductInventory;

public sealed class GetProductInventoryQueryHandler : IQueryHandler<GetProductInventoryQuery, ProductInventoryDto?>
{
    private readonly ExpendableDbContext _dbContext;
    private readonly ICacheService _cache;

    public GetProductInventoryQueryHandler(ExpendableDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async ValueTask<ProductInventoryDto?> Handle(GetProductInventoryQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"inventory:{query.ProductId}:{query.WarehouseLocationId}";
        var cached = await _cache.GetItemAsync<ProductInventoryDto>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var inventory = await _dbContext.ProductInventories
            .FirstOrDefaultAsync(pi =>
                pi.ProductId == query.ProductId &&
                pi.WarehouseLocationId == query.WarehouseLocationId,
                cancellationToken);

        if (inventory == null) return null;

        var dto = inventory.ToProductInventoryDto();
        await _cache.SetItemAsync(cacheKey, dto, TimeSpan.FromHours(1), cancellationToken);

        return dto;
    }
}

