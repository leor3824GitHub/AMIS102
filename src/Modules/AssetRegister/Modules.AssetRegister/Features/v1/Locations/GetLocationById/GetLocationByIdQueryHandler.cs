using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Features.v1.Locations;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.GetLocationById;

public sealed class GetLocationByIdQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetLocationByIdQuery, LocationDto>
{
    public async ValueTask<LocationDto> Handle(GetLocationByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var location = await db.Locations
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new LocationDto(
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                x.ParentLocationId,
                x.Description))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Location with ID {query.Id} not found.");

        return location;
    }
}
