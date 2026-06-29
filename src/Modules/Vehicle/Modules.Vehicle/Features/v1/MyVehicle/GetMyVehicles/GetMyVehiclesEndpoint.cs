using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.Permissions;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.MyVehicle.GetMyVehicles;

public static class GetMyVehiclesEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetMyVehiclesQuery(), ct);
            return TypedResults.Ok(result);
        })
        .WithName("Vehicle_GetMyVehicles")
        .WithSummary("List the vehicles the current user is accountable for")
        .Produces<List<VehicleDto>>(StatusCodes.Status200OK)
        .RequirePermission(VehiclePermissions.MyVehicle.View);
}
