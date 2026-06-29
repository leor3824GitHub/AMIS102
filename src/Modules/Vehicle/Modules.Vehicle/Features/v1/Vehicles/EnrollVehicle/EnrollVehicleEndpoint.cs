using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.Vehicle.Contracts.Permissions;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.EnrollVehicle;

public static class EnrollVehicleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/", async (EnrollVehicleCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return TypedResults.Created($"/api/v1/vehicle/vehicles/{result.Id}", result);
        })
        .WithName(nameof(EnrollVehicleCommand))
        .WithSummary("Enroll a vehicle from a PPE motor-vehicle asset")
        .Produces<VehicleDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .RequirePermission(VehiclePermissions.Vehicles.Create);
}
