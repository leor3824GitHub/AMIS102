using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Maintenance.SearchMaintenanceLogs;

public static class SearchMaintenanceLogsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/logs/search", async (
            MaintenanceLogSearchRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new SearchMaintenanceLogsQuery(
                request.MaintenanceType,
                request.VehicleId,
                request.ScheduleId,
                request.PerformedDateFrom,
                request.PerformedDateTo);

            var result = await mediator.Send(query, ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(SearchMaintenanceLogsQuery))
        .WithSummary("Search maintenance logs")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.View);
}

