using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetManagement.Features.v1.Locations.DeleteLocation;

public sealed class DeleteLocationCommandHandler(
    AssetManagementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<DeleteLocationCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteLocationCommand command, CancellationToken cancellationToken)
    {
        var location = await dbContext.Locations
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (location is null)
        {
            throw new NotFoundException($"Location with ID {command.Id} not found.");
        }

        var inUse = await dbContext.AssetRegistry
            .AnyAsync(x => x.CurrentLocationId == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (inUse)
        {
            throw new InvalidOperationException("Location is currently assigned to one or more assets and cannot be deleted.");
        }

        location.IsDeleted = true;
        location.DeletedOnUtc = DateTimeOffset.UtcNow;
        location.DeletedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
