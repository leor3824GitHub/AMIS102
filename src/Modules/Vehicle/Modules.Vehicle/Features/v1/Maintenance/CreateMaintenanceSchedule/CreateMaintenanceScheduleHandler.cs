using FSH.Framework.Core.Context;
using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using FSH.Modules.Vehicle.Data;
using FSH.Modules.Vehicle.Domain.Maintenance;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.CreateMaintenanceSchedule;

public sealed class CreateMaintenanceScheduleHandler(
    VehicleDbContext db,
    ICurrentUser currentUser) : ICommandHandler<CreateMaintenanceScheduleCommand, CreateMaintenanceScheduleResponse>
{
    public async ValueTask<CreateMaintenanceScheduleResponse> Handle(
        CreateMaintenanceScheduleCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = currentUser.GetTenant() ?? throw new InvalidOperationException("Tenant ID required");

        var schedule = MaintenanceSchedule.Create(
            tenantId,
            command.VehicleId,
            command.MaintenanceType,
            command.Description,
            command.IntervalDays,
            command.IntervalMileage,
            command.InitialDueDate,
            command.InitialDueMileage);

        db.MaintenanceSchedules.Add(schedule);
        await db.SaveChangesAsync(cancellationToken);
        return new CreateMaintenanceScheduleResponse(schedule.Id);
    }
}
