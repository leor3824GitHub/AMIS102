using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using FSH.Framework.Shared.Identity.Authorization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.GetMotorVehicleInventory;

public static class GetMotorVehicleInventoryEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/inventory-report", GetInventory)
            .WithName(nameof(GetMotorVehicleInventoryQuery))
            .WithSummary("Inventory of Motor Vehicles report with specifications and accountable officers")
            .Produces<List<MotorVehicleInventoryItemDto>>(StatusCodes.Status200OK)
            .RequirePermission(VehicleModuleConstants.Permissions.Vehicles.View);

    private static async Task<IResult> GetInventory(
        [AsParameters] GetMotorVehicleInventoryQuery query,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return TypedResults.Ok(result);
    }
}
