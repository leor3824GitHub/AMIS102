using FSH.Framework.Core.Exceptions;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceSchedule;

public sealed class DeleteMaintenanceScheduleHandler(
    VehicleDbContext db) : ICommandHandler<DeleteMaintenanceScheduleCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        DeleteMaintenanceScheduleCommand command,
        CancellationToken cancellationToken)
    {
        var schedule = await db.MaintenanceSchedules.FirstOrDefaultAsync(x => x.Id == command.ScheduleId, cancellationToken)
            ?? throw new NotFoundException($"Maintenance schedule {command.ScheduleId} not found");

        schedule.SoftDelete();
        db.MaintenanceSchedules.Update(schedule);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
