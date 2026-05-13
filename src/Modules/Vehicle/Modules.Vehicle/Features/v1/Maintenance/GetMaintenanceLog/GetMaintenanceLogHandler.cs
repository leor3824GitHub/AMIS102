using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceLog;

public sealed class GetMaintenanceLogHandler(
    VehicleDbContext db) : IQueryHandler<GetMaintenanceLogQuery, MaintenanceLogDto>
{
    public async ValueTask<MaintenanceLogDto> Handle(
        GetMaintenanceLogQuery query,
        CancellationToken cancellationToken)
    {
        var log = await db.MaintenanceLogs.FirstOrDefaultAsync(x => x.Id == query.LogId, cancellationToken)
            ?? throw new NotFoundException($"Maintenance log {query.LogId} not found");

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

