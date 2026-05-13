using FluentValidation.Results;
using AMIS.Framework.Core.Context;
using AMIS.Modules.Vehicle.Contracts.v1.Repairs;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Repairs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Repairs.CancelRepair;

public sealed class CancelRepairCommandHandler(VehicleDbContext db, ICurrentUser currentUser)
    : ICommandHandler<CancelRepairCommand, Unit>
{
    public async ValueTask<Unit> Handle(CancelRepairCommand cmd, CancellationToken ct)
    {
        var record = await db.RepairRecords.FirstOrDefaultAsync(r => r.Id == cmd.Id, ct).ConfigureAwait(false)
            ?? throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), "Repair record not found.")]);

        if (record.Status == RepairStatus.Completed || record.Status == RepairStatus.Cancelled)
            throw new FluentValidation.ValidationException(
            [new ValidationFailure(nameof(cmd.Id), $"Cannot cancel a repair that is already {record.Status}.")]);

        var wasInProgress = record.Status == RepairStatus.InProgress;
        record.Cancel();
        record.SetLastModifiedBy(currentUser.GetUserId().ToString());

        // If we cancelled an in-progress repair, check if vehicle should be reactivated
        if (wasInProgress)
        {
            var otherActiveRepairs = await db.RepairRecords
                .AnyAsync(r => r.VehicleId == record.VehicleId && r.Id != record.Id && r.Status == RepairStatus.InProgress, ct)
                .ConfigureAwait(false);

            if (!otherActiveRepairs)
            {
                var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == record.VehicleId, ct).ConfigureAwait(false);
                vehicle?.Reactivate();
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}

