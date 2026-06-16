using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Features.v1.Locations;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.UpdateLocation;

public sealed class UpdateLocationCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<UpdateLocationCommand, LocationDto>
{
    public async ValueTask<LocationDto> Handle(UpdateLocationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var location = await db.Locations
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Location with ID {command.Id} not found.");

        var code = command.Code.Trim();
        var name = command.Name.Trim();

        var duplicateCode = await db.Locations
            .AnyAsync(x => x.Id != command.Id && x.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (duplicateCode)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), $"Location code '{code}' already exists.")
            ]);
        }

        if (command.ParentLocationId == command.Id)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.ParentLocationId), "Location cannot be its own parent.")
            ]);
        }

        if (command.ParentLocationId.HasValue)
        {
            var parentExists = await db.Locations
                .AnyAsync(x => x.Id == command.ParentLocationId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!parentExists)
            {
                throw new NotFoundException($"Parent location with ID {command.ParentLocationId.Value} not found.");
            }
        }

        location.Update(code, name, command.Type, command.ParentLocationId, command.Description?.Trim());

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new LocationDto(
            location.Id,
            location.Code,
            location.Name,
            location.Type,
            location.ParentLocationId,
            location.Description);
    }
}
