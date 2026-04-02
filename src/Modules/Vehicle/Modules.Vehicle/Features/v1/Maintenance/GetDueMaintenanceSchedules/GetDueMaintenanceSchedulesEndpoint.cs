using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.GetDueMaintenanceSchedules;

public static class GetDueMaintenanceSchedulesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/schedules/due", async (
            DueMaintenanceScheduleSearchRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetDueMaintenanceSchedulesQuery(request.VehicleId, request.DaysAhead);
            var result = await mediator.Send(query, ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(GetDueMaintenanceSchedulesQuery))
        .WithSummary("Get maintenance schedules that are due soon")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.View);
}
