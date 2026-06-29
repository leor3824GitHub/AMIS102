using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Repairs;

public sealed class AcceptRepairCommandHandler(AssetRegisterDbContext db, ICurrentUser currentUser)
    : ICommandHandler<AcceptRepairCommand, PropertyRepairDto>
{
    public async ValueTask<PropertyRepairDto> Handle(AcceptRepairCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var repair = await db.PropertyRepairs.FirstOrDefaultAsync(x => x.Id == cmd.RepairId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Repair (RPRI) not found.");

        repair.Accept(cmd.AcceptedBy, cmd.AcceptedOn);
        repair.SetLastModifiedBy(currentUser.GetUserId().ToString());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var asset = await db.AssetRegistries.FirstAsync(a => a.Id == repair.AssetRegistryId, cancellationToken).ConfigureAwait(false);
        return repair.ToDto(asset);
    }
}
