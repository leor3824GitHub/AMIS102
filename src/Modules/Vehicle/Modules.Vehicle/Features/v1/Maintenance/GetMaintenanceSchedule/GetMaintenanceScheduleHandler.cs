using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceSchedule;

public sealed class GetMaintenanceScheduleHandler(
    VehicleDbContext db) : IQueryHandler<GetMaintenanceScheduleQuery, MaintenanceScheduleDto>
{
    public async ValueTask<MaintenanceScheduleDto> Handle(
        GetMaintenanceScheduleQuery query,
        CancellationToken cancellationToken)
    {
        var schedule = await db.MaintenanceSchedules.FirstOrDefaultAsync(x => x.Id == query.ScheduleId, cancellationToken)
            ?? throw new NotFoundException($"Maintenance schedule {query.ScheduleId} not found");

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

