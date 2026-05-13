using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.DecommissionVehicle;

public static class DecommissionVehicleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/decommission", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DecommissionVehicleCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(DecommissionVehicleCommand))
        .WithSummary("Mark vehicle as decommissioned")
        .Produces(StatusCodes.Status204NoContent)
        .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.Update);
}

