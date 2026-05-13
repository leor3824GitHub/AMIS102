using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.StartRepair;

public sealed class StartRepairCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<StartRepairCommand, Unit>
{
    public async ValueTask<Unit> Handle(StartRepairCommand cmd, CancellationToken ct)
    {
        var record = await db.RepairRecords.FirstOrDefaultAsync(r => r.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Repair record not found.")]);

        if (record.Status != RepairStatus.Pending)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Only pending repairs can be started.")]);

        record.StartRepair();
        record.SetLastModifiedBy(currentUser.GetUserId().ToString());

        // Mark the vehicle as under repair
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == record.VehicleId, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Vehicle not found for this repair record.")]);

        try
        {
            vehicle.MarkUnderRepair();
        }
        catch (InvalidOperationException ex)
        {
            throw new FluentValidation.ValidationException([new ValidationFailure(nameof(cmd.Id), ex.Message)]);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}

