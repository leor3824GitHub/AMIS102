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
        var inventories = _dbContext.ProductInventories.AsNoTracking()
            .Where(pi => pi.WarehouseLocationId == query.WarehouseLocationId)
            .OrderBy(pi => pi.ProductCode);

        var projected = inventories.Select(i => i.ToProductInventoryDto());
        return await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }
}

