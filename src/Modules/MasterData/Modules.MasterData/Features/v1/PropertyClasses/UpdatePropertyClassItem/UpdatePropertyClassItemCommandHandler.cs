using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.PropertyClasses;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.PropertyClasses.UpdatePropertyClassItem;

public sealed class UpdatePropertyClassItemCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdatePropertyClassItemCommand>
{
    public async ValueTask<Unit> Handle(UpdatePropertyClassItemCommand command, CancellationToken cancellationToken)
    {
        var item = await db.PropertyClassItems
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
            throw new KeyNotFoundException($"PropertyClassItem {command.Id} not found.");

        var codeInUse = await db.PropertyClassItems
            .AnyAsync(x => x.PropertyClassId == item.PropertyClassId
                        && x.ItemCode == command.ItemCode.ToUpperInvariant()
                        && x.Id != command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.ItemCode), "An item with this code already exists under this class.")
            ]);
        }

        item.Update(command.ItemCode, command.Name, command.Description, command.IsActive);
        item.LastModifiedBy = currentUser.GetUserId().ToString();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
