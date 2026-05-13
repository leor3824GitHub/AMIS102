using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceSchedule;

public static class DeleteMaintenanceScheduleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/schedules/{scheduleId}", async (
            Guid scheduleId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new DeleteMaintenanceScheduleCommand(scheduleId);
            await mediator.Send(command, ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(DeleteMaintenanceScheduleCommand))
        .WithSummary("Delete a maintenance schedule")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.Delete);
}

