using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.GetMaintenanceSchedule;

public static class GetMaintenanceScheduleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/schedules/{scheduleId}", async (
            Guid scheduleId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetMaintenanceScheduleQuery(scheduleId);
            var result = await mediator.Send(query, ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(GetMaintenanceScheduleQuery))
        .WithSummary("Get a maintenance schedule by ID")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.View);
}
