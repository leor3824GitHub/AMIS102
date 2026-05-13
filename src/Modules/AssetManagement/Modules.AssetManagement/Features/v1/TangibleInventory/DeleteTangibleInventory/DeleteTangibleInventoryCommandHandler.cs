using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleInventory.DeleteTangibleInventory;

public sealed class DeleteTangibleInventoryCommandHandler(
    AssetManagementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<DeleteTangibleInventoryCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        DeleteTangibleInventoryCommand command,
        CancellationToken cancellationToken)
    {
        var inventory = await dbContext.TangibleInventories
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (inventory is null)
            throw new KeyNotFoundException($"Tangible Inventory with ID {command.Id} not found.");

        inventory.IsDeleted = true;
        inventory.DeletedOnUtc = DateTimeOffset.UtcNow;
        inventory.DeletedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

