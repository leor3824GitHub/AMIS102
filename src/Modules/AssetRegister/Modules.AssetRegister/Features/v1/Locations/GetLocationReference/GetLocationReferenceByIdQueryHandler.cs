using AMIS.Modules.AssetRegister.Contracts.v1.Locations;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.GetLocationReference;

/// <summary>
/// Resolves a location id to its code/name for cross-module consumers (e.g. printed reports).
/// Returns <c>null</c> when the location is missing rather than throwing — callers on a print
/// path degrade gracefully to a blank field.
/// </summary>
public sealed class GetLocationReferenceByIdQueryHandler(AssetRegisterDbContext db)
    : IQueryHandler<GetLocationReferenceByIdQuery, LocationReferenceDto?>
{
    public async ValueTask<LocationReferenceDto?> Handle(GetLocationReferenceByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await db.Locations
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new LocationReferenceDto(x.Id, x.Code, x.Name))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
