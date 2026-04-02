using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.CreateVehicle;

public static class CreateVehicleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (CreateVehicleCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return TypedResults.Created($"/api/v1/vehicle/vehicles/{result.Id}", result);
        })
        .WithName(nameof(CreateVehicleCommand))
        .WithSummary("Register a new vehicle")
        .Produces<VehicleDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.Create);
}
