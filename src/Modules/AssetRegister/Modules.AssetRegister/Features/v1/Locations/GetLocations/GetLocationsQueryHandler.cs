using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Features.v1.Locations;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.GetLocations;

public sealed class GetLocationsQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetLocationsQuery, PagedLocationsResponse>
{
    public async ValueTask<PagedLocationsResponse> Handle(
        GetLocationsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.Locations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim().ToLower();
            q = q.Where(x =>
                x.Code.ToLower().Contains(keyword) ||
                x.Name.ToLower().Contains(keyword));
        }

        if (query.Type.HasValue)
        {
            q = q.Where(x => x.Type == query.Type.Value);
        }

        if (query.ParentLocationId.HasValue)
        {
            q = q.Where(x => x.ParentLocationId == query.ParentLocationId.Value);
        }

        var totalCount = await q.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var items = await q
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LocationDto(
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                x.ParentLocationId,
                x.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedLocationsResponse(items, pageNumber, pageSize, totalCount);
    }
}
