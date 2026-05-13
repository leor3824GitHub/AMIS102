using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceSchedule;

public sealed class UpdateMaintenanceScheduleHandler(
    VehicleDbContext db) : ICommandHandler<UpdateMaintenanceScheduleCommand, MaintenanceScheduleDto>
{
    public async ValueTask<MaintenanceScheduleDto> Handle(
        UpdateMaintenanceScheduleCommand command,
        CancellationToken cancellationToken)
    {
        var schedule = await db.MaintenanceSchedules.FirstOrDefaultAsync(x => x.Id == command.ScheduleId, cancellationToken)
            ?? throw new NotFoundException($"Maintenance schedule {command.ScheduleId} not found");

        schedule.Update(
            command.MaintenanceType,
            command.Description,
            command.IntervalDays,
            command.IntervalMileage,
            command.DueDate,
            command.DueMileage);

        db.MaintenanceSchedules.Update(schedule);
        await db.SaveChangesAsync(cancellationToken);

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

