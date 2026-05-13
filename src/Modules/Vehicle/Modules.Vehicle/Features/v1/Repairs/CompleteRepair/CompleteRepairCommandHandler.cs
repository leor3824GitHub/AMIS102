using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Repairs;
using AMIS.Modules.Vehicle.Domain.Vehicles;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.CompleteRepair;

public sealed class CompleteRepairCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<CompleteRepairCommand, Unit>
{
    public async ValueTask<Unit> Handle(CompleteRepairCommand cmd, CancellationToken ct)
    {
        var record = await db.RepairRecords.FirstOrDefaultAsync(r => r.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Repair record not found.")]);

        if (record.Status != RepairStatus.InProgress)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Only in-progress repairs can be completed.")]);

        record.Complete(cmd.CompletedDate);
        record.SetLastModifiedBy(currentUser.GetUserId().ToString());

        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == record.VehicleId, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Vehicle not found for this repair record.")]);

        // Reactivate vehicle if no other in-progress repairs remain
        var otherActiveRepairs = await db.RepairRecords
            .AnyAsync(r => r.VehicleId == record.VehicleId && r.Id != record.Id && r.Status == RepairStatus.InProgress, ct)
            .ConfigureAwait(false);

        if (!otherActiveRepairs && vehicle.Status != VehicleStatus.Active)
        {
            try
            {
                vehicle.Reactivate();
            }
            catch (InvalidOperationException ex)
            {
                throw new FluentValidation.ValidationException([new ValidationFailure(nameof(cmd.Id), ex.Message)]);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}

