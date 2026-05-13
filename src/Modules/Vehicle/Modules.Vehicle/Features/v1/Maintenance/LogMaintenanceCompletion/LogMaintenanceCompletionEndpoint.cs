using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.LogMaintenanceCompletion;

public static class LogMaintenanceCompletionEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/logs", async (
            LogMaintenanceCompletionRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new LogMaintenanceCompletionCommand(
                request.VehicleId,
                request.ScheduleId,
                request.MaintenanceType,
                request.PerformedDate,
                request.OdometerAtService,
                request.Description,
                request.Cost,
                request.PerformedBy,
                request.Notes);

            var result = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/vehicle/maintenance/logs/{result.Id}", result);
        })
        .WithName(nameof(LogMaintenanceCompletionCommand))
        .WithSummary("Log a completed maintenance operation")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.Create);
}

