using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceLogs;

public sealed class SearchMaintenanceLogsHandler(
    VehicleDbContext db) : IQueryHandler<SearchMaintenanceLogsQuery, List<MaintenanceLogDto>>
{
    public async ValueTask<List<MaintenanceLogDto>> Handle(
        SearchMaintenanceLogsQuery query,
        CancellationToken cancellationToken)
    {
        var logs = await db.MaintenanceLogs
            .Where(x => (string.IsNullOrEmpty(query.MaintenanceType) || x.MaintenanceType.Contains(query.MaintenanceType)) &&
                     (!query.VehicleId.HasValue || x.VehicleId == query.VehicleId) &&
                     (!query.ScheduleId.HasValue || x.ScheduleId == query.ScheduleId) &&
                     (!query.PerformedDateFrom.HasValue || x.PerformedDate >= query.PerformedDateFrom) &&
                     (!query.PerformedDateTo.HasValue || x.PerformedDate <= query.PerformedDateTo) &&
                     !x.IsDeleted)
            .ToListAsync(cancellationToken);

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

