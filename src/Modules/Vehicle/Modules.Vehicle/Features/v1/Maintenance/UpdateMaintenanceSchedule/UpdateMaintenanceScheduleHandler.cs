using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceSchedule;

public sealed class UpdateMaintenanceScheduleHandler(
    VehicleDbContext db) : ICommandHandler<UpdateMaintenanceScheduleCommand, MaintenanceScheduleDto>
{
    public async ValueTask<MaintenanceScheduleDto> Handle(
        UpdateMaintenanceScheduleCommand command,
        CancellationToken ct)
    {
        var schedule = await db.MaintenanceSchedules.FirstOrDefaultAsync(x => x.Id == command.ScheduleId, ct)
            ?? throw new ApplicationException($"Maintenance schedule {command.ScheduleId} not found");

        schedule.Update(
            command.MaintenanceType,
            command.Description,
            command.IntervalDays,
            command.IntervalMileage,
            command.DueDate,
            command.DueMileage);

        db.MaintenanceSchedules.Update(schedule);
        await db.SaveChangesAsync(ct);

        return new MaintenanceScheduleDto(
            schedule.Id,
            schedule.VehicleId,
            schedule.MaintenanceType,
            schedule.Description,
            schedule.IntervalDays,
            schedule.IntervalMileage,
            schedule.DueDate,
            schedule.DueMileage,
            schedule.LastDoneDate,
            schedule.LastDoneMileage,
            schedule.IsActive);
    }
}
