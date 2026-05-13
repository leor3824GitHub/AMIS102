using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.PropertyClasses;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;


namespace AMIS.Modules.MasterData.Features.v1.PropertyClasses.UpdatePropertyClass;

public sealed class UpdatePropertyClassCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdatePropertyClassCommand>
{
    public async ValueTask<Unit> Handle(UpdatePropertyClassCommand command, CancellationToken cancellationToken)
    {
        var entity = await db.PropertyClasses
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            throw new KeyNotFoundException($"PropertyClass {command.Id} not found.");

        entity.Update(command.Name, command.Description, command.IsActive);
        entity.LastModifiedBy = currentUser.GetUserId().ToString();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

