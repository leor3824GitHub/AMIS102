using FSH.Framework.Core.Exceptions;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.DeactivateMaintenanceSchedule;

public sealed class DeactivateMaintenanceScheduleHandler(
    VehicleDbContext db) : ICommandHandler<DeactivateMaintenanceScheduleCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        DeactivateMaintenanceScheduleCommand command,
        CancellationToken cancellationToken)
    {
        var schedule = await db.MaintenanceSchedules.FirstOrDefaultAsync(x => x.Id == command.ScheduleId, cancellationToken)
            ?? throw new NotFoundException($"Maintenance schedule {command.ScheduleId} not found");

        schedule.Deactivate();
        db.MaintenanceSchedules.Update(schedule);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
