using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.FuelOdometer.UpdateVehicleDailyUsage;

public static class UpdateVehicleDailyUsageEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", async (Guid id, UpdateVehicleDailyUsageCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd with { Id = id }, ct);
            return TypedResults.Ok(result);
        })
        .WithName(nameof(UpdateVehicleDailyUsageCommand))
        .WithSummary("Update a daily fuel and odometer record")
        .Produces<VehicleDailyUsageDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .RequirePermission(VehicleModuleConstants.Permissions.FuelOdometer.Update);
}

