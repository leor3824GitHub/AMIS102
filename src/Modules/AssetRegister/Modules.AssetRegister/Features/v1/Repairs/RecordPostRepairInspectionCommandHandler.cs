using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1.Repairs;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Repairs;

public sealed class RecordPostRepairInspectionCommandHandler(AssetRegisterDbContext db, ICurrentUser currentUser)
    : ICommandHandler<RecordPostRepairInspectionCommand, PropertyRepairDto>
{
    public async ValueTask<PropertyRepairDto> Handle(RecordPostRepairInspectionCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var repair = await db.PropertyRepairs.FirstOrDefaultAsync(x => x.Id == cmd.RepairId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Repair (RPRI) not found.");

        repair.RecordPostRepairInspection(
            cmd.RepairShop, cmd.JobOrderNo, cmd.InvoiceNo, cmd.InvoiceDate, cmd.AmountPerJO,
            cmd.Findings, cmd.PostInspectedBy, cmd.PostInspectedOn,
            cmd.PrNo, cmd.PoJoNo, cmd.BurNo, cmd.DvNo);
        repair.SetLastModifiedBy(currentUser.GetUserId().ToString());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var asset = await db.AssetRegistries.FirstAsync(a => a.Id == repair.AssetRegistryId, cancellationToken).ConfigureAwait(false);
        return repair.ToDto(asset);
    }
}
