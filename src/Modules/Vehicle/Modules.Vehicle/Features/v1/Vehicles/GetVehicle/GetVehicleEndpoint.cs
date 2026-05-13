using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.GetVehicle;

public static class GetVehicleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetVehicleQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName(nameof(GetVehicleQuery))
        .WithSummary("Get vehicle by ID")
        .Produces<VehicleDto>()
        .Produces(StatusCodes.Status404NotFound)
        .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.View);
}

