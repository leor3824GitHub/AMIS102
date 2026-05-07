using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.GetDueMaintenanceSchedules;

public sealed class GetDueMaintenanceSchedulesHandler(
    VehicleDbContext db) : IQueryHandler<GetDueMaintenanceSchedulesQuery, List<MaintenanceScheduleDto>>
{
    public async ValueTask<List<MaintenanceScheduleDto>> Handle(
        GetDueMaintenanceSchedulesQuery query,
        CancellationToken cancellationToken)
    {
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(query.DaysAhead));

        var schedules = await db.MaintenanceSchedules
            .Where(x => (!query.VehicleId.HasValue || x.VehicleId == query.VehicleId) &&
                     x.IsActive &&
                     !x.IsDeleted &&
                     x.DueDate.HasValue &&
                     x.DueDate <= dueDate)
            .ToListAsync(cancellationToken);

        return schedules
            .Select(x => new MaintenanceScheduleDto(
                x.Id,
                x.VehicleId,
                x.MaintenanceType,
                x.Description,
                x.IntervalDays,
                x.IntervalMileage,
                x.DueDate,
                x.DueMileage,
                x.LastDoneDate,
                x.LastDoneMileage,
                x.IsActive))
            .ToList();
    }
}
