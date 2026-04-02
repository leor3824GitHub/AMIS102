using FluentValidation.Results;
using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Repairs;
using FSH.Modules.Vehicle.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Repairs.DeleteRepairRecord;

public sealed class DeleteRepairRecordCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<DeleteRepairRecordCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteRepairRecordCommand cmd, CancellationToken ct)
    {
        var record = await db.RepairRecords.FirstOrDefaultAsync(r => r.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Repair record not found.")]);

        record.SoftDelete(currentUser.GetUserId().ToString());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
