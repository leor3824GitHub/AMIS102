using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Features.v1.Locations;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Locations.GetLocationById;

public sealed class GetLocationByIdQueryHandler(AssetManagementDbContext dbContext)
    : IQueryHandler<GetLocationByIdQuery, LocationDto>
{
    public async ValueTask<LocationDto> Handle(GetLocationByIdQuery query, CancellationToken cancellationToken)
    {
        var location = await dbContext.Locations
            .Where(x => x.Id == query.Id)
            .Select(x => new LocationDto(
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                x.ParentLocationId,
                x.Description))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (location is null)
        {
            throw new NotFoundException($"Location with ID {query.Id} not found.");
        }

        return location;
    }
}
