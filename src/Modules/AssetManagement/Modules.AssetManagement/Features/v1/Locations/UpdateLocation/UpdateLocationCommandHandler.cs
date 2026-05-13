using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Features.v1.Locations;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Locations.UpdateLocation;

public sealed class UpdateLocationCommandHandler(
    AssetManagementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<UpdateLocationCommand, LocationDto>
{
    public async ValueTask<LocationDto> Handle(UpdateLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await dbContext.Locations
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (location is null)
        {
            throw new NotFoundException($"Location with ID {command.Id} not found.");
        }

        var code = command.Code.Trim();
        var name = command.Name.Trim();

        var duplicateCode = await dbContext.Locations
            .AnyAsync(x => x.Id != command.Id && x.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (duplicateCode)
        {
            throw new InvalidOperationException($"Location code '{code}' already exists.");
        }

        if (command.ParentLocationId == command.Id)
        {
            throw new InvalidOperationException("Location cannot be its own parent.");
        }

        if (command.ParentLocationId.HasValue)
        {
            var parentExists = await dbContext.Locations
                .AnyAsync(x => x.Id == command.ParentLocationId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!parentExists)
            {
                throw new NotFoundException($"Parent location with ID {command.ParentLocationId.Value} not found.");
            }
        }

        location.Update(code, name, command.Type, command.ParentLocationId, command.Description?.Trim());
        location.LastModifiedBy = currentUser.GetUserId().ToString();

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
