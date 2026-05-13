using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Persistence;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Domain.Purchases;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Expendable.Features.v1.Warehouse.GetPendingInspections;

public sealed class GetPendingInspectionsQueryHandler : IQueryHandler<GetPendingInspectionsQuery, PagedResponse<PurchaseInspectionDto>>
{
    private readonly ExpendableDbContext _dbContext;

    public GetPendingInspectionsQueryHandler(ExpendableDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<PurchaseInspectionDto>> Handle(GetPendingInspectionsQuery query, CancellationToken cancellationToken)
    {
        var inspections = _dbContext.PurchaseInspections.AsNoTracking()
            .Where(pi => pi.Status == InspectionStatus.Pending);

        if (query.WarehouseLocationId.HasValue && query.WarehouseLocationId != Guid.Empty)
            inspections = inspections.Where(pi => pi.WarehouseLocationId == query.WarehouseLocationId);

        inspections = inspections.OrderByDescending(pi => pi.InspectionDate);

        var projected = inspections.Select(i => i.ToPurchaseInspectionDto());
        return await projected.ToPagedResponseAsync(query, cancellationToken).ConfigureAwait(false);
    }
}

