using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AMIS.Modules.Vehicle.Contracts.Permissions;

namespace AMIS.Modules.Vehicle.Features.v1.Vehicles.GetEnrollableVehicleAssets;

public static class GetEnrollableVehicleAssetsEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/enrollable-assets", async ([AsParameters] GetEnrollableVehicleAssetsQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(query, ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(GetEnrollableVehicleAssetsQuery))
        .WithSummary("List PPE motor-vehicle assets eligible for enrollment")
        .Produces<List<EnrollableVehicleAssetDto>>(StatusCodes.Status200OK)
        .RequirePermission(VehiclePermissions.Vehicles.Create);
}
