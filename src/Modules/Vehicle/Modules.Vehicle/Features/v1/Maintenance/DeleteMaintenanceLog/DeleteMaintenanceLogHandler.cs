using FSH.Framework.Core.Exceptions;
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
        CancellationToken cancellationToken)
    {
        var log = await db.MaintenanceLogs.FirstOrDefaultAsync(x => x.Id == command.LogId, cancellationToken)
            ?? throw new NotFoundException($"Maintenance log {command.LogId} not found");

        log.SoftDelete();
        db.MaintenanceLogs.Update(log);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
