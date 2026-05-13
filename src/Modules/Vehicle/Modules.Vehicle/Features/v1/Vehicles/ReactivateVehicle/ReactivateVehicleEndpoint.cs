using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.ReactivateVehicle;

public static class ReactivateVehicleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/reactivate", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ReactivateVehicleCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(ReactivateVehicleCommand))
        .WithSummary("Reactivate a retired or decommissioned vehicle")
        .Produces(StatusCodes.Status204NoContent)
        .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.Update);
}

