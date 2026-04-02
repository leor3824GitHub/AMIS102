using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceLog;

public static class GetMaintenanceLogEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/logs/{logId}", async (
            Guid logId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetMaintenanceLogQuery(logId);
            var result = await mediator.Send(query, ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(GetMaintenanceLogQuery))
        .WithSummary("Get a maintenance log by ID")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.View);
}
