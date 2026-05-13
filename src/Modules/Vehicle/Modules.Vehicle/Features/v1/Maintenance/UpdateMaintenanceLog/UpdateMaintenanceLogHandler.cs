using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceLog;

public sealed class UpdateMaintenanceLogHandler(
    VehicleDbContext db) : ICommandHandler<UpdateMaintenanceLogCommand, MaintenanceLogDto>
{
    public async ValueTask<MaintenanceLogDto> Handle(
        UpdateMaintenanceLogCommand command,
        CancellationToken cancellationToken)
    {
        var log = await db.MaintenanceLogs.FirstOrDefaultAsync(x => x.Id == command.LogId, cancellationToken)
            ?? throw new NotFoundException($"Maintenance log {command.LogId} not found");

        log.Update(
            command.MaintenanceType,
            command.PerformedDate,
            command.OdometerAtService,
            command.Description,
            command.Cost,
            command.PerformedBy,
            command.Notes);

        db.MaintenanceLogs.Update(log);
        await db.SaveChangesAsync(cancellationToken);

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

