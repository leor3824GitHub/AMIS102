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
        CancellationToken ct)
    {
        var schedule = await db.MaintenanceSchedules.FirstOrDefaultAsync(x => x.Id == command.ScheduleId, ct)
            ?? throw new ApplicationException($"Maintenance schedule {command.ScheduleId} not found");

        schedule.Deactivate();
        db.MaintenanceSchedules.Update(schedule);
        await db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
