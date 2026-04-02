using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceLog;

public sealed class DeleteMaintenanceLogHandler(
    VehicleDbContext db) : ICommandHandler<DeleteMaintenanceLogCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        DeleteMaintenanceLogCommand command,
        CancellationToken ct)
    {
        var log = await db.MaintenanceLogs.FirstOrDefaultAsync(x => x.Id == command.LogId, ct)
            ?? throw new ApplicationException($"Maintenance log {command.LogId} not found");

        log.SoftDelete();
        db.MaintenanceLogs.Update(log);
        await db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
