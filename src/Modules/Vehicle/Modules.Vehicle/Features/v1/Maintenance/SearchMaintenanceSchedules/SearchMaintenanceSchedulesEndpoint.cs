using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceSchedules;

public static class SearchMaintenanceSchedulesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/schedules/search", async (
            MaintenanceScheduleSearchRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new SearchMaintenanceSchedulesQuery(
                request.MaintenanceType,
                request.VehicleId,
                request.IsActive);

            var result = await mediator.Send(query, ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(SearchMaintenanceSchedulesQuery))
        .WithSummary("Search maintenance schedules")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.View);
}

