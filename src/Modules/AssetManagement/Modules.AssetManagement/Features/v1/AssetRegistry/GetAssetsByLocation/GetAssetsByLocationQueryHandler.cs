using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.AssetRegistryQueries.GetAssetsByLocation;

public sealed class GetAssetsByLocationQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetAssetsByLocationQuery, PagedAssetsByLocationResponse>
{
    public async ValueTask<PagedAssetsByLocationResponse> Handle(
        GetAssetsByLocationQuery query,
        CancellationToken cancellationToken)
    {
        var q =
            from registry in dbContext.AssetRegistry
            join location in dbContext.Locations on registry.CurrentLocationId equals location.Id into locations
            from location in locations.DefaultIfEmpty()
            where registry.CurrentLocationId == query.LocationId
            select new AssetRegistryByLocationDto(
                registry.Id,
                registry.TangibleInventoryItemId,
                registry.PropertyNo,
                registry.AssetType.ToString(),
                registry.LifecycleState.ToString(),
                registry.CurrentPropertyStatus.HasValue ? registry.CurrentPropertyStatus.Value.ToString() : null,
                registry.CurrentCustodianId,
                registry.CurrentLocationId,
                location != null ? location.Name : null,
                registry.AcquisitionDate,
                registry.UnitCost);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim().ToLower();
            q = q.Where(x => x.PropertyNo.ToLower().Contains(keyword));
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

        var items = await q
            .OrderBy(x => x.PropertyNo)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedAssetsByLocationResponse(items, pageNumber, pageSize, totalCount);
    }
}

