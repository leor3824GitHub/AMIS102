using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.UpdateVehicle;

public static class UpdateVehicleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", async (Guid id, UpdateVehicleCommand cmd, IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(cmd with { Id = id }, ct)))
        .WithName(nameof(UpdateVehicleCommand))
        .WithSummary("Update vehicle details")
        .Produces<VehicleDto>()
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.Update);
}
