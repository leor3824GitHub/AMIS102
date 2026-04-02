using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceLog;

public sealed class UpdateMaintenanceLogHandler(
    VehicleDbContext db) : ICommandHandler<UpdateMaintenanceLogCommand, MaintenanceLogDto>
{
    public async ValueTask<MaintenanceLogDto> Handle(
        UpdateMaintenanceLogCommand command,
        CancellationToken ct)
    {
        var log = await db.MaintenanceLogs.FirstOrDefaultAsync(x => x.Id == command.LogId, ct)
            ?? throw new ApplicationException($"Maintenance log {command.LogId} not found");

        log.Update(
            command.MaintenanceType,
            command.PerformedDate,
            command.OdometerAtService,
            command.Description,
            command.Cost,
            command.PerformedBy,
            command.Notes);

        db.MaintenanceLogs.Update(log);
        await db.SaveChangesAsync(ct);

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
