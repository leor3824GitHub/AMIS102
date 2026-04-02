using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceLog;

public sealed class GetMaintenanceLogHandler(
    VehicleDbContext db) : IQueryHandler<GetMaintenanceLogQuery, MaintenanceLogDto>
{
    public async ValueTask<MaintenanceLogDto> Handle(
        GetMaintenanceLogQuery query,
        CancellationToken ct)
    {
        var log = await db.MaintenanceLogs.FirstOrDefaultAsync(x => x.Id == query.LogId, ct)
            ?? throw new ApplicationException($"Maintenance log {query.LogId} not found");

        return new MaintenanceLogDto(
            log.Id,
            log.VehicleId,
            log.ScheduleId,
            log.MaintenanceType,
            log.PerformedDate,
            log.OdometerAtService,
            log.Description,
            log.Cost,
            log.PerformedBy,
            log.Notes);
    }
}
