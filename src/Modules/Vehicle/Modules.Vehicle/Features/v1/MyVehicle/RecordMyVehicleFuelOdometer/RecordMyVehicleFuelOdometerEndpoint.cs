using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Vehicle.Contracts.Permissions;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Vehicle.Features.v1.MyVehicle.RecordMyVehicleFuelOdometer;

public static class RecordMyVehicleFuelOdometerEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/fuel-odometer", async (RecordMyVehicleFuelOdometerCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return TypedResults.Created($"/api/v1/vehicle/my-vehicles/fuel-odometer/{result.Id}", result);
        })
        .WithName("Vehicle_RecordMyVehicleFuelOdometer")
        .WithSummary("Record fuel/odometer for one of the current user's vehicles")
        .Produces<VehicleDailyUsageDto>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .RequirePermission(VehiclePermissions.MyVehicle.RecordFuelOdometer);
}
