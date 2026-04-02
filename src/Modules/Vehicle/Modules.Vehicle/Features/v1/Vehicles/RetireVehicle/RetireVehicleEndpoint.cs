using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.RetireVehicle;

public static class RetireVehicleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{id:guid}/retire", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new RetireVehicleCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(RetireVehicleCommand))
        .WithSummary("Mark vehicle as retired")
        .Produces(StatusCodes.Status204NoContent)
        .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.Update);
}
