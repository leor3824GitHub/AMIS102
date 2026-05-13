using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.AssetManagement.Domain;
using AMIS.Modules.AssetManagement.Features.v1.Locations;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.Locations.CreateLocation;

public sealed class CreateLocationCommandHandler(
    AssetManagementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateLocationCommand, LocationDto>
{
    public async ValueTask<LocationDto> Handle(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        var code = command.Code.Trim();
        var name = command.Name.Trim();

        var codeExists = await dbContext.Locations
            .AnyAsync(x => x.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (codeExists)
        {
            throw new InvalidOperationException($"Location code '{code}' already exists.");
        }

        if (command.ParentLocationId.HasValue)
        {
            var parentExists = await dbContext.Locations
                .AnyAsync(x => x.Id == command.ParentLocationId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!parentExists)
            {
                throw new KeyNotFoundException($"Parent location with ID {command.ParentLocationId.Value} not found.");
            }
        }

        var location = Location.Create(
            currentUser.GetTenant() ?? string.Empty,
            code,
            name,
            command.Type,
            command.ParentLocationId,
            command.Description?.Trim());

        location.CreatedBy = currentUser.GetUserId().ToString();
        dbContext.Locations.Add(location);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new LocationDto(
            location.Id,
            location.Code,
            location.Name,
            location.Type,
            location.ParentLocationId,
            location.Description);
    }
}

