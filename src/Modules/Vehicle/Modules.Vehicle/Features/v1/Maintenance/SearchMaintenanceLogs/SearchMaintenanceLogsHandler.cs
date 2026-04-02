using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceLogs;

public sealed class SearchMaintenanceLogsHandler(
    VehicleDbContext db) : IQueryHandler<SearchMaintenanceLogsQuery, List<MaintenanceLogDto>>
{
    public async ValueTask<List<MaintenanceLogDto>> Handle(
        SearchMaintenanceLogsQuery query,
        CancellationToken ct)
    {
        var logs = await db.MaintenanceLogs
            .Where(x => (string.IsNullOrEmpty(query.MaintenanceType) || x.MaintenanceType.Contains(query.MaintenanceType)) &&
                     (!query.VehicleId.HasValue || x.VehicleId == query.VehicleId) &&
                     (!query.ScheduleId.HasValue || x.ScheduleId == query.ScheduleId) &&
                     (!query.PerformedDateFrom.HasValue || x.PerformedDate >= query.PerformedDateFrom) &&
                     (!query.PerformedDateTo.HasValue || x.PerformedDate <= query.PerformedDateTo) &&
                     !x.IsDeleted)
            .ToListAsync(ct);

        return logs
            .Select(x => new MaintenanceLogDto(
                x.Id,
                x.VehicleId,
                x.ScheduleId,
                x.MaintenanceType,
                x.PerformedDate,
                x.OdometerAtService,
                x.Description,
                x.Cost,
                x.PerformedBy,
                x.Notes))
            .ToList();
    }
}
