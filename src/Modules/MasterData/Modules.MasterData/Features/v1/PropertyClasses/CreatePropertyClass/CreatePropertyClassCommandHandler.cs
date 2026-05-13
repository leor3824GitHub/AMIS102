using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.PropertyClasses;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.PropertyClasses.CreatePropertyClass;

public sealed class CreatePropertyClassCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<CreatePropertyClassCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePropertyClassCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await db.PropertyClasses
            .AnyAsync(x => x.Code == command.Code.ToUpperInvariant(), cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A property class with this code already exists.")
            ]);
        }

        var entity = PropertyClass.Create(command.Code, command.Name, command.Description);
        entity.CreatedBy = currentUser.GetUserId().ToString();

        db.PropertyClasses.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entity.Id;
    }
}

