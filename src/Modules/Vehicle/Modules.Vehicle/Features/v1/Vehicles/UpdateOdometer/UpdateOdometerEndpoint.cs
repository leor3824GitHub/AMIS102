using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.UpdateOdometer;

public static class UpdateOdometerEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/odometer", async (Guid id, UpdateOdometerCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(cmd with { Id = id }, ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(UpdateOdometerCommand))
        .WithSummary("Update vehicle odometer reading")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.Update);
}
