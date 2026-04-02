using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceSchedule;

public static class UpdateMaintenanceScheduleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/schedules/{scheduleId}", async (
            Guid scheduleId,
            UpdateMaintenanceScheduleRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new UpdateMaintenanceScheduleCommand(
                scheduleId,
                request.MaintenanceType,
                request.Description,
                request.IntervalDays,
                request.IntervalMileage,
                request.DueDate,
                request.DueMileage);

            var result = await mediator.Send(command, ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(UpdateMaintenanceScheduleCommand))
        .WithSummary("Update an existing maintenance schedule")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.Update);
}
