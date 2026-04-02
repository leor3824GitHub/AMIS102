using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.AssignVehicle;

public static class AssignVehicleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}/assignment", async (Guid id, AssignVehicleCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(cmd with { Id = id }, ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(AssignVehicleCommand))
        .WithSummary("Assign vehicle to a department or driver")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.Update);
}
