using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using AMIS.Modules.Vehicle.Data;
using AMIS.Modules.Vehicle.Domain.Maintenance;
using Mediator;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.LogMaintenanceCompletion;

public sealed class LogMaintenanceCompletionHandler(
    VehicleDbContext db,
    ICurrentUser currentUser) : ICommandHandler<LogMaintenanceCompletionCommand, LogMaintenanceCompletionResponse>
{
    public async ValueTask<LogMaintenanceCompletionResponse> Handle(
        LogMaintenanceCompletionCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required");

        // Create the maintenance log
        var log = MaintenanceLog.Create(
            tenantId,
            command.VehicleId,
            command.ScheduleId,
            command.MaintenanceType,
            command.PerformedDate,
            command.OdometerAtService,
            command.Description,
            command.Cost,
            command.PerformedBy,
            command.Notes);

        db.MaintenanceLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);

        // If a schedule is associated, advance its due dates
        if (command.ScheduleId.HasValue)
        {
            var schedule = await db.MaintenanceSchedules.FindAsync(new object[] { command.ScheduleId.Value }, cancellationToken: cancellationToken)
                ?? throw new NotFoundException($"Maintenance schedule {command.ScheduleId} not found");

            schedule.RecordCompletion(command.PerformedDate, command.OdometerAtService);
            db.MaintenanceSchedules.Update(schedule);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new LogMaintenanceCompletionResponse(log.Id);
    }
}

