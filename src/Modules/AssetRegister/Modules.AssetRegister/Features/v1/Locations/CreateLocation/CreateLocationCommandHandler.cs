using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Locations;
using AMIS.Modules.AssetRegister.Features.v1.Locations;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.CreateLocation;

public sealed class CreateLocationCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<CreateLocationCommand, LocationDto>
{
    public async ValueTask<LocationDto> Handle(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var code = command.Code.Trim();
        var name = command.Name.Trim();

        var codeExists = await db.Locations
            .AnyAsync(x => x.Code == code, cancellationToken)
            .ConfigureAwait(false);

        if (codeExists)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), $"Location code '{code}' already exists.")
            ]);
        }

        if (command.ParentLocationId.HasValue)
        {
            var parentExists = await db.Locations
                .AnyAsync(x => x.Id == command.ParentLocationId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!parentExists)
            {
                throw new KeyNotFoundException($"Parent location with ID {command.ParentLocationId.Value} not found.");
            }
        }

        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var location = Location.Create(
            tenantId,
            code,
            name,
            command.Type,
            command.ParentLocationId,
            command.Description?.Trim());

        db.Locations.Add(location);
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
