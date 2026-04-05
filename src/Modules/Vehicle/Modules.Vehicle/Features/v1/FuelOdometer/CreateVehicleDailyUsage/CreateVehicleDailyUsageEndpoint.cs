using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.FuelOdometer.CreateVehicleDailyUsage;

public static class CreateVehicleDailyUsageEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (CreateVehicleDailyUsageCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return TypedResults.Created($"/api/v1/vehicle/fuel-odometer/{result.Id}", result);
        })
        .WithName(nameof(CreateVehicleDailyUsageCommand))
        .WithSummary("Create a daily fuel and odometer record")
        .Produces<VehicleDailyUsageDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(VehicleModuleConstants.Permissions.FuelOdometer.Create);
}
