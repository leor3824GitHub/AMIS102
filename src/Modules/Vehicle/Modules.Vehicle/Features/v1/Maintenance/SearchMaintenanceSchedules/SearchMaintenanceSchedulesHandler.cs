using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceSchedules;

public sealed class SearchMaintenanceSchedulesHandler(
    VehicleDbContext db) : IQueryHandler<SearchMaintenanceSchedulesQuery, List<MaintenanceScheduleDto>>
{
    public async ValueTask<List<MaintenanceScheduleDto>> Handle(
        SearchMaintenanceSchedulesQuery query,
        CancellationToken ct)
    {
        var schedules = await db.MaintenanceSchedules
            .Where(x => (string.IsNullOrEmpty(query.MaintenanceType) || x.MaintenanceType.Contains(query.MaintenanceType)) &&
                     (!query.VehicleId.HasValue || x.VehicleId == query.VehicleId) &&
                     (!query.IsActive.HasValue || x.IsActive == query.IsActive) &&
                     !x.IsDeleted)
            .ToListAsync(ct);

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
