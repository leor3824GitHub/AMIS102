using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.DeleteVehicle;

public static class DeleteVehicleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeleteVehicleCommand(id), ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(DeleteVehicleCommand))
        .WithSummary("Soft-delete a vehicle")
        .Produces(StatusCodes.Status204NoContent)
        .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.Delete);
}
