using AMIS.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.DeleteMaintenanceLog;

public static class DeleteMaintenanceLogEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/logs/{logId}", async (
            Guid logId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new DeleteMaintenanceLogCommand(logId);
            await mediator.Send(command, ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(DeleteMaintenanceLogCommand))
        .WithSummary("Delete a maintenance log")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.Delete);
}

