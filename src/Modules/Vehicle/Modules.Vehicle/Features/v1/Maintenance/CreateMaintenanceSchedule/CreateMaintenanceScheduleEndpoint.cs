using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Vehicle.Contracts.v1.Maintenance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Vehicle.Features.v1.Maintenance.CreateMaintenanceSchedule;

public static class CreateMaintenanceScheduleEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/schedules", async (
            CreateMaintenanceScheduleRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new CreateMaintenanceScheduleCommand(
                request.VehicleId,
                request.MaintenanceType,
                request.Description,
                request.IntervalDays,
                request.IntervalMileage,
                request.InitialDueDate,
                request.InitialDueMileage);

            var result = await mediator.Send(command, ct);
            return TypedResults.Created($"/api/v1/vehicle/maintenance/schedules/{result.Id}", result);
        })
        .WithName(nameof(CreateMaintenanceScheduleCommand))
        .WithSummary("Create a new maintenance schedule")
        .RequirePermission(VehicleModuleConstants.Permissions.Maintenance.Create);
}
