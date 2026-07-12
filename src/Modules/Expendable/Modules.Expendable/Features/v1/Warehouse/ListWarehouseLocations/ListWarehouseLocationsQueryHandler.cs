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
        => await dbContext.ProductInventories.AsNoTracking()
            .Where(pi => pi.WarehouseLocationId != Guid.Empty)
            .Select(pi => new WarehouseLocationDto(pi.WarehouseLocationId, pi.WarehouseLocationName))
            .Distinct()
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
