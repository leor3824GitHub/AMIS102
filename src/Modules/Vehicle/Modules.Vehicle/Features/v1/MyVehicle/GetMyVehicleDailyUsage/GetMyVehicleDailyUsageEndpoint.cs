using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.Permissions;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.MyVehicle.GetMyVehicleDailyUsage;

public static class GetMyVehicleDailyUsageEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/fuel-odometer/summary", async ([AsParameters] GetMyVehicleDailyUsageQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(query, ct);
            return TypedResults.Ok(result);
        })
        .WithName("Vehicle_GetMyVehicleDailyUsage")
        .WithSummary("Fuel/odometer usage summary for one of the current user's vehicles")
        .Produces<VehicleDailyUsageSummaryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .RequirePermission(VehiclePermissions.MyVehicle.View);
}
