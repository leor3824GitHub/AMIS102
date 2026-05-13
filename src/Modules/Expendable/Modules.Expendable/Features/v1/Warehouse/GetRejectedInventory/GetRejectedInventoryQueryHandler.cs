using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetRejectedInventory;

public sealed class GetRejectedInventoryQueryHandler : IQueryHandler<GetRejectedInventoryQuery, PagedResponse<RejectedInventoryDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetRejectedInventoryQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<RejectedInventoryDto>> Handle(GetRejectedInventoryQuery query, CancellationToken cancellationToken)
    {
        var rejected = _dbContext.RejectedInventories.AsNoTracking();

        if (query.WarehouseLocationId.HasValue && query.WarehouseLocationId != Guid.Empty)
            rejected = rejected.Where(ri => ri.WarehouseLocationId == query.WarehouseLocationId);

        if (!string.IsNullOrWhiteSpace(query.Status))
            rejected = rejected.Where(ri => ri.Status.ToString() == query.Status);

        rejected = rejected.OrderByDescending(ri => ri.RejectionDate);

        var projected = rejected.Select(ri => ri.ToRejectedInventoryDto());
        return await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }
}

