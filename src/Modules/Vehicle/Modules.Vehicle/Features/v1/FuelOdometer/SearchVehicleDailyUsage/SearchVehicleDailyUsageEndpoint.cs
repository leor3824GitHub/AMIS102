using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.FuelOdometer.SearchVehicleDailyUsage;

public static class SearchVehicleDailyUsageEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async ([AsParameters] SearchVehicleDailyUsageQuery query, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(query, ct)))
        .WithName(nameof(SearchVehicleDailyUsageQuery))
        .WithSummary("Search daily fuel and odometer records")
        .Produces<PagedResponse<VehicleDailyUsageDto>>()
        .RequirePermission(VehicleModuleConstants.Permissions.FuelOdometer.View);
}
