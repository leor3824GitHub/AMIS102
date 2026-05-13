using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.UpdateMaintenanceLog;

public static class UpdateMaintenanceLogEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/logs/{logId}", async (
            Guid logId,
            UpdateMaintenanceLogRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new UpdateMaintenanceLogCommand(
                logId,
                request.MaintenanceType,
                request.PerformedDate,
                request.OdometerAtService,
                request.Description,
                request.Cost,
                request.PerformedBy,
                request.Notes);

            var result = await mediator.Send(command, ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(UpdateMaintenanceLogCommand))
        .WithSummary("Update an existing maintenance log")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.Update);
}

