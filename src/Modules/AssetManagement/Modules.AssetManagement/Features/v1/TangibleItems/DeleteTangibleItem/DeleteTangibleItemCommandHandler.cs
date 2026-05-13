using AMIS.Framework.Core.Context;
using AMIS.Modules.AssetManagement.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.TangibleItems.DeleteTangibleItem;

public sealed class DeleteTangibleItemCommandHandler(
    AssetManagementDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<DeleteTangibleItemCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        DeleteTangibleItemCommand command,
        CancellationToken cancellationToken)
    {
        var tangibleItem = await dbContext.TangibleItems
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (tangibleItem is null)
        {
            throw new KeyNotFoundException($"Tangible item with ID {command.Id} not found.");
        }

        tangibleItem.IsDeleted = true;
        tangibleItem.DeletedOnUtc = DateTimeOffset.UtcNow;
        tangibleItem.DeletedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}

