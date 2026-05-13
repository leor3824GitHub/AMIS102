using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.DeactivateMaintenanceSchedule;

public static class DeactivateMaintenanceScheduleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPatch("/schedules/{scheduleId}/deactivate", async (
            Guid scheduleId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new DeactivateMaintenanceScheduleCommand(scheduleId);
            await mediator.Send(command, ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(DeactivateMaintenanceScheduleCommand))
        .WithSummary("Deactivate a maintenance schedule")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.Update);
}

