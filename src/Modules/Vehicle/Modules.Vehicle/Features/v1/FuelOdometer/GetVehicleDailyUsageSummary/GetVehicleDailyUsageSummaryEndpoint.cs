using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.FuelOdometer.GetVehicleDailyUsageSummary;

public static class GetVehicleDailyUsageSummaryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/summary", async ([AsParameters] GetVehicleDailyUsageSummaryQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(GetVehicleDailyUsageSummaryQuery))
        .WithSummary("Get summary metrics for daily fuel and odometer records")
        .Produces<VehicleDailyUsageSummaryDto>(StatusCodes.Status200OK)
        .RequirePermission(VehicleModuleConstants.Permissions.FuelOdometer.View);
}
