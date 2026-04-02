using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.LogMaintenanceCompletion;

public sealed class LogMaintenanceCompletionHandler(
    VehicleDbContext db,
    ICurrentUser currentUser) : ICommandHandler<LogMaintenanceCompletionCommand, LogMaintenanceCompletionResponse>
{
    public async ValueTask<LogMaintenanceCompletionResponse> Handle(
        LogMaintenanceCompletionCommand command,
        CancellationToken ct)
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
        await db.SaveChangesAsync(ct);

        // If a schedule is associated, advance its due dates
        if (command.ScheduleId.HasValue)
        {
            var schedule = await db.MaintenanceSchedules.FindAsync(new object[] { command.ScheduleId.Value }, cancellationToken: ct)
                ?? throw new ApplicationException($"Maintenance schedule {command.ScheduleId} not found");

            schedule.RecordCompletion(command.PerformedDate, command.OdometerAtService);
            db.MaintenanceSchedules.Update(schedule);
            await db.SaveChangesAsync(ct);
        }

        return new LogMaintenanceCompletionResponse(log.Id);
    }
}
