using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.PropertyClasses;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.PropertyClasses.CreatePropertyClassItem;

public sealed class CreatePropertyClassItemCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<CreatePropertyClassItemCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePropertyClassItemCommand command, CancellationToken cancellationToken)
    {
        var parent = await db.PropertyClasses
            .FirstOrDefaultAsync(x => x.Id == command.PropertyClassId, cancellationToken)
            .ConfigureAwait(false);

        if (parent is null)
            throw new KeyNotFoundException($"PropertyClass {command.PropertyClassId} not found.");

        var codeInUse = await db.PropertyClassItems
            .AnyAsync(x => x.PropertyClassId == command.PropertyClassId
                        && x.ItemCode == command.ItemCode.ToUpperInvariant(), cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.ItemCode), "An item with this code already exists under this class.")
            ]);
        }

        var item = PropertyClassItem.Create(
            command.PropertyClassId,
            parent.Code,
            command.ItemCode,
            command.Name,
            command.Description);
        item.CreatedBy = currentUser.GetUserId().ToString();

        db.PropertyClassItems.Add(item);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return item.Id;
    }
}

