using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.ListWarehouseLocations;

public sealed class ListWarehouseLocationsQueryHandler(ExpendableDbContext dbContext)
    : IQueryHandler<ListWarehouseLocationsQuery, IReadOnlyList<WarehouseLocationDto>>
{
    public async ValueTask<IReadOnlyList<WarehouseLocationDto>> Handle(
        ListWarehouseLocationsQuery query,
        CancellationToken cancellationToken)
    {
        // Location name is no longer snapshotted on the inventory row — pull the distinct ids that hold stock
        // and resolve each name from the location constant (single-storeroom today).
        var ids = await dbContext.ProductInventories.AsNoTracking()
            .Where(pi => pi.WarehouseLocationId != Guid.Empty)
            .Select(pi => pi.WarehouseLocationId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ids
            .Select(id => new WarehouseLocationDto(id, ExpendableModuleConstants.ResolveWarehouseName(id)))
            .OrderBy(w => w.Name, StringComparer.Ordinal)
            .ToList();
    }
}
