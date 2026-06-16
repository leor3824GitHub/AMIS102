using System.Net;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Locations.DeleteLocation;

public sealed class DeleteLocationCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<DeleteLocationCommand>
{
    public async ValueTask<Unit> Handle(DeleteLocationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var location = await db.Locations
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Location with ID {command.Id} not found.");

        var inUse = await db.AssetRegistries
            .AnyAsync(x => x.CurrentLocationId == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (inUse)
        {
            throw new CustomException(
                "Location is currently assigned to one or more assets and cannot be deleted.",
                errors: null,
                HttpStatusCode.Conflict);
        }

        // Soft-delete handled by the auditing/soft-delete interceptor on the DbContext.
        db.Locations.Remove(location);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
